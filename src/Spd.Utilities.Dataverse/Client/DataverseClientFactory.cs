using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Model;
using Polly;
using Polly.CircuitBreaker;

namespace Spd.Utilities.Dataverse;

public interface IDataverseClientFactory
{
    /// <summary>
    /// Returns a ready-to-use, thread-safe client cloned from a shared connection.
    /// </summary>
    ServiceClient CreateClient();
}

public sealed class DataverseClientFactory : IDataverseClientFactory, IDisposable
{
    private readonly DataverseOptions _options;
    private readonly ILogger<DataverseClientFactory> _logger;
    private readonly ResiliencePipeline _connectionPipeline;
    private readonly object _connectionLock = new();
    private ServiceClient? _masterClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataverseClientFactory"/> class with the specified options.
    /// </summary>
    /// <param name="options">The Dataverse options used to configure the connection.</param>
    /// <param name="logger">The logger used to log connection events and errors.</param>
    /// <param name="dataverseStateReporter">The reporter used to track the state of the Dataverse service.</param>
    public DataverseClientFactory(
        IOptions<DataverseOptions> options,
        ILogger<DataverseClientFactory> logger
    )
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Returns a ready-to-use, thread-safe client cloned from a shared connection.
    /// The first call will establish the connection, which may take some time.
    /// Subsequent calls will return a cloned client that is ready to use immediately.
    /// </summary>
    public ServiceClient CreateClient()
    {
        var client = GetOrConnectMasterClient().Clone();

        if (client is null || !client.IsReady)
        {
            throw new InvalidOperationException($"Failed to clone Dataverse connection: {client?.LastError}", client?.LastException);
        }

        return client;
    }

    /// <summary>
    /// Gets the master Dataverse client, establishing the connection if necessary. Uses a circuit breaker to prevent
    /// repeated connection attempts if the service is unavailable.
    /// </summary>
    /// <returns>The master Dataverse client.</returns>
    private ServiceClient GetOrConnectMasterClient()
    {
        if (_masterClient is { IsReady: true } ready)
        {
            return ready;
        }

        lock (_connectionLock)
        {
            if (_masterClient is { IsReady: true } readyAfterLock)
            {
                return readyAfterLock;
            }

            try
            {
                return _masterClient = _connectionPipeline.Execute(() => Connect(_options, _logger));
            }
            catch (BrokenCircuitException error)
            {
                throw new InvalidOperationException(
                    "Dataverse connection circuit breaker is open due to repeated connection failures",
                    error
                );
            }
        }
    }

    /// <summary>
    /// Establishes a new connection to Dataverse using the provided options. Throws an exception if the connection fails.
    /// </summary>
    /// <param name="options">The Dataverse connection options.</param>
    /// <param name="_logger">The logger instance for logging connection attempts and errors.</param>
    /// <returns>The established Dataverse client.</returns>
    private static ServiceClient Connect(DataverseOptions options, ILogger _logger)
    {
        ServiceClient.MaxConnectionTimeout = TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds);

        var connectionOptions = new ConnectionOptions
        {
            AuthenticationType = AuthenticationType.ClientSecret,
            ServiceUri = options.DynamicsApiEndpoint,
            ClientId = options.EntraId.ClientId,
            ClientSecret = options.EntraId.ClientSecret,
            RequireNewInstance = true,
        };

        var configurationOptions = new ConfigurationOptions
        {
            MaxRetryCount = options.MaxRetryCount,
            RetryPauseTime = TimeSpan.FromSeconds(options.RetryPauseSeconds),
            EnableAffinityCookie = options.EnableAffinityCookie,
        };

        var client = new ServiceClient(connectionOptions, serviceClientConfiguration: configurationOptions);

        if (!client.IsReady)
        {
            throw new InvalidOperationException($"Failed to connect to Dataverse: {client.LastError}", client.LastException);
        }

        return client;
    }

    /// <summary>
    /// Disposes the master Dataverse client.
    /// </summary>
    public void Dispose()
    {
        _masterClient?.Dispose();
    }
}
