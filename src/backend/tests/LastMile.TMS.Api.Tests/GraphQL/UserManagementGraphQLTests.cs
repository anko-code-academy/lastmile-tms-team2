using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Api.Tests.GraphQL;

public class UserManagementGraphQLTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    public Task InitializeAsync()
    {
        factory.EmailService.Clear();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Users_WithoutToken_ReturnsAuthorizationError()
    {
        var document = await PostGraphQLAsync(
            """
            query {
              users(skip: 0, take: 10) {
                totalCount
              }
            }
            """);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString()
            .Should().Contain("authorized");
    }

    [Fact]
    public async Task Users_WithNonAdminToken_ReturnsAuthorizationError()
    {
        var nonAdminUser = await SeedUserAsync(
            $"dispatcher-{Guid.NewGuid():N}@lastmile.test",
            "Dispatch123",
            PredefinedRole.Dispatcher);
        var token = await GetAccessTokenAsync(nonAdminUser.Email!, "Dispatch123");

        var document = await PostGraphQLAsync(
            """
            query {
              users(skip: 0, take: 10) {
                totalCount
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString()
            .Should().Contain("authorized");
    }

    [Fact]
    public async Task UserManagementLookups_WithAdminToken_ReturnsRolesAndAssignments()
    {
        var (depotId, zoneId) = await SeedDepotAndZoneAsync("lookups");
        var token = await GetAdminAccessTokenAsync();

        var document = await PostGraphQLAsync(
            """
            query {
              userManagementLookups {
                roles {
                  value
                  label
                }
                depots {
                  id
                  name
                }
                zones {
                  id
                  depotId
                  name
                }
              }
            }
            """,
            accessToken: token);

        var lookups = document.RootElement
            .GetProperty("data")
            .GetProperty("userManagementLookups");

        lookups.GetProperty("roles").EnumerateArray()
            .Select(x => x.GetProperty("value").GetString())
            .Should().Contain([
                nameof(PredefinedRole.Admin),
                nameof(PredefinedRole.OperationsManager),
                nameof(PredefinedRole.Dispatcher),
                nameof(PredefinedRole.WarehouseOperator),
                nameof(PredefinedRole.Driver)
            ]);

        lookups.GetProperty("depots").EnumerateArray()
            .Select(x => x.GetProperty("id").GetString())
            .Should().Contain(depotId.ToString());

        lookups.GetProperty("zones").EnumerateArray()
            .Should()
            .Contain(x =>
                x.GetProperty("id").GetString() == zoneId.ToString() &&
                x.GetProperty("depotId").GetString() == depotId.ToString());
    }

    [Fact]
    public async Task Users_WithAdminToken_ReturnsProtectedSeededAdmin()
    {
        var token = await GetAdminAccessTokenAsync();

        var document = await PostGraphQLAsync(
            """
            query {
              users(skip: 0, take: 20) {
                items {
                  email
                  isProtected
                }
              }
            }
            """,
            accessToken: token);

        document.RootElement
            .GetProperty("data")
            .GetProperty("users")
            .GetProperty("items")
            .EnumerateArray()
            .Should()
            .Contain(x =>
                x.GetProperty("email").GetString() == "admin@lastmile.com" &&
                x.GetProperty("isProtected").GetBoolean());
    }

    [Fact]
    public async Task CreateUser_WithValidInput_CreatesUserAndSendsSetupEmail()
    {
        var (depotId, zoneId) = await SeedDepotAndZoneAsync("create");
        var token = await GetAdminAccessTokenAsync();
        var email = $"create-{Guid.NewGuid():N}@lastmile.test";

        var document = await PostGraphQLAsync(
            """
            mutation CreateUser($input: CreateUserInput!) {
              createUser(input: $input) {
                id
                email
                role
                depotId
                zoneId
                isActive
              }
            }
            """,
            new
            {
                input = new
                {
                    firstName = "Casey",
                    lastName = "Create",
                    email,
                    phone = "+10000000001",
                    role = nameof(PredefinedRole.Dispatcher),
                    depotId,
                    zoneId
                }
            },
            token);

        var createdUser = document.RootElement
            .GetProperty("data")
            .GetProperty("createUser");

        createdUser.GetProperty("email").GetString().Should().Be(email);
        createdUser.GetProperty("role").GetString().Should().Be(nameof(PredefinedRole.Dispatcher));
        createdUser.GetProperty("depotId").GetString().Should().Be(depotId.ToString());
        createdUser.GetProperty("zoneId").GetString().Should().Be(zoneId.ToString());
        createdUser.GetProperty("isActive").GetBoolean().Should().BeTrue();

        factory.EmailService.Emails.Should()
            .Contain(x => x.Email == email && x.Kind == "setup");
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsValidationError()
    {
        var existingEmail = $"duplicate-{Guid.NewGuid():N}@lastmile.test";
        await SeedUserAsync(existingEmail, "Duplicate1", PredefinedRole.Dispatcher);
        var token = await GetAdminAccessTokenAsync();

        var document = await PostGraphQLAsync(
            """
            mutation CreateUser($input: CreateUserInput!) {
              createUser(input: $input) {
                id
              }
            }
            """,
            new
            {
                input = new
                {
                    firstName = "Duplicate",
                    lastName = "User",
                    email = existingEmail,
                    phone = "+10000000009",
                    role = nameof(PredefinedRole.Driver)
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString()
            .Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateUser_WithZoneFromAnotherDepot_ReturnsValidationError()
    {
        var (depotId, _) = await SeedDepotAndZoneAsync("create-valid");
        var (_, otherZoneId) = await SeedDepotAndZoneAsync("create-invalid");
        var token = await GetAdminAccessTokenAsync();

        var document = await PostGraphQLAsync(
            """
            mutation CreateUser($input: CreateUserInput!) {
              createUser(input: $input) {
                id
              }
            }
            """,
            new
            {
                input = new
                {
                    firstName = "Invalid",
                    lastName = "Assignment",
                    email = $"assignment-{Guid.NewGuid():N}@lastmile.test",
                    phone = "+10000000010",
                    role = nameof(PredefinedRole.Dispatcher),
                    depotId,
                    zoneId = otherZoneId
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString()
            .Should().Contain("does not belong");
    }

    [Fact]
    public async Task CreateUser_WithInactiveDepot_ReturnsValidationError()
    {
        var (depotId, _) = await SeedDepotAndZoneAsync("inactive");
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var depot = dbContext.Depots.Single(x => x.Id == depotId);
        depot.IsActive = false;
        await dbContext.SaveChangesAsync();

        var token = await GetAdminAccessTokenAsync();
        var document = await PostGraphQLAsync(
            """
            mutation CreateUser($input: CreateUserInput!) {
              createUser(input: $input) {
                id
              }
            }
            """,
            new
            {
                input = new
                {
                    firstName = "Inactive",
                    lastName = "Depot",
                    email = $"inactive-{Guid.NewGuid():N}@lastmile.test",
                    phone = "+10000000011",
                    role = nameof(PredefinedRole.Dispatcher),
                    depotId
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString()
            .Should().Contain("does not exist or is inactive");
    }

    [Fact]
    public async Task Users_WithSearchAndRoleFilter_ReturnsExpectedUser()
    {
        var (depotId, zoneId) = await SeedDepotAndZoneAsync("query");
        var targetEmail = $"dispatch-{Guid.NewGuid():N}@lastmile.test";
        await SeedUserAsync(
            targetEmail,
            "Dispatch1",
            PredefinedRole.Dispatcher,
            depotId,
            zoneId);
        await SeedUserAsync(
            $"driver-{Guid.NewGuid():N}@lastmile.test",
            "Driver123",
            PredefinedRole.Driver);

        var token = await GetAdminAccessTokenAsync();
        var document = await PostGraphQLAsync(
            """
            query Users($search: String, $role: UserRole, $depotId: UUID, $zoneId: UUID) {
              users(search: $search, role: $role, depotId: $depotId, zoneId: $zoneId, skip: 0, take: 10) {
                totalCount
                items {
                  email
                  role
                  depotId
                  zoneId
                }
              }
            }
            """,
            new
            {
                search = "dispatch",
                role = nameof(PredefinedRole.Dispatcher),
                depotId,
                zoneId
            },
            token);

        var users = document.RootElement
            .GetProperty("data")
            .GetProperty("users");

        users.GetProperty("totalCount").GetInt32().Should().Be(1);
        var item = users.GetProperty("items")[0];
        item.GetProperty("email").GetString().Should().Be(targetEmail);
        item.GetProperty("role").GetString().Should().Be(nameof(PredefinedRole.Dispatcher));
    }

    [Fact]
    public async Task Users_WithStatusFilterAndPaging_ReturnsExpectedSlice()
    {
        var activeA = await SeedUserAsync(
            $"active-a-{Guid.NewGuid():N}@lastmile.test",
            "ActiveA12",
            PredefinedRole.Dispatcher);
        var activeB = await SeedUserAsync(
            $"active-b-{Guid.NewGuid():N}@lastmile.test",
            "ActiveB12",
            PredefinedRole.Dispatcher);
        var inactive = await SeedUserAsync(
            $"inactive-{Guid.NewGuid():N}@lastmile.test",
            "Inactive1",
            PredefinedRole.Dispatcher);

        inactive.IsActive = false;
        await UpdateUserAsync(inactive);

        var token = await GetAdminAccessTokenAsync();
        var document = await PostGraphQLAsync(
            """
            query Users($isActive: Boolean!, $skip: Int!, $take: Int!) {
              users(isActive: $isActive, skip: $skip, take: $take) {
                totalCount
                items {
                  email
                  isActive
                }
              }
            }
            """,
            new
            {
                isActive = true,
                skip = 0,
                take = 1
            },
            token);

        var users = document.RootElement
            .GetProperty("data")
            .GetProperty("users");

        users.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(2);
        users.GetProperty("items").GetArrayLength().Should().Be(1);
        users.GetProperty("items")[0].GetProperty("isActive").GetBoolean().Should().BeTrue();
        users.GetProperty("items")[0].GetProperty("email").GetString()
            .Should().NotBe(inactive.Email);
    }

    [Fact]
    public async Task UpdateUser_WithValidInput_UpdatesUserAndRole()
    {
        var (depotId, zoneId) = await SeedDepotAndZoneAsync("update");
        var user = await SeedUserAsync(
            $"update-{Guid.NewGuid():N}@lastmile.test",
            "Update123",
            PredefinedRole.Dispatcher);
        var token = await GetAdminAccessTokenAsync();

        var document = await PostGraphQLAsync(
            """
            mutation UpdateUser($input: UpdateUserInput!) {
              updateUser(input: $input) {
                id
                email
                role
                depotId
                zoneId
                isActive
              }
            }
            """,
            new
            {
                input = new
                {
                    id = user.Id,
                    firstName = "Updated",
                    lastName = "Manager",
                    email = $"manager-{Guid.NewGuid():N}@lastmile.test",
                    phone = "+10000000002",
                    role = nameof(PredefinedRole.OperationsManager),
                    depotId,
                    zoneId,
                    isActive = true
                }
            },
            token);

        var updatedUser = document.RootElement
            .GetProperty("data")
            .GetProperty("updateUser");

        updatedUser.GetProperty("role").GetString().Should().Be(nameof(PredefinedRole.OperationsManager));
        updatedUser.GetProperty("depotId").GetString().Should().Be(depotId.ToString());
        updatedUser.GetProperty("zoneId").GetString().Should().Be(zoneId.ToString());

        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var reloadedUser = await userManager.FindByIdAsync(user.Id.ToString());
        var roles = await userManager.GetRolesAsync(reloadedUser!);
        roles.Should().ContainSingle().Which.Should().Be(nameof(PredefinedRole.OperationsManager));
    }

    [Fact]
    public async Task UpdateUser_WithProtectedAdminToken_ReturnsValidationError()
    {
        var adminToken = await GetAdminAccessTokenAsync();
        var protectedAdminId = await FindUserIdByEmailAsync("admin@lastmile.com");

        var document = await PostGraphQLAsync(
            """
            mutation UpdateUser($input: UpdateUserInput!) {
              updateUser(input: $input) {
                id
              }
            }
            """,
            new
            {
                input = new
                {
                    id = protectedAdminId,
                    firstName = "System",
                    lastName = "Admin",
                    email = "admin@lastmile.com",
                    phone = "+10000000042",
                    role = nameof(PredefinedRole.Admin),
                    depotId = (Guid?)null,
                    zoneId = (Guid?)null,
                    isActive = true
                }
            },
            adminToken);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString()
            .Should().Contain("protected");
    }

    [Fact]
    public async Task DeactivateUser_WithAnotherAdminToken_RejectsProtectedAdmin()
    {
        var secondAdminEmail = $"admin-{Guid.NewGuid():N}@lastmile.test";
        await SeedUserAsync(
            secondAdminEmail,
            "OtherAdmin1",
            PredefinedRole.Admin);
        var otherAdminToken = await GetAccessTokenAsync(secondAdminEmail, "OtherAdmin1");
        var protectedAdminId = await FindUserIdByEmailAsync("admin@lastmile.com");

        var document = await PostGraphQLAsync(
            """
            mutation DeactivateUser($userId: UUID!) {
              deactivateUser(userId: $userId) {
                id
              }
            }
            """,
            new { userId = protectedAdminId },
            otherAdminToken);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString()
            .Should().Contain("protected");
    }

    [Fact]
    public async Task SendPasswordResetEmail_WithProtectedAdmin_RejectsMutation()
    {
        var secondAdminEmail = $"admin-{Guid.NewGuid():N}@lastmile.test";
        await SeedUserAsync(secondAdminEmail, "OtherAdmin1", PredefinedRole.Admin);
        var otherAdminToken = await GetAccessTokenAsync(secondAdminEmail, "OtherAdmin1");
        var protectedAdminId = await FindUserIdByEmailAsync("admin@lastmile.com");

        var document = await PostGraphQLAsync(
            """
            mutation SendPasswordResetEmail($userId: UUID!) {
              sendPasswordResetEmail(userId: $userId) {
                success
              }
            }
            """,
            new { userId = protectedAdminId },
            otherAdminToken);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString()
            .Should().Contain("protected");
    }

    [Fact]
    public async Task DeactivateUser_WithExistingAccessToken_RevokesAccessImmediately()
    {
        var user = await SeedUserAsync(
            $"deactivate-{Guid.NewGuid():N}@lastmile.test",
            "Deactivate1",
            PredefinedRole.Dispatcher);

        var userToken = await GetAccessTokenAsync(user.Email!, "Deactivate1");
        var adminToken = await GetAdminAccessTokenAsync();

        var document = await PostGraphQLAsync(
            """
            mutation DeactivateUser($userId: UUID!) {
              deactivateUser(userId: $userId) {
                id
                isActive
              }
            }
            """,
            new { userId = user.Id },
            adminToken);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        document.RootElement
            .GetProperty("data")
            .GetProperty("deactivateUser")
            .GetProperty("isActive")
            .GetBoolean()
            .Should()
            .BeFalse();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PasswordResetFlow_SendsEmail_AndAcceptsNewPassword()
    {
        var email = $"reset-{Guid.NewGuid():N}@lastmile.test";
        await SeedUserAsync(email, "Reset1234", PredefinedRole.Dispatcher);
        var adminToken = await GetAdminAccessTokenAsync();

        var sendDocument = await PostGraphQLAsync(
            """
            mutation SendPasswordResetEmail($userId: UUID!) {
              sendPasswordResetEmail(userId: $userId) {
                success
                message
              }
            }
            """,
            new
            {
                userId = (await FindUserIdByEmailAsync(email)).ToString()
            },
            adminToken);

        sendDocument.RootElement
            .GetProperty("data")
            .GetProperty("sendPasswordResetEmail")
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeTrue();

        var resetEmail = factory.EmailService.Emails
            .Last(x => x.Email == email && x.Kind == "reset");
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetEmail.Token));

        var resetDocument = await PostGraphQLAsync(
            """
            mutation CompletePasswordReset($input: CompletePasswordResetInput!) {
              completePasswordReset(input: $input) {
                success
                message
              }
            }
            """,
            new
            {
                input = new
                {
                    email,
                    token = encodedToken,
                    newPassword = "BrandNew1"
                }
            });

        resetDocument.RootElement
            .GetProperty("data")
            .GetProperty("completePasswordReset")
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeTrue();

        var newAccessToken = await GetAccessTokenAsync(email, "BrandNew1");
        newAccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RequestPasswordReset_ForActiveUser_QueuesResetEmail()
    {
        var email = $"public-reset-{Guid.NewGuid():N}@lastmile.test";
        await SeedUserAsync(email, "Reset1234", PredefinedRole.Dispatcher);

        var document = await PostGraphQLAsync(
            """
            mutation RequestPasswordReset($email: String!) {
              requestPasswordReset(email: $email) {
                success
                message
              }
            }
            """,
            new { email });

        var result = document.RootElement
            .GetProperty("data")
            .GetProperty("requestPasswordReset");

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("message").GetString().Should().Contain("If the email exists");
        factory.EmailService.Emails.Should().Contain(x => x.Email == email && x.Kind == "reset");
    }

    [Fact]
    public async Task RequestPasswordReset_WithAllowedOrigin_UsesOriginForResetLink()
    {
        var email = $"origin-reset-{Guid.NewGuid():N}@lastmile.test";
        await SeedUserAsync(email, "Reset1234", PredefinedRole.Dispatcher);

        var document = await PostGraphQLAsync(
            """
            mutation RequestPasswordReset($email: String!) {
              requestPasswordReset(email: $email) {
                success
              }
            }
            """,
            new { email },
            headers: new Dictionary<string, string>
            {
                ["Origin"] = "http://localhost:3000"
            });

        document.RootElement
            .GetProperty("data")
            .GetProperty("requestPasswordReset")
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeTrue();

        factory.EmailService.Emails
            .Last(x => x.Email == email && x.Kind == "reset")
            .FrontendBaseUrl
            .Should()
            .Be("http://localhost:3000");
    }

    [Fact]
    public async Task RequestPasswordReset_WithUntrustedOrigin_FallsBackToConfiguredBaseUrl()
    {
        var email = $"fallback-reset-{Guid.NewGuid():N}@lastmile.test";
        await SeedUserAsync(email, "Reset1234", PredefinedRole.Dispatcher);

        var document = await PostGraphQLAsync(
            """
            mutation RequestPasswordReset($email: String!) {
              requestPasswordReset(email: $email) {
                success
              }
            }
            """,
            new { email },
            headers: new Dictionary<string, string>
            {
                ["Origin"] = "https://evil.example"
            });

        document.RootElement
            .GetProperty("data")
            .GetProperty("requestPasswordReset")
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeTrue();

        factory.EmailService.Emails
            .Last(x => x.Email == email && x.Kind == "reset")
            .FrontendBaseUrl
            .Should()
            .Be("http://localhost");
    }

    [Fact]
    public async Task RequestPasswordReset_ForMissingOrInactiveUsers_ReturnsGenericSuccessWithoutEmail()
    {
        var inactiveEmail = $"inactive-reset-{Guid.NewGuid():N}@lastmile.test";
        var inactiveUser = await SeedUserAsync(inactiveEmail, "Reset1234", PredefinedRole.Dispatcher);
        inactiveUser.IsActive = false;
        await UpdateUserAsync(inactiveUser);

        foreach (var email in new[] { $"missing-{Guid.NewGuid():N}@lastmile.test", inactiveEmail })
        {
            factory.EmailService.Clear();

            var document = await PostGraphQLAsync(
                """
                mutation RequestPasswordReset($email: String!) {
                  requestPasswordReset(email: $email) {
                    success
                    message
                  }
                }
                """,
                new { email });

            var result = document.RootElement
                .GetProperty("data")
                .GetProperty("requestPasswordReset");

            result.GetProperty("success").GetBoolean().Should().BeTrue();
            result.GetProperty("message").GetString().Should().Contain("If the email exists");
            factory.EmailService.Emails.Should().BeEmpty();
        }
    }

    private async Task<Guid> FindUserIdByEmailAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user!.Id;
    }

    private async Task<(Guid DepotId, Guid ZoneId)> SeedDepotAndZoneAsync(string suffix)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var address = new Address
        {
            Street1 = $"1 {suffix} Street",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        var depot = new Depot
        {
            Name = $"Depot {suffix} {Guid.NewGuid():N}",
            Address = address,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var zone = new Zone
        {
            Name = $"Zone {suffix} {Guid.NewGuid():N}",
            Depot = depot,
            Boundary = geometryFactory.CreatePolygon([
                new Coordinate(0, 0),
                new Coordinate(0, 1),
                new Coordinate(1, 1),
                new Coordinate(1, 0),
                new Coordinate(0, 0)
            ]),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Addresses.Add(address);
        dbContext.Depots.Add(depot);
        dbContext.Zones.Add(zone);
        await dbContext.SaveChangesAsync();

        return (depot.Id, zone.Id);
    }

    private async Task<ApplicationUser> SeedUserAsync(
        string email,
        string password,
        PredefinedRole role,
        Guid? depotId = null,
        Guid? zoneId = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = role.ToString(),
            LastName = "Tester",
            PhoneNumber = "+10000000000",
            IsActive = true,
            DepotId = depotId,
            ZoneId = zoneId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        var createResult = await userManager.CreateAsync(user, password);
        createResult.Succeeded.Should().BeTrue(string.Join(", ", createResult.Errors.Select(x => x.Description)));

        var roleResult = await userManager.AddToRoleAsync(user, role.ToString());
        roleResult.Succeeded.Should().BeTrue(string.Join(", ", roleResult.Errors.Select(x => x.Description)));

        return user;
    }

    private async Task UpdateUserAsync(ApplicationUser user)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existingUser = await userManager.FindByIdAsync(user.Id.ToString());
        existingUser.Should().NotBeNull();

        existingUser!.FirstName = user.FirstName;
        existingUser.LastName = user.LastName;
        existingUser.Email = user.Email;
        existingUser.UserName = user.UserName;
        existingUser.PhoneNumber = user.PhoneNumber;
        existingUser.IsActive = user.IsActive;
        existingUser.IsSystemAdmin = user.IsSystemAdmin;
        existingUser.DepotId = user.DepotId;
        existingUser.ZoneId = user.ZoneId;
        existingUser.CreatedAt = user.CreatedAt;
        existingUser.CreatedBy = user.CreatedBy;
        existingUser.LastModifiedAt = user.LastModifiedAt;
        existingUser.LastModifiedBy = user.LastModifiedBy;

        var result = await userManager.UpdateAsync(existingUser);
        result.Succeeded.Should().BeTrue(string.Join(", ", result.Errors.Select(x => x.Description)));
    }

    private async Task<string> GetAdminAccessTokenAsync() =>
        await GetAccessTokenAsync("admin@lastmile.com", "Admin@12345");

    private async Task<string> GetAccessTokenAsync(string username, string password)
    {
        var response = await _client.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = username,
                ["password"] = password
            }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<JsonDocument> PostGraphQLAsync(
        string query,
        object? variables = null,
        string? accessToken = null,
        IDictionary<string, string>? headers = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = JsonContent.Create(new
            {
                query,
                variables
            })
        };

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);
        return JsonDocument.Parse(content);
    }
}
