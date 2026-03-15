using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Tests.Entities;

public class PermissionTests
{

    [Fact]
    public void Permission_ShouldHaveNameProperty()
    {
        // Arrange & Act
        var permission = new Permission
        {
            Name = "view_deliveries",
            Module = "Deliveries",
            Scope = PermissionScope.Read
        };

        // Assert
        permission.Name.Should().Be("view_deliveries");
    }

    [Fact]
    public void Permission_ShouldHaveModuleProperty()
    {
        // Arrange & Act
        var permission = new Permission
        {
            Name = "manage_users",
            Module = "Users",
            Scope = PermissionScope.All
        };

        // Assert
        permission.Module.Should().Be("Users");
    }

    [Fact]
    public void Permission_ShouldHaveScopeEnum()
    {
        // Arrange & Act
        var permission = new Permission
        {
            Name = "test",
            Module = "Test",
            Scope = PermissionScope.Read
        };

        // Assert
        permission.Scope.Should().Be(PermissionScope.Read);
    }

    [Fact]
    public void PermissionScope_ShouldContainExpectedValues()
    {
        // Assert
        Enum.GetValues<PermissionScope>().Should().Contain(PermissionScope.Read);
        Enum.GetValues<PermissionScope>().Should().Contain(PermissionScope.Write);
        Enum.GetValues<PermissionScope>().Should().Contain(PermissionScope.Delete);
        Enum.GetValues<PermissionScope>().Should().Contain(PermissionScope.All);
    }
}