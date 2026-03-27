using FluentAssertions;
using LastMile.TMS.Application.Users.Common;
using LastMile.TMS.Application.Users.Reads;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Tests.Users;

public class UserReadServiceTests
{
    private static AppDbContext MakeDbContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
    }

    private static async Task<ApplicationUser> SeedUser(
        AppDbContext db,
        string email,
        string firstName = "Test",
        string lastName = "User",
        bool isActive = true)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = "+10000000000",
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        var db = MakeDbContext();
        await SeedUser(db, "a@test.com");
        await SeedUser(db, "b@test.com");
        await SeedUser(db, "c@test.com");

        var service = new UserReadService(db);
        var result = await service.GetUsers().ToListAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetUsers_MapsAllScalarFieldsCorrectly()
    {
        var db = MakeDbContext();
        var user = await SeedUser(db, "mapper@test.com", "Jane", "Doe");

        var service = new UserReadService(db);
        var result = await service.GetUsers().ToListAsync();

        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Id.Should().Be(user.Id);
        dto.FirstName.Should().Be("Jane");
        dto.LastName.Should().Be("Doe");
        dto.FullName.Should().Be("Jane Doe");
        dto.Email.Should().Be("mapper@test.com");
        dto.Phone.Should().Be("+10000000000");
        dto.IsActive.Should().BeTrue();
        dto.IsProtected.Should().BeFalse();
        dto.CreatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetUsers_FiltersByIsActive()
    {
        var db = MakeDbContext();
        await SeedUser(db, "active@test.com", isActive: true);
        await SeedUser(db, "inactive@test.com", isActive: false);

        var service = new UserReadService(db);
        var result = await service.GetUsers()
            .Where(u => u.IsActive)
            .ToListAsync();

        result.Should().HaveCount(1);
        result[0].Email.Should().Be("active@test.com");
    }

    // Role resolution via correlated subquery is verified through
    // GraphQL integration tests (UserManagementGraphQLTests) which
    // exercise the full EF Core → PostgreSQL translation path.
}
