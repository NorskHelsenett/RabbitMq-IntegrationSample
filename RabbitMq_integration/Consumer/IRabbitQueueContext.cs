﻿namespace RabbitMq_integration.Consumer;

/// <summary>
/// A place to store the queue that is returned from ServiceBusManager v2 API, so that we know which queue to listen to later.
/// </summary>
public interface IRabbitQueueContext
{
    string QueueName { get; set; }
}