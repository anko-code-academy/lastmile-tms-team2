using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Application.Routes.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Application.Routes.Services;

public sealed class RoutePlanningService(
    IAppDbContext dbContext,
    IGeocodingService geocodingService,
    IRouteRoutingService routeRoutingService)
    : IRoutePlanningService
{
    private static readonly ParcelStatus[] EligibleParcelStatuses = [ParcelStatus.Sorted];
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public async Task<RoutePlanComputationResult> BuildPlanAsync(
        RoutePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var zone = await dbContext.Zones
            .Include(candidate => candidate.Depot)
            .ThenInclude(depot => depot.Address)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.ZoneId && candidate.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException("Zone not found or inactive.");

        var warnings = new List<string>();

        Vehicle? vehicle = null;
        if (request.VehicleId.HasValue)
        {
            vehicle = await dbContext.Vehicles
                .Include(candidate => candidate.Depot)
                .ThenInclude(depot => depot.Address)
                .FirstOrDefaultAsync(candidate => candidate.Id == request.VehicleId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Vehicle not found.");

            if (!RouteAssignmentSupport.IsVehicleAssignableStatus(vehicle.Status))
            {
                throw new InvalidOperationException($"Vehicle is not available. Current status: {vehicle.Status}");
            }

            if (vehicle.DepotId != zone.DepotId)
            {
                throw new InvalidOperationException("Vehicle must belong to the selected zone's depot.");
            }
        }

        Driver? driver = null;
        if (request.DriverId.HasValue)
        {
            driver = await dbContext.Drivers
                .Include(candidate => candidate.AvailabilitySchedule)
                .FirstOrDefaultAsync(candidate => candidate.Id == request.DriverId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Driver not found.");

            if (driver.Status != DriverStatus.Active)
            {
                throw new InvalidOperationException($"Driver is not available. Current status: {driver.Status}");
            }

            if (!RouteAssignmentSupport.IsDriverScheduleCompatible(driver.AvailabilitySchedule, request.StartDate))
            {
                throw new InvalidOperationException("Driver is not available for the route service date.");
            }

            if (driver.ZoneId != zone.Id)
            {
                throw new InvalidOperationException("Driver must belong to the selected zone.");
            }

            if (driver.DepotId != zone.DepotId)
            {
                throw new InvalidOperationException("Driver must belong to the selected zone's depot.");
            }
        }

        if (vehicle is not null && driver is not null && vehicle.DepotId != driver.DepotId)
        {
            throw new InvalidOperationException("Driver and vehicle must belong to the same depot.");
        }

        var candidateParcels = await LoadEligibleParcelsAsync(zone.Id, cancellationToken);
        var selectedParcels = SelectParcels(candidateParcels, request);

        await EnsureGeocodedAddressesAsync(selectedParcels, cancellationToken);

        var candidateDtos = candidateParcels
            .OrderBy(parcel => parcel.TrackingNumber)
            .Select(parcel => ToCandidateDto(parcel, selectedParcels.Any(selected => selected.Id == parcel.Id)))
            .ToList();

        await EnsureDepotGeocodedAsync(zone.Depot.Address, cancellationToken);

        if (selectedParcels.Count == 0)
        {
            if (zone.Depot.Address.GeoLocation is null)
            {
                warnings.Add("Depot coordinates are unavailable, so the route preview starts without a mapped depot.");
            }

            return new RoutePlanComputationResult
            {
                ZoneId = zone.Id,
                ZoneName = zone.Name,
                DepotId = zone.DepotId,
                DepotName = zone.Depot.Name,
                DepotAddressLine = BuildAddressLine(zone.Depot.Address),
                DepotLongitude = zone.Depot.Address.GeoLocation?.X,
                DepotLatitude = zone.Depot.Address.GeoLocation?.Y,
                CandidateParcels = candidateDtos,
                Warnings = warnings,
            };
        }

        var plannedStops = BuildStops(selectedParcels, request.StopMode, request.Stops, warnings);
        plannedStops = await ApplyAutoOrderingAsync(plannedStops, zone.Depot.Address, request.StopMode, warnings, cancellationToken);
        var routeMetrics = await BuildRouteMetricsAsync(zone.Depot.Address, plannedStops, warnings, cancellationToken);

        return new RoutePlanComputationResult
        {
            ZoneId = zone.Id,
            ZoneName = zone.Name,
            DepotId = zone.DepotId,
            DepotName = zone.Depot.Name,
            DepotAddressLine = BuildAddressLine(zone.Depot.Address),
            DepotLongitude = zone.Depot.Address.GeoLocation?.X,
            DepotLatitude = zone.Depot.Address.GeoLocation?.Y,
            CandidateParcels = candidateDtos,
            Stops = plannedStops,
            Path = routeMetrics.Path,
            PlannedDistanceMeters = routeMetrics.DistanceMeters,
            PlannedDurationSeconds = routeMetrics.DurationSeconds,
            PlannedPath = routeMetrics.LineString,
            Warnings = warnings,
        };
    }

    public Task EnsureParcelRecipientGeocodedAsync(
        Parcel parcel,
        CancellationToken cancellationToken = default) =>
        EnsureGeocodedAddressesAsync([parcel], cancellationToken);

    public async Task ApplyMetricsToPersistedRouteAsync(
        Route route,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(route);

        var depotAddress = route.Zone?.Depot?.Address;
        if (depotAddress is null)
        {
            depotAddress = await dbContext.Zones
                .Where(zone => zone.Id == route.ZoneId)
                .Select(zone => zone.Depot.Address)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("Route depot could not be resolved.");
        }

        await EnsureDepotGeocodedAsync(depotAddress, cancellationToken);

        var warnings = new List<string>();
        var plannedStops = route.Stops
            .OrderBy(stop => stop.Sequence)
            .Select(stop => new RoutePlannedStop
            {
                Id = stop.Id.ToString(),
                Sequence = stop.Sequence,
                RecipientLabel = stop.RecipientLabel,
                AddressLine = BuildAddressLine(
                    stop.Street1,
                    stop.Street2,
                    stop.City,
                    stop.State,
                    stop.PostalCode),
                StopLocation = stop.StopLocation,
            })
            .ToList();

        var metrics = await BuildRouteMetricsAsync(
            depotAddress,
            plannedStops,
            warnings,
            cancellationToken);

        route.PlannedDistanceMeters = metrics.DistanceMeters;
        route.PlannedDurationSeconds = metrics.DurationSeconds;
        route.PlannedPath = metrics.LineString;
    }

    private async Task<List<Parcel>> LoadEligibleParcelsAsync(Guid zoneId, CancellationToken cancellationToken)
    {
        var activeStatuses = RouteAssignmentSupport.ActiveAssignmentStatuses;

        return await dbContext.Parcels
            .Include(parcel => parcel.RecipientAddress)
            .Include(parcel => parcel.Zone)
            .ThenInclude(zone => zone.Depot)
            .Where(parcel =>
                EligibleParcelStatuses.Contains(parcel.Status)
                && parcel.ZoneId == zoneId
                && !dbContext.Routes
                    .Where(route => activeStatuses.Contains(route.Status))
                    .Any(route => route.Parcels.Any(assignedParcel => assignedParcel.Id == parcel.Id)))
            .ToListAsync(cancellationToken);
    }

    private static List<Parcel> SelectParcels(IReadOnlyList<Parcel> candidateParcels, RoutePlanRequest request)
    {
        if (request.AssignmentMode == RouteAssignmentMode.AutoByZone)
        {
            return candidateParcels
                .OrderBy(parcel => parcel.TrackingNumber)
                .ToList();
        }

        var selectedIds = request.ParcelIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

        return candidateParcels
            .Where(parcel => selectedIds.Contains(parcel.Id))
            .OrderBy(parcel => parcel.TrackingNumber)
            .ToList();
    }

    private async Task EnsureGeocodedAddressesAsync(
        IReadOnlyCollection<Parcel> parcels,
        CancellationToken cancellationToken)
    {
        var pendingAddresses = parcels
            .Select(parcel => parcel.RecipientAddress)
            .Where(address => address.GeoLocation is null)
            .DistinctBy(address => address.Id)
            .ToList();

        if (pendingAddresses.Count == 0)
        {
            return;
        }

        foreach (var address in pendingAddresses)
        {
            var addressString = BuildAddressString(address);
            var point = await geocodingService.GeocodeAsync(addressString, cancellationToken);
            if (point is null)
            {
                throw new InvalidOperationException(
                    $"Could not geocode a route stop address. Address: {addressString}");
            }

            address.GeoLocation = point;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDepotGeocodedAsync(Address depotAddress, CancellationToken cancellationToken)
    {
        if (depotAddress.GeoLocation is not null)
        {
            return;
        }

        var addressString = BuildAddressString(depotAddress);
        var point = await geocodingService.GeocodeAsync(addressString, cancellationToken);
        if (point is null)
        {
            return;
        }

        depotAddress.GeoLocation = point;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static RoutePlanParcelCandidateDto ToCandidateDto(Parcel parcel, bool isSelected)
    {
        var recipientAddress = parcel.RecipientAddress;
        return new RoutePlanParcelCandidateDto
        {
            Id = parcel.Id,
            TrackingNumber = parcel.TrackingNumber,
            Weight = parcel.Weight,
            WeightUnit = parcel.WeightUnit,
            ZoneId = parcel.ZoneId,
            ZoneName = parcel.Zone?.Name ?? string.Empty,
            RecipientLabel = BuildRecipientLabel(parcel),
            AddressLine = BuildAddressLine(recipientAddress),
            Longitude = recipientAddress.GeoLocation?.X,
            Latitude = recipientAddress.GeoLocation?.Y,
            IsSelected = isSelected,
        };
    }

    private static IReadOnlyList<RoutePlannedStop> BuildStops(
        IReadOnlyList<Parcel> selectedParcels,
        RouteStopMode stopMode,
        IReadOnlyList<RouteStopDraftDto> manualDrafts,
        ICollection<string> warnings)
    {
        if (stopMode == RouteStopMode.Manual && manualDrafts.Count > 0)
        {
            return BuildManualStops(selectedParcels, manualDrafts, warnings);
        }

        if (stopMode == RouteStopMode.Manual)
        {
            return selectedParcels
                .OrderBy(parcel => parcel.TrackingNumber)
                .Select((parcel, index) => CreateStop(
                    $"draft-{index + 1}",
                    index + 1,
                    [parcel],
                    warnings))
                .ToList();
        }

        return selectedParcels
            .GroupBy(parcel => BuildStopKey(parcel.RecipientAddress.GeoLocation!))
            .Select((group, index) => CreateStop(
                group.Key,
                index + 1,
                group.OrderBy(parcel => parcel.TrackingNumber).ToList(),
                warnings))
            .OrderBy(stop => stop.RecipientLabel)
            .ThenBy(stop => stop.AddressLine)
            .Select((stop, index) => stop with { Sequence = index + 1 })
            .ToList();
    }

    private static IReadOnlyList<RoutePlannedStop> BuildManualStops(
        IReadOnlyList<Parcel> selectedParcels,
        IReadOnlyList<RouteStopDraftDto> manualDrafts,
        ICollection<string> warnings)
    {
        var parcelById = selectedParcels.ToDictionary(parcel => parcel.Id);
        var assignedIds = new HashSet<Guid>();
        var plannedStops = new List<RoutePlannedStop>(manualDrafts.Count);

        foreach (var draft in manualDrafts.OrderBy(draft => draft.Sequence))
        {
            var parcels = new List<Parcel>();
            foreach (var parcelId in draft.ParcelIds.Distinct())
            {
                if (!parcelById.TryGetValue(parcelId, out var parcel))
                {
                    throw new InvalidOperationException("Manual stop assignments contain an ineligible parcel.");
                }

                if (!assignedIds.Add(parcelId))
                {
                    throw new InvalidOperationException("A parcel can only belong to one route stop.");
                }

                parcels.Add(parcel);
            }

            if (parcels.Count == 0)
            {
                continue;
            }

            plannedStops.Add(CreateStop($"draft-{draft.Sequence}", draft.Sequence, parcels, warnings));
        }

        if (assignedIds.Count != selectedParcels.Count)
        {
            throw new InvalidOperationException("Manual stop assignments must include every selected parcel exactly once.");
        }

        return plannedStops;
    }

    private static RoutePlannedStop CreateStop(
        string id,
        int sequence,
        IReadOnlyList<Parcel> parcels,
        ICollection<string> warnings)
    {
        var anchorParcel = parcels[0];
        var anchorPoint = anchorParcel.RecipientAddress.GeoLocation
            ?? throw new InvalidOperationException("Route stop is missing geocoded coordinates.");

        if (parcels.Any(parcel =>
                parcel.RecipientAddress.GeoLocation is { } location
                && (Math.Abs(location.X - anchorPoint.X) > 0.000001 || Math.Abs(location.Y - anchorPoint.Y) > 0.000001)))
        {
            warnings.Add("One or more manual stop groups combine parcels from different curbside points.");
        }

        return new RoutePlannedStop
        {
            Id = id,
            Sequence = sequence,
            RecipientLabel = parcels.Count == 1
                ? BuildRecipientLabel(anchorParcel)
                : $"{BuildRecipientLabel(anchorParcel)} +{parcels.Count - 1}",
            AddressLine = BuildAddressLine(anchorParcel.RecipientAddress),
            StopLocation = GeometryFactory.CreatePoint(new Coordinate(anchorPoint.X, anchorPoint.Y)),
            Parcels = parcels
                .OrderBy(parcel => parcel.TrackingNumber)
                .Select(parcel => new RoutePlannedStopParcel
                {
                    ParcelId = parcel.Id,
                    TrackingNumber = parcel.TrackingNumber,
                    RecipientLabel = BuildRecipientLabel(parcel),
                    AddressLine = BuildAddressLine(parcel.RecipientAddress),
                    Status = parcel.Status,
                })
                .ToList(),
        };
    }

    private async Task<IReadOnlyList<RoutePlannedStop>> ApplyAutoOrderingAsync(
        IReadOnlyList<RoutePlannedStop> stops,
        Address depotAddress,
        RouteStopMode stopMode,
        ICollection<string> warnings,
        CancellationToken cancellationToken)
    {
        if (stops.Count <= 1 || stopMode != RouteStopMode.Auto)
        {
            return stops
                .OrderBy(stop => stop.Sequence)
                .ToList();
        }

        if (depotAddress.GeoLocation is null)
        {
            var point = await geocodingService.GeocodeAsync(BuildAddressString(depotAddress), cancellationToken);
            if (point is not null)
            {
                depotAddress.GeoLocation = point;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if (depotAddress.GeoLocation is null)
        {
            warnings.Add("Depot coordinates are unavailable, so automatic stop ordering could not be optimized.");
            return stops.OrderBy(stop => stop.Sequence).ToList();
        }

        var matrixCoordinates = new List<Point>(stops.Count + 1) { depotAddress.GeoLocation };
        matrixCoordinates.AddRange(stops.Select(stop => stop.StopLocation));

        if (matrixCoordinates.Count > 25)
        {
            warnings.Add("Too many stops for Matrix-based optimization. Review the stop order or split the route.");
            return stops.OrderBy(stop => stop.Sequence).ToList();
        }

        var matrix = await routeRoutingService.GetMatrixAsync(matrixCoordinates, cancellationToken);
        var remaining = Enumerable.Range(1, stops.Count).ToHashSet();
        var orderedStops = new List<RoutePlannedStop>(stops.Count);
        var currentIndex = 0;
        var nextSequence = 1;

        while (remaining.Count > 0)
        {
            var nextIndex = remaining
                .OrderBy(index => matrix.Durations[currentIndex][index] ?? double.MaxValue)
                .ThenBy(index => stops[index - 1].Sequence)
                .First();

            orderedStops.Add(stops[nextIndex - 1] with { Sequence = nextSequence++ });
            remaining.Remove(nextIndex);
            currentIndex = nextIndex;
        }

        return orderedStops;
    }

    private async Task<(int DistanceMeters, int DurationSeconds, IReadOnlyList<RoutePathPointDto> Path, LineString? LineString)>
        BuildRouteMetricsAsync(
            Address depotAddress,
            IReadOnlyList<RoutePlannedStop> stops,
            ICollection<string> warnings,
            CancellationToken cancellationToken)
    {
        if (stops.Count == 0)
        {
            return (0, 0, [], null);
        }

        if (depotAddress.GeoLocation is null)
        {
            warnings.Add("Depot coordinates are unavailable, so route distance could not be calculated.");
            return (0, 0, [], null);
        }

        var coordinates = new List<Point>(stops.Count + 2)
        {
            depotAddress.GeoLocation,
        };
        coordinates.AddRange(stops.Select(stop => stop.StopLocation));
        coordinates.Add(depotAddress.GeoLocation);

        var allPoints = new List<RouteCoordinateResult>();
        var totalDistance = 0;
        var totalDuration = 0;
        var offset = 0;

        while (offset < coordinates.Count - 1)
        {
            var count = Math.Min(25, coordinates.Count - offset);
            var chunk = coordinates.Skip(offset).Take(count).ToList();
            if (chunk.Count < 2)
            {
                break;
            }

            var directions = await routeRoutingService.GetDirectionsAsync(chunk, cancellationToken);
            totalDistance += directions.DistanceMeters;
            totalDuration += directions.DurationSeconds;

            if (allPoints.Count == 0)
            {
                allPoints.AddRange(directions.Path);
            }
            else
            {
                allPoints.AddRange(directions.Path.Skip(1));
            }

            if (offset + count >= coordinates.Count)
            {
                break;
            }

            offset += count - 1;
        }

        if (allPoints.Count == 0)
        {
            return (0, 0, [], null);
        }

        var pathDtos = allPoints
            .Select(point => new RoutePathPointDto
            {
                Longitude = point.Longitude,
                Latitude = point.Latitude,
            })
            .ToList();

        var lineString = GeometryFactory.CreateLineString(
            allPoints.Select(point => new Coordinate(point.Longitude, point.Latitude)).ToArray());

        return (totalDistance, totalDuration, pathDtos, lineString);
    }

    private static string BuildRecipientLabel(Parcel parcel)
    {
        var address = parcel.RecipientAddress;
        return ParcelChangeSupport.NormalizeOptional(address.ContactName)
            ?? ParcelChangeSupport.NormalizeOptional(address.CompanyName)
            ?? parcel.TrackingNumber;
    }

    private static string BuildAddressLine(Address address)
    {
        return BuildAddressLine(
            ParcelChangeSupport.NormalizeRequired(address.Street1),
            ParcelChangeSupport.NormalizeOptional(address.Street2),
            ParcelChangeSupport.NormalizeRequired(address.City),
            ParcelChangeSupport.NormalizeRequired(address.State),
            ParcelChangeSupport.NormalizeRequired(address.PostalCode));
    }

    private static string BuildAddressLine(
        string street1,
        string? street2,
        string city,
        string state,
        string postalCode)
    {
        var parts = new[]
        {
            street1,
            street2,
            city,
            state,
            postalCode,
        };

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string BuildAddressString(Address address)
    {
        var parts = new[]
        {
            ParcelChangeSupport.NormalizeRequired(address.Street1),
            ParcelChangeSupport.NormalizeOptional(address.Street2),
            ParcelChangeSupport.NormalizeRequired(address.City),
            ParcelChangeSupport.NormalizeRequired(address.State),
            ParcelChangeSupport.NormalizeRequired(address.PostalCode),
            ParcelChangeSupport.NormalizeRequired(address.CountryCode),
        };

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string BuildStopKey(Point point) =>
        $"{Math.Round(point.X, 6):0.000000}|{Math.Round(point.Y, 6):0.000000}";
}
