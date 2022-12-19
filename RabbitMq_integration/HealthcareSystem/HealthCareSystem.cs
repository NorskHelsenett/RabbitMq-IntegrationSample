using System;

namespace RabbitMq_integration.HealthcareSystem;

public interface IHealthCareSystem
{
    void CPUpdate(CommunicationParty.CommunicationParty communicationParty);
}

public class HealthCareSystem : IHealthCareSystem
{
    private readonly CommunicationParty.CommunicationParty _communicationParty;
    public HealthCareSystem(CommunicationParty.CommunicationParty communicationParty)
    {
        _communicationParty = communicationParty;
    }

    public void CPUpdate(CommunicationParty.CommunicationParty communicationParty)
    {
        Console.WriteLine("Test. Update to cpp");
    }
}