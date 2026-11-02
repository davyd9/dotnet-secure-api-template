using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotnetSecureApi.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace DotnetSecureApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;

    // In a real application, you would use a user service to validate credentials
    private static readonly Dictionary<string, (string Password, string[] Roles)> Users = new()
    {
        { "admin", ("Admin123!", new[] { "Admin", "User" }) },
        { "user", ("User123!", new[] { "User" }) }
    };

    public AuthController(JwtSettings jwtSettings, ILogger<AuthController> logger)
    {
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        if (!Users.TryGetValue(request.Username, out var userDetails) || userDetails.Password != request.Password)
        {
            _logger.LogWarning("Invalid credentials for user: {Username}", request.Username);
            return Unauthorized("Invalid username or password.");
        }

        var token = GenerateJwtToken(request.Username, userDetails.Roles);

        _logger.LogInformation("User {Username} logged in successfully", request.Username);
        return Ok(new LoginResponse { AccessToken = token });
    }

    private string GenerateJwtToken(string username, IEnumerable<string> roles)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username), // Subject
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64), // Issued At
            new(ClaimTypes.Name, username)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}

public record LoginRequest(string Username, string Password);
public record LoginResponse(string AccessToken);
