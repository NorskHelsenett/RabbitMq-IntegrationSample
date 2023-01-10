using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMq_integration.AmqpInterop;
using RabbitMq_integration.CommunicationParty;
using RabbitMq_integration.Configuration;
using RabbitMq_integration.HealthcareSystem;
using RabbitMq_integration.ServiceBusManagerServiceV2;

namespace RabbitMq_integration.BackgroundServices;

public class AmqpQueueConsumer : BackgroundService
{
    // Dependencies
    private readonly IServiceBusManagerV2 _serviceBusManager;
    private readonly ILogger _logger;
    private readonly RegisterPlatformSettings _settings;
    private readonly IConnectionFactory _busConnectionFactory;
    private readonly ICommunicationPartyService _communicationPartyService;
    private readonly IHealthCareSystem _healthCareSystem;

    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(25);

    public AmqpQueueConsumer(IOptions<RegisterPlatformSettings> settings, IServiceBusManagerV2 serviceBusManager, ICommunicationPartyService communicationPartyService, IConnectionFactory busConnectionFactory, IHealthCareSystem healthCareSystem, ILogger<AmqpQueueConsumer> logger)
    {
        _serviceBusManager = serviceBusManager;
        _logger = logger;
        _healthCareSystem = healthCareSystem;
        _busConnectionFactory = busConnectionFactory;
        _communicationPartyService = communicationPartyService;
        _settings = settings.Value;

        // TODO: Unsubscribe on shutdown
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = await EnsureSubscriptionExistsAsync();

        await StartListeningToSubscriptionAsync(queueName, stoppingToken);
    }

    /// <summary>
    /// Ask ServiceBusManager to subscribe to all events on AddressRegister topic, and return the queue name.
    /// </summary>
    private async Task<string> EnsureSubscriptionExistsAsync()
    {
        var eventSource = SubscriptionEventSource.AddressRegister;
        var eventName = string.Empty; // subscribe to all events on event source
        var systemIdent = _settings.SubscriptionIdentifier;

        EventSubscription subscription = await _serviceBusManager.SubscribeAsync(eventSource, eventName, systemIdent);
        return subscription.QueueName;
    }

    private async Task StartListeningToSubscriptionAsync(string queueName, CancellationToken stoppingToken)
    {
        // Subscription is created asynchronously, and may not be ready yet, so we need retry logic
        while (!stoppingToken.IsCancellationRequested && !TrySetupConsumer(queueName, stoppingToken))
        {
            await Task.Delay(_retryDelay, stoppingToken);
        }
    }

    private bool TrySetupConsumer(string queueName, CancellationToken stoppingToken)
    {
        IConnection? connection = null;
        try
        {
            connection = _busConnectionFactory.CreateConnection(); 
            IModel channel = connection.CreateModel();
            
            // Set up a consumer that handles AMQP10 properties
            var consumer = new Amqp10AwareAsyncConsumer(channel);
            // Register event handler
            consumer.Received += (_, eventArgs) => OnReceivedAsync(eventArgs, channel);
            // Start consuming
            var consumerTag = channel.BasicConsume(queue: queueName,
                autoAck: false,
                consumer: consumer);

            // Handle application shutdown gracefully:
            stoppingToken.Register(() => CloseChannel(channel, consumerTag, connection));

            _logger.LogInformation("Subscription set up successfully. Listening for changes in Address Registry...");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to set up connection to queue {QueueName} on RabbitMq. It may not have been created yet, will retry in a moment...", queueName);

            connection?.Close();
            connection?.Dispose();
            return false;
        }
    }

    /// <summary>
    /// Handle the incoming message. Send Ack if it was processed successfully, and Nack if it could not be processed.
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <param name="channel"></param>
    /// <returns></returns>
    private async Task OnReceivedAsync(BasicDeliverEventArgs eventArgs, IModel channel)
    {
        var messageHandled = false;
        try
        {
            await HandleMessage(eventArgs);
            messageHandled = true;
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Error processing message with MessageId: {MessageId}, Delivery tag: {DeliveryTag}. Will send Nack and requeue the message.",
                eventArgs.BasicProperties?.MessageId, eventArgs.DeliveryTag);
        }

        try
        {
            if (messageHandled)
            {
                channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            else
            {
                channel.BasicNack(eventArgs.DeliveryTag, false, true);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not send {Signal} for MessageId: {MessageId}, Delivery tag: {DeliveryTag}. Message will time out eventually.",
                messageHandled ? "Ack" : "Nack", eventArgs.BasicProperties?.MessageId, eventArgs.DeliveryTag);
        }
    }

    private async Task HandleMessage(BasicDeliverEventArgs eventArgs)
    {
        var eventName = GetExpectedHeader<string>(eventArgs.BasicProperties.Headers, "eventName");

        var relevantEvents = new[] { "CommunicationPartyCreated", "CommunicationPartyUpdated" };
        if (relevantEvents.Contains(eventName))
        {
            // A Communication Party has been created or updated, Fetch the latest version so we can send it to the health care system
            var herId = GetExpectedHeader<int>(eventArgs.BasicProperties.Headers, "herId");
            var communicationParty = await _communicationPartyService.GetCommunicationPartyDetailsAsync(herId);

            // Send the update to the health care system
            _healthCareSystem.CommunicationPartyUpdated(communicationParty);
        }
    }

    private static T GetExpectedHeader<T>(IDictionary<string, object> basicPropertiesHeaders, string headerName)
    {
        var foundHeaderValue = basicPropertiesHeaders[headerName];
        var convertedType = Convert.ChangeType(foundHeaderValue, typeof(T));
        return (T)convertedType;
    }

    private void CloseChannel(IModel channel, string consumerTag, IConnection? connection)
    {
        channel.BasicCancel(consumerTag);

        // Allow already received messages some time to process before we tear down the channel:
        Thread.Sleep(TimeSpan.FromSeconds(3));

        connection?.Close();
        connection?.Dispose();
    }
}