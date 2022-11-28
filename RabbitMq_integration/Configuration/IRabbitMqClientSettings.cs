namespace RabbitMq_integration.Configuration;

public interface IRabbitMqClientSettings
{
    /// <summary>
    /// Defines if a subscription should be set up, and messages should be read from the Rabbit MQ queue.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Some unique identifier for this subscription, to ensure that the same subscription is used across sessions.
    /// </summary>
    string SubscriptionIdentifier { get; set; }

    /// <summary>
    /// RabbitMQ endpoint Host name
    /// </summary>
    string EndpointHostname { get; set; }

    /// <summary>
    /// RabbitMQ endpoint Port number
    /// </summary>
    int Port { get; set; }

    /// <summary>
    /// RabbitMQ Username
    /// </summary>
    string Username { get; set; }

    /// <summary>
    /// RabbitMQ Password
    /// </summary>
    string Password { get; set; }

    /// <summary>
    /// Enable SSL
    /// </summary>
    bool SslEnabled { get; set; }
}