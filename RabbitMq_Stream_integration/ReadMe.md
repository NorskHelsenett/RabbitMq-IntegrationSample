# Example for RabbitMq stream client setup
In order to keep a local copy of AdresseRegisteret up to date, there is a need to know about changes that happens to the communication parties. 
Therefore we publish events every time a communication party is created, or updated with new information. 

This repo contains examples of how to set up a consumer of these events - either from a stream, or from a dedicated AMQP-queue that NHN publishes to. 

This is a example on how to setup a RabbitMq stream client and how to connect it to a stream, and further how to use the events to sync updates to a local copy of AddresseRegisteret.

AddresseRegisteret publishes changes to a stream where you will find the events `CommunicationPartyUpdated` and `CommunicationPartyCreated`. 
All clients will connect to the same stream to consume the events, meaning that the stream name will be predefined.
The name is found in the configuration `QueueName`. 

The events on the stream are published with timestamp set with `DateTime.UtcNow`, so does not have any local time. 
When an event is received, CommunicationPartyService is used to fetch updated data about the communication party. 
The client (your system) are expected to keep track of what communication parties you are interested in updating (if not all), as the stream will contain events for all communication parties in AddresseRegisteret.

## Requirements
In order to use ArExportService, CommunicationPartyService and to consume our the events that are published, we require an OrgUsr in RegisterPlattformen.
If you already have AMQP-queues on your organization, you will already have an OrgUser. In any case - contact our help desk to get username and password (kundesenter@nhn.no). 

### Contracts
WSDL for 
* [ArExportService](https://ws-web.test.nhn.no/v1/ARExport) (Used to get the initial copy of AR - will get the current version of all communication parties.)
* [CommunicationPartyService](https://ws-web.test.nhn.no/v1/AR) (Used to get updated data for individual communication parties.)

### Initial population
The initial population job shows how you can do a first sync of communication parties. This only has to be done once (by supplying the command line flag `"initpop"`), and can then be left out.
For convenience, two run profiles are included - one that runs the initial population and one that does not.

## Getting started
* Acquire an OrgUsr in RegisterPlattformen (see Requirements above)
* Fill out the OrgUsr Username and Password in the `appsettings.json`
* Fill out SubscriptionIdentifier, this need to be unique for the user. If the user is used for multiple subscriptions then these need to have their own unique SubscriptionIdentifier 
* Make sure BusHostname is correct and that the BusPort is `5551` and BusSslEnabled is `true`
* Run the application `dotnet run` or (`dotnet run "initpop"` the first time) 


## Additional information
### Offset tracking
In order to keep track of what events you have already consumed from the queue (and where in the stream you need to pick up again to get up to date), we use offset tracking. 
It is possible to use a different offset type than what is used in the example, where OffsetTypeTimestamp is used for the initial population, otherwise OffsetTypeOffset.
The stream client supports OffsetTypeFirst, OffsetTypeLast, OffsetTypeNext, OffsetTypeOffset and OffsetTypeTimestamp.
Serverside Offset tracking is not supported as it requires writing access to the stream.
You need to implement your own solution for storing the message offset when a message is consumed. The example shows you how to do it if you want to store after every 10. message.
More information about the different [offset types](https://github.com/rabbitmq/rabbitmq-stream-dotnet-client#offset-types).

### Information about RabbitMq
Information about RabbitMq and RabbitMq Clients in general. 
* [rabbitmq .net](https://www.rabbitmq.com/dotnet.html)

There is also examples for other languages(Java and Go).

### Stream client
Client for consuming streams. This client is work in progress.
* [.net stream client](https://github.com/rabbitmq/rabbitmq-stream-dotnet-client)


