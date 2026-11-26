using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceReference;
using Spd.Utilities.Hosting;
using System.ServiceModel;

namespace Spd.Utilities.BCeIDWS;

public class Configurer : IConfigureComponents
{
    public void Configure(ConfigurationContext configurationServices)
    {
        var configuration = configurationServices.Configuration;
        var services = configurationServices.Services;

        services.AddOptions<BCeIDSettings>().Bind(configuration.GetSection("BCeIDWebService"));

        services.AddSingleton<BCeIDServiceSoap>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BCeIDSettings>>().Value;

            // Validate required configuration
            if (options.Url == null)
                throw new InvalidOperationException("BCeIDWebService:Url is required");
            if (string.IsNullOrEmpty(options.ServiceAccountUser))
                throw new InvalidOperationException("BCeIDWebService:ServiceAccountUser is required");
            if (string.IsNullOrEmpty(options.ServiceAccountPassword))
                throw new InvalidOperationException("BCeIDWebService:ServiceAccountPassword is required");

            // Configure BasicHttpBinding with proper security settings for HTTPS with Basic authentication
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport)
            {
                MaxReceivedMessageSize = int.MaxValue,
                OpenTimeout = TimeSpan.FromSeconds(30),
                CloseTimeout = TimeSpan.FromSeconds(30),
                SendTimeout = TimeSpan.FromSeconds(30),
                ReceiveTimeout = TimeSpan.FromSeconds(30)
            };

            // Explicitly set the security configuration
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            var client = new BCeIDServiceSoapClient(binding, new EndpointAddress(options.Url));

            // Set credentials - this must happen after client instantiation
            client.ClientCredentials.UserName.UserName = options.ServiceAccountUser;
            client.ClientCredentials.UserName.Password = options.ServiceAccountPassword;

            return client;
        });
        services.AddTransient<IBCeIDService, BCeIDService>();
    }
}