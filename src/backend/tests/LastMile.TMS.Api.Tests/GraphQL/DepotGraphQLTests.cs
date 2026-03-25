using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LastMile.TMS.Api.Tests.GraphQL;

public class DepotGraphQLTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    [Fact]
    public async Task Depots_WithAdminToken_ReturnsFullDepotFields()
    {
        var depotId = await SeedDepotAsync();
        var token = await GetAdminAccessTokenAsync();

        var document = await PostGraphQLAsync(
            """
            query {
              depots {
                id
                name
                isActive
                createdAt
                updatedAt
                address {
                  street1
                  city
                  countryCode
                }
                operatingHours {
                  openTime
                  closedTime
                  isClosed
                }
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse();

        var depots = document.RootElement
            .GetProperty("data")
            .GetProperty("depots")
            .EnumerateArray()
            .ToList();

        var depot = depots.Single(x => x.GetProperty("id").GetString() == depotId.ToString());

        depot.GetProperty("name").GetString().Should().StartWith("Depot GraphQL");
        depot.GetProperty("isActive").GetBoolean().Should().BeFalse();
        depot.GetProperty("createdAt").GetString().Should().NotBeNullOrWhiteSpace();
        depot.GetProperty("updatedAt").ValueKind.Should().Be(JsonValueKind.Null);
        depot.GetProperty("address").GetProperty("city").GetString().Should().Be("Melbourne");
        depot.GetProperty("address").GetProperty("countryCode").GetString().Should().Be("AU");
        depot.GetProperty("operatingHours").GetArrayLength().Should().BeGreaterThan(0);
    }

    private async Task<Guid> SeedDepotAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var depot = new Depot
        {
            Name = $"Depot GraphQL {Guid.NewGuid():N}",
            IsActive = false,
            Address = new Address
            {
                Street1 = "101 Market Street",
                City = "Melbourne",
                State = "VIC",
                PostalCode = "3000",
                CountryCode = "AU",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "tests"
            },
            OperatingHours =
            [
                new OperatingHours
                {
                    DayOfWeek = DayOfWeek.Monday,
                    OpenTime = new TimeOnly(8, 0),
                    ClosedTime = new TimeOnly(17, 0),
                    IsClosed = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = "tests"
                }
            ],
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Depots.Add(depot);
        await dbContext.SaveChangesAsync();

        return depot.Id;
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
        string? accessToken = null)
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

        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);
        return JsonDocument.Parse(content);
    }
}
