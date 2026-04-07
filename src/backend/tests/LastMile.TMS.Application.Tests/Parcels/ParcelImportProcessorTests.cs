using FluentAssertions;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Infrastructure.Services;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Parcels;

public class ParcelImportProcessorTests
{
    private static readonly GeometryFactory GeoFactory = new(new PrecisionModel(), 4326);

    private static CountingAppDbContext MakeDbContext()
    {
        return new CountingAppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static Point MakePoint(double lon, double lat)
    {
        var point = GeoFactory.CreatePoint(new Coordinate(lon, lat));
        point.SRID = 4326;
        return point;
    }

    private static async Task<(Address shipperAddress, Zone zone)> SeedShipperAndZoneAsync(AppDbContext db)
    {
        var shipperAddress = new Address
        {
            Street1 = "10 Shipper Lane",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
        };
        db.Addresses.Add(shipperAddress);

        var depotAddress = new Address
        {
            Street1 = "1 Depot Road",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
        };
        db.Addresses.Add(depotAddress);

        var depot = new Depot
        {
            Name = "Sydney Central",
            AddressId = depotAddress.Id,
            Address = depotAddress,
            IsActive = true,
        };
        db.Depots.Add(depot);

        var polygon = GeoFactory.CreatePolygon(
        [
            new Coordinate(151.0, -33.0),
            new Coordinate(152.0, -33.0),
            new Coordinate(152.0, -34.0),
            new Coordinate(151.0, -34.0),
            new Coordinate(151.0, -33.0),
        ]);
        polygon.SRID = 4326;

        var zone = new Zone
        {
            Name = "Sydney Metro",
            Boundary = polygon,
            IsActive = true,
            DepotId = depot.Id,
            Depot = depot,
        };
        db.Zones.Add(zone);

        await db.SaveChangesAsync();

        return (shipperAddress, zone);
    }

    [Fact]
    public async Task ProcessAsync_MixedRows_ImportsValidRowsAndRecordsRowFailures()
    {
        var db = MakeDbContext();
        var (shipperAddress, zone) = await SeedShipperAndZoneAsync(db);

        var parcelImport = new ParcelImport
        {
            FileName = "parcels.csv",
            FileFormat = ParcelImportFileFormat.Csv,
            ShipperAddressId = shipperAddress.Id,
            Status = ParcelImportStatus.Queued,
            SourceFile = [1, 2, 3],
            CreatedBy = "ops.manager",
        };
        db.ParcelImports.Add(parcelImport);
        await db.SaveChangesAsync();

        var parser = Substitute.For<IParcelImportFileParser>();
        parser.ParseAsync(
                Arg.Any<string>(),
                Arg.Any<Stream>(),
                Arg.Any<CancellationToken>())
            .Returns(new ParcelImportParsedFile(
                3,
                [
                    new ParcelImportParsedRow(1, CreateRow(
                        street1: "15 George Street",
                        weight: "2.5")),
                    new ParcelImportParsedRow(2, CreateRow(
                        street1: "17 Pitt Street",
                        weight: "abc")),
                    new ParcelImportParsedRow(3, CreateRow(
                        street1: "Unknown Address",
                        weight: "3.5")),
                ]));

        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.GeocodeAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(MakePoint(151.3, -33.8));
        geocoding.GeocodeAsync(
                Arg.Is<string>(value => value.Contains("Unknown Address", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>())
            .Returns((Point?)null);

        var zoneMatching = Substitute.For<IZoneMatchingService>();
        zoneMatching.FindZoneIdAsync(Arg.Any<Point>(), Arg.Any<CancellationToken>())
            .Returns(zone.Id);

        var currentUser = Substitute.For<LastMile.TMS.Application.Common.Interfaces.ICurrentUserService>();
        currentUser.UserName.Returns("ops.manager");

        var registrationService = new ParcelRegistrationService(db, geocoding, zoneMatching, currentUser);
        var processor = new ParcelImportProcessor(db, new InMemoryFileStorageService(), parser, registrationService);

        await processor.ProcessAsync(parcelImport.Id, CancellationToken.None);

        var persistedImport = await db.ParcelImports
            .Include(x => x.RowFailures)
            .SingleAsync(x => x.Id == parcelImport.Id);

        persistedImport.Status.Should().Be(ParcelImportStatus.CompletedWithErrors);
        persistedImport.TotalRows.Should().Be(3);
        persistedImport.ProcessedRows.Should().Be(3);
        persistedImport.ImportedRows.Should().Be(1);
        persistedImport.RejectedRows.Should().Be(2);
        persistedImport.StartedAt.Should().NotBeNull();
        persistedImport.CompletedAt.Should().NotBeNull();
        persistedImport.RowFailures.Should().HaveCount(2);
        persistedImport.RowFailures.Select(x => x.RowNumber).Should().BeEquivalentTo([2, 3]);
        persistedImport.RowFailures.Should().Contain(x => x.ErrorMessage.Contains("weight must be a valid number.", StringComparison.OrdinalIgnoreCase));
        persistedImport.RowFailures.Should().Contain(x => x.ErrorMessage.Contains("could not be geocoded", StringComparison.OrdinalIgnoreCase));

        var importedParcel = await db.Parcels.SingleAsync();
        importedParcel.ParcelImportId.Should().Be(parcelImport.Id);
        importedParcel.TrackingNumber.Should().StartWith("LM");
    }

    [Fact]
    public async Task ProcessAsync_WithoutCurrentUser_UsesParcelImportCreatorForAuditFields()
    {
        var db = MakeDbContext();
        var (shipperAddress, zone) = await SeedShipperAndZoneAsync(db);

        var parcelImport = new ParcelImport
        {
            FileName = "parcels.csv",
            FileFormat = ParcelImportFileFormat.Csv,
            ShipperAddressId = shipperAddress.Id,
            Status = ParcelImportStatus.Queued,
            SourceFile = [1, 2, 3],
            CreatedBy = "ops.manager",
        };
        db.ParcelImports.Add(parcelImport);
        await db.SaveChangesAsync();

        var parser = Substitute.For<IParcelImportFileParser>();
        parser.ParseAsync(
                Arg.Any<string>(),
                Arg.Any<Stream>(),
                Arg.Any<CancellationToken>())
            .Returns(new ParcelImportParsedFile(
                1,
                [
                    new ParcelImportParsedRow(1, CreateRow(
                        street1: "15 George Street",
                        weight: "2.5")),
                ]));

        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.GeocodeAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(MakePoint(151.3, -33.8));

        var zoneMatching = Substitute.For<IZoneMatchingService>();
        zoneMatching.FindZoneIdAsync(Arg.Any<Point>(), Arg.Any<CancellationToken>())
            .Returns(zone.Id);

        var currentUser = Substitute.For<LastMile.TMS.Application.Common.Interfaces.ICurrentUserService>();
        currentUser.UserName.Returns((string?)null);
        currentUser.UserId.Returns((string?)null);

        var registrationService = new ParcelRegistrationService(db, geocoding, zoneMatching, currentUser);
        var processor = new ParcelImportProcessor(db, new InMemoryFileStorageService(), parser, registrationService);

        await processor.ProcessAsync(parcelImport.Id, CancellationToken.None);

        var importedParcel = await db.Parcels
            .Include(x => x.RecipientAddress)
            .SingleAsync();

        importedParcel.CreatedBy.Should().Be("ops.manager");
        importedParcel.RecipientAddress.CreatedBy.Should().Be("ops.manager");
    }

    [Fact]
    public async Task ProcessAsync_WithManyInvalidRows_BatchesProgressSaves()
    {
        var db = MakeDbContext();

        var parcelImport = new ParcelImport
        {
            FileName = "parcels.csv",
            FileFormat = ParcelImportFileFormat.Csv,
            ShipperAddressId = Guid.NewGuid(),
            Status = ParcelImportStatus.Queued,
            SourceFile = [1, 2, 3],
            CreatedBy = "ops.manager",
        };
        db.ParcelImports.Add(parcelImport);
        await db.SaveChangesAsync();
        db.ResetSaveChangesCalls();

        var parser = Substitute.For<IParcelImportFileParser>();
        parser.ParseAsync(
                Arg.Any<string>(),
                Arg.Any<Stream>(),
                Arg.Any<CancellationToken>())
            .Returns(new ParcelImportParsedFile(
                100,
                Enumerable.Range(1, 100)
                    .Select(index => new ParcelImportParsedRow(
                        index,
                        CreateRow(
                            street1: $"Invalid Street {index}",
                            weight: "abc")))
                    .ToArray()));

        var registrationService = Substitute.For<IParcelRegistrationService>();
        var processor = new ParcelImportProcessor(db, new InMemoryFileStorageService(), parser, registrationService);

        await processor.ProcessAsync(parcelImport.Id, CancellationToken.None);

        db.SaveChangesCalls.Should().BeLessThan(20);
        await registrationService.DidNotReceiveWithAnyArgs()
            .RegisterAsync(default!, default, default, default);
    }

    [Fact]
    public async Task ProcessAsync_WithoutStoredSourceFile_FailsGracefully()
    {
        var db = MakeDbContext();

        var parcelImport = new ParcelImport
        {
            FileName = "parcels.csv",
            FileFormat = ParcelImportFileFormat.Csv,
            ShipperAddressId = Guid.NewGuid(),
            Status = ParcelImportStatus.Queued,
            CreatedBy = "ops.manager",
        };
        db.ParcelImports.Add(parcelImport);
        await db.SaveChangesAsync();

        var parser = Substitute.For<IParcelImportFileParser>();
        var registrationService = Substitute.For<IParcelRegistrationService>();
        var processor = new ParcelImportProcessor(db, new InMemoryFileStorageService(), parser, registrationService);

        var action = () => processor.ProcessAsync(parcelImport.Id, CancellationToken.None);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"Parcel import '{parcelImport.Id}' has no source file content.");

        var persistedImport = await db.ParcelImports.SingleAsync(x => x.Id == parcelImport.Id);
        persistedImport.Status.Should().Be(ParcelImportStatus.Failed);
        persistedImport.FailureMessage.Should().Be($"Parcel import '{parcelImport.Id}' has no source file content.");
        await parser.DidNotReceiveWithAnyArgs()
            .ParseAsync(default!, default!, default);
    }

    private sealed class CountingAppDbContext(DbContextOptions<AppDbContext> options)
        : AppDbContext(options)
    {
        public int SaveChangesCalls { get; private set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return base.SaveChangesAsync(cancellationToken);
        }

        public void ResetSaveChangesCalls()
        {
            SaveChangesCalls = 0;
        }
    }

    private static Dictionary<string, string?> CreateRow(
        string street1,
        string weight)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["recipient_street1"] = street1,
            ["recipient_street2"] = null,
            ["recipient_city"] = "Sydney",
            ["recipient_state"] = "NSW",
            ["recipient_postal_code"] = "2000",
            ["recipient_country_code"] = "AU",
            ["recipient_is_residential"] = "true",
            ["recipient_contact_name"] = "Taylor Smith",
            ["recipient_company_name"] = "Acme",
            ["recipient_phone"] = "+61000000000",
            ["recipient_email"] = "taylor@example.com",
            ["description"] = "Box",
            ["parcel_type"] = "Package",
            ["service_type"] = "STANDARD",
            ["weight"] = weight,
            ["weight_unit"] = "KG",
            ["length"] = "20",
            ["width"] = "10",
            ["height"] = "5",
            ["dimension_unit"] = "CM",
            ["declared_value"] = "100",
            ["currency"] = "AUD",
            ["estimated_delivery_date"] = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)).ToString("yyyy-MM-dd"),
        };
    }
}
