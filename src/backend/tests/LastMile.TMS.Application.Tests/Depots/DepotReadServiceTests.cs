using FluentAssertions;
using LastMile.TMS.Application.Depots.Reads;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Tests.Depots;

public class DepotReadServiceTests
{
    private static AppDbContext MakeDbContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
    }

    private static async Task<Depot> SeedDepot(
        AppDbContext db,
        string name,
        bool isActive = true,
        string city = "Cairo")
    {
        var address = new Address
        {
            Street1 = $"{name} Street",
            City = city,
            State = "Cairo",
            PostalCode = "12345",
            CountryCode = "EG",
        };
        db.Addresses.Add(address);

        var depot = new Depot
        {
            Name = name,
            AddressId = address.Id,
            Address = address,
            IsActive = isActive,
        };
        depot.OperatingHours.Add(new OperatingHours
        {
            DayOfWeek = DayOfWeek.Monday,
            OpenTime = new TimeOnly(9, 0),
            ClosedTime = new TimeOnly(17, 0),
            IsClosed = false,
        });
        db.Depots.Add(depot);
        await db.SaveChangesAsync();
        return depot;
    }

    [Fact]
    public async Task GetDepots_ReturnsAllDepots()
    {
        var db = MakeDbContext();
        var depot = await SeedDepot(db, "Cairo Central");
        db.ChangeTracker.Clear();

        var service = new DepotReadService(db);
        var result = await service.GetDepots().ToListAsync();

        result.Should().HaveCount(1);
        var entity = result[0];
        entity.Id.Should().Be(depot.Id);
        entity.Name.Should().Be("Cairo Central");
        entity.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetDepots_ReturnsMultipleDepots()
    {
        var db = MakeDbContext();
        await SeedDepot(db, "Cairo Central");
        await SeedDepot(db, "Alex Hub");
        db.ChangeTracker.Clear();

        var service = new DepotReadService(db);
        var result = await service.GetDepots().ToListAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDepots_DoesNotTrackMaterializedEntities()
    {
        var db = MakeDbContext();
        await SeedDepot(db, "Cairo Central");
        db.ChangeTracker.Clear();

        var service = new DepotReadService(db);
        var result = await service.GetDepots().ToListAsync();

        result.Should().HaveCount(1);
        db.ChangeTracker.Entries<Depot>().Should().BeEmpty();
    }

    [Fact]
    public async Task GetDepotById_ReturnsDepotOrNull()
    {
        var db = MakeDbContext();
        var depot = await SeedDepot(db, "Cairo Central");
        var missingId = Guid.NewGuid();
        db.ChangeTracker.Clear();

        var service = new DepotReadService(db);

        var found = await service.GetDepotById(depot.Id).ToListAsync();
        found.Should().HaveCount(1);
        found[0].Name.Should().Be("Cairo Central");
        db.ChangeTracker.Entries<Depot>().Should().BeEmpty();

        var notFound = await service.GetDepotById(missingId).ToListAsync();
        notFound.Should().BeEmpty();
    }
}
