using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;

namespace RabbitMq_integration.Stream_Client;
// Work in progress - version 1.0.0 of the stream client is not released. This example is based on version 1.0.0-rc.8
public class RabbitStreamConsumer
{
    public static async Task Start()
    {
        // How to get ip from URL
        IPAddress ipAddress = (await Dns.GetHostEntryAsync("www.nhn.no")).AddressList[0];
        
        var config = new StreamSystemConfig
        {
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            Endpoints = new List<EndPoint> {new IPEndPoint(ipAddress, 5552)},
            Ssl = new SslOption()
            {
                Enabled = true
            }
        };
        // Connect to the broker and create the system object
        // the entry point for the client.
        // Create it once and reuse it.
        var system = await StreamSystem.Create(config);
        
        // Name of the stream
        const string stream = "my_first_stream";
        
        // Refeerence for the connection, this need to be a unique id.
        const string reference = "my_consumer";
        
        //Getting the offest from the server.
        var trackedOffset = await system.QueryOffset(reference, stream);
        int messagesConsumed = 0;
        
        // Create a consumer
        var consumer = await RabbitMQ.Stream.Client.Reliable.Consumer.Create(
            new ConsumerConfig(system, stream)
            {
                Reference = reference,
                // Consume the stream from the offest from the server
                OffsetSpec = new OffsetTypeOffset(trackedOffset),
                // Receive the messages
                MessageHandler = async (sourceStream, consumer, ctx, message) =>
                {
                    //Storing offest after each 100 message is consumed.
                    if (++messagesConsumed % 100 == 0)
                    {
                        await consumer.StoreOffset(ctx.Offset);
                    }
                    Console.WriteLine(
                        $"message: coming from {sourceStream} data: {Encoding.Default.GetString(message.Data.Contents.ToArray())} - consumed");
                    await Task.CompletedTask;
                }
            });
        Console.WriteLine($"Press to stop");
        Console.ReadLine();

        await consumer.Close();
        await system.Close();
    }
}