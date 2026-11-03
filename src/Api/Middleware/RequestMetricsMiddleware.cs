using System.Diagnostics;
using DotnetSecureApi.Api.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

namespace DotnetSecureApi.Api.Middleware;

/// <summary>
/// Middleware for capturing and recording HTTP request metrics.
/// </summary>
public sealed class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiMetrics _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestMetricsMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="metrics">The metrics service for instrumentation.</param>
    public RequestMetricsMiddleware(RequestDelegate next, ApiMetrics metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request, record metrics, and pass to the next middleware.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var endpointFeature = context.Features.Get<IEndpointFeature>();
            var endpoint = endpointFeature?.Endpoint?.DisplayName ?? "unknown";

            _metrics.IncrementRequests(endpoint, context.Request.Method, context.Response.StatusCode);
            _metrics.RecordRequestDuration(stopwatch.Elapsed.TotalMilliseconds, endpoint, context.Request.Method);
        }
    }
}

/// <summary>
/// Extension methods for adding the <see cref="RequestMetricsMiddleware"/> to the application's request pipeline.
/// </summary>
public static class RequestMetricsMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="RequestMetricsMiddleware"/> to the specified <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IApplicationBuilder UseRequestMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestMetricsMiddleware>();
    }
}
