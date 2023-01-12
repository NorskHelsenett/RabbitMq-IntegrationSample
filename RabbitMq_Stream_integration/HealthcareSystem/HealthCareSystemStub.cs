using Microsoft.Extensions.Logging;

namespace RabbitMq_Stream_integration.HealthcareSystem;

public interface IHealthCareSystem
{
    void CommunicationPartyUpdated(CommunicationParty.CommunicationParty communicationParty);
}

/// <summary>
/// Dummy implementation of a health care system. Here, we should send the data to the actual health care system.
/// </summary>
public class HealthCareSystemStub : IHealthCareSystem
{
    private readonly ILogger<HealthCareSystemStub> _logger;

    public HealthCareSystemStub(ILogger<HealthCareSystemStub> logger)
    {
        _logger = logger;
    }

    public void CommunicationPartyUpdated(CommunicationParty.CommunicationParty communicationParty)
    {
        _logger.LogInformation("Health Care System received an update to communication party " + communicationParty.HerId);
    }
}