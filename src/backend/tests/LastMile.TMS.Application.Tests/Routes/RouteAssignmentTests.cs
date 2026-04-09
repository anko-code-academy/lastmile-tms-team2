using FluentAssertions;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Routes.Commands;
using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Application.Routes.Queries;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Routes;

public class GetRouteAssignmentCandidatesQueryHandlerTests
{
    [Fact]
    public async Task Handle_FiltersSameDayConflictsAndKeepsCurrentAssignmentSelectable()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle1 = await db.Vehicles.SingleAsync(vehicle => vehicle.Id == data.Vehicle1.Id);
        var vehicle2 = await db.Vehicles.SingleAsync(vehicle => vehicle.Id == data.Vehicle2.Id);
        var vehicle4 = await db.Vehicles.SingleAsync(vehicle => vehicle.Id == data.Vehicle4.Id);
        var driver1 = await db.Drivers.SingleAsync(driver => driver.Id == data.Driver1.Id);
        var driver2 = await db.Drivers.SingleAsync(driver => driver.Id == data.Driver2.Id);
        var driver3 = await db.Drivers.SingleAsync(driver => driver.Id == data.Driver3.Id);
        var currentRoute = RouteAssignmentTestData.CreateRoute(
            vehicle1,
            driver1,
            data.ServiceDate,
            RouteStatus.Planned);

        var conflictingRoute = RouteAssignmentTestData.CreateRoute(
            vehicle2,
            driver2,
            data.ServiceDate.AddHours(1),
            RouteStatus.InProgress);

        var completedRoute = RouteAssignmentTestData.CreateRoute(
            vehicle4,
            driver3,
            data.ServiceDate.AddHours(2),
            RouteStatus.Completed);

        db.Routes.AddRange(currentRoute, conflictingRoute, completedRoute);
        await db.SaveChangesAsync();

        var handler = new GetRouteAssignmentCandidatesQueryHandler(db);

        var result = await handler.Handle(
            new GetRouteAssignmentCandidatesQuery(data.ServiceDate, currentRoute.Id),
            CancellationToken.None);

        result.Vehicles.Select(vehicle => vehicle.Id).Should().Contain(currentRoute.VehicleId);
        result.Vehicles.Select(vehicle => vehicle.Id).Should().Contain(data.Vehicle4.Id);
        result.Vehicles.Select(vehicle => vehicle.Id).Should().NotContain(data.Vehicle2.Id);
        result.Vehicles.Select(vehicle => vehicle.Id).Should().NotContain(data.Vehicle3.Id);

        result.Drivers.Select(driver => driver.Id).Should().Contain(currentRoute.DriverId);
        result.Drivers.Select(driver => driver.Id).Should().Contain(data.Driver3.Id);
        result.Drivers.Select(driver => driver.Id).Should().NotContain(data.Driver2.Id);

        var driverWithoutSchedule = result.Drivers.Single(driver => driver.Id == data.Driver3.Id);
        driverWithoutSchedule.WorkloadRoutes.Should().ContainSingle();
        driverWithoutSchedule.WorkloadRoutes[0].RouteId.Should().Be(completedRoute.Id);
        driverWithoutSchedule.WorkloadRoutes[0].Status.Should().Be(RouteStatus.Completed);
    }
}

