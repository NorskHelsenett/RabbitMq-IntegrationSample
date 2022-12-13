using System.Runtime.Serialization;
using System.Xml;
using InitialPopulation.ArExportService;

namespace RabbitMq_integration.ArExportService;

public class CommunicationPartyExportService
{
    private readonly IARExportService _arExportService;
    private static byte[] _cachedData;
    private static DateTime _lastLoad;

    public CommunicationPartyExportService(IARExportService arExportService)
    {
        _arExportService = arExportService;
    }

    public List<InitialPopulation.CommunicationParty.CommunicationParty>? GetAllCommunicationPartiesXml()
    {
        try
        {
            var res = _arExportService.GetAllCommunicationPartiesXmlAsync().GetAwaiter().GetResult();
            var dcs = new DataContractSerializer(typeof(List<InitialPopulation.CommunicationParty.CommunicationParty>));
            List<InitialPopulation.CommunicationParty.CommunicationParty> result;
            using (var xmlReader = XmlReader.Create(res))
            {
                result = dcs.ReadObject(xmlReader) as List<InitialPopulation.CommunicationParty.CommunicationParty>;
            }
            return result; 
        }
        catch (Exception ex)
        {
            Console.WriteLine("Cache generation failed with {0}", ex);
            throw;
        }
    }
}