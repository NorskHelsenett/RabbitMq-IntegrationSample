// See https://aka.ms/new-console-template for more information

using System.ServiceModel;
using InitialPopulation.ArExportService;
using InitialPopulation.HealthcareSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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