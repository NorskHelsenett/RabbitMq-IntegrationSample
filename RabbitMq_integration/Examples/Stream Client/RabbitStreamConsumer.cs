using System.Buffers;
using System.Net;
using System.Text;
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
        
        // Refeerence for the connection.
        const string reference = "my_consumer";
        
        // Create a consumer
        var trackedOffset = await system.QueryOffset(reference, stream);
        var consumer = await RabbitMQ.Stream.Client.Reliable.Consumer.Create(
            new ConsumerConfig(system, stream)
            {
                Reference = reference,
                // Consume the stream from the beginning 
                // See also other OffsetSpec 
                OffsetSpec = new OffsetTypeOffset(trackedOffset),
                // Receive the messages
                MessageHandler = async (sourceStream, consumer, ctx, message) =>
                {
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