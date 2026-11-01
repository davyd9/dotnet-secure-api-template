using Microsoft.AspNetCore.Mvc;

namespace DotnetSecureApi.Api.Controllers;

/// <summary>
/// Provides endpoints to check the health of the API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthCheckController : ControllerBase
{
    /// <summary>
    /// Gets the current operational status of the API.
    /// </summary>
    /// <returns>An object indicating the API is healthy.</returns>
    /// <response code="200">Returns the health status and timestamp.</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetHealthStatus()
    {
        var healthStatus = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        };

        return Ok(healthStatus);
    }
}
