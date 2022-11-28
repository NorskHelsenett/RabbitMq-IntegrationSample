using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace RabbitMq_integration.ArExportService;

public class CommunicationPartyExportService
{
    private readonly IARExportService _arExportService;
    private static byte[] _cachedData;
    private static object _syncRoot = new object();
    private static DateTime _lastLoad;
    private static readonly TimeSpan _lifeTime = TimeSpan.FromMinutes(15);
    private static readonly XmlWriterSettings XmlWriterSettings = new XmlWriterSettings
    {
        Encoding = Encoding.UTF8,
        Indent = true,
        OmitXmlDeclaration = false,
        CloseOutput = false
    };

    public CommunicationPartyExportService(IARExportService arExportService)
    {
        _arExportService = arExportService;
    }

    public Stream GetAllCommunicationPartiesXml()
    {
        try
        {
            MemoryStream ms;

            lock (_syncRoot)
            {
                if (_cachedData == null || _lastLoad + _lifeTime < DateTime.Now)
                {
                    using (ms = new MemoryStream())
                    {
                        var res = _arExportService.GetAllCommunicationPartiesXmlAsync();
                        using (var xmlWriter = XmlWriter.Create(ms, XmlWriterSettings))
                        {
                            var dcs = new DataContractSerializer(typeof(List<CommunicationParty.CommunicationParty>));
                            dcs.WriteObject(xmlWriter, res.Result);
                        }
                        _cachedData = ms.ToArray();
                        _lastLoad = DateTime.Now;
                    }
                }
                else
                {
                    Console.WriteLine("Cache hit.");
                }

                if (_cachedData == null)
                {
                    throw new InvalidOperationException("Error loading data");
                }

                ms = new MemoryStream(_cachedData);
            }

            return ms;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Cache generation failed with {0}", ex);
            throw;
        }
    }
}