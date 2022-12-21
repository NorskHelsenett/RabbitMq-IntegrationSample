namespace RabbitMq_integration.Configuration;

public class ServiceBusManagerServiceSettings
{
    /// <summary>
    /// The URL to the service
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// The username used to access the service
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// The password used to access the service
    /// </summary>
    public string Password { get; set; }
}