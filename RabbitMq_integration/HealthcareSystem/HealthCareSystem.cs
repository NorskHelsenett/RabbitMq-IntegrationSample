using System;

namespace RabbitMq_integration.HealthcareSystem;

public interface IHealthCareSystem
{
    void CPUpdate(CommunicationParty.CommunicationParty communicationParty);
}

public class HealthCareSystem : IHealthCareSystem
{
	public HealthCareSystem()
    {
    }

    public void CPUpdate(CommunicationParty.CommunicationParty communicationParty)
    {
        Console.WriteLine("Test. Update to cpp");
    }
}