using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Extensions.Client;
using Spd.Utilities.Hosting;

namespace Spd.Utilities.Dynamics;

/// <summary>
/// Configures OData-based Dynamics client (legacy).
/// NOTE: This is being replaced by Spd.Utilities.Dataverse.Configurer which uses ServiceClient.
/// 
/// Migration path:
/// 1. Ensure Spd.Utilities.Dataverse project is referenced
/// 2. Add Spd.Utilities.Dataverse.Configurer to your startup configuration
/// 3. Gradually migrate repository code to use IDataverseContextFactory instead of IDynamicsContextFactory
/// 4. Once all repositories are migrated, this Configurer can be removed
/// </summary>
public class Configurer : IConfigureComponents, IProvideInstrumentationSources
{
    public void Configure(ConfigurationContext configurationServices)
    {
        var configuration = configurationServices.Configuration;
        var services = configurationServices.Services;

        var options = configuration.GetSection("Dynamics").Get<DynamicsSettings>()!;

        services.Configure<DynamicsSettings>(opts => configuration.GetSection("Dynamics").Bind(opts));

        services.AddHttpClient("oauth").SetHandlerLifetime(TimeSpan.FromMinutes(50));

        switch (options.AuthenticationType)
        {
            case DynamicsAuthenticationType.OnPremise:
                services.AddSingleton<ISecurityTokenProvider, AdfsSecurityTokenProvider>();
                break;
            case DynamicsAuthenticationType.Cloud:
                services.AddSingleton<ISecurityTokenProvider, EntraIdSecurityTokenProvider>();
                break;
            default:
                throw new InvalidOperationException($"Unsupported Dynamics authentication type: {options.AuthenticationType}");
        }

        services
            .AddODataClient("dynamics")
            .AddODataClientHandler<ODataClientHandler>()
            .AddHttpClient()
            .ConfigureHttpClient(c => c.Timeout = options.HttpClientTimeout)
            .SetHandlerLifetime(TimeSpan.FromMinutes(50));

        services.AddSingleton<IDynamicsContextFactory, DynamicsContextFactory>();
        services.AddTransient<DynamicsHealthCheck>();
    }

    public InstrumentationSources GetInstrumentationSources() =>
        new InstrumentationSources { TraceSources = [DynamicsContext.ActivitySourceName] };
}
