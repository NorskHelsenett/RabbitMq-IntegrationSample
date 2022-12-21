namespace RabbitMq_integration.Configuration;

public class RabbitMqClientSettings
{
	/// <summary>
	/// Some unique identifier for this subscription, to ensure that the same subscription is used across sessions.
	/// </summary>
	public string SubscriptionIdentifier { get; set; }

	/// <summary>
	/// RabbitMQ endpoint Host name
	/// </summary>
	public string EndpointHostname { get; set; }

	/// <summary>
	/// RabbitMQ endpoint Port number
	/// </summary>
	public int Port { get; set; }

	/// <summary>
	/// RabbitMQ Username
	/// </summary>
	public string Username { get; set; }

	/// <summary>
	/// RabbitMQ Password
	/// </summary>
	public string Password { get; set; }

	/// <summary>
	/// Enable SSL
	/// </summary>
	public bool SslEnabled { get; set; }
}
