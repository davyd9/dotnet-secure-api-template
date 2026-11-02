namespace DotnetSecureApi.Api.Configuration;

/// <summary>
/// Represents the JWT settings required for token generation and validation.
/// These settings are typically stored in appsettings.json.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// The issuer of the JWT token (e.g., your API's name or URL).
    /// </summary>
    public string Issuer { get; init; } = null!;

    /// <summary>
    /// The audience of the JWT token (e.g., the intended recipient of the token).
    /// </summary>
    public string Audience { get; init; } = null!;

    /// <summary>
    /// The secret key used to sign the JWT token.
    /// IMPORTANT: This should be a long, complex, and securely stored string.
    /// For production, use User Secrets, Azure Key Vault, or another secret management tool.
    /// </summary>
    public string Secret { get; init; } = null!;

    /// <summary>
    /// The expiration time for the access token in minutes.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; init; }

    /// <summary>
    /// The expiration time for the refresh token in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; init; }
}
