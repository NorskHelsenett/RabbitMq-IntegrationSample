namespace RabbitMq_integration.Configuration;

public class RegisterPlatformSettings
{
    /// <summary>
    /// Username from Registerplattformen. It has to a user with the OrgUsr prefix.
    /// </summary>
    public string RegisterUserName { get; set; }

    /// <summary>
    /// Password from Registerplattformen.
    /// </summary>
    public string RegisterPassword { get; set; }


    /// <summary>
    /// The URL to ServiceBusManager v2 in the relevant environment.
    /// </summary>
    public string ServiceBusManagerUrl { get; set; }

    /// <summary>
    /// Some unique identifier for this subscription, to ensure that the same subscription is kept across sessions.
    /// </summary>
    public string SubscriptionIdentifier { get; set; }


    /// <summary>
    /// RabbitMQ endpoint Host name
    /// </summary>
    public string BusHostname { get; set; }

    /// <summary>
    /// RabbitMQ endpoint Port number
    /// </summary>
    public int BusPort { get; set; }

    /// <summary>
    /// Enable SSL for connection to RabbitMQ
    /// </summary>
    public bool BusSslEnabled { get; set; }


    /// <summary>
    /// The URL to ArExportService in the relevant environment.
    /// </summary>
    public string ArExportServiceUrl { get; set; }
}