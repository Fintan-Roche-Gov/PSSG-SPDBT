namespace Spd.Utilities.BCeIDWS;

/// <summary>
/// Configuration settings for BCeID Web Service integration.
/// 
/// Configuration Structure (appsettings.json):
/// {
///   "BCeIDWebService": {
///     "Url": "https://idir.bcgov/webservices/client/v2",
///     "OnlineServiceId": "your-online-service-id",
///     "ServiceAccountUser": "service-account-username",
///     "ServiceAccountPassword": "service-account-password"
///   }
/// }
/// 
/// Note: Credentials should be stored securely using User Secrets (development) or 
/// environment variables (production), not hardcoded in appsettings.json.
/// 
/// The SOAP client uses Basic HTTP authentication over HTTPS (Transport security mode).
/// Ensure your service account has the appropriate permissions in the BCeID system.
/// </summary>
public class BCeIDSettings
{
    public Uri? Url { get; set; }
    public string? OnlineServiceId { get; set; }
    public string? ServiceAccountUser { get; set; }
    public string? ServiceAccountPassword { get; set; }
}
