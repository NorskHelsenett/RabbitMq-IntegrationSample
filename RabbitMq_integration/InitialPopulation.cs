using System.Runtime.Serialization;
using Microsoft.Extensions.Hosting;
using RabbitMq_integration.ArExportService;
using RabbitMq_integration.HealthcareSystem;

namespace RabbitMq_integration;

public class InitialPopulation : IHostedService
{
    private readonly CommunicationPartyExportService _communicationPartyExportService;

    private static readonly DataContractSerializer ComPartyListSerializer =
        new(typeof(List<CommunicationParty.CommunicationParty>));

    private readonly IHealthCareSystem _healthCareSystem;

    public InitialPopulation(CommunicationPartyExportService communicationPartyExportService,
        IHealthCareSystem healthcareSystem)
    {
        _communicationPartyExportService = communicationPartyExportService;
        _healthCareSystem = healthcareSystem;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting InitialPopulation....");
        SyncAllCommunicationParties();
        return StopAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Stopping InitialPopulation...");
        return Task.CompletedTask;
    }

    private void SyncAllCommunicationParties()
    {
        try
        {
            using (var stream = _communicationPartyExportService.GetAllCommunicationPartiesXml())
            {
                var obj = ComPartyListSerializer.ReadObject(stream) as List<CommunicationParty.CommunicationParty>;
                foreach (var cp in obj)
                {
                    _healthCareSystem.CPUpdate(cp);
                }
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong when populating Healthcare system with Cpp/a");
            Console.WriteLine(e.Message);
        }
    }
}