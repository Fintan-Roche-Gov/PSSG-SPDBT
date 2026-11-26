using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Spd.Utilities.Dataverse;

/// <summary>
/// Initializes the Dataverse connection in the background so it's ready before the first real request.
/// If the warmup fails, the first real request will attempt to connect again. This is an optimization to avoid the
/// first real request having to wait for the connection to be established.
/// </summary>
internal class DataverseConnectionWarmup : IHostedService
{
    private readonly IDataverseClientFactory _clientFactory;
    private readonly ILogger<DataverseConnectionWarmup> _logger;

    public DataverseConnectionWarmup(IDataverseClientFactory clientFactory, ILogger<DataverseConnectionWarmup> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(
            () =>
            {
                try
                {
                    using var client = _clientFactory.CreateClient();
                    _logger.LogInformation("Dataverse connection warmed up successfully");
                }
                catch (Exception error)
                {
                    _logger.LogWarning(error, "Dataverse connection warm-up failed. Will retry on first use.");
                }
            },
            cancellationToken
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
