
using System;
using System.ServiceModel;
using ArExportService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMq_integration.BackgroundServices;
using RabbitMq_integration.CommunicationParty;
using RabbitMq_integration.Configuration;
using RabbitMq_integration.HealthcareSystem;
using RabbitMq_integration.ServiceBusManagerServiceV2;

namespace RabbitMq_integration
{
	class Program 
    {
        static void Main(string[] args)
        {
	        bool performInitialPopulation = args.Contains("initpop", StringComparer.InvariantCultureIgnoreCase);

	        var host = Host.CreateDefaultBuilder()
		        .ConfigureLogging(builder =>
			        builder.AddSimpleConsole(configure =>
				        configure.TimestampFormat = "HH:mm:ss "))
		        .ConfigureAppConfiguration(builder => builder.AddCommandLine(args))
		        .ConfigureServices((hostContext, services) =>
		        {
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

			        // A RegisterPlatform service for creating a subscription on RabbitMq
			        services.AddSingleton<IServiceBusManagerV2>(CreateServiceBusManagerService);
			        // A RegisterPlatform service for fetching CommunicationParties from AddressRegister
			        services.AddSingleton<ICommunicationPartyService>(CreateCommunicationPartyService);
			        // A factory for creating connections to RabbitMq
			        services.AddSingleton<IConnectionFactory>(CreateRabbitMqConnectionFactory);
					
			        // A hosted service that will listen to events on RabbitMq, fetch changes from AddressRegistry and send them to a Health Care System
			        services.AddHostedService<AmqpQueueConsumer>();
		        })
		        .Build();
	        host.Run();
        }

        /// <summary>
		/// Create a WCF Proxy for CommunicationPartyService
		/// </summary>
        private static ICommunicationPartyService CreateCommunicationPartyService(IServiceProvider serviceProvider)
        {
	        var settings = serviceProvider.GetRequiredService<IOptions<RegisterPlatformSettings>>().Value;

	        var client = new CommunicationPartyServiceClient(CommunicationPartyServiceClient.EndpointConfiguration
		        .WSHttpBinding_ICommunicationPartyService);
	        
	        client.ClientCredentials.UserName.UserName = settings.RegisterUserName;
	        client.ClientCredentials.UserName.Password = settings.RegisterPassword;

	        return client;
        }

		/// <summary>
		/// Create a WCF Proxy for ServiceBusManager
		/// </summary>
		/// <param name="serviceProvider"></param>
		/// <returns></returns>
        private static IServiceBusManagerV2 CreateServiceBusManagerService(IServiceProvider serviceProvider)
        {
            var settings = serviceProvider.GetRequiredService<IOptions<RegisterPlatformSettings>>().Value;

            var client = new ServiceBusManagerV2Client(ServiceBusManagerV2Client.EndpointConfiguration.WSHttpBinding_IServiceBusManagerV2, settings.ServiceBusManagerUrl);
            client.ClientCredentials.UserName.UserName = settings.RegisterUserName;
            client.ClientCredentials.UserName.Password = settings.RegisterPassword;
            return client;
        }

		/// <summary>
		/// Configure a ConnectionFactory for RabbitMq
		/// </summary>
        private static ConnectionFactory CreateRabbitMqConnectionFactory(IServiceProvider serviceProvider)
        {
	        var settings = serviceProvider.GetRequiredService<IOptions<RegisterPlatformSettings>>().Value;

	        return new ConnectionFactory
			{
				UserName = settings.RegisterUserName,
				Password = settings.RegisterPassword,
				HostName = settings.BusHostname,
				Port = settings.BusPort,
				Ssl = new SslOption
				{
					Enabled = settings.BusSslEnabled,
					ServerName = settings.BusHostname
				},
				ClientProvidedName = settings.SubscriptionIdentifier,
				DispatchConsumersAsync = true,
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