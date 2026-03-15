using FluentAssertions;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Domain.Tests;

public class DepotTests
{
    [Fact]
    public void Depot_ShouldInitializeWithDefaultValues()
    {
        var depot = new Depot();

        depot.Name.Should().Be(string.Empty);
        depot.IsActive.Should().BeTrue();
        depot.Zones.Should().BeEmpty();
    }

    [Fact]
    public void Depot_ShouldHaveCorrectBaseType()
    {
        var depot = new Depot { Id = Guid.NewGuid() };

        depot.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Depot_ShouldAllowSettingProperties()
    {
        var depot = new Depot
        {
            Name = "Sydney Depot",
            AddressId = Guid.NewGuid(),
            Address = new Address
            {
                Street1 = "123 Main St",
                City = "Sydney",
                State = "NSW",
                PostalCode = "2000",
                CountryCode = "AU"
            },
            IsActive = true
        };

        depot.Name.Should().Be("Sydney Depot");
        depot.Address.Should().NotBeNull();
        depot.Address!.City.Should().Be("Sydney");
        depot.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Depot_ShouldAllowZonesCollection()
    {
        var depot = new Depot { Name = "Test Depot" };
        var zone = new Zone { Name = "Zone 1" };

        depot.Zones.Add(zone);

        depot.Zones.Should().HaveCount(1);
        depot.Zones.First().Name.Should().Be("Zone 1");
    }

    [Fact]
    public void Depot_ShouldAllowOperatingHoursCollection()
    {
        var depot = new Depot { Name = "Test Depot" };
        var operatingHour = new OperatingHours
        {
            DayOfWeek = DayOfWeek.Monday,
            OpenTime = new TimeOnly(8, 0),
            ClosedTime = new TimeOnly(18, 0),
            IsClosed = false
        };

        depot.OperatingHours.Add(operatingHour);

        depot.OperatingHours.Should().HaveCount(1);
        depot.OperatingHours.First().DayOfWeek.Should().Be(DayOfWeek.Monday);
    }
}