# Example for RabbitMq client setup
In order to keep a local copy of AdresseRegisteret up to date, there is a need to know about changes that happens to the communication parties. 
Therefore we publish events every time a communication party is created, or updated with new information. 

This repo contains examples of how to set up a consumer of these events - either from a stream, or from a dedicated AMQP-queue that NHN publishes to. 

The AmqpQueueConsumer is configured to consume AMQP queues, this means that you have to use [ServiceBusManagerV2](https://register-web.test.nhn.no/docs/api/NHN.DtoContracts.ServiceBus.Service.IServiceBusManagerV2.html) to retrieve the name of your AMQP queue.
We create a client for ServiceBusManagerV2 in Program.cs.

When an event is received, CommunicationPartyService is used to fetch updated data about the communication party. 
The client (your system) are expected to keep track of what communication parties you are interested in updating (if not all), as the queue will contain events for all communication parties in AddresseRegisteret.

## Requirements
In order to use ArExportService, CommunicationPartyService and to consume our the events that are published, we require an OrgUsr in RegisterPlattformen.
If you already have AMQP-queues on your organization, you will already have an OrgUser. In any case - contact our help desk to get username and password (kundesenter@nhn.no). 

### Contracts
WSDL for 
* [ArExportService](https://ws-web.test.nhn.no/v1/ARExport) (Used to get the initial copy of AR - will get the current version of all communication parties.)
* [CommunicationPartyService](https://ws-web.test.nhn.no/v1/AR) (Used to get updated data for individual communication parties.)
* [ServiceBusManagerV2](https://ws-web.test.nhn.no/v2/servicebusmanager)

### Initial population
The initial population job shows how you can do a first sync of communication parties. This only has to be done once (by supplying the command line flag `--initpop`), and can then be left out.
For convenience, two run profiles are included - one that runs the initial population and one that does not.

## Getting started:
* Acquire an OrgUsr in RegisterPlattformen (see Requirements above)
* Fill out the OrgUsr Username and Password in the `appsettings.json`
* Fill out SubscriptionIdentifier, this need to be unique for the user. If the user is used for multiple subscriptions then these need to have their own unique SubscriptionIdentifier 
* Make sure BusHostname is correct and that the BusPort is `5671` and BusSslEnabled is `true`
* Run the application `dotnet run`

## RabbitMq
### Information about RabbitMq
Information about RabbitMq and RabbitMq Clients in general. 
* [rabbitmq .net](https://www.rabbitmq.com/dotnet.html)

There is also examples for other languages(Java and Go).
