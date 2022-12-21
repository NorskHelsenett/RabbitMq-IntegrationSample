
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq_integration.Configuration;
using RabbitMq_integration.ServiceBusManagerServiceV2;

namespace RabbitMq_integration.Consumer;

public class ServiceBusManagerAccessor : BackgroundService
{
    private readonly IServiceBusManagerV2 _serviceBusManager;
    private readonly RabbitQueueContext _queueContext;
    private readonly ILogger<ServiceBusManagerAccessor> _logger;
    private readonly RabbitMqClientSettings _rabbitSettings;
    private readonly TimeSpan _retryTimeout;
    
    public ServiceBusManagerAccessor(IOptions<RabbitMqClientSettings> rabbitSettings, IServiceBusManagerV2 serviceBusManager, RabbitQueueContext queueContext, ILogger<ServiceBusManagerAccessor> logger)
    {
        _serviceBusManager = serviceBusManager;
        _queueContext = queueContext;
        _logger = logger;
        _rabbitSettings = rabbitSettings.Value;
        _retryTimeout = TimeSpan.FromMinutes(1);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
	    while (!await TryCreateSubscriptionAsync())
        {
            await Task.Delay(_retryTimeout, stoppingToken);
        }
    }
    
    private async Task<bool> TryCreateSubscriptionAsync()
    {
        try
        {
	        _logger.LogInformation("Ensure subscription exists.");
	        _queueContext.QueueName = await EnsureRabbitMqSubscriptionExistsAsync(string.Empty, _rabbitSettings.SubscriptionIdentifier);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to set up subscription in RabbitMq.");
            return false;
        }
    }

    public async Task<string> EnsureRabbitMqSubscriptionExistsAsync(string eventName, string systemIdent)
    {
        EventSubscription subscription = await _serviceBusManager.SubscribeAsync(SubscriptionEventSource.AddressRegister, eventName, systemIdent);

        return subscription.QueueName;
    }
}