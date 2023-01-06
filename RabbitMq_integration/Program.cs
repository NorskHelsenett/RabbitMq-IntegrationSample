
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
	        var host = Host.CreateDefaultBuilder()
		        .ConfigureLogging(builder =>
			        builder.AddSimpleConsole(configure =>
				        configure.TimestampFormat = "HH:mm:ss "))
                .ConfigureServices((hostContext, services) =>
                {
					// Load config into a class
					services.Configure<RegisterPlatformSettings>(hostContext.Configuration.GetSection("RegisterPlatformSettings"));

					// A RegisterPlatform service for creating a subscription on RabbitMq
					services.AddSingleton<IServiceBusManagerV2>(CreateServiceBusManagerService);
					// A RegisterPlatform service for fetching CommunicationParties from AddressRegister
					services.AddSingleton<ICommunicationPartyService>(CreateCommunicationPartyService);
					// A factory for creating connections to RabbitMq
					services.AddSingleton<IConnectionFactory>(CreateRabbitMqConnectionFactory);
					// A dummy health care system that will receive updates from AddressRegister
					services.AddSingleton<IHealthCareSystem, HealthCareSystemStub>();

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
    }
}