using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Infrastructure.Services;
using LastMile.TMS.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LastMile.TMS.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class StorageBackfillRunnerTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RunAsync_IsIdempotent_AndBackfillsLegacyData()
    {
        await using var setupScope = factory.Services.CreateAsyncScope();
        var env = setupScope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var db = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var webRoot = !string.IsNullOrEmpty(env.WebRootPath)
            ? env.WebRootPath
            : Path.Combine(env.ContentRootPath, "wwwroot");
        var driversDir = Path.Combine(webRoot, "uploads", "drivers");
        Directory.CreateDirectory(driversDir);

        var driverFileName = $"{Guid.NewGuid():N}.jpg";
        var driverPath = Path.Combine(driversDir, driverFileName);
        await File.WriteAllBytesAsync(driverPath, "legacy-driver-photo"u8.ToArray());

        var driver = await db.Drivers.FindAsync(DbSeeder.TestDriverId);
        driver.Should().NotBeNull();
        driver!.PhotoUrl = $"/uploads/drivers/{driverFileName}";

        var parcelImport = new ParcelImport
        {
            Id = Guid.NewGuid(),
            FileName = "legacy.csv",
            FileFormat = ParcelImportFileFormat.Csv,
            ShipperAddressId = DbSeeder.TestDepotAddressId,
            Status = ParcelImportStatus.Queued,
            SourceFile = "legacy-parcel-import"u8.ToArray(),
            CreatedBy = "test",
        };

        var deliveryConfirmation = new DeliveryConfirmation
        {
            Id = Guid.NewGuid(),
            ParcelId = DbSeeder.TestParcelId,
            DeliveredAt = DateTimeOffset.UtcNow,
            Photo = "delivery-photo"u8.ToArray(),
            SignatureImage = "delivery-signature"u8.ToArray(),
            CreatedBy = "test",
        };

        db.ParcelImports.Add(parcelImport);
        db.DeliveryConfirmations.Add(deliveryConfirmation);
        await db.SaveChangesAsync();

        var runner = setupScope.ServiceProvider.GetRequiredService<StorageBackfillRunner>();
        var firstRun = await runner.RunAsync();
        var secondRun = await runner.RunAsync();

        firstRun.DriverPhotosMigrated.Should().Be(1);
        firstRun.ParcelImportFilesMigrated.Should().Be(1);
        firstRun.DeliveryConfirmationPhotosMigrated.Should().Be(1);
        firstRun.DeliveryConfirmationSignaturesMigrated.Should().Be(1);

        secondRun.DriverPhotosMigrated.Should().Be(0);
        secondRun.ParcelImportFilesMigrated.Should().Be(0);
        secondRun.DeliveryConfirmationPhotosMigrated.Should().Be(0);
        secondRun.DeliveryConfirmationSignaturesMigrated.Should().Be(0);

        await db.Entry(driver).ReloadAsync();
        await db.Entry(parcelImport).ReloadAsync();
        await db.Entry(deliveryConfirmation).ReloadAsync();

        driver.PhotoUrl.Should().Be($"/api/drivers/photo/{driverFileName}");
        File.Exists(driverPath).Should().BeFalse();

        parcelImport.SourceFileKey.Should().NotBeNullOrWhiteSpace();
        parcelImport.SourceFile.Should().BeNull();

        deliveryConfirmation.PhotoKey.Should().NotBeNullOrWhiteSpace();
        deliveryConfirmation.SignatureImageKey.Should().NotBeNullOrWhiteSpace();
        deliveryConfirmation.Photo.Should().BeNull();
        deliveryConfirmation.SignatureImage.Should().BeNull();

        var fileStorage = setupScope.ServiceProvider.GetRequiredService<InMemoryFileStorageService>();
        var keys = await fileStorage.ListKeysAsync(string.Empty);
        keys.Should().Contain($"drivers/{driverFileName}");
        keys.Should().Contain(parcelImport.SourceFileKey!);
        keys.Should().Contain(deliveryConfirmation.PhotoKey!);
        keys.Should().Contain(deliveryConfirmation.SignatureImageKey!);
    }
}
