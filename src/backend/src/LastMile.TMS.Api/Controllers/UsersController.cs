using LastMile.TMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LastMile.TMS.Api.Controllers;

/// <summary>
/// Resource-oriented controller for user-related endpoints.
/// Currently exposes the "me" endpoint; future AU-001 endpoints
/// (create, edit, deactivate, list) will be added here.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Returns the current authenticated user's identity information.
    /// </summary>
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            userId = currentUser.UserId,
            userName = currentUser.UserName,
            roles = currentUser.Roles
        });
    }
}
