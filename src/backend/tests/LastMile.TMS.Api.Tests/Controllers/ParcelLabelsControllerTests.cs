using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LastMile.TMS.Api.Tests.Controllers;

[Collection(ApiTestCollection.Name)]
public class ParcelLabelsControllerTests(CustomWebApplicationFactory factory)
    : IAsyncLifetime
{
    private static readonly Guid TestParcelShipperAddressId =
        new("00000000-0000-0000-0000-000000000004");

    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetThermalLabel_WithAdminToken_ReturnsAttachment()
    {
        var token = await GetAccessTokenAsync("admin@lastmile.com", "Admin@12345");
        var parcel = await RegisterParcelAsync(token, "thermal");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/parcels/{parcel.Id}/labels/4x6.zpl");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Content.Headers.ContentDisposition!.FileNameStar.Should().EndWith(".zpl");
        content.Should().Contain(parcel.TrackingNumber);
        content.Should().Contain("^BCN");
        content.Should().Contain("^BQN");
    }

    [Fact]
    public async Task GetA4Label_WithAdminToken_ReturnsPdfAttachment()
    {
        var token = await GetAccessTokenAsync("admin@lastmile.com", "Admin@12345");
        var parcel = await RegisterParcelAsync(token, "pdf");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/parcels/{parcel.Id}/labels/a4.pdf");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Content.Headers.ContentDisposition!.FileNameStar.Should().EndWith(".pdf");
        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task BulkThermalLabels_WithAdminToken_ReturnsCombinedAttachment()
    {
        var token = await GetAccessTokenAsync("admin@lastmile.com", "Admin@12345");
        var firstParcel = await RegisterParcelAsync(token, "bulk-1");
        var secondParcel = await RegisterParcelAsync(token, "bulk-2");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/parcels/labels/4x6.zpl")
        {
            Content = JsonContent.Create(new
            {
                parcelIds = new[] { firstParcel.Id, secondParcel.Id }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition!.FileNameStar.Should().Be("parcel-labels-4x6.zpl");
        content.Should().Contain(firstParcel.TrackingNumber);
        content.Should().Contain(secondParcel.TrackingNumber);
    }

    [Fact]
    public async Task GetThermalLabel_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync($"/api/parcels/{Guid.NewGuid()}/labels/4x6.zpl");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetThermalLabel_WithDriverRole_ReturnsForbidden()
    {
        var adminToken = await GetAccessTokenAsync("admin@lastmile.com", "Admin@12345");
        var parcel = await RegisterParcelAsync(adminToken, "driver");
        var driverEmail = $"driver-{Guid.NewGuid():N}@lastmile.test";
        await SeedUserAsync(driverEmail, "Driver123!", PredefinedRole.Driver);
        var driverToken = await GetAccessTokenAsync(driverEmail, "Driver123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/parcels/{parcel.Id}/labels/4x6.zpl");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<(Guid Id, string TrackingNumber)> RegisterParcelAsync(string accessToken, string suffix)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = JsonContent.Create(new
            {
                query =
                    """
                    mutation RegisterParcel($input: RegisterParcelInput!) {
                      registerParcel(input: $input) {
                        id
                        trackingNumber
                      }
                    }
                    """,
                variables = new
                {
                    input = new
                    {
                        shipperAddressId = TestParcelShipperAddressId,
                        recipientAddress = new
                        {
                            street1 = $"15 Labelary {suffix}",
                            city = "Cairo",
                            state = "Cairo",
                            postalCode = "11511",
                            countryCode = "EG",
                            isResidential = true,
                            contactName = "Omar Farouk",
                            phone = "+201234567890",
                            email = $"labels-{suffix}@example.com"
                        },
                        description = $"Parcel {suffix}",
                        parcelType = "Box",
                        serviceType = "STANDARD",
                        weight = 1.5,
                        weightUnit = "KG",
                        length = 20.0,
                        width = 15.0,
                        height = 10.0,
                        dimensionUnit = "CM",
                        declaredValue = 200.0,
                        currency = "USD",
                        estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(3).ToString("o")
                    }
                }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var payload = document.RootElement.GetProperty("data").GetProperty("registerParcel");
        return (
            Guid.Parse(payload.GetProperty("id").GetString()!),
            payload.GetProperty("trackingNumber").GetString()!);
    }

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

    private async Task<ApplicationUser> SeedUserAsync(
        string email,
        string password,
        PredefinedRole role)
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
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        var createResult = await userManager.CreateAsync(user, password);
        createResult.Succeeded.Should().BeTrue(string.Join(", ", createResult.Errors.Select(x => x.Description)));

        var roleResult = await userManager.AddToRoleAsync(user, role.ToString());
        roleResult.Succeeded.Should().BeTrue(string.Join(", ", roleResult.Errors.Select(x => x.Description)));

        return user;
    }
}
