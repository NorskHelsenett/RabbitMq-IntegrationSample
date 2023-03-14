# RabbitMq-IntegrationSample
In order to keep a local copy of AdresseRegisteret up to date, there is a need to know about changes that happens to the communication parties. 
Therefore we publish events every time a communication party is created, or updated with new information. 

This repo contains examples of how to set up a consumer (stream or AMQP-client) of these events - either from a stream, or from a dedicated AMQP-queue that NHN publishes to. 

These examples are proof of consept. The examples are tested and they "work on my machine". 
It is your responsibility to make it work in your environment. We have created ReadMe's that should get you started, and hopefully this in addition to documentation about RabbitMq will be of good use. 

## RabbitMq
### Information about RabbitMq
Information about RabbitMq and RabbitMq Clients in general. 
* [rabbitmq .net](https://www.rabbitmq.com/dotnet.html)

There is also examples for other languages(Java and Go).
