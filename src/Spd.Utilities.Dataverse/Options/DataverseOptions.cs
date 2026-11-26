namespace Spd.Utilities.Dataverse;

public class DataverseOptions
{
    /// <summary>
    /// Example: https://your-host.api.crm3.dynamics.com/
    /// </summary>
    public Uri DynamicsApiEndpoint { get; set; } = null!;

    /// <summary>
    /// Example: https://your-host.api.crm3.dynamics.com/api/data/v9.2/
    /// </summary>
    public Uri DynamicsApiBaseUri { get; set; } = null!;

    public EntraIdOptions EntraId { get; set; } = new EntraIdOptions();

    /// <summary>
    /// Applies to every ServiceClient connection in the process (SDK-wide static setting).
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Number of retries the SDK performs internally for throttled/transient Dataverse failures.
    /// </summary>
    public int MaxRetryCount { get; set; } = 4;

    /// <summary>
    /// Delay between SDK-managed retries.
    /// </summary>
    public int RetryPauseSeconds { get; set; } = 2;

    /// <summary>
    /// Number of consecutive connection failures before the circuit breaker opens.
    /// </summary>
    public int CircuitBreakerNumberOfErrors { get; set; } = 3;

    /// <summary>
    /// How long the circuit breaker stays open before allowing a retry.
    /// </summary>
    public int CircuitBreakerResetInSeconds { get; set; } = 10;

    /// <summary>
    /// Indicates whether the client will 'prefer' a given node when making requests.    /// </summary>
    public bool EnableAffinityCookie { get; set; } = true;
}

/// <summary>
/// Cloud Dynamics authentication options for Entra ID authentication
/// </summary>
public class EntraIdOptions
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
