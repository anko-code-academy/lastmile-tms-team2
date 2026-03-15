using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Tests.Entities;

public class ApplicationRoleTests
{
    [Fact]
    public void PredefinedRole_ShouldHaveAllExpectedValues()
    {
        // Assert - All predefined roles should exist
        Enum.GetValues<PredefinedRole>().Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(PredefinedRole.Admin)]
    [InlineData(PredefinedRole.OperationsManager)]
    [InlineData(PredefinedRole.Dispatcher)]
    [InlineData(PredefinedRole.WarehouseOperator)]
    [InlineData(PredefinedRole.Driver)]
    public void PredefinedRole_ShouldContainExpectedRoles(PredefinedRole role)
    {
        // Assert
        role.Should().BeDefined();
    }

    [Fact]
    public void ApplicationRole_ShouldSetNameFromPredefinedRole()
    {
        // Arrange & Act
        var role = new ApplicationRole
        {
            Name = PredefinedRole.Admin.ToString(),
            Description = "System administrator with full access"
        };

        // Assert
        role.Name.Should().Be("Admin");
    }

    [Fact]
    public void ApplicationRole_ShouldAllowIsDefaultFlag()
    {
        // Arrange & Act
        var role = new ApplicationRole
        {
            Name = PredefinedRole.Driver.ToString(),
            IsDefault = true
        };

        // Assert
        role.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void ApplicationRole_ShouldHaveGuidId()
    {
        // Arrange & Act
        var role = new ApplicationRole();

        // Assert — IdentityRole<Guid> uses Guid Id, defaults to Guid.Empty
        role.Id.Should().Be(Guid.Empty);
    }
}
