namespace InitialPopulation.HealthcareSystem;

public interface IHealthCareSystem
{
    void CPUpdate(CommunicationParty.CommunicationParty cp);
}

public class HealthCareSystem : IHealthCareSystem
{
    public HealthCareSystem()
    {
    }

    public void CPUpdate(CommunicationParty.CommunicationParty cp)
    {
        Console.WriteLine("Test. Update to cpp");
    }
}