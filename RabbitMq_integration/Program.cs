using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq_integration.Ar;
using RabbitMq_integration.Configuration;
using RabbitMq_integration.Consumer;
using RabbitMq_integration.ServiceBusManagerServiceV2;

namespace RabbitMq_integration {
    class Program 
    {
        static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<IRabbitMqClientSettings>(hostContext.Configuration.GetSection("RabbitMqClientSettings"));
                    services.Configure<ServiceBusManagerServiceSettings>(hostContext.Configuration.GetSection("ServiceBusManagerServiceSettings"));
                    
                    services.AddScoped<CommunicationPartyServiceAccessor>();
                    
                    //Setting up ServiceBusManager and getting the RabbitMq queuenames
                    services.AddSingleton<IServiceBusManagerV2>(CreateServiceBusManagerService);
                    services.AddHostedService<ServiceBusManagerAccessor>();
                    
                    //RabbitMq Client setup
                    services.AddHostedService<RabbitQueueConsumer>();

                    //See InitialPopulation to see how to do a initial population of your healthcaresystem
                })
                .Build();
            host.Run();
        }

        private static IServiceBusManagerV2 CreateServiceBusManagerService(IServiceProvider serviceProvider)
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ServiceBusManagerServiceSettings>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            var client = new ServiceBusManagerV2Client(ServiceBusManagerV2Client.EndpointConfiguration.WSHttpBinding_IServiceBusManagerV2, settings.Url);
            client.ClientCredentials.UserName.UserName = settings.UserName;
            client.ClientCredentials.UserName.Password = settings.Password;
            return client;
        }
    }
}