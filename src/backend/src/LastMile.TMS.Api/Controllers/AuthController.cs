using System.Security.Claims;
using LastMile.TMS.Domain.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace LastMile.TMS.Api.Controllers;

/// <summary>
/// Handles OAuth 2.0 token requests (password grant + refresh token grant).
/// Maps to the standard OpenIddict /connect/token endpoint.
/// </summary>
[ApiController]
[Route("connect")]
public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsPasswordGrantType())
            return await HandlePasswordGrantAsync(request);

        if (request.IsRefreshTokenGrantType())
            return await HandleRefreshTokenGrantAsync();

        return BadRequest(new OpenIddictResponse
        {
            Error = OpenIddictConstants.Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }

    // ── Password grant ────────────────────────────────────────────────────────

    private async Task<IActionResult> HandlePasswordGrantAsync(OpenIddictRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Username!);
        if (user is null || !user.IsActive)
            return ForbidWithInvalidGrant("The username/password couple is invalid.");

        var result = await signInManager.CheckPasswordSignInAsync(
            user, request.Password!, lockoutOnFailure: true);

        if (!result.Succeeded)
            return ForbidWithInvalidGrant("The username/password couple is invalid.");

        var principal = await CreateClaimsPrincipalAsync(user);
        principal.SetScopes(request.GetScopes());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // ── Refresh token grant ───────────────────────────────────────────────────

    private async Task<IActionResult> HandleRefreshTokenGrantAsync()
    {
        // Retrieve the claims principal stored in the refresh token.
        var result = await HttpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal is null)
            return ForbidWithInvalidGrant("The refresh token is no longer valid.");

        // Ensure the user still exists and is active.
        var userId = result.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = userId is not null ? await userManager.FindByIdAsync(userId) : null;

        if (user is null || !user.IsActive)
            return ForbidWithInvalidGrant("The user associated with the refresh token no longer exists or is deactivated.");

        // Ensure the user can still sign in.
        if (!await signInManager.CanSignInAsync(user))
            return ForbidWithInvalidGrant("The user is no longer allowed to sign in.");

        // Re-create the principal with fresh claims (roles may have changed).
        var principal = await CreateClaimsPrincipalAsync(user);
        principal.SetScopes(result.Principal.GetScopes());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id.ToString())
            .SetDestinations(OpenIddictConstants.Destinations.AccessToken));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.Email!)
            .SetDestinations(OpenIddictConstants.Destinations.AccessToken));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email!)
            .SetDestinations(OpenIddictConstants.Destinations.AccessToken));

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role)
                .SetDestinations(OpenIddictConstants.Destinations.AccessToken));
        }

        return new ClaimsPrincipal(identity);
    }

    private IActionResult ForbidWithInvalidGrant(string description) =>
        Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));
}
