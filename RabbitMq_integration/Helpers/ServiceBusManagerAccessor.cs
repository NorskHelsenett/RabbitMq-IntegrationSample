
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMq_integration.Configuration;
using RabbitMq_integration.ServiceBusManagerServiceV2;

namespace RabbitMq_integration.Consumer;

public class ServiceBusManagerAccessor : BackgroundService
{
    private readonly IServiceBusManagerV2 _serviceBusManager;
    private readonly RabbitQueueContext _queueContext;
    private readonly RabbitMqClientSettings _rabbitSettings;
    private readonly TimeSpan _retryTimeout;
    
    public ServiceBusManagerAccessor(IOptions<RabbitMqClientSettings> rabbitSettings, IServiceBusManagerV2 serviceBusManager, RabbitQueueContext queueContext)
    {
        _serviceBusManager = serviceBusManager;
        _queueContext = queueContext;
        _rabbitSettings = rabbitSettings.Value;
        _retryTimeout = TimeSpan.FromMinutes(1);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Rabbit MQ integration is enabled.");

        if (!_rabbitSettings.Enabled)
        {
            return;
        }

        while (!await TryCreateSubscriptionAsync())
        {
            await Task.Delay(_retryTimeout, stoppingToken);
        }
    }
    
    private async Task<bool> TryCreateSubscriptionAsync()
    {
        try
        {
            _queueContext.QueueName = await EnsureRabbitMqSubscriptionExistsAsync(string.Empty, _rabbitSettings.SubscriptionIdentifier);

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to set up subscription in RabbitMq.");
            return false;
        }
    }

    public async Task<string> EnsureRabbitMqSubscriptionExistsAsync(string eventName, string systemIdent)
    {
        EventSubscription subscription = await _serviceBusManager.SubscribeAsync(SubscriptionEventSource.AddressRegister, eventName, systemIdent);

        return subscription.QueueName;
    }
}