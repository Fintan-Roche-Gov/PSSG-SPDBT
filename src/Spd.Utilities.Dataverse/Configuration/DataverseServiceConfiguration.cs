using Microsoft.Dynamics.CRM.Microsoft.Dynamics.CRM;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Spd.Utilities.Dataverse;
using Spd.Utilities.Dynamics;
using System.Configuration;
using static IdentityModel.ClaimComparer;

namespace Spd.Utilities.Dataverse;

public class Configuration
{
    private readonly IConfiguration _configuration;
    private readonly IServiceCollection _services;
    var options = configuration.GetSection("Dynamics").Get<DynamicsSettings>()!;


    public Configuration( IConfiguration configuration, IServiceCollection services)
    {
        _services = services;
        _configuration = configuration;
    }
    public void ConfigureServices()
    {
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
        // Get "Dynamics" section from configuration and bind it to DataverseOptions
        _services.Configure<DataverseOptions>(opts => _configuration.GetSection("Dynamics").Bind(opts));

        // Register the Dataverse options validator, which ensures that the configuration for Dataverse is valid
        _services.AddSingleton<IValidateOptions<DataverseOptions>, DataverseOptionsValidator>();

        _services.AddSingleton<IDataverseClientFactory, DataverseClientFactory>();
        _services.AddSingleton<IDataverseContextFactory, DataverseContextFactory>();

        _services.AddSingleton<IOrganizationServiceAsync2>(sp => sp.GetRequiredService<IDataverseClientFactory>().CreateClient());

        // Register a hosted service to initialize the Dataverse connection on application startup
        _services.AddHostedService<DataverseConnectionWarmup>();
    }
}
