using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace DotnetSecureApi.IntegrationTests;

public static class TestAuthConstants
{
    public const string Issuer = "test-issuer";
    public const string Audience = "test-audience";
    public const string Key = "this-is-a-super-secret-key-for-testing-12345"; // Must be long enough for the algorithm
}

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use a different configuration for tests to override JWT settings
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = TestAuthConstants.Issuer,
                ["JwtSettings:Audience"] = TestAuthConstants.Audience,
                ["JwtSettings:Key"] = TestAuthConstants.Key,
                ["JwtSettings:ExpiryMinutes"] = "60",

                // Lower log levels for cleaner test output
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["Serilog:MinimumLevel:Override:Microsoft"] = "Warning",
                ["Serilog:MinimumLevel:Override:System"] = "Warning"
            });
        });

        base.ConfigureWebHost(builder);
    }
}
