using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMq_integration.Configuration;
using RabbitMq_integration.ServiceBusManagerServiceV2;

namespace RabbitMq_integration.Consumer;

public abstract class ServiceBusManagerAccessor : BackgroundService
{
    private readonly IServiceBusManagerV2 _serviceBusManager;
    private readonly IRabbitQueueContext _queueContext;
    private readonly IRabbitMqClientSettings _rabbitSettings;
    private readonly TimeSpan _retryTimeout;
    
    /// <summary>
    /// A single event on the AR topic to subscribe to. Leave this empty to subscribe to _all_ events on the AR topic.
    /// </summary>
    protected abstract string EventNameToSubscribeTo { get; }

    public ServiceBusManagerAccessor(IOptions<IRabbitMqClientSettings> rabbitSettings, IServiceBusManagerV2 serviceBusManager, IRabbitQueueContext queueContext)
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
            _queueContext.QueueName = await EnsureRabbitMqSubscriptionExistsAsync(EventNameToSubscribeTo, _rabbitSettings.SubscriptionIdentifier);

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