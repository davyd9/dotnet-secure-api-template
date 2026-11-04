using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DotnetSecureApi.Api.Middleware;

/// <summary>
/// Middleware for handling exceptions globally and returning standardized error responses.
/// </summary>
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception has occurred: {Message}", exception.Message);

        ProblemDetails problemDetails;
        HttpStatusCode statusCode;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails = new ProblemDetails
                {
                    Status = (int)statusCode,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "One or more validation errors occurred.",
                    Detail = "The request contains invalid parameters. See errors for details."
                };
                problemDetails.Extensions["errors"] = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => JsonNamingPolicy.CamelCase.ConvertName(g.Key),
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError;
                problemDetails = new ProblemDetails
                {
                    Status = (int)statusCode,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "An internal server error occurred.",
                    Detail = "An unexpected error occurred while processing your request. Please try again later."
                };
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}

public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
