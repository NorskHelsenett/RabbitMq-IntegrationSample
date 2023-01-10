using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using InitialPopulation.ArExportService;
using InitialPopulation.HealthcareSystem;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InitialPopulation;

internal class InitialPopulationJob :BackgroundService
{
	private readonly IHealthCareSystem _healthCareSystem;
	private readonly ILogger<InitialPopulationJob> _logger;
	private readonly IARExportService _arExportService;

	public InitialPopulationJob(IHealthCareSystem healthCareSystem, ILogger<InitialPopulationJob> logger, IARExportService arExportService)
	{
		_healthCareSystem = healthCareSystem;
		_logger = logger;
		_arExportService = arExportService;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Initial population start...");
		await SyncAllCommunicationPartiesAsync(stoppingToken);
		_logger.LogInformation("Initial population end...");
	}

	private async Task SyncAllCommunicationPartiesAsync(CancellationToken stoppingToken)
	{
		var comParties = StreamAllCommunicationParties(stoppingToken);

		await foreach (var comParty in comParties.WithCancellation(stoppingToken))
		{
			try
			{
				_healthCareSystem.CommunicationPartyUpdate(comParty);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Could not run initial population on communication party {HerId}", comParty.HerId);
			}
		}
	}

	private async IAsyncEnumerable<CommunicationParty.CommunicationParty> StreamAllCommunicationParties([EnumeratorCancellation] CancellationToken stoppingToken)
	{
		Stream? res = await _arExportService.GetAllCommunicationPartiesXmlAsync();
		_logger.LogDebug("Stream established");
		
		var cpSerializer =
			new DataContractSerializer(typeof(InitialPopulation.CommunicationParty.CommunicationParty));

		var xmlReaderSettings = new XmlReaderSettings
		{
			Async = true

		};
		using var xmlReader = XmlReader.Create(res, xmlReaderSettings);
		await xmlReader.MoveToContentAsync();

		while (await xmlReader.ReadAsync() && stoppingToken.IsCancellationRequested == false)
		{
			if (xmlReader.NodeType == XmlNodeType.Element)
			{
				var communicationParty =
					(InitialPopulation.CommunicationParty.CommunicationParty)cpSerializer.ReadObject(
						xmlReader)!;

				yield return communicationParty;
			}
		}
	}
}