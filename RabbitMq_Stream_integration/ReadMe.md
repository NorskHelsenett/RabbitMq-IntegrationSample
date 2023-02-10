# Example for RabbitMq stream client setup
This is a example on how to setup a RabbitMq stream client and how to connect it to a stream.
How to use the events to sync updates to a healthcare system.

RabbitMq is set up with a stream (maybe queues also) where you will find the events `CommunicationPartyUpdated` and `CommunicationPartyCreated`. 
All clients connects to the same stream, meaning that the stream name will be predefined.

The publish timestamp on the messages in the stream is set with `DateTime.UtcNow`.

CommunicationPartyService is used to fetch data about the communicationParty when the consumer receives an event.

ArExportService and CommunicationPartyService and RabbitMq requires an OrgUsr in RegisterPlattformen.  
WSDL for [ArExportService](https://ws-web.test.nhn.no/v1/ARExport) and [CommunicationPartyService](https://register-web.test.nhn.no/v1/AR)

### Initial population
The initial population job shows how you can do a first sync of communication parties. This only has to be done once (by supplying the command line flag `--initpop`), and can then be left out.
For convenience, two run profiles are included - one that runs the initial population and one that does not.

## Getting started
* Acquire an OrgUsr in RegisterPlattformen
* Fill out the OrgUsr Username and Password in the `appsettings.json`
* Fill out SubscriptionIdentifier, this need to be unique for the user. If the user is used for multiple subscriptions then these need to have their own unique SubscriptionIdentifier 
* Make sure BusHostname is correct and that the BusPort is `5551` and BusSslEnabled is `true`
* Run the application `dotnet run`

## RabbitMq
### Information about RabbitMq
Information about RabbitMq and RabbitMq Clients in general. 
* [rabbitmq .net](https://www.rabbitmq.com/dotnet.html)

There is also examples for other languages(Java and Go).

## Stream client
Client for consuming streams. This client is work in progress.
* [.net stream client](https://github.com/rabbitmq/rabbitmq-stream-dotnet-client)

### Offset tracking
It is possible to use a different offset type then what we have in the example. We use OffsetTypeTimestamp if there is no offset stored on the server for the SubscriptionIdentifier.
After the client has read a message the offset will be stored on the server.
The stream client supports OffsetTypeFirst, OffsetTypeLast, OffsetTypeNext, OffsetTypeOffset and OffsetTypeTimestamp.
More information about the different [offset types](https://github.com/rabbitmq/rabbitmq-stream-dotnet-client#offset-types).

