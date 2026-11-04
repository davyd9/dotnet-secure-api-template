using System.Text;
using DotnetSecureApi.Api.Configuration;
using DotnetSecureApi.Api.Diagnostics;
using DotnetSecureApi.Api.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Configure Serilog for bootstrap logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- Logging Configuration ---
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // --- Configuration ---
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

    // --- Service Registration ---
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add validation services from the current assembly
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // --- Diagnostics and Metrics ---
    builder.Services.AddSingleton<ApiMetrics>();

    // --- Authentication & Authorization ---
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("UserAccessible", policy => policy.RequireRole("Admin", "User"));
    });

    var app = builder.Build();

    // --- Middleware Pipeline ---
    app.UseSerilogRequestLogging();

    // Custom middleware registration
    app.UseErrorHandlingMiddleware();
    app.UseMiddleware<RequestMetricsMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}
