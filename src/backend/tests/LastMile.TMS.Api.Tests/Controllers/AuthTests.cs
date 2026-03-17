using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LastMile.TMS.Api.Tests.Controllers;

/// <summary>
/// Integration tests for the OpenIddict /connect/token endpoint and protected endpoints.
/// </summary>
public class AuthTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static FormUrlEncodedContent PasswordGrantBody(
        string username, string password, bool requestRefreshToken = false)
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "password"),
            new("username", username),
            new("password", password)
        };

        if (requestRefreshToken)
            pairs.Add(new("scope", "offline_access"));

        return new FormUrlEncodedContent(pairs);
    }

    private static FormUrlEncodedContent RefreshTokenGrantBody(string refreshToken) =>
        new([
            new("grant_type", "refresh_token"),
            new("refresh_token", refreshToken)
        ]);

    private async Task<string> GetAccessTokenAsync()
    {
        var body = PasswordGrantBody("admin@lastmile.com", "Admin@12345");
        var response = await _client.PostAsync("/connect/token", body);
        var json = await ParseJsonAsync(response);
        return json["access_token"].GetString()!;
    }

    // ── Password Grant Tests ────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidAdminCredentials_Returns200AndTokens()
    {
        // Arrange
        var body = PasswordGrantBody("admin@lastmile.com", "Admin@12345");

        // Act
        var response = await _client.PostAsync("/connect/token", body);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);

        var json = await ParseJsonAsync(response);
        json.Should().ContainKey("access_token");
        json.Should().ContainKey("token_type");
        json["token_type"]!.GetString().Should().BeEquivalentTo("Bearer");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsBadRequest()
    {
        // Arrange
        var body = PasswordGrantBody("admin@lastmile.com", "WrongPassword!");

        // Act
        var response = await _client.PostAsync("/connect/token", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsBadRequest()
    {
        // Arrange
        var body = PasswordGrantBody("nobody@example.com", "Admin@12345");

        // Act
        var response = await _client.PostAsync("/connect/token", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Refresh Token Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithOfflineAccessScope_ReturnsRefreshToken()
    {
        // Arrange
        var body = PasswordGrantBody("admin@lastmile.com", "Admin@12345", requestRefreshToken: true);

        // Act
        var response = await _client.PostAsync("/connect/token", body);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);

        var json = await ParseJsonAsync(response);
        json.Should().ContainKey("refresh_token");
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewAccessToken()
    {
        // Arrange — get refresh token via password grant
        var loginBody = PasswordGrantBody("admin@lastmile.com", "Admin@12345", requestRefreshToken: true);
        var loginResponse = await _client.PostAsync("/connect/token", loginBody);
        var loginJson = await ParseJsonAsync(loginResponse);
        var refreshToken = loginJson["refresh_token"].GetString()!;

        var refreshBody = RefreshTokenGrantBody(refreshToken);

        // Act
        var response = await _client.PostAsync("/connect/token", refreshBody);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);

        var json = await ParseJsonAsync(response);
        json.Should().ContainKey("access_token");
        json["token_type"]!.GetString().Should().BeEquivalentTo("Bearer");
    }

    // ── Protected Endpoint Tests ────────────────────────────────────────────

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Returns200()
    {
        // Arrange — get a valid access token
        var token = await GetAccessTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);

        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content)!;
        json.Should().ContainKey("userId");
        json.Should().ContainKey("userName");
        json.Should().ContainKey("roles");
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static async Task<Dictionary<string, JsonElement>> ParseJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content)!;
    }
}