public class UpdateRouteAssignmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRouteIsNotPlanned_Throws()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle1 = await db.Vehicles.SingleAsync(vehicle => vehicle.Id == data.Vehicle1.Id);
        var driver1 = await db.Drivers.SingleAsync(driver => driver.Id == data.Driver1.Id);
        var route = RouteAssignmentTestData.CreateRoute(
            vehicle1,
            driver1,
            data.ServiceDate,
            RouteStatus.InProgress);
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new UpdateRouteAssignmentCommandHandler(
            db,
            Substitute.For<ICurrentUserService>());

        var act = () => handler.Handle(
            new UpdateRouteAssignmentCommand(
                route.Id,
                new UpdateRouteAssignmentDto
                {
                    DriverId = data.Driver2.Id,
                    VehicleId = data.Vehicle2.Id,
                }),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only planned routes can be reassigned before dispatch*");
    }

    [Fact]
    public async Task Handle_WhenDriverAndVehicleBelongToDifferentDepots_Throws()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle1 = await db.Vehicles.SingleAsync(vehicle => vehicle.Id == data.Vehicle1.Id);
        var driver1 = await db.Drivers.SingleAsync(driver => driver.Id == data.Driver1.Id);
        var route = RouteAssignmentTestData.CreateRoute(
            vehicle1,
            driver1,
            data.ServiceDate,
            RouteStatus.Planned);
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new UpdateRouteAssignmentCommandHandler(
            db,
            Substitute.For<ICurrentUserService>());

        var act = () => handler.Handle(
            new UpdateRouteAssignmentCommand(
                route.Id,
                new UpdateRouteAssignmentDto
                {
                    DriverId = data.Driver5.Id,
                    VehicleId = data.Vehicle2.Id,
                }),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*same depot*");
    }

    [Fact]
    public async Task Handle_WhenRouteParcelsDoNotMatchDriverZone_Throws()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle1 = await db.Vehicles.SingleAsync(vehicle => vehicle.Id == data.Vehicle1.Id);
        var driver1 = await db.Drivers.SingleAsync(driver => driver.Id == data.Driver1.Id);
        var parcel1 = await db.Parcels.SingleAsync(parcel => parcel.Id == data.Parcel1.Id);

        var route = RouteAssignmentTestData.CreateRoute(
            vehicle1,
            driver1,
            data.ServiceDate,
            RouteStatus.Planned,
            parcel1);
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new UpdateRouteAssignmentCommandHandler(
            db,
            Substitute.For<ICurrentUserService>());

        var act = () => handler.Handle(
            new UpdateRouteAssignmentCommand(
                route.Id,
                new UpdateRouteAssignmentDto
                {
                    DriverId = data.Driver3.Id,
                    VehicleId = data.Vehicle2.Id,
                }),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*driver's zone*");
    }
}

internal static class RouteAssignmentTestData
{
    private static readonly GeometryFactory GeoFactory = new(new PrecisionModel(), 4326);

