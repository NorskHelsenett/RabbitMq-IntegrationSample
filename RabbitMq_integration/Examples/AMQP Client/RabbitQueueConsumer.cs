
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq_integration.Configuration;
using RabbitMq_integration.HealthcareSystem;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMq_integration.CommunicationParty;
using RabbitMq_integration.Consumer;
using RabbitMqExtensions.RabbitMqTools;

namespace RabbitMq_integration.Examples.AMQP_Client;

public class RabbitQueueConsumer : BackgroundService
{
    private readonly RabbitQueueContext _queueContext;
    private readonly ConnectionFactory _connectionFactory;
    private readonly ICommunicationPartyService _communicationPartyService;
    private readonly ILogger _logger;
    private readonly IHealthCareSystem _healthCareSystem;
    private IConnection _connection;
    
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(20);
    
    public RabbitQueueConsumer(IOptions<RabbitMqClientSettings> settingsOptions, RabbitQueueContext queueContext, IHealthCareSystem healthCareSystem, ICommunicationPartyService communicationPartyService, ILogger<RabbitQueueConsumer> logger)
    {	    var settings = settingsOptions.Value;

	    _queueContext = queueContext;
	    _healthCareSystem = healthCareSystem;
        _communicationPartyService = communicationPartyService;
        _logger = logger;
        _connectionFactory = new ConnectionFactory
        {
            HostName = settings.EndpointHostname,
            UserName = settings.Username,
            Password = settings.Password,
            Port = settings.Port,
            Ssl = new SslOption
            {
                Enabled = settings.SslEnabled,
                ServerName = settings.EndpointHostname
            },
            ClientProvidedName = settings.SubscriptionIdentifier,
            DispatchConsumersAsync = true,
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool success;
        do
        {
			_logger.LogInformation("Try to set up RabbitMq consumer...");
            success = TrySetupConsumer(stoppingToken);
            if (!success)
            {
                await Task.Delay(RetryDelay, stoppingToken);
            }
        } while (!success && !stoppingToken.IsCancellationRequested);
    }
    
    private bool TrySetupConsumer(CancellationToken cancellationToken)
    {
        try
        {
            _connection = _connectionFactory.CreateConnection();
            IModel channel = _connection.CreateModel();
            var consumer = new Amqp10AwareAsyncConsumer(channel);
            consumer.Received += (_, eventArgs) => OnReceivedAsync(eventArgs, channel);
            var consumerTag = channel.BasicConsume(queue: _queueContext.QueueName,
                autoAck: false,
                consumer: consumer);
            
            // Handle application shutdown gracefully:
            cancellationToken.Register(() => CloseChannel(channel, consumerTag));
            return true;
        }
        catch (Exception e)
        {
			 
            _logger.LogError(e, "Failed to set up connection to RabbitMQ on host: {RabbitMqHostname}, port: {RabbitMqPort}, username: {RabbitMqUsername}, queue name: {QueueName}. Application will not receive updates from AR yet.",
	            _connectionFactory.HostName,
	            _connectionFactory.Port,
	            _connectionFactory.UserName,
	            _queueContext.QueueName);
            _connection?.Close();
            _connection?.Dispose();
            _connection = null;

            return false;
        }
    }

    private async Task OnReceivedAsync(BasicDeliverEventArgs eventArgs, IModel channel)
    {
        var success = true;
        try
        {
            var eventName = eventArgs.BasicProperties.Headers["eventName"];
            if (eventName.ToString()!.Contains("CommunicationPartyUpdated") || eventName.ToString()!.Contains("CommunicationPartyCreated"))
            {
				// A Communication Party has been created or updated, Fetch the latest version so we can send it to the EPJ
                var herId = Convert.ToInt32(eventArgs.BasicProperties.Headers["herId"]);
                var communicationParty = await _communicationPartyService.GetCommunicationPartyDetailsAsync(herId);
                
                //Filter based on a predefined list of herids. For cases where you only want to get information on specific herids
                //Example
                /* 
                int[] Herids = {2134566, 1231233, 5464645, 353555, 345345345};
                if (Herids.Contains(herId)) 
                {
                    var communicationParty = await _communicationPartyServiceAccessor.GetValidCommunicationPartyAsync(herId);
                }
                */

                //Implement connection to Healthcare system
                _healthCareSystem.CPUpdate(communicationParty);
            }
        }
        catch (Exception e)
        {
            success = false;
            _logger.LogError(e,
                "Error processing message from Rabbit with MessageId: {MessageId}, Delivery tag: {DeliveryTag}. Will send Nack and requeue the message.", 
                eventArgs.BasicProperties.MessageId, eventArgs.DeliveryTag);
        }

        try
        {
            if (success)
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
				success ? "Ack" : "Nack", eventArgs.BasicProperties.MessageId, eventArgs.DeliveryTag);
        }
    }

    private void CloseChannel(IModel channel, string consumerTag)
    {
        channel.BasicCancel(consumerTag);
        _connection.Close();
        _connection.Dispose();
        _connection = null;
    }
}