
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using RabbitMq_integration.CommunicationParty;
using RabbitMq_integration.Configuration;
using RabbitMq_integration.Consumer;
using RabbitMq_integration.Examples.AMQP_Client;
using RabbitMq_integration.HealthcareSystem;
using RabbitMq_integration.ServiceBusManagerServiceV2;

namespace RabbitMq_integration {
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
					// Load config into helper classes
                    services.Configure<RabbitMqClientSettings>(hostContext.Configuration.GetSection("RabbitMqClientSettings"));
                    services.Configure<ServiceBusManagerServiceSettings>(hostContext.Configuration.GetSection("ServiceBusManagerServiceSettings"));
                    
                    //Setting up ServiceBusManager and getting the RabbitMq queuenames
                    services.AddSingleton<IServiceBusManagerV2>(CreateServiceBusManagerService);
                    services.AddSingleton<RabbitQueueContext, RabbitQueueContext>();
                    services.AddHostedService<ServiceBusManagerAccessor>();
                    
                    //RabbitMq Client setup
                    services.AddSingleton<IHealthCareSystem, HealthCareSystem>();
                    services.AddSingleton<ICommunicationPartyService>(CreateCommunicationPartyService);
                    services.AddHostedService<RabbitQueueConsumer>();
                })
                .Build();
            host.Run();
        }

        private static ICommunicationPartyService CreateCommunicationPartyService(IServiceProvider serviceProvider)
        {
	        var settings = serviceProvider.GetRequiredService<IOptions<ServiceBusManagerServiceSettings>>().Value;

	        var client = new CommunicationPartyServiceClient(CommunicationPartyServiceClient.EndpointConfiguration
		        .WSHttpBinding_ICommunicationPartyService);
	        
	        client.ClientCredentials.UserName.UserName = settings.UserName;
	        client.ClientCredentials.UserName.Password = settings.Password;

	        return client;
        }

        private static IServiceBusManagerV2 CreateServiceBusManagerService(IServiceProvider serviceProvider)
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ServiceBusManagerServiceSettings>>().Value;

            var client = new ServiceBusManagerV2Client(ServiceBusManagerV2Client.EndpointConfiguration.WSHttpBinding_IServiceBusManagerV2, settings.Url);
            client.ClientCredentials.UserName.UserName = settings.UserName;
            client.ClientCredentials.UserName.Password = settings.Password;
            return client;
        }
    }
}