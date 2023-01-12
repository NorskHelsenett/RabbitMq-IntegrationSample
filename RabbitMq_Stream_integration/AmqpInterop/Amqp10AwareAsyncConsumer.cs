using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMq_Stream_integration.AmqpInterop
{
    public class Amqp10AwareAsyncConsumer : AsyncEventingBasicConsumer
    {
        public Amqp10AwareAsyncConsumer(IModel model) : base(model)
        {
        }

        public override Task HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered,
            string exchange, string routingKey, IBasicProperties properties,
            ReadOnlyMemory<byte> body)
        {
            AmqpPropertyMapper.MapMissingAmqp10PropertiesToHeaders(properties);
            AmqpPropertyMapper.MapFromAmqp10AppPropertiesToHeaders(properties);

            return base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties,
                body);
        }
    }
}