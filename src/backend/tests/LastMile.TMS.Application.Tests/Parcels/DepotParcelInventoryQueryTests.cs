using FluentAssertions;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Queries;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Parcels;

public class DepotParcelInventoryQueryTests
{
    private static readonly GeometryFactory GeoFactory = new(new PrecisionModel(), 4326);

    private static AppDbContext MakeDbContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private static ICurrentUserService CreateCurrentUser(ApplicationUser user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id.ToString());
        currentUser.UserName.Returns(user.UserName);
        currentUser.Roles.Returns(["OperationsManager"]);
        return currentUser;
    }

    [Fact]
    public async Task GetDepotParcelInventory_NoAssignedDepot_ReturnsNull()
    {
        await using var db = MakeDbContext();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "ops@test.com",
            Email = "ops@test.com",
            FirstName = "Ops",
            LastName = "User",
            IsActive = true,
            DepotId = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new GetDepotParcelInventoryQueryHandler(db, CreateCurrentUser(user));

        var result = await handler.Handle(
            new GetDepotParcelInventoryQuery(AgingThresholdMinutes: 240),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDepotParcelInventory_AssignedDepot_ReturnsScopedCountsZonesAndAging()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedInventoryFixtureAsync(db);
        var handler = new GetDepotParcelInventoryQueryHandler(db, CreateCurrentUser(fixture.Operator));

        var result = await handler.Handle(
            new GetDepotParcelInventoryQuery(AgingThresholdMinutes: 240),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.DepotName.Should().Be("Depot Alpha");
        result.StatusCounts.Select(item => (item.Status, item.Count)).Should().Equal(
            ("RECEIVED_AT_DEPOT", 2),
            ("SORTED", 1),
            ("STAGED", 0),
            ("LOADED", 1),
            ("EXCEPTION", 1));
        result.ZoneCounts.Select(item => (item.ZoneName, item.Count)).Should().Equal(
            ("Zone A1", 3),
            ("Zone A2", 2));
        result.AgingAlert.ThresholdMinutes.Should().Be(240);
        result.AgingAlert.Count.Should().Be(3);
    }

    [Fact]
    public async Task GetDepotParcelInventoryParcels_FiltersByStatusZoneAndAging_AndUsesCursorPaging()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedInventoryFixtureAsync(db);
        var handler = new GetDepotParcelInventoryParcelsQueryHandler(db, CreateCurrentUser(fixture.Operator));

        var firstPage = await handler.Handle(
            new GetDepotParcelInventoryParcelsQuery(
                AgingThresholdMinutes: 240,
                Status: null,
                ZoneId: fixture.ZoneA1.Id,
                AgingOnly: true,
                First: 1,
                After: null),
            CancellationToken.None);

        firstPage.TotalCount.Should().Be(2);
        firstPage.Nodes.Should().HaveCount(1);
        firstPage.Nodes[0].TrackingNumber.Should().Be("LM-ALPHA-RECEIVED-OLD");
        firstPage.Nodes[0].ZoneName.Should().Be("Zone A1");
        firstPage.Nodes[0].AgeMinutes.Should().BeGreaterThan(240);
        firstPage.PageInfo.HasNextPage.Should().BeTrue();
        firstPage.PageInfo.EndCursor.Should().Be("1");

        var secondPage = await handler.Handle(
            new GetDepotParcelInventoryParcelsQuery(
                AgingThresholdMinutes: 240,
                Status: null,
                ZoneId: fixture.ZoneA1.Id,
                AgingOnly: true,
                First: 1,
                After: firstPage.PageInfo.EndCursor),
            CancellationToken.None);

        secondPage.TotalCount.Should().Be(2);
        secondPage.Nodes.Should().HaveCount(1);
        secondPage.Nodes[0].TrackingNumber.Should().Be("LM-ALPHA-LOADED-OLD");
        secondPage.PageInfo.HasPreviousPage.Should().BeTrue();

        var statusFiltered = await handler.Handle(
            new GetDepotParcelInventoryParcelsQuery(
                AgingThresholdMinutes: 240,
                Status: ParcelStatus.ReceivedAtDepot,
                ZoneId: null,
                AgingOnly: false,
                First: 10,
                After: null),
            CancellationToken.None);

        statusFiltered.TotalCount.Should().Be(2);
        statusFiltered.Nodes.Should().OnlyContain(node => node.Status == "RECEIVED_AT_DEPOT");
    }

    private static async Task<InventoryFixture> SeedInventoryFixtureAsync(AppDbContext db)
    {
        var depotAddressA = CreateAddress("1 Alpha Street", "Sydney", "2000");
        var depotAddressB = CreateAddress("2 Beta Street", "Melbourne", "3000");
        var shipperAddress = CreateAddress("10 Shipper Lane", "Sydney", "2001");
        var recipientAddress = CreateAddress("20 Recipient Road", "Sydney", "2002");

        var depotA = new Depot
        {
            Id = Guid.NewGuid(),
            Name = "Depot Alpha",
            AddressId = depotAddressA.Id,
            Address = depotAddressA,
            IsActive = true,
        };

        var depotB = new Depot
        {
            Id = Guid.NewGuid(),
            Name = "Depot Beta",
            AddressId = depotAddressB.Id,
            Address = depotAddressB,
            IsActive = true,
        };

        var zoneA1 = CreateZone("Zone A1", depotA);
        var zoneA2 = CreateZone("Zone A2", depotA);
        var zoneB1 = CreateZone("Zone B1", depotB);

        var operatorUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "ops.alpha@test.com",
            Email = "ops.alpha@test.com",
            FirstName = "Alpha",
            LastName = "Ops",
            IsActive = true,
            DepotId = depotA.Id,
            ZoneId = zoneA1.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.AddRange(depotAddressA, depotAddressB, shipperAddress, recipientAddress);
        db.AddRange(depotA, depotB, zoneA1, zoneA2, zoneB1);
        db.Users.Add(operatorUser);

        db.Parcels.AddRange(
            CreateParcel("LM-ALPHA-RECEIVED-OLD", ParcelStatus.ReceivedAtDepot, zoneA1, shipperAddress, recipientAddress, 8, 8),
            CreateParcel("LM-ALPHA-RECEIVED-NEW", ParcelStatus.ReceivedAtDepot, zoneA2, shipperAddress, recipientAddress, 1, 1),
            CreateParcel("LM-ALPHA-SORTED-NEW", ParcelStatus.Sorted, zoneA1, shipperAddress, recipientAddress, 2, 2),
            CreateParcel("LM-ALPHA-LOADED-OLD", ParcelStatus.Loaded, zoneA1, shipperAddress, recipientAddress, 7, 5),
            CreateParcel("LM-ALPHA-EXCEPTION-OLD", ParcelStatus.Exception, zoneA2, shipperAddress, recipientAddress, 9, 9),
            CreateParcel("LM-ALPHA-REGISTERED", ParcelStatus.Registered, zoneA1, shipperAddress, recipientAddress, 12, 12),
            CreateParcel("LM-ALPHA-DELIVERED", ParcelStatus.Delivered, zoneA2, shipperAddress, recipientAddress, 10, 10),
            CreateParcel("LM-BETA-EXCEPTION", ParcelStatus.Exception, zoneB1, shipperAddress, recipientAddress, 11, 11));

        await db.SaveChangesAsync();

        return new InventoryFixture(operatorUser, zoneA1, zoneA2);
    }

    private static Address CreateAddress(string street1, string city, string postalCode) =>
        new()
        {
            Id = Guid.NewGuid(),
            Street1 = street1,
            City = city,
            State = "NSW",
            PostalCode = postalCode,
            CountryCode = "AU",
        };

    private static Zone CreateZone(string name, Depot depot) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Boundary = MakePolygon(),
            DepotId = depot.Id,
            Depot = depot,
            IsActive = true,
        };

    private static Polygon MakePolygon()
    {
        var polygon = GeoFactory.CreatePolygon(
            [
                new Coordinate(151.0, -33.0),
                new Coordinate(152.0, -33.0),
                new Coordinate(152.0, -34.0),
                new Coordinate(151.0, -34.0),
                new Coordinate(151.0, -33.0),
            ]);
        polygon.SRID = 4326;
        return polygon;
    }

    private static Parcel CreateParcel(
        string trackingNumber,
        ParcelStatus status,
        Zone zone,
        Address shipperAddress,
        Address recipientAddress,
        int createdHoursAgo,
        int? lastModifiedHoursAgo)
    {
        var createdAt = DateTimeOffset.UtcNow.AddHours(-createdHoursAgo);
        var lastModifiedAt = lastModifiedHoursAgo.HasValue
            ? DateTimeOffset.UtcNow.AddHours(-lastModifiedHoursAgo.Value)
            : (DateTimeOffset?)null;

        return new Parcel
        {
            Id = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            Description = "Depot inventory test parcel",
            ServiceType = ServiceType.Standard,
            Status = status,
            ShipperAddressId = shipperAddress.Id,
            ShipperAddress = shipperAddress,
            RecipientAddressId = recipientAddress.Id,
            RecipientAddress = recipientAddress,
            Weight = 1.25m,
            WeightUnit = WeightUnit.Kg,
            Length = 10m,
            Width = 10m,
            Height = 5m,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 25m,
            Currency = "AUD",
            EstimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(2),
            DeliveryAttempts = 0,
            ZoneId = zone.Id,
            Zone = zone,
            CreatedAt = createdAt,
            CreatedBy = "tests",
            LastModifiedAt = lastModifiedAt,
            LastModifiedBy = lastModifiedAt.HasValue ? "tests" : null,
        };
    }

    private sealed record InventoryFixture(
        ApplicationUser Operator,
        Zone ZoneA1,
        Zone ZoneA2);
}
