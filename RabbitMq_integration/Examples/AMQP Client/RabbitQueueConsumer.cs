using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMq_integration.Ar;
using RabbitMq_integration.Configuration;
using RabbitMq_integration.HealthcareSystem;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMq_integration.Examples.AMQP_Client;

public class RabbitQueueConsumer : BackgroundService
{
    private readonly string _queueName;
    private readonly ConnectionFactory _connectionFactory;
    private readonly CommunicationPartyServiceAccessor _communicationPartyServiceAccessor;
    private readonly IHealthCareSystem _healthCareSystem;
    private IConnection _connection;
    
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(60);
    
    public RabbitQueueConsumer(IOptions<RabbitMqClientSettings>  settings, IOptions<CommunicationPartyServiceAccessor> communicationPartyServiceAccessor, IOptions<IHealthCareSystem> healthCareSystem)
    {
        _queueName = settings.Value.QueueName;
        _communicationPartyServiceAccessor = communicationPartyServiceAccessor.Value;
        _healthCareSystem = healthCareSystem.Value;
        _connectionFactory = new ConnectionFactory
        {
            HostName = settings.Value.EndpointHostname,
            UserName = settings.Value.Username,
            Password = settings.Value.Password,
            Port = settings.Value.Port,
            Ssl = new SslOption
            {
                Enabled = settings.Value.SslEnabled,
                ServerName = settings.Value.EndpointHostname
            },
            ClientProvidedName = settings.Value.SubscriptionIdentifier,
            DispatchConsumersAsync = true,
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool success;
        do
        {
            success = Consumer(stoppingToken);
            if (!success)
            {
                await Task.Delay(RetryDelay, stoppingToken);
            }
        } while (!success && !stoppingToken.IsCancellationRequested);
    }
    
    private bool Consumer(CancellationToken cancellationToken)
    {
        try
        {
            _connection = _connectionFactory.CreateConnection();
            IModel channel = _connection.CreateModel();
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, eventArgs) => OnReceivedAsync(eventArgs, channel);
            var consumerTag = channel.BasicConsume(queue: _queueName,
                autoAck: false,
                consumer: consumer);
            
            // Handle application shutdown gracefully:
            cancellationToken.Register(() => CloseChannel(channel, consumerTag));
            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
            return true;
        }
        catch (Exception e)
        {

            Console.WriteLine("Failed to set up connection to RabbitMQ on host: {RabbitMqHostname}, port: {RabbitMqPort}, username: {RabbitMqUsername}, queue name: {QueueName}. Application will not receive updates from AR yet. Retry in {RetryDelay}.");
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
            if (eventName.ToString()!.Contains("AddressRegister.CommunicationPartyUpdated") || eventName.ToString()!.Contains("AddressRegister.CommunicationPartyCreated"))
            {
                var herId = Convert.ToInt32(eventArgs.BasicProperties.Headers["herId"]);
                
                //Get communicationparty based on herid
                var communicationParty = await _communicationPartyServiceAccessor.GetValidCommunicationPartyAsync(herId);
                
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
            Console.WriteLine(
                "Error processing message from Rabbit with MessageId: {MessageId}, Delivery tag: {DeliveryTag}. Will sleep a bit, then send Nack and requeue the message.");
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
            Console.WriteLine("Could not send {Signal} for MessageId: {MessageId}, Delivery tag: {DeliveryTag}. Message will time out eventually.");
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