using System;
using Microsoft.Extensions.Logging;

namespace InitialPopulation.HealthcareSystem;

public interface IHealthCareSystem
{
	void CommunicationPartyUpdate(CommunicationParty.CommunicationParty cp);
}

public class HealthCareSystem : IHealthCareSystem
{
	private readonly ILogger<HealthCareSystem> _logger;

	public HealthCareSystem(ILogger<HealthCareSystem> logger)
	{
		_logger = logger;
	}

	public void CommunicationPartyUpdate(CommunicationParty.CommunicationParty cp)
	{
		_logger.LogInformation("Initial population: Received communication party: {HerId}", cp.HerId);
	}
}