# Example for RabbitMq client setup
This is a example on how to setup a RabbitMq AMQP client and how to connect it to a queue.
How to use the events to sync updates to a healthcare system.

RabbitMq is set up with a stream (maybe queues also) where you will find the events `CommunicationPartyUpdated` and `CommunicationPartyCreated`.  
The AmqpQueueConsumer is configured to consume AMQP queues, this means that you have to use ServiceBusManagerV2 to retrieve the name of your AMQP queue.
We create a client for ServiceBusManagerV2 in Program.cs.

CommunicationPartyService is used to fetch data about the communicationParty when the consumer receives an event.

ArExportService, CommunicationPartyService, ServiceBusManagerServiceV2 and RabbitMq requires an OrgUsr in RegisterPlattformen.  
WSDL for [ArExportService](https://ws-web.test.nhn.no/v1/ARExport), [CommunicationPartyService](https://register-web.test.nhn.no/v1/AR) and [ServiceBusManagerServiceV2](https://register-web.test.nhn.no/v2/servicebusmanager)

### Initial population
The initial population job shows how you can do a first sync of communication parties. This only has to be done once (by supplying the command line flag `--initpop`), and can then be left out.
For convenience, two run profiles are included - one that runs the initial population and one that does not.

## Getting started:
* Acquire an OrgUsr in RegisterPlattformen
* Fill out the OrgUsr Username and Password in the `appsettings.json`
* Fill out SubscriptionIdentifier, this need to be unique for the user. If the user is used for multiple subscriptions then these need to have their own unique SubscriptionIdentifier 
* Make sure BusHostname is correct og that the BusPort is `5671` and BusSslEnabled is `true`
* Run the application `dotnet run`

## RabbitMq
### Information about RabbitMq
Information about RabbitMq and RabbitMq Clients in general. 
* [rabbitmq .net](https://www.rabbitmq.com/dotnet.html)

There is also examples for other languages(Java and Go).

## Queue client
Client for consuming AMQP queues and AMQP streams.
The example is found under BackgroundServices/AmqpQueueConsumer.
* [.net client and api](https://www.rabbitmq.com/dotnet-api-guide.html)