﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq_Stream_integration.CommunicationParty;
using RabbitMq_Stream_integration.Configuration;
using RabbitMq_Stream_integration.HealthcareSystem;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using Message = RabbitMQ.Stream.Client.Message;

namespace RabbitMq_Stream_integration.BackgroundServices;
// This example is based on version 1.0.0
public class RabbitStreamConsumer : BackgroundService
{
    private readonly ILogger _logger;
    private readonly ILogger<Consumer> _consumeLogger;
    private readonly RegisterPlatformSettings _settings;
    private readonly StreamSystemConfig _busStreamSystemConfig;
    private readonly ICommunicationPartyService _communicationPartyService;
    private readonly IHealthCareSystem _healthCareSystem;
    private readonly InitialPopulationData _initialPopulation;
    
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(25);
    private readonly Random rnd = new Random();
    
    public RabbitStreamConsumer(IOptions<RegisterPlatformSettings> settings, ICommunicationPartyService communicationPartyService, StreamSystemConfig busStreamSystemConfig, IHealthCareSystem healthCareSystem, ILogger<RabbitStreamConsumer> logger, ILogger<Consumer> consumeLogger, InitialPopulationData initialPopulation)
    {
        _logger = logger;
        _consumeLogger = consumeLogger;
        _initialPopulation = initialPopulation;
        _healthCareSystem = healthCareSystem;
        _busStreamSystemConfig = busStreamSystemConfig;
        _communicationPartyService = communicationPartyService;
        _settings = settings.Value;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartListeningToSubscriptionAsync(stoppingToken);
    }
    
    private async Task StartListeningToSubscriptionAsync(CancellationToken stoppingToken)
    {
        // Subscription is created asynchronously, and may not be ready yet, so we need retry logic
        while (!stoppingToken.IsCancellationRequested && !await TrySetupConsumer(stoppingToken))
        {
            await Task.Delay(_retryDelay, stoppingToken);
        }
    }
    
    /// <summary>
    /// Setting up stream consumer
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    private async Task<bool> TrySetupConsumer(CancellationToken stoppingToken)
    {
        StreamSystem? system = null;
        Consumer? consumer = null;
        
        try
        {
            // Connect to the broker and create the system object
            // the entry point for the client.
            // Create it once and reuse it.
            system = await StreamSystem.Create(_busStreamSystemConfig);

            // Name of the stream
            string stream = _settings.QueueName;

            // Reference for the connection, this need to be a unique id.
            string reference = _settings.SubscriptionIdentifier;

            int messagesConsumed = 0;
            ulong trackedOffset = 0;
            
            //Getting the offest from the a database or file.
            try
            {
                trackedOffset = await GetOffset();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not receive the offset");
            }

            // Create a consumer
            consumer = await Consumer.Create(
                new ConsumerConfig(system, stream)
                {
                    Reference = reference,
                    /*
                    Consume the stream from the offest recived
                    If the initial population job is started then we use timestamp to get the first message after the specified time.
                    Else we start with offset type first.
                    */
                    OffsetSpec = _initialPopulation.PerformInitialPopulation ? new OffsetTypeTimestamp((long)_initialPopulation.StartTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds) : new OffsetTypeOffset(trackedOffset),
                    // Receive the messages
                    MessageHandler = async (sourceStream, consumer, ctx, message) =>
                    {
                        //Storing offest after each 10 message is consumed.
                        if (++messagesConsumed % 10 == 0)
                        {
                            // HERE: Implement function for storing the offset.
                            _logger.LogInformation("Stored offset: {ctx.Offset}", ctx.Offset);
                        }

                        await HandleMessage(sourceStream, message, messagesConsumed);
                        await Task.CompletedTask;
                    }
                }, _consumeLogger);
            
            // Handle application shutdown gracefully:
            stoppingToken.Register(() => CloseChannel(system, consumer));
            return true;
        } catch (Exception e)
        {
            _logger.LogError(e, "Failed to set up connection to stream {_settings.QueueName} on RabbitMq. It may not have been created yet, will retry in a moment...", _settings.QueueName);
            await consumer?.Close()!;
            await system?.Close()!;
            return false;
        }
        
    }

    /// <summary>
    /// Handle the incoming message.
    /// </summary>
    /// <param name="sourceStream"></param>
    /// <param name="message"></param>
    private async Task HandleMessage(string sourceStream, Message message, int messagesConsumed)
    {
        var props = message.ApplicationProperties;
        var relevantEvents = new[] { "CommunicationPartyCreated", "CommunicationPartyUpdated" };
        if (relevantEvents.Contains(props["eventName"]))
        {
            // A Communication Party has been created or updated, Fetch the latest version so we can send it to the health care system
            
            var herId = props["herId"].ToString();
            Console.WriteLine("Read message: " + messagesConsumed + ", with herId: " + herId);
            
            int mseconds = rnd.Next(50,5000);
            await Task.Delay(mseconds); // Add random sleeptime to spread out load when a new event comes
            var communicationParty = await _communicationPartyService.GetCommunicationPartyDetailsAsync(Int32.Parse(herId!));

            // Send the update to the health care system
            _healthCareSystem.CommunicationPartyUpdated(communicationParty);
        }
    }

    private Task<ulong> GetOffset()
    {
        //Implement you own solution for getting the offset from your storage.
        ulong offset = 9000;
        return Task.FromResult(offset);
    }
    
    private void CloseChannel(StreamSystem system, Consumer consumer)
    {
        // Allow already received messages some time to process before we tear down the channel:
        Thread.Sleep(TimeSpan.FromSeconds(1));

        system?.Close();
        consumer?.Close();
    }
}