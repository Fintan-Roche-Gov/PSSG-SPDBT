//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;
//using Spd.Utilities.Dynamics;
//using Spd.Utilities.Hosting;

//namespace Spd.Utilities.Dataverse;

///// <summary>
///// Configures dependency injection for Dataverse utilities.
///// 
///// The Dataverse adapter wraps the underlying OData (Dynamics) client and reuses
///// the same authentication infrastructure via DynamicsSettings.
///// 
///// Configuration Structure (appsettings.json):
///// 
///// For Cloud (Entra ID):
///// {
/////   "Dynamics": {
/////     "AuthenticationType": "Cloud",
/////     "ServiceUrl": "https://your-org.crm.dynamics.com/api/data/v9.2",
/////     "EntityBaseUri": "https://your-org.crm.dynamics.com",
/////     "EntraId": {
/////       "TenantId": "your-tenant-id",
/////       "ClientId": "your-client-id",
/////       "ClientSecret": "your-client-secret",
/////       "ResourceName": "https://your-org.crm.dynamics.com"
/////     }
/////   }
///// }
///// 
///// For On-Premise (ADFS):
///// {
/////   "Dynamics": {
/////     "AuthenticationType": "OnPremise",
/////     "ServiceUrl": "https://dynamics-on-prem/api/data/v9.2",
/////     "EntityBaseUri": "https://dynamics-on-prem",
/////     "AuthenticationSettings": {
/////       "OAuth2TokenEndpointUrl": "https://adfs/adfs/oauth2/token",
/////       "ClientId": "client-id",
/////       "ClientSecret": "secret",
/////       "ServiceAccountDomain": "DOMAIN",
/////       "ServiceAccountName": "serviceaccount",
/////       "ServiceAccountPassword": "password",
/////       "ResourceName": "https://dynamics-on-prem"
/////     }
/////   }
///// }
///// 
///// Note: Secrets should be stored securely using User Secrets (development) or 
///// environment variables (production). See DATAVERSE_DI_SETUP_GUIDE.md for details.
///// </summary>
//public class Configurer : IConfigureComponents
//{
//    public void Configure(ConfigurationContext configurationServices)
//    {
//        var services = configurationServices.Services;
//        var configuration = configurationServices.Configuration;

//        // Ensure Dynamics is configured (which handles authentication)
//        // The DynamicsSettings class supports both Cloud (Entra ID) and OnPremise (ADFS)
//        var dynamicsSettings = configuration.GetSection("Dynamics").Get<DynamicsSettings>();
//        if (dynamicsSettings == null)
//        {
//            throw new InvalidOperationException(
//                "Dynamics configuration is not configured. Add a 'Dynamics' section to appsettings.json with " +
//                "AuthenticationType, ServiceUrl, EntityBaseUri, and either EntraId or AuthenticationSettings sections.");
//        }

//        // Register Dataverse ServiceClient factory and options validation.
//        services.Configure<DataverseOptions>(opts => configuration.GetSection("Dynamics").Bind(opts));
//        services.AddSingleton<IValidateOptions<DataverseOptions>, DataverseOptionsValidator>();
//        services.AddSingleton<IDataverseClientFactory, DataverseClientFactory>();

//        // Register the Dataverse adapter factory
//        // It wraps the existing IDynamicsContextFactory from Spd.Utilities.Dynamics
//        services.AddSingleton<IDataverseContextFactory>(provider =>
//            new DataverseContextFactory(provider.GetRequiredService<IDynamicsContextFactory>())
//        );
//    }
//}

