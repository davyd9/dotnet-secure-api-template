using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DotnetSecureApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication for all actions in this controller
public class ProtectedController : ControllerBase
{
    private readonly ILogger<ProtectedController> _logger;

    public ProtectedController(ILogger<ProtectedController> logger)
    {
        _logger = logger;
    }

    [HttpGet("user-profile")]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetUserProfile()
    {
        // User is guaranteed to be authenticated due to [Authorize]
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        _logger.LogInformation("Fetching profile for user: {Username}", username);

        if (string.IsNullOrEmpty(username))
        {
            // This should theoretically not happen if [Authorize] is working
            return Unauthorized();
        }

        var userProfile = new UserProfile(username, roles, DateTime.UtcNow);
        return Ok(userProfile);
    }

    [HttpGet("admin-data")]
    [Authorize(Roles = "Admin")] // Requires the user to have the "Admin" role
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetAdminData()
    {
        var username = User.Identity?.Name;
        _logger.LogInformation("User {Username} accessed admin-only data", username);

        return Ok($"Welcome, Admin {username}! This is a secret message for administrators only.");
    }
    
    [HttpGet("policy-data")]
    [Authorize(Policy = "UserAccessible")] // Requires the user to have either "Admin" or "User" role
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetPolicyData()
    {
        var username = User.Identity?.Name;
        _logger.LogInformation("User {Username} accessed policy-protected data (UserAccessible)", username);

        return Ok($"Welcome, {username}! You have access to this data via the 'UserAccessible' policy.");
    }
}

public record UserProfile(string Username, List<string> Roles, DateTime Timestamp);
