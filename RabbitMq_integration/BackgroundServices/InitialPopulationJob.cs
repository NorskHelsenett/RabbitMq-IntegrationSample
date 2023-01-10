using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using ArExportService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq_integration.Configuration;
using RabbitMq_integration.HealthcareSystem;

namespace RabbitMq_integration.BackgroundServices;

internal class InitialPopulationJob : IHostedService
{
    private readonly IHealthCareSystem _healthCareSystem;
    private readonly ILogger<InitialPopulationJob> _logger;
    private readonly IARExportService _arExportService;

    public InitialPopulationJob(IHealthCareSystem healthCareSystem, IARExportService arExportService,
        IOptions<RegisterPlatformSettings> options, ILogger<InitialPopulationJob> logger)
    {
        _healthCareSystem = healthCareSystem;
        _logger = logger;
        _arExportService = arExportService;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PerformInitialPopulation flag is set to true. Initial population started. Setting up the stream from the Address Registry could take a few minutes...");
        await SyncAllCommunicationPartiesAsync(cancellationToken);
        _logger.LogInformation("Initial population end...");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task SyncAllCommunicationPartiesAsync(CancellationToken cancellationToken)
    {
        var comParties = StreamAllCommunicationParties(cancellationToken);

        await foreach (var comParty in comParties.WithCancellation(cancellationToken))
        {
            try
            {
                _healthCareSystem.CommunicationPartyUpdated(comParty);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not run initial population on communication party {HerId}", comParty.HerId);
            }
        }
    }

    private async IAsyncEnumerable<CommunicationParty.CommunicationParty> StreamAllCommunicationParties([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Stream? res = await _arExportService.GetAllCommunicationPartiesXmlAsync();
        _logger.LogDebug("Stream established");
        
        var cpSerializer =
            new DataContractSerializer(typeof(CommunicationParty.CommunicationParty));

        var xmlReaderSettings = new XmlReaderSettings
        {
            Async = true

        };
        using var xmlReader = XmlReader.Create(res, xmlReaderSettings);
        await xmlReader.MoveToContentAsync();

        while (await xmlReader.ReadAsync() && cancellationToken.IsCancellationRequested == false)
        {
            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                var communicationParty =
                    (CommunicationParty.CommunicationParty)cpSerializer.ReadObject(
                        xmlReader)!;

                yield return communicationParty;
            }
        }
    }
}