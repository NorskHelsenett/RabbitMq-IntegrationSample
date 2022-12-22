using System;
using Microsoft.Extensions.Logging;

namespace RabbitMq_integration.HealthcareSystem;

public interface IHealthCareSystem
{
    void CPUpdate(CommunicationParty.CommunicationParty communicationParty);
}

public class HealthCareSystem : IHealthCareSystem
{
	private readonly ILogger<HealthCareSystem> _logger;

	public HealthCareSystem(ILogger<HealthCareSystem> logger)
	{
		_logger = logger;
	}

    public void CPUpdate(CommunicationParty.CommunicationParty communicationParty)
    {
	    _logger.LogInformation("Health Care System received an update to communication party " + communicationParty.HerId);
    }
}