using FluentAssertions;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Domain.Tests;

public class DriverAvailabilityTests
{
    [Fact]
    public void DriverAvailability_ShouldInitializeWithDefaultValues()
    {
        var availability = new DriverAvailability();

        availability.ShiftStart.Should().BeNull();
        availability.ShiftEnd.Should().BeNull();
        availability.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void DriverAvailability_ShouldAllowSettingProperties()
    {
        var driver = new Driver { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };

        var availability = new DriverAvailability
        {
            DriverId = driver.Id,
            Driver = driver,
            DayOfWeek = DayOfWeek.Wednesday,
            ShiftStart = new TimeOnly(6, 0),
            ShiftEnd = new TimeOnly(14, 0),
            IsAvailable = true
        };

        availability.DriverId.Should().Be(driver.Id);
        availability.Driver.FirstName.Should().Be("John");
        availability.DayOfWeek.Should().Be(DayOfWeek.Wednesday);
        availability.ShiftStart.Should().Be(new TimeOnly(6, 0));
        availability.ShiftEnd.Should().Be(new TimeOnly(14, 0));
        availability.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void DriverAvailability_ShouldAllowDayOff()
    {
        var availability = new DriverAvailability
        {
            DayOfWeek = DayOfWeek.Sunday,
            IsAvailable = false,
            ShiftStart = null,
            ShiftEnd = null
        };

        availability.IsAvailable.Should().BeFalse();
        availability.ShiftStart.Should().BeNull();
        availability.ShiftEnd.Should().BeNull();
    }
}
