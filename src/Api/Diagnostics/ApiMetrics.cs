using System.Diagnostics.Metrics;

namespace DotnetSecureApi.Api.Diagnostics;

/// <summary>
/// Encapsulates the application's metrics instrumentation using System.Diagnostics.Metrics.
/// This class is responsible for creating and managing the Meter and its associated instruments (counters, histograms).
/// </summary>
public sealed class ApiMetrics : IDisposable
{
    private const string MeterName = "DotnetSecureApi.Api";

    private readonly Meter _meter;
    private readonly Counter<long> _requestsCounter;
    private readonly Histogram<double> _requestDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiMetrics"/> class.
    /// </summary>
    /// <param name="meterFactory">The factory to create the Meter.</param>
    public ApiMetrics(IMeterFactory meterFactory)
    { 
        _meter = meterFactory.Create(MeterName);

        _requestsCounter = _meter.CreateCounter<long>(
            "api.http.requests.count",
            unit: "{requests}",
            description: "The total number of HTTP requests handled by the API.");

        _requestDuration = _meter.CreateHistogram<double>(
            "api.http.requests.duration",
            unit: "ms",
            description: "The duration of HTTP requests handled by the API in milliseconds.");
    }

    /// <summary>
    /// Increments the request counter for a given endpoint and HTTP method.
    /// </summary>
    /// <param name="endpoint">The request endpoint pattern.</param>
    /// <param name="httpMethod">The HTTP method of the request.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    public void IncrementRequests(string endpoint, string httpMethod, int statusCode)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("endpoint", endpoint),
            new("http_method", httpMethod),
            new("status_code", statusCode)
        };
        _requestsCounter.Add(1, tags);
    }

    /// <summary>
    /// Records the duration of a request.
    /// </summary>
    /// <param name="durationMs">The request duration in milliseconds.</param>
    /// <param name="endpoint">The request endpoint pattern.</param>
    /// <param name="httpMethod">The HTTP method of the request.</param>
    public void RecordRequestDuration(double durationMs, string endpoint, string httpMethod)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("endpoint", endpoint),
            new("http_method", httpMethod)
        };
        _requestDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Disposes the underlying Meter.
    /// </summary>
    public void Dispose()
    {
        _meter.Dispose();
    }
}
