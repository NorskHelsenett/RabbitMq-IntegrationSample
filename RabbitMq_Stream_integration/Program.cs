using System.Net;
using System.ServiceModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq_Stream_integration.ArExportService;
using RabbitMq_Stream_integration.BackgroundServices;
using RabbitMq_Stream_integration.CommunicationParty;
using RabbitMq_Stream_integration.Configuration;
using RabbitMq_Stream_integration.HealthcareSystem;
using RabbitMQ.Stream.Client;
using SslOption = RabbitMQ.Stream.Client.SslOption;

namespace RabbitMq_Stream_integration
{
    class Program 
    {
        static void Main(string[] args)
        {
            bool performInitialPopulation = args.Contains("initpop", StringComparer.InvariantCultureIgnoreCase);

            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging(builder =>
                {
                    builder.AddSimpleConsole(configure =>
                        configure.TimestampFormat = "HH:mm:ss ");
                    builder.AddFilter("RabbitMQ.Stream", LogLevel.Information);
                })
                .ConfigureAppConfiguration(builder => builder.AddCommandLine(args))
                .ConfigureServices((hostContext, services) =>
                {
                    // Set init pop data for globl use
                    services.AddSingleton<InitialPopulationData>(SetInitialPopulationData(performInitialPopulation));
                    
                    // Load config into a class
                    services.Configure<RegisterPlatformSettings>(hostContext.Configuration.GetSection("RegisterPlatformSettings"));

                    // A dummy health care system that will receive updates from AddressRegister
                    services.AddSingleton<IHealthCareSystem, HealthCareSystemStub>();

                    // Run init pop only if command line flag is specified
                    if (performInitialPopulation)
                    {
                        services.AddSingleton<IARExportService>(CreateArExportService);
                        // A hosted service that will perform the initial population of communication parties that already exist in the Address Registry
                        services.AddHostedService<InitialPopulationJob>();
                    }

                    // A RegisterPlatform service for fetching CommunicationParties from AddressRegister
                    services.AddSingleton<ICommunicationPartyService>(CreateCommunicationPartyService);
                    // A factory for creating connections to RabbitMq
                    services.AddSingleton<StreamSystemConfig>(CreateRabbitMqConnectionFactory);

                    // A hosted service that will listen to events on RabbitMq, fetch changes from AddressRegistry and send them to a Health Care System
                    services.AddHostedService<RabbitStreamConsumer>();
                })
                .Build();
            host.Run();
        }

        private static InitialPopulationData SetInitialPopulationData(bool performInitialPopulation)
        {
            return new InitialPopulationData
            {
                PerformInitialPopulation = performInitialPopulation,
                StartTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Create a WCF Proxy for CommunicationPartyService
        /// </summary>
        private static ICommunicationPartyService CreateCommunicationPartyService(IServiceProvider serviceProvider)
        {
            var settings = serviceProvider.GetRequiredService<IOptions<RegisterPlatformSettings>>().Value;

            var client = new CommunicationPartyServiceClient(CommunicationPartyServiceClient.EndpointConfiguration
                .WSHttpBinding_ICommunicationPartyService, settings.CommunicationPartyServiceUrl);
            
            client.ClientCredentials.UserName.UserName = settings.RegisterUserName;
            client.ClientCredentials.UserName.Password = settings.RegisterPassword;

            return client;
        }
        
        /// <summary>
        /// Configure a ConnectionFactory for RabbitMq
        /// </summary>
        private static StreamSystemConfig CreateRabbitMqConnectionFactory(IServiceProvider serviceProvider)
        {
            var settings = serviceProvider.GetRequiredService<IOptions<RegisterPlatformSettings>>().Value;

            return new StreamSystemConfig
            {
                UserName = settings.RegisterUserName,
                Password = settings.RegisterPassword,
                VirtualHost = "/",
                Endpoints = new List<EndPoint> {new DnsEndPoint(settings.BusHostname, settings.BusPort)},
                AddressResolver = new AddressResolver(new DnsEndPoint(settings.BusHostname, settings.BusPort)),
                Ssl = new SslOption()
                {
                    Enabled = settings.BusSslEnabled,
                    ServerName = settings.BusHostname
                },
                ClientProvidedName = settings.SubscriptionIdentifier
            };
        }

        /// <summary>
        /// Configure a WCF Proxy for ArExportService. This is used for initial population of the Health Care System,
        /// and will stream all the communication parties from Address Registry.
        /// The TransferMode on the binding has to be set to StreamedResponse for the streaming to work.
        /// </summary>
        private static IARExportService CreateArExportService(IServiceProvider serviceProvider)
        {
            var settings = serviceProvider.GetRequiredService<IOptions<RegisterPlatformSettings>>().Value;

            var client = new ARExportServiceClient(
                ARExportServiceClient.EndpointConfiguration.BasicHttpBinding_IARExportService,
                settings.ArExportServiceUrl);

            var basicHttpBinding = (BasicHttpBinding)client.Endpoint.Binding;
            // This must be set in order to properly stream the response instead of downloading everything to memory first:
            basicHttpBinding.TransferMode = TransferMode.StreamedResponse;
            // Increase timeouts to allow server to build the response:
            basicHttpBinding.ReceiveTimeout = TimeSpan.FromMinutes(10);
            basicHttpBinding.CloseTimeout = TimeSpan.FromMinutes(10);
            basicHttpBinding.OpenTimeout = TimeSpan.FromMinutes(10);
            basicHttpBinding.SendTimeout = TimeSpan.FromMinutes(10);

            client.ClientCredentials.UserName.UserName = settings.RegisterUserName;
            client.ClientCredentials.UserName.Password = settings.RegisterPassword;
            return client;
        }

}
}