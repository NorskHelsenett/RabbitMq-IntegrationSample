# Examples for RabbitMq client setup
This repo contains examples on how to setup a RabbitMq client and how to connect it to a queue.
How to use the events to sync updates to a healthcare system.

RabbitMq is set up with a stream (maybe queues also) where you will find the events `CommunicationPartyUpdated` and `CommunicationPartyCreated`.  
The AmqpQueueConsumer is configured to consume AMQP queues, this means that you have to use ServiceBusManagerV2 to retrieve your AMQP queue name.
We create a client for ServiceBusManagerV2 in Program.cs.

The stream client is not dependent on ServiceBuManagerV2 as it only consume streams. All clients can connect to the same stream, meaning that the name of a stream will be predefined.

CommunicationPartyService is used to fetch data about the communicationParty when the consumer receives an event.

CommunicationPartyService, ServiceBusManagerV2 and RabbitMq requires an QrgUsr in RegisterPlattformen.

# Initial population
The initial population job shows how you can do a first sync of communication parties. This only has to be done once (by supplying the command line flag "initpop"), and can then be left out.
For convenience, two run profiles are included - one that runs the initial population and one that does not.

## Getting started:
* Aquire an OrgUsr in RegisterPlattformen
* Fill out the OrgUsr Username and Password in the `appsettings.json`
* Fill out SubscriptionIdentifier with the OrgUsr Username this needs to be unique, so it is best to use your OrgUsr Username
* Make sure BusHostname is correct og that the BusPort is `5671` and BusSslEnabled is `true`
* Run the application

## RabbitMq
### Information about RabbitMq
Information about RabbitMq and RabbitMq Clients in general. 
* [rabbitmq .net](https://www.rabbitmq.com/dotnet.html)

There is also examples for other languages(Java and Go).

## Queue client
Client for consuming AMQP queues and AMQP streams.
The example is found under BackgroundServices/AmqpQueueConsumer.
* [.net client and api](https://www.rabbitmq.com/dotnet-api-guide.html)

## Stream client
Client for consuming streams. This example is work in progress.
* [.net stream client](https://github.com/rabbitmq/rabbitmq-stream-dotnet-client)