    internal static AppDbContext CreateDbContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    internal static async Task<SeededRouteAssignmentData> SeedAsync(AppDbContext db)
    {
        var serviceDate = new DateTimeOffset(2026, 4, 9, 8, 0, 0, TimeSpan.Zero);

        var depotAddress1 = CreateAddress("1 Depot Way");
        var depotAddress2 = CreateAddress("2 Depot Way");
        var shipperAddress = CreateAddress("10 Shipper St");
        var recipientAddress = CreateAddress("20 Recipient St");

        var depot1 = new Depot
        {
            Id = Guid.NewGuid(),
            Name = "North Depot",
            Address = depotAddress1,
            AddressId = depotAddress1.Id,
            IsActive = true,
            CreatedAt = serviceDate.AddDays(-10),
            CreatedBy = "tests",
        };

        var depot2 = new Depot
        {
            Id = Guid.NewGuid(),
            Name = "South Depot",
            Address = depotAddress2,
            AddressId = depotAddress2.Id,
            IsActive = true,
            CreatedAt = serviceDate.AddDays(-10),
            CreatedBy = "tests",
        };

        var zone1 = new Zone
        {
            Id = Guid.NewGuid(),
            Name = "North Zone",
            Boundary = CreateBoundary(151.0, -33.0),
            Depot = depot1,
            DepotId = depot1.Id,
            IsActive = true,
            CreatedAt = serviceDate.AddDays(-10),
            CreatedBy = "tests",
        };

        var zone2 = new Zone
        {
            Id = Guid.NewGuid(),
            Name = "Central Zone",
            Boundary = CreateBoundary(152.0, -34.0),
            Depot = depot1,
            DepotId = depot1.Id,
            IsActive = true,
            CreatedAt = serviceDate.AddDays(-10),
            CreatedBy = "tests",
        };

        var zone3 = new Zone
        {
            Id = Guid.NewGuid(),
            Name = "South Zone",
            Boundary = CreateBoundary(153.0, -35.0),
            Depot = depot2,
            DepotId = depot2.Id,
            IsActive = true,
            CreatedAt = serviceDate.AddDays(-10),
            CreatedBy = "tests",
        };

        var user1 = CreateUser("driver1@lastmile.test", "North", "One", serviceDate, depot1, zone1);
        var user2 = CreateUser("driver2@lastmile.test", "North", "Two", serviceDate, depot1, zone1);
        var user3 = CreateUser("driver3@lastmile.test", "South", "Three", serviceDate, depot1, zone2);
        var user4 = CreateUser("driver4@lastmile.test", "North", "Four", serviceDate, depot1, zone1);
        var user5 = CreateUser("driver5@lastmile.test", "South", "Five", serviceDate, depot2, zone3);

        var driver1 = CreateDriver("North", "One", "LIC-001", user1, depot1, zone1, serviceDate);
        var driver2 = CreateDriver("North", "Two", "LIC-002", user2, depot1, zone1, serviceDate);
        var driver3 = CreateDriver("South", "Three", "LIC-003", user3, depot1, zone2, serviceDate);
        var driver4 = CreateDriver("North", "Four", "LIC-004", user4, depot1, zone1, serviceDate);
        var driver5 = CreateDriver("South", "Five", "LIC-005", user5, depot2, zone3, serviceDate);

        var vehicle1 = CreateVehicle("VAN-001", depot1, VehicleStatus.Available, 50, 500m, serviceDate);
        var vehicle2 = CreateVehicle("VAN-002", depot1, VehicleStatus.Available, 50, 500m, serviceDate);
        var vehicle3 = CreateVehicle("VAN-003", depot1, VehicleStatus.Maintenance, 50, 500m, serviceDate);
        var vehicle4 = CreateVehicle("VAN-004", depot1, VehicleStatus.Available, 50, 500m, serviceDate);

        var parcel1 = CreateParcel("LMASSIGN0001", zone1, shipperAddress, recipientAddress, 8m, serviceDate);
        var parcel2 = CreateParcel("LMASSIGN0002", zone1, shipperAddress, recipientAddress, 5m, serviceDate);
        var parcel3 = CreateParcel("LMASSIGN0003", zone2, shipperAddress, recipientAddress, 4m, serviceDate);

        db.AddRange(
            depotAddress1,
            depotAddress2,
            shipperAddress,
            recipientAddress,
            depot1,
            depot2,
            zone1,
            zone2,
            zone3,
            user1,
            user2,
            user3,
            user4,
            user5,
            driver1,
            driver2,
            driver3,
            driver4,
            driver5,
            vehicle1,
            vehicle2,
            vehicle3,
            vehicle4,
            parcel1,
            parcel2,
            parcel3);

        await db.SaveChangesAsync();

        return new SeededRouteAssignmentData(
            serviceDate,
            depot1,
            depot2,
            zone1,
            zone2,
            zone3,
            driver1,
            driver2,
            driver3,
            driver4,
            driver5,
            vehicle1,
            vehicle2,
            vehicle3,
            vehicle4,
            parcel1,
            parcel2,
            parcel3);
    }

    internal static DriverAvailability CreateAvailability(
        Driver driver,
        DayOfWeek dayOfWeek,
        bool isAvailable)
    {
        return new DriverAvailability
        {
            Id = Guid.NewGuid(),
            Driver = driver,
            DriverId = driver.Id,
            DayOfWeek = dayOfWeek,
            ShiftStart = new TimeOnly(8, 0),
            ShiftEnd = new TimeOnly(17, 0),
            IsAvailable = isAvailable,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };
    }

