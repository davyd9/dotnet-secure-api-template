using System.Text;
using DotnetSecureApi.Api.Configuration;
using DotnetSecureApi.Api.Diagnostics;
using DotnetSecureApi.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// Configure Serilog for bootstrap logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up the application");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Replace default logger with Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // --- Service Configuration ---

    // 1. Add strongly-typed configuration
    var jwtSettings = new JwtSettings();
    builder.Configuration.Bind(nameof(JwtSettings), jwtSettings);
    builder.Services.AddSingleton(jwtSettings);

    // 2. Add Authentication and Authorization services
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("UserAccessible", policy => policy.RequireRole("Admin", "User"));
    });

    // 3. Add Metrics and Diagnostics
    builder.Services.AddSingleton<ApiMetrics>();
    builder.Services.AddMetrics();

    // 4. Add Controllers and API Explorer
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Dotnet Secure API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // --- Application Pipeline Configuration ---

    var app = builder.Build();

    // Use Serilog for rich, structured request logging
    app.UseSerilogRequestLogging();

    // Use custom middleware for capturing request metrics
    app.UseRequestMetrics();

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
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down the application");
    Log.CloseAndFlush();
}
