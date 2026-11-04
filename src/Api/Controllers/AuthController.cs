using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotnetSecureApi.Api.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DotnetSecureApi.Api.Controllers;

/// <summary>
/// Represents the request body for the login endpoint.
/// </summary>
/// <param name="Username">The user's username.</param>
/// <param name="Password">The user's password.</param>
public record LoginRequest(string Username, string Password);

[ApiController]
[Route("api/[controller]")]
public class AuthController(IOptions<JwtSettings> jwtSettings, ILogger<AuthController> logger) : ControllerBase
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly ILogger<AuthController> _logger = logger;

    // In a real application, this would be a database lookup.
    private static readonly List<(string Username, string Password, string Role)> Users =
    [
        ("admin", "AdminPassword123!", "Admin"),
        ("user", "UserPassword123!", "User")
    ];

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        var user = Users.FirstOrDefault(u => u.Username == request.Username && u.Password == request.Password);

        if (user == default)
        {
            _logger.LogWarning("Invalid credentials for user: {Username}", request.Username);
            return Unauthorized("Invalid credentials.");
        }

        var token = GenerateJwtToken(user.Username, user.Role);
        _logger.LogInformation("JWT token generated for user: {Username}", request.Username);

        return Ok(new { Token = token });
    }

    private string GenerateJwtToken(string username, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
