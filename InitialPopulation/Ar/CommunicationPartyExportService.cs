using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using InitialPopulation.ArExportService;

namespace RabbitMq_integration.ArExportService;

public class CommunicationPartyExportService
{
    private readonly IARExportService _arExportService;

    public CommunicationPartyExportService(IARExportService arExportService)
    {
        _arExportService = arExportService;
    }

    public async IAsyncEnumerable<InitialPopulation.CommunicationParty.CommunicationParty>
	    GetAllCommunicationPartiesXmlAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
	    Stream? res = await _arExportService.GetAllCommunicationPartiesXmlAsync();
	    Console.WriteLine("Done downloading");

	    var cpSerializer =
		    new DataContractSerializer(typeof(InitialPopulation.CommunicationParty.CommunicationParty));

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
				    (InitialPopulation.CommunicationParty.CommunicationParty) cpSerializer.ReadObject(
					    xmlReader)!;

			    yield return communicationParty;
		    }
	    }
    }
}