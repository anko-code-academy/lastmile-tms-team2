using FluentAssertions;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Domain.Tests.Entities;

public class ApplicationUserTests
{
    [Fact]
    public void ApplicationUser_ShouldHaveAuditProperties()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.CreatedAt.Should().Be(default);
        user.CreatedBy.Should().BeNull();
        user.LastModifiedAt.Should().BeNull();
        user.LastModifiedBy.Should().BeNull();
    }

    [Fact]
    public void ApplicationUser_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "+1234567890"
        };

        // Assert
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Email.Should().Be("john.doe@example.com");
        user.PhoneNumber.Should().Be("+1234567890");
    }

    [Fact]
    public void ApplicationUser_ShouldHaveIsActiveProperty()
    {
        // Arrange & Act
        var user = new ApplicationUser { IsActive = true };

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ApplicationUser_ShouldHaveIsActiveTrueByDefault()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ApplicationUser_ShouldHaveGuidId()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert — IdentityUser<Guid> uses Guid Id, defaults to Guid.Empty
        user.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ApplicationUser_ShouldBeSoftDeletable()
    {
        // Arrange
        var user = new ApplicationUser { IsActive = true };

        // Act - soft delete
        user.IsActive = false;

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ApplicationUser_ShouldInheritPasswordHashFromIdentity()
    {
        // Arrange & Act
        var user = new ApplicationUser();
        user.PasswordHash = "hashed_password";

        // Assert — PasswordHash is inherited from IdentityUser
        user.PasswordHash.Should().Be("hashed_password");
    }
}
