// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using InitialPopulation.ArExportService;
using InitialPopulation.HealthcareSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMq_integration.ArExportService;

namespace InitialPopulation;

class Program
{
    static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                //See InitialPopulation to see how to do a initial population of your health care system
                services.AddSingleton<IARExportService>(CreateArExportService);
                services.AddSingleton<IHealthCareSystem, HealthCareSystem>();
                services.AddScoped<CommunicationPartyExportService>();
                services.AddHostedService<InitialPopulationJob>();
            })
            .Build();
        host.Run();
    }

    private static IARExportService CreateArExportService(IServiceProvider provider)
    {
	    var client = new ARExportServiceClient(
		    ARExportServiceClient.EndpointConfiguration.BasicHttpBinding_IARExportService,
		    "https://ws-web.test.nhn.no/v1/ARExport");
	    
	    var basicHttpBinding = (BasicHttpBinding)client.Endpoint.Binding;
		// This must be set in order to properly stream the response instead of downloading everything to memory first:
		basicHttpBinding.TransferMode = TransferMode.StreamedResponse;
		// Increase timeouts to allow server to build the response:
	    basicHttpBinding.ReceiveTimeout = TimeSpan.FromMinutes(10);
	    basicHttpBinding.CloseTimeout = TimeSpan.FromMinutes(10);
	    basicHttpBinding.OpenTimeout = TimeSpan.FromMinutes(10);
	    basicHttpBinding.SendTimeout = TimeSpan.FromMinutes(10);

	    client.ClientCredentials.UserName.UserName = ""; // Register platform user
	    client.ClientCredentials.UserName.Password = "";
	    return client;
    }
}

internal class InitialPopulationJob :BackgroundService
{
    private readonly CommunicationPartyExportService _communicationPartyExportService;

    private readonly IHealthCareSystem _healthCareSystem;
    private readonly ILogger<InitialPopulationJob> _logger;

    public InitialPopulationJob(CommunicationPartyExportService communicationPartyExportService, IHealthCareSystem healthCareSystem, ILogger<InitialPopulationJob> logger)
    {
	    _communicationPartyExportService = communicationPartyExportService;
	    _healthCareSystem = healthCareSystem;
	    _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
		_logger.LogInformation("Initial population start...");
	    await SyncAllCommunicationPartiesAsync(stoppingToken);
	    _logger.LogInformation("Initial population end...");
    }

    private async Task SyncAllCommunicationPartiesAsync(CancellationToken stoppingToken)
    {
	    var comParties = _communicationPartyExportService.GetAllCommunicationPartiesXmlAsync(stoppingToken);
	 
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
}
