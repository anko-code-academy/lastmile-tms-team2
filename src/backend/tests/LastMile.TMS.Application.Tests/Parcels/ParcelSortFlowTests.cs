using FluentAssertions;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Commands;
using LastMile.TMS.Application.Parcels.Queries;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Parcels;

public class ParcelSortFlowTests
{
    private static AppDbContext MakeDbContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private static ICurrentUserService CreateCurrentUser() =>
        CreateCurrentUser("sorter@test.local");

    private static ICurrentUserService CreateCurrentUser(string userName)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid().ToString());
        currentUser.UserName.Returns(userName);
        return currentUser;
    }

    [Fact]
    public async Task GetParcelSortInstruction_ReceivedAtDepotWithBin_ReturnsCanSortAndTargetBin()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedSortFixtureAsync(db);
        var handler = new GetParcelSortInstructionQueryHandler(db);

        var instruction = await handler.Handle(
            new GetParcelSortInstructionQuery(fixture.Parcel.TrackingNumber, null),
            CancellationToken.None);

        instruction.Should().NotBeNull();
        instruction!.CanSort.Should().BeTrue();
        instruction.TargetBins.Should().ContainSingle();
        instruction.RecommendedBinLocationId.Should().Be(fixture.Bin!.Id);
        instruction.TargetBins[0].IsRecommended.Should().BeTrue();
        instruction.DeliveryZoneName.Should().Be(fixture.Zone.Name);
    }

    [Fact]
    public async Task GetParcelSortInstruction_SortedStatus_ReturnsWrongStatus()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedSortFixtureAsync(db, ParcelStatus.Sorted);
        var handler = new GetParcelSortInstructionQueryHandler(db);

        var instruction = await handler.Handle(
            new GetParcelSortInstructionQuery(fixture.Parcel.TrackingNumber, null),
            CancellationToken.None);

        instruction.Should().NotBeNull();
        instruction!.CanSort.Should().BeFalse();
        instruction.BlockReasonCode.Should().Be("WRONG_STATUS");
    }

    [Fact]
    public async Task GetParcelSortInstruction_NoBins_ReturnsNoTargetBins()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedSortFixtureAsync(db, includeBin: false);
        var handler = new GetParcelSortInstructionQueryHandler(db);

        var instruction = await handler.Handle(
            new GetParcelSortInstructionQuery(fixture.Parcel.TrackingNumber, null),
            CancellationToken.None);

        instruction.Should().NotBeNull();
        instruction!.CanSort.Should().BeFalse();
        instruction.BlockReasonCode.Should().Be("NO_TARGET_BINS");
        instruction.TargetBins.Should().BeEmpty();
    }

    [Fact]
    public async Task GetParcelSortInstruction_DepotFilterMismatch_ReturnsWrongDepot()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedSortFixtureAsync(db);
        var handler = new GetParcelSortInstructionQueryHandler(db);

        var instruction = await handler.Handle(
            new GetParcelSortInstructionQuery(fixture.Parcel.TrackingNumber, Guid.NewGuid()),
            CancellationToken.None);

        instruction.Should().NotBeNull();
        instruction!.CanSort.Should().BeFalse();
        instruction.BlockReasonCode.Should().Be("WRONG_DEPOT");
    }

    [Fact]
    public async Task ConfirmParcelSort_ValidBin_TransitionsToSorted()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedSortFixtureAsync(db);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new ConfirmParcelSortCommandHandler(db, CreateCurrentUser(), notifier);

        var dto = await handler.Handle(
            new ConfirmParcelSortCommand(fixture.Parcel.Id, fixture.Bin!.Id),
            CancellationToken.None);

        dto.Status.Should().Be("Sorted");

        var parcel = await db.Parcels
            .Include(p => p.TrackingEvents)
            .SingleAsync(p => p.Id == fixture.Parcel.Id);

        parcel.Status.Should().Be(ParcelStatus.Sorted);
        parcel.TrackingEvents.Should().Contain(e => e.Description.Contains("sorted into", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ConfirmParcelSort_BinForOtherZone_ThrowsMisSort()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedSortFixtureAsync(db, otherZoneForWrongBin: true);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new ConfirmParcelSortCommandHandler(db, CreateCurrentUser(), notifier);

        var act = () => handler.Handle(
            new ConfirmParcelSortCommand(fixture.Parcel.Id, fixture.WrongZoneBin!.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Mis-sort*");
    }

    [Fact]
    public async Task GetParcelSortInstruction_InactiveZone_ReturnsZoneInactive()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedSortFixtureAsync(db, zoneIsActive: false);
        var handler = new GetParcelSortInstructionQueryHandler(db);

        var instruction = await handler.Handle(
            new GetParcelSortInstructionQuery(fixture.Parcel.TrackingNumber, null),
            CancellationToken.None);

        instruction.Should().NotBeNull();
        instruction!.CanSort.Should().BeFalse();
        instruction.BlockReasonCode.Should().Be("ZONE_INACTIVE");
        instruction.DeliveryZoneIsActive.Should().BeFalse();
        instruction.TargetBins.Should().BeEmpty();
        instruction.RecommendedBinLocationId.Should().BeNull();
    }

    [Fact]
    public async Task GetParcelSortInstruction_RegisteredStatus_ReturnsWrongStatus()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedSortFixtureAsync(db, ParcelStatus.Registered);
        var handler = new GetParcelSortInstructionQueryHandler(db);

        var instruction = await handler.Handle(
            new GetParcelSortInstructionQuery(fixture.Parcel.TrackingNumber, null),
            CancellationToken.None);

        instruction.Should().NotBeNull();
        instruction!.CanSort.Should().BeFalse();
        instruction.BlockReasonCode.Should().Be("WRONG_STATUS");
        instruction.BlockReasonMessage.Should().Contain("Registered");
    }

    [Fact]
    public async Task ConfirmParcelSort_InactiveBin_ThrowsMisSort()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedSortFixtureAsync(db, inactiveBin: true);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new ConfirmParcelSortCommandHandler(db, CreateCurrentUser(), notifier);

        var act = () => handler.Handle(
            new ConfirmParcelSortCommand(fixture.Parcel.Id, fixture.InactiveBin!.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Mis-sort*inactive*");
    }

    [Fact]
    public async Task ConfirmParcelSort_BinWithoutDeliveryZone_ThrowsMisSort()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedSortFixtureAsync(db, binWithoutZone: true);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new ConfirmParcelSortCommandHandler(db, CreateCurrentUser(), notifier);

        var act = () => handler.Handle(
            new ConfirmParcelSortCommand(fixture.Parcel.Id, fixture.NoZoneBin!.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Mis-sort*not linked to a delivery zone*");
    }

    private static async Task<SortFixture> SeedSortFixtureAsync(
        AppDbContext db,
        ParcelStatus parcelStatus = ParcelStatus.ReceivedAtDepot,
        bool includeBin = true,
        bool otherZoneForWrongBin = false,
        bool zoneIsActive = true,
        bool inactiveBin = false,
        bool binWithoutZone = false)
    {
        var depotAddress = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "1 Depot",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
        };

        var depot = new Depot
        {
            Id = Guid.NewGuid(),
            Name = "Sort Test Depot",
            AddressId = depotAddress.Id,
            Address = depotAddress,
            IsActive = true,
        };

        var zone = new Zone
        {
            Id = Guid.NewGuid(),
            Name = "Sort Zone A",
            Boundary = TestsPolygonFactory.CreateDefault(),
            DepotId = depot.Id,
            Depot = depot,
            IsActive = zoneIsActive,
        };

        var otherZone = new Zone
        {
            Id = Guid.NewGuid(),
            Name = "Sort Zone B",
            Boundary = TestsPolygonFactory.CreateOffset(10),
            DepotId = depot.Id,
            Depot = depot,
            IsActive = true,
        };

        var shipper = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "Ship",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2001",
            CountryCode = "AU",
        };

        var recipient = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "Recv",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2002",
            CountryCode = "AU",
        };

        var parcel = new Parcel
        {
            Id = Guid.NewGuid(),
            TrackingNumber = "LMSORTTEST0001",
            Description = "Sort test",
            ServiceType = ServiceType.Standard,
            Status = parcelStatus,
            ShipperAddressId = shipper.Id,
            ShipperAddress = shipper,
            RecipientAddressId = recipient.Id,
            RecipientAddress = recipient,
            Weight = 1m,
            WeightUnit = WeightUnit.Kg,
            Length = 10,
            Width = 10,
            Height = 10,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 10m,
            Currency = "USD",
            EstimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(1),
            ZoneId = zone.Id,
            Zone = zone,
        };

        var storageZone = new StorageZone
        {
            Id = Guid.NewGuid(),
            Name = "Warehouse North",
            NormalizedName = "WAREHOUSE NORTH",
            DepotId = depot.Id,
            Depot = depot,
        };

        var aisle = new StorageAisle
        {
            Id = Guid.NewGuid(),
            Name = "Aisle 1",
            NormalizedName = "AISLE 1",
            StorageZoneId = storageZone.Id,
            StorageZone = storageZone,
        };

        BinLocation? bin = null;
        BinLocation? wrongZoneBin = null;
        BinLocation? inactiveBinEntity = null;
        BinLocation? noZoneBinEntity = null;

        if (includeBin)
        {
            bin = new BinLocation
            {
                Id = Guid.NewGuid(),
                Name = "Bin-A1",
                NormalizedName = "BIN-A1",
                IsActive = true,
                DeliveryZoneId = zone.Id,
                DeliveryZone = zone,
                StorageAisleId = aisle.Id,
                StorageAisle = aisle,
            };
        }

        if (otherZoneForWrongBin)
        {
            wrongZoneBin = new BinLocation
            {
                Id = Guid.NewGuid(),
                Name = "Bin-Other",
                NormalizedName = "BIN-OTHER",
                IsActive = true,
                DeliveryZoneId = otherZone.Id,
                DeliveryZone = otherZone,
                StorageAisleId = aisle.Id,
                StorageAisle = aisle,
            };
        }

        if (inactiveBin)
        {
            inactiveBinEntity = new BinLocation
            {
                Id = Guid.NewGuid(),
                Name = "Bin-Inactive",
                NormalizedName = "BIN-INACTIVE",
                IsActive = false,
                DeliveryZoneId = zone.Id,
                DeliveryZone = zone,
                StorageAisleId = aisle.Id,
                StorageAisle = aisle,
            };
        }

        if (binWithoutZone)
        {
            noZoneBinEntity = new BinLocation
            {
                Id = Guid.NewGuid(),
                Name = "Bin-NoZone",
                NormalizedName = "BIN-NOZONE",
                IsActive = true,
                DeliveryZoneId = null,
                StorageAisleId = aisle.Id,
                StorageAisle = aisle,
            };
        }

        db.AddRange(
            depotAddress,
            depot,
            zone,
            otherZone,
            shipper,
            recipient,
            parcel,
            storageZone,
            aisle);

        if (bin is not null)
        {
            db.Add(bin);
        }

        if (wrongZoneBin is not null)
        {
            db.Add(wrongZoneBin);
        }

        if (inactiveBinEntity is not null)
        {
            db.Add(inactiveBinEntity);
        }

        if (noZoneBinEntity is not null)
        {
            db.Add(noZoneBinEntity);
        }

        await db.SaveChangesAsync();

        return new SortFixture(parcel, zone, depot, bin, wrongZoneBin, inactiveBinEntity, noZoneBinEntity);
    }

    private sealed record SortFixture(
        Parcel Parcel,
        Zone Zone,
        Depot Depot,
        BinLocation? Bin,
        BinLocation? WrongZoneBin,
        BinLocation? InactiveBin,
        BinLocation? NoZoneBin);
}