    internal static Route CreateRoute(
        Vehicle vehicle,
        Driver driver,
        DateTimeOffset startDate,
        RouteStatus status,
        params Parcel[] parcels)
    {
        return new Route
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicle.Id,
            DriverId = driver.Id,
            StartDate = startDate,
            StartMileage = 100,
            EndMileage = status == RouteStatus.Completed ? 160 : 0,
            Status = status,
            StagingArea = StagingArea.A,
            CreatedAt = startDate.AddDays(-1),
            CreatedBy = "tests",
            Parcels = parcels.ToList(),
        };
    }

    private static Address CreateAddress(string street1)
    {
        return new Address
        {
            Id = Guid.NewGuid(),
            Street1 = street1,
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
            IsResidential = false,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };
    }

    private static ApplicationUser CreateUser(
        string email,
        string firstName,
        string lastName,
        DateTimeOffset createdAt,
        Depot depot,
        Zone zone)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            Depot = depot,
            DepotId = depot.Id,
            Zone = zone,
            ZoneId = zone.Id,
            CreatedAt = createdAt.AddDays(-10),
            CreatedBy = "tests",
        };
    }

    private static Driver CreateDriver(
        string firstName,
        string lastName,
        string licenseNumber,
        ApplicationUser user,
        Depot depot,
        Zone zone,
        DateTimeOffset createdAt)
    {
        return new Driver
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = user.Email,
            Phone = "+61000000000",
            LicenseNumber = licenseNumber,
            LicenseExpiryDate = createdAt.AddYears(1),
            User = user,
            UserId = user.Id,
            Depot = depot,
            DepotId = depot.Id,
            Zone = zone,
            ZoneId = zone.Id,
            Status = DriverStatus.Active,
            CreatedAt = createdAt.AddDays(-10),
            CreatedBy = "tests",
        };
    }

    private static Vehicle CreateVehicle(
        string plate,
        Depot depot,
        VehicleStatus status,
        int parcelCapacity,
        decimal weightCapacity,
        DateTimeOffset createdAt)
    {
        return new Vehicle
        {
            Id = Guid.NewGuid(),
            RegistrationPlate = plate,
            Type = VehicleType.Van,
            ParcelCapacity = parcelCapacity,
            WeightCapacity = weightCapacity,
            Status = status,
            Depot = depot,
            DepotId = depot.Id,
            CreatedAt = createdAt.AddDays(-10),
            CreatedBy = "tests",
        };
    }

    private static Parcel CreateParcel(
        string trackingNumber,
        Zone zone,
        Address shipperAddress,
        Address recipientAddress,
        decimal weightKg,
        DateTimeOffset createdAt)
    {
        return new Parcel
        {
            Id = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            Description = "Route assignment test parcel",
            ServiceType = ServiceType.Standard,
            Status = ParcelStatus.Sorted,
            ShipperAddress = shipperAddress,
            ShipperAddressId = shipperAddress.Id,
            RecipientAddress = recipientAddress,
            RecipientAddressId = recipientAddress.Id,
            Weight = weightKg,
            WeightUnit = WeightUnit.Kg,
            Length = 30m,
            Width = 20m,
            Height = 10m,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 100m,
            Currency = "AUD",
            EstimatedDeliveryDate = createdAt.AddDays(2),
            DeliveryAttempts = 0,
            Zone = zone,
            ZoneId = zone.Id,
            CreatedAt = createdAt.AddDays(-5),
            CreatedBy = "tests",
        };
    }

    private static Polygon CreateBoundary(double x, double y)
    {
        var ring = GeoFactory.CreateLinearRing(
        [
            new Coordinate(x, y),
            new Coordinate(x + 0.2, y),
            new Coordinate(x + 0.2, y + 0.2),
            new Coordinate(x, y + 0.2),
            new Coordinate(x, y),
        ]);
        ring.SRID = 4326;
        return GeoFactory.CreatePolygon(ring);
    }
}

internal sealed record SeededRouteAssignmentData(
    DateTimeOffset ServiceDate,
    Depot Depot1,
    Depot Depot2,
    Zone Zone1,
    Zone Zone2,
    Zone Zone3,
    Driver Driver1,
    Driver Driver2,
    Driver Driver3,
    Driver Driver4,
    Driver Driver5,
    Vehicle Vehicle1,
    Vehicle Vehicle2,
    Vehicle Vehicle3,
    Vehicle Vehicle4,
    Parcel Parcel1,
    Parcel Parcel2,
    Parcel Parcel3);
