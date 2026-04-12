using FluentAssertions;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Routes.Commands;
using LastMile.TMS.Application.Routes.Queries;
using LastMile.TMS.Application.Routes.Services;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Routes;

public class RouteParcelAdjustmentCommandHandlerTests
{
    [Fact]
    public async Task AddParcelToDispatchedRoute_MergesMatchingStopTransitionsParcelAndLogsAudit()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var driver = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var existingParcel = await db.Parcels
            .Include(candidate => candidate.RecipientAddress)
            .Include(candidate => candidate.ShipperAddress)
            .Include(candidate => candidate.ChangeHistory)
            .Include(candidate => candidate.TrackingEvents)
            .SingleAsync(candidate => candidate.Id == data.Parcel1.Id);
        existingParcel.Status = ParcelStatus.OutForDelivery;
        existingParcel.RecipientAddress.GeoLocation = CreatePoint(151.215, -33.872);

        var candidateParcel = CreateParcel(
            "LMADJUST0001",
            data.Zone1,
            existingParcel.ShipperAddress,
            existingParcel.RecipientAddress,
            ParcelStatus.Staged,
            data.ServiceDate);
        candidateParcel.RecipientAddress.GeoLocation = CreatePoint(151.215, -33.872);

        var route = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver,
            data.ServiceDate,
            RouteStatus.Dispatched,
            existingParcel);
        route.Stops.Add(CreateStop(route, existingParcel, 1, data.ServiceDate));

        db.Parcels.Add(candidateParcel);
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserName.Returns("dispatcher@test");
        var parcelUpdateNotifier = Substitute.For<IParcelUpdateNotifier>();
        var routeUpdateNotifier = Substitute.For<IRouteUpdateNotifier>();
        var routePlanningService = CreateRoutePlanningService();

        var handler = new AddParcelToDispatchedRouteCommandHandler(
            db,
            currentUser,
            parcelUpdateNotifier,
            routeUpdateNotifier,
            routePlanningService);

        await handler.Handle(
            new AddParcelToDispatchedRouteCommand(
                route.Id,
                new()
                {
                    ParcelId = candidateParcel.Id,
                    Reason = "Late staged handoff",
                }),
            CancellationToken.None);

        var persistedRoute = await db.Routes
            .Include(candidate => candidate.Parcels)
            .ThenInclude(parcel => parcel.ChangeHistory)
            .Include(candidate => candidate.Stops)
            .ThenInclude(stop => stop.Parcels)
            .Include(candidate => candidate.ParcelAdjustmentAuditTrail)
            .SingleAsync(candidate => candidate.Id == route.Id);
        var persistedParcel = await db.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .SingleAsync(candidate => candidate.Id == candidateParcel.Id);

        persistedRoute.Parcels.Should().HaveCount(2);
        persistedRoute.Stops.Should().HaveCount(1);
        persistedRoute.Stops.Single().Parcels.Should().HaveCount(2);
        persistedRoute.PlannedDistanceMeters.Should().Be(9100);
        persistedRoute.PlannedDurationSeconds.Should().Be(1500);
        persistedParcel.Status.Should().Be(ParcelStatus.OutForDelivery);
        persistedParcel.ChangeHistory.Should().ContainSingle(entry =>
            entry.FieldName == "Status"
            && entry.BeforeValue == "Staged"
            && entry.AfterValue == "Out For Delivery");
        persistedRoute.ParcelAdjustmentAuditTrail.Should().ContainSingle(entry =>
            entry.Action == RouteParcelAdjustmentAction.Added
            && entry.ParcelId == candidateParcel.Id
            && entry.Reason == "Late staged handoff"
            && entry.AffectedStopSequence == 1);
        await routeUpdateNotifier.Received(1).NotifyRouteUpdatedAsync(
            Arg.Is<RouteUpdateNotification>(notification =>
                notification.RouteId == route.Id
                && notification.Action == RouteParcelAdjustmentAction.Added.ToString()
                && notification.TrackingNumber == candidateParcel.TrackingNumber),
            Arg.Any<CancellationToken>());
        await parcelUpdateNotifier.Received(1).NotifyParcelUpdatedAsync(
            Arg.Is<ParcelUpdateNotification>(notification =>
                notification.TrackingNumber == candidateParcel.TrackingNumber
                && notification.Status == ParcelStatus.OutForDelivery.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveParcelFromDispatchedRoute_RemovesEmptyStopResequencesRouteAndLogsAudit()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var driver = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var parcel1 = await db.Parcels
            .Include(candidate => candidate.RecipientAddress)
            .Include(candidate => candidate.ShipperAddress)
            .Include(candidate => candidate.ChangeHistory)
            .Include(candidate => candidate.TrackingEvents)
            .SingleAsync(candidate => candidate.Id == data.Parcel1.Id);
        parcel1.Status = ParcelStatus.OutForDelivery;
        parcel1.RecipientAddress.GeoLocation = CreatePoint(151.215, -33.872);

        var recipientAddress2 = CreateAddress("99 Harbour Road", 151.235, -33.881, data.ServiceDate);
        var parcel2 = CreateParcel(
            "LMADJUST0002",
            data.Zone1,
            parcel1.ShipperAddress,
            recipientAddress2,
            ParcelStatus.OutForDelivery,
            data.ServiceDate);

        var route = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver,
            data.ServiceDate,
            RouteStatus.Dispatched,
            parcel1,
            parcel2);
        route.Stops.Add(CreateStop(route, parcel1, 1, data.ServiceDate));
        route.Stops.Add(CreateStop(route, parcel2, 2, data.ServiceDate));

        db.Add(recipientAddress2);
        db.Add(parcel2);
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserName.Returns("dispatcher@test");
        var parcelUpdateNotifier = Substitute.For<IParcelUpdateNotifier>();
        var routeUpdateNotifier = Substitute.For<IRouteUpdateNotifier>();
        var routePlanningService = CreateRoutePlanningService();

        var handler = new RemoveParcelFromDispatchedRouteCommandHandler(
            db,
            currentUser,
            parcelUpdateNotifier,
            routeUpdateNotifier,
            routePlanningService);

        await handler.Handle(
            new RemoveParcelFromDispatchedRouteCommand(
                route.Id,
                new()
                {
                    ParcelId = parcel1.Id,
                    Reason = "Customer cancelled at depot",
                }),
            CancellationToken.None);

        var persistedRoute = await db.Routes
            .Include(candidate => candidate.Parcels)
            .Include(candidate => candidate.Stops)
            .ThenInclude(stop => stop.Parcels)
            .Include(candidate => candidate.ParcelAdjustmentAuditTrail)
            .SingleAsync(candidate => candidate.Id == route.Id);
        var removedParcel = await db.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .SingleAsync(candidate => candidate.Id == parcel1.Id);

        persistedRoute.Parcels.Should().ContainSingle(candidate => candidate.Id == parcel2.Id);
        persistedRoute.Stops.Should().ContainSingle();
        persistedRoute.Stops.Single().Sequence.Should().Be(1);
        persistedRoute.Stops.Single().Parcels.Should().ContainSingle(candidate => candidate.Id == parcel2.Id);
        persistedRoute.PlannedDistanceMeters.Should().Be(9100);
        persistedRoute.PlannedDurationSeconds.Should().Be(1500);
        removedParcel.Status.Should().Be(ParcelStatus.Staged);
        removedParcel.ChangeHistory.Should().ContainSingle(entry =>
            entry.FieldName == "Status"
            && entry.BeforeValue == "Out For Delivery"
            && entry.AfterValue == "Staged");
        persistedRoute.ParcelAdjustmentAuditTrail.Should().ContainSingle(entry =>
            entry.Action == RouteParcelAdjustmentAction.Removed
            && entry.ParcelId == parcel1.Id
            && entry.Reason == "Customer cancelled at depot"
            && entry.AffectedStopSequence == null);
        await routeUpdateNotifier.Received(1).NotifyRouteUpdatedAsync(
            Arg.Is<RouteUpdateNotification>(notification =>
                notification.RouteId == route.Id
                && notification.Action == RouteParcelAdjustmentAction.Removed.ToString()
                && notification.TrackingNumber == parcel1.TrackingNumber),
            Arg.Any<CancellationToken>());
        await parcelUpdateNotifier.Received(1).NotifyParcelUpdatedAsync(
            Arg.Is<ParcelUpdateNotification>(notification =>
                notification.TrackingNumber == parcel1.TrackingNumber
                && notification.Status == ParcelStatus.Staged.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddParcelToDispatchedRoute_WhenParcelIsAlreadyAssignedElsewhere_Throws()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle1 = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var vehicle2 = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle2.Id);
        var driver1 = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var driver2 = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver2.Id);
        var routeParcel = await db.Parcels
            .Include(candidate => candidate.RecipientAddress)
            .Include(candidate => candidate.ShipperAddress)
            .SingleAsync(candidate => candidate.Id == data.Parcel1.Id);
        routeParcel.Status = ParcelStatus.OutForDelivery;
        routeParcel.RecipientAddress.GeoLocation = CreatePoint(151.215, -33.872);

        var candidateParcel = CreateParcel(
            "LMADJUST0003",
            data.Zone1,
            routeParcel.ShipperAddress,
            CreateAddress("42 Conflict Street", 151.241, -33.89, data.ServiceDate),
            ParcelStatus.Staged,
            data.ServiceDate);

        var adjustedRoute = RouteAssignmentTestData.CreateRoute(
            vehicle1,
            driver1,
            data.ServiceDate,
            RouteStatus.Dispatched,
            routeParcel);
        adjustedRoute.Stops.Add(CreateStop(adjustedRoute, routeParcel, 1, data.ServiceDate));

        var conflictingRoute = RouteAssignmentTestData.CreateRoute(
            vehicle2,
            driver2,
            data.ServiceDate.AddHours(1),
            RouteStatus.Draft,
            candidateParcel);

        db.Add(candidateParcel.RecipientAddress);
        db.Add(candidateParcel);
        db.AddRange(adjustedRoute, conflictingRoute);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new AddParcelToDispatchedRouteCommandHandler(
            db,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IParcelUpdateNotifier>(),
            Substitute.For<IRouteUpdateNotifier>(),
            CreateRoutePlanningService());

        var act = () => handler.Handle(
            new AddParcelToDispatchedRouteCommand(
                adjustedRoute.Id,
                new()
                {
                    ParcelId = candidateParcel.Id,
                    Reason = "Attempt duplicate assignment",
                }),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*already assigned to another active route*");
    }

    [Fact]
    public async Task RemoveParcelFromDispatchedRoute_WhenRemovingFinalParcel_Throws()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var driver = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var parcel = await db.Parcels
            .Include(candidate => candidate.RecipientAddress)
            .Include(candidate => candidate.ShipperAddress)
            .SingleAsync(candidate => candidate.Id == data.Parcel1.Id);
        parcel.Status = ParcelStatus.OutForDelivery;
        parcel.RecipientAddress.GeoLocation = CreatePoint(151.215, -33.872);

        var route = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver,
            data.ServiceDate,
            RouteStatus.Dispatched,
            parcel);
        route.Stops.Add(CreateStop(route, parcel, 1, data.ServiceDate));

        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new RemoveParcelFromDispatchedRouteCommandHandler(
            db,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IParcelUpdateNotifier>(),
            Substitute.For<IRouteUpdateNotifier>(),
            CreateRoutePlanningService());

        var act = () => handler.Handle(
            new RemoveParcelFromDispatchedRouteCommand(
                route.Id,
                new()
                {
                    ParcelId = parcel.Id,
                    Reason = "Nothing left on route",
                }),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*final parcel cannot be removed*");
    }

    private static IRoutePlanningService CreateRoutePlanningService()
    {
        var routePlanningService = Substitute.For<IRoutePlanningService>();
        routePlanningService
            .EnsureParcelRecipientGeocodedAsync(Arg.Any<Parcel>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        routePlanningService
            .ApplyMetricsToPersistedRouteAsync(Arg.Any<Route>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var route = callInfo.Arg<Route>();
                route.PlannedDistanceMeters = 9100;
                route.PlannedDurationSeconds = 1500;
                return Task.CompletedTask;
            });

        return routePlanningService;
    }

    private static RouteStop CreateStop(
        Route route,
        Parcel parcel,
        int sequence,
        DateTimeOffset timestamp)
    {
        return new RouteStop
        {
            Route = route,
            RouteId = route.Id,
            Sequence = sequence,
            RecipientLabel = parcel.RecipientAddress.ContactName ?? parcel.TrackingNumber,
            Street1 = parcel.RecipientAddress.Street1,
            Street2 = parcel.RecipientAddress.Street2,
            City = parcel.RecipientAddress.City,
            State = parcel.RecipientAddress.State,
            PostalCode = parcel.RecipientAddress.PostalCode,
            CountryCode = parcel.RecipientAddress.CountryCode,
            StopLocation = parcel.RecipientAddress.GeoLocation!.Copy() as Point ?? parcel.RecipientAddress.GeoLocation!,
            CreatedAt = timestamp,
            CreatedBy = "tests",
            Parcels = [parcel],
        };
    }

    private static Parcel CreateParcel(
        string trackingNumber,
        Zone zone,
        Address shipperAddress,
        Address recipientAddress,
        ParcelStatus status,
        DateTimeOffset createdAt)
    {
        return new Parcel
        {
            Id = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            Description = "Route adjustment test parcel",
            ServiceType = ServiceType.Standard,
            Status = status,
            ShipperAddress = shipperAddress,
            ShipperAddressId = shipperAddress.Id,
            RecipientAddress = recipientAddress,
            RecipientAddressId = recipientAddress.Id,
            Weight = 4m,
            WeightUnit = WeightUnit.Kg,
            Length = 30m,
            Width = 20m,
            Height = 10m,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 100m,
            Currency = "AUD",
            EstimatedDeliveryDate = createdAt.AddDays(2),
            DeliveryAttempts = 0,
            ZoneId = zone.Id,
            CreatedAt = createdAt.AddDays(-1),
            CreatedBy = "tests",
        };
    }

    private static Address CreateAddress(
        string street1,
        double longitude,
        double latitude,
        DateTimeOffset createdAt)
    {
        return new Address
        {
            Id = Guid.NewGuid(),
            Street1 = street1,
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
            IsResidential = true,
            GeoLocation = CreatePoint(longitude, latitude),
            ContactName = "Adjustment Customer",
            CreatedAt = createdAt.AddDays(-1),
            CreatedBy = "tests",
        };
    }

    private static Point CreatePoint(double longitude, double latitude)
    {
        var point = new Point(longitude, latitude) { SRID = 4326 };
        return point;
    }
}

public class GetDispatchedRouteParcelCandidatesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyUnassignedStagedParcelsForTheRouteZone()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle1 = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var vehicle2 = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle2.Id);
        var driver1 = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var driver2 = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver2.Id);
        var routeParcel = await db.Parcels
            .Include(candidate => candidate.RecipientAddress)
            .Include(candidate => candidate.ShipperAddress)
            .SingleAsync(candidate => candidate.Id == data.Parcel1.Id);
        routeParcel.Status = ParcelStatus.OutForDelivery;
        routeParcel.RecipientAddress.GeoLocation = CreatePoint(151.215, -33.872);

        var availableParcel = CreateParcel(
            "LMCAND0001",
            data.Zone1,
            routeParcel.ShipperAddress,
            CreateAddress("12 Eligible Street", 151.23, -33.881, data.ServiceDate),
            ParcelStatus.Staged,
            data.ServiceDate);
        var wrongZoneParcel = CreateParcel(
            "LMCAND0002",
            data.Zone2,
            routeParcel.ShipperAddress,
            CreateAddress("13 Wrong Zone Street", 151.24, -33.882, data.ServiceDate),
            ParcelStatus.Staged,
            data.ServiceDate);
        var assignedParcel = CreateParcel(
            "LMCAND0003",
            data.Zone1,
            routeParcel.ShipperAddress,
            CreateAddress("14 Assigned Street", 151.25, -33.883, data.ServiceDate),
            ParcelStatus.Staged,
            data.ServiceDate);

        var route = RouteAssignmentTestData.CreateRoute(
            vehicle1,
            driver1,
            data.ServiceDate,
            RouteStatus.Dispatched,
            routeParcel);
        route.Stops.Add(CreateStop(route, routeParcel, 1, data.ServiceDate));

        var siblingRoute = RouteAssignmentTestData.CreateRoute(
            vehicle2,
            driver2,
            data.ServiceDate.AddHours(1),
            RouteStatus.Draft,
            assignedParcel);

        db.AddRange(
            availableParcel.RecipientAddress,
            wrongZoneParcel.RecipientAddress,
            assignedParcel.RecipientAddress,
            availableParcel,
            wrongZoneParcel,
            assignedParcel,
            route,
            siblingRoute);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new GetDispatchedRouteParcelCandidatesQueryHandler(db);

        var result = await handler.Handle(
            new GetDispatchedRouteParcelCandidatesQuery(route.Id),
            CancellationToken.None);

        result.Select(candidate => candidate.TrackingNumber).Should().Contain(availableParcel.TrackingNumber);
        result.Select(candidate => candidate.TrackingNumber).Should().NotContain(wrongZoneParcel.TrackingNumber);
        result.Select(candidate => candidate.TrackingNumber).Should().NotContain(assignedParcel.TrackingNumber);
    }

    private static RouteStop CreateStop(
        Route route,
        Parcel parcel,
        int sequence,
        DateTimeOffset timestamp)
    {
        return new RouteStop
        {
            Route = route,
            RouteId = route.Id,
            Sequence = sequence,
            RecipientLabel = parcel.RecipientAddress.ContactName ?? parcel.TrackingNumber,
            Street1 = parcel.RecipientAddress.Street1,
            Street2 = parcel.RecipientAddress.Street2,
            City = parcel.RecipientAddress.City,
            State = parcel.RecipientAddress.State,
            PostalCode = parcel.RecipientAddress.PostalCode,
            CountryCode = parcel.RecipientAddress.CountryCode,
            StopLocation = parcel.RecipientAddress.GeoLocation!.Copy() as Point ?? parcel.RecipientAddress.GeoLocation!,
            CreatedAt = timestamp,
            CreatedBy = "tests",
            Parcels = [parcel],
        };
    }

    private static Parcel CreateParcel(
        string trackingNumber,
        Zone zone,
        Address shipperAddress,
        Address recipientAddress,
        ParcelStatus status,
        DateTimeOffset createdAt)
    {
        return new Parcel
        {
            Id = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            Description = "Route adjustment candidate parcel",
            ServiceType = ServiceType.Standard,
            Status = status,
            ShipperAddress = shipperAddress,
            ShipperAddressId = shipperAddress.Id,
            RecipientAddress = recipientAddress,
            RecipientAddressId = recipientAddress.Id,
            Weight = 4m,
            WeightUnit = WeightUnit.Kg,
            Length = 30m,
            Width = 20m,
            Height = 10m,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 100m,
            Currency = "AUD",
            EstimatedDeliveryDate = createdAt.AddDays(2),
            DeliveryAttempts = 0,
            ZoneId = zone.Id,
            CreatedAt = createdAt.AddDays(-1),
            CreatedBy = "tests",
        };
    }

    private static Address CreateAddress(
        string street1,
        double longitude,
        double latitude,
        DateTimeOffset createdAt)
    {
        return new Address
        {
            Id = Guid.NewGuid(),
            Street1 = street1,
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
            IsResidential = true,
            GeoLocation = CreatePoint(longitude, latitude),
            ContactName = "Candidate Customer",
            CreatedAt = createdAt.AddDays(-1),
            CreatedBy = "tests",
        };
    }

    private static Point CreatePoint(double longitude, double latitude)
    {
        var point = new Point(longitude, latitude) { SRID = 4326 };
        return point;
    }
}
