// See https://aka.ms/new-console-template for more information

using System.Runtime.Serialization;
using InitialPopulation.ArExportService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMq_integration.ArExportService;

namespace InitialPopulation;

class Program
{
    static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                //See InitialPopulation to see how to do a initial population of your healthcaresystem
                services.AddSingleton<IARExportService>(CreateARExportService);
                services.AddScoped<CommunicationPartyExportService>();
                services.AddHostedService<InitialPopulationJob>();
            })
            .Build();
        host.Run();
    }

    private static IARExportService CreateARExportService(IServiceProvider provider)
    {
        System.ServiceModel.BasicHttpBinding binding = new System.ServiceModel.BasicHttpBinding();
        binding.MaxBufferSize = int.MaxValue;
        binding.ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max;
        binding.MaxReceivedMessageSize = int.MaxValue;
        binding.AllowCookies = true;
        binding.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;
        binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Basic;
        binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
        binding.CloseTimeout = new TimeSpan(0, 10, 0);
        binding.OpenTimeout = new TimeSpan(0, 10, 0);
        binding.SendTimeout = new TimeSpan(0, 10, 0);
        
        var client = new ARExportServiceClient(binding, "https://ws.dev.nhn.no/v1/ARExport");
        client.ClientCredentials.UserName.UserName = "";
        client.ClientCredentials.UserName.Password = "";
        return client;
    }
}

internal class InitialPopulationJob : IHostedService
{
    private readonly CommunicationPartyExportService _communicationPartyExportService;

    private static readonly DataContractSerializer ComPartyListSerializer =
        new(typeof(List<InitialPopulation.CommunicationParty.CommunicationParty>));

    //private readonly IHealthCareSystem _healthCareSystem;

    public InitialPopulationJob(CommunicationPartyExportService communicationPartyExportService)
    {
        _communicationPartyExportService = communicationPartyExportService;
        //_healthCareSystem = healthcareSystem;
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
            var data = _communicationPartyExportService.GetAllCommunicationPartiesXml();
            foreach (var cp in data)
            {
                //_healthCareSystem.CPUpdate(cp);
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong when populating Healthcare system with Cpp/a");
            Console.WriteLine(e.Message);
        }
    }
}
