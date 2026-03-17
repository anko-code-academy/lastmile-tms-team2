using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Tests;

public class DriverTests
{
    [Fact]
    public void Driver_ShouldInitializeWithDefaultValues()
    {
        var driver = new Driver();

        driver.FirstName.Should().Be(string.Empty);
        driver.LastName.Should().Be(string.Empty);
        driver.LicenseNumber.Should().Be(string.Empty);
        driver.Phone.Should().BeNull();
        driver.Email.Should().BeNull();
        driver.PhotoUrl.Should().BeNull();
        driver.Status.Should().Be(DriverStatus.Active);
        driver.AvailabilitySchedule.Should().BeEmpty();
    }

    [Fact]
    public void Driver_ShouldHaveCorrectBaseType()
    {
        var driver = new Driver { Id = Guid.NewGuid() };

        driver.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Driver_ShouldAllowSettingProperties()
    {
        var zone = new Zone { Id = Guid.NewGuid(), Name = "Sydney Central" };
        var depot = new Depot { Id = Guid.NewGuid(), Name = "Sydney Depot" };
        var user = new ApplicationUser { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
        var expiryDate = DateTimeOffset.UtcNow.AddYears(1);

        var driver = new Driver
        {
            FirstName = "John",
            LastName = "Doe",
            Phone = "+61412345678",
            Email = "john.doe@example.com",
            LicenseNumber = "DL123456",
            LicenseExpiryDate = expiryDate,
            PhotoUrl = "https://example.com/photos/john.jpg",
            ZoneId = zone.Id,
            Zone = zone,
            DepotId = depot.Id,
            Depot = depot,
            UserId = user.Id,
            User = user,
            Status = DriverStatus.Active
        };

        driver.FirstName.Should().Be("John");
        driver.LastName.Should().Be("Doe");
        driver.Phone.Should().Be("+61412345678");
        driver.Email.Should().Be("john.doe@example.com");
        driver.LicenseNumber.Should().Be("DL123456");
        driver.LicenseExpiryDate.Should().Be(expiryDate);
        driver.PhotoUrl.Should().Be("https://example.com/photos/john.jpg");
        driver.ZoneId.Should().Be(zone.Id);
        driver.Zone.Name.Should().Be("Sydney Central");
        driver.DepotId.Should().Be(depot.Id);
        driver.Depot.Name.Should().Be("Sydney Depot");
        driver.UserId.Should().Be(user.Id);
        driver.User.FirstName.Should().Be("John");
        driver.Status.Should().Be(DriverStatus.Active);
    }

    [Fact]
    public void Driver_ShouldAllowAvailabilityScheduleCollection()
    {
        var driver = new Driver { FirstName = "Jane", LastName = "Smith" };
        var availability = new DriverAvailability
        {
            DayOfWeek = DayOfWeek.Monday,
            ShiftStart = new TimeOnly(8, 0),
            ShiftEnd = new TimeOnly(17, 0),
            IsAvailable = true
        };

        driver.AvailabilitySchedule.Add(availability);

        driver.AvailabilitySchedule.Should().HaveCount(1);
        driver.AvailabilitySchedule.First().DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public void IsLicenseExpired_ShouldReturnTrue_WhenLicenseIsExpired()
    {
        var driver = new Driver
        {
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddDays(-30)
        };

        driver.IsLicenseExpired(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsLicenseExpired_ShouldReturnFalse_WhenLicenseIsValid()
    {
        var driver = new Driver
        {
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(1)
        };

        driver.IsLicenseExpired(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void Driver_ShouldAllowStatusChange()
    {
        var driver = new Driver { Status = DriverStatus.Active };

        driver.Status.Should().Be(DriverStatus.Active);

        driver.Status = DriverStatus.Inactive;

        driver.Status.Should().Be(DriverStatus.Inactive);
    }

    [Fact]
    public void Driver_ShouldAllowOnLeaveStatus()
    {
        var driver = new Driver { Status = DriverStatus.Active };

        driver.Status = DriverStatus.OnLeave;

        driver.Status.Should().Be(DriverStatus.OnLeave);
    }

    [Fact]
    public void Driver_ShouldAllowSuspendedStatus()
    {
        var driver = new Driver { Status = DriverStatus.Active };

        driver.Status = DriverStatus.Suspended;

        driver.Status.Should().Be(DriverStatus.Suspended);
    }
}
