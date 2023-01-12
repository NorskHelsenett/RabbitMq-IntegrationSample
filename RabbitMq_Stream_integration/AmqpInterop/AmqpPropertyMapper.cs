using Amqp.Framing;
using Amqp.Types;
using RabbitMQ.Client;

namespace RabbitMq_Stream_integration.AmqpInterop
{
    public static class AmqpPropertyMapper
    {
        static AmqpPropertyMapper()
        {
            try
            {
                // AMQP 1.0 descriptor for message properties:
                // https://docs.oasis-open.org/amqp/core/v1.0/os/amqp-core-messaging-v1.0-os.html#type-properties
                var propDescriptor = new Descriptor(0x0000000000000073, "amqp:properties:list");
                Encoder.AddKnownDescribed(propDescriptor, () => new Properties());
            }
            catch (ArgumentException)
            {
                // The descriptor must already be added to Encoder by another library - not a problem.
            }

            try
            {
                // AMQP 1.0 descriptor for application properties:
                // https://docs.oasis-open.org/amqp/core/v1.0/os/amqp-core-messaging-v1.0-os.html#type-application-properties
                var appPropDescriptor = new Descriptor(0x0000000000000074, "amqp:application-properties:map");
                Encoder.AddKnownDescribed(appPropDescriptor, () => new ApplicationProperties());
            }
            catch (ArgumentException)
            {
                // The descriptor must already be added to Encoder by another library - not a problem.
            }
        }

        public static void MapMissingAmqp10PropertiesToHeaders(IBasicProperties basicProperties)
        {
            if (basicProperties.Headers == null
                || !basicProperties.Headers.TryGetValue(AmqpConstants.AMQP10_PROPERTIES_KEY, out var bytesObj)
                || !(bytesObj is byte[] bytes)) return;

            var properties = AmqpEncoder.DecodeObject<Properties>(bytes);
            if (properties == null) return;

            AddToHeaderIfNotExistAlready(basicProperties, AmqpConstants.AMQP10_PROPERTIES_TO_KEY, properties.To);
            AddToHeaderIfNotExistAlready(basicProperties, AmqpConstants.AMQP10_PROPERTIES_SUBJECT_KEY, properties.Subject);
            AddToHeaderIfNotExistAlready(basicProperties, AmqpConstants.AMQP10_PROPERTIES_ABSOLUTEEXPIRTYTIME_KEY, properties.AbsoluteExpiryTime);

            basicProperties.Headers.Remove(AmqpConstants.AMQP10_PROPERTIES_KEY);
        }

        public static void MapFromAmqp10AppPropertiesToHeaders(IBasicProperties basicProperties)
        {
            if (basicProperties?.Headers == null
                || !basicProperties.Headers.TryGetValue(AmqpConstants.AMQP10_APPPROPERTIES_KEY, out var bytesObj)
                || !(bytesObj is byte[] bytes)
                || bytes.Length == 0) return;

            var properties = AmqpEncoder.DecodeObject<ApplicationProperties>(bytes);
            if (properties?.Map == null) return;

            foreach (var kv in properties.Map)
            {
                AddToHeaderIfNotExistAlready(basicProperties, kv.Key, kv.Value);
            }

            basicProperties.Headers.Remove(AmqpConstants.AMQP10_APPPROPERTIES_KEY);
        }

        public static void MapToAmqp10PropertiesWhenSending(IBasicProperties basicProperties)
        {
            var amqp10Properties = new Properties
            {
                MessageId = basicProperties.MessageId,
                UserId = AmqpEncoder.ConvertToBase64(basicProperties.UserId),
                ReplyTo = basicProperties.ReplyTo,
                CorrelationId = basicProperties.CorrelationId,
                ContentType = basicProperties.ContentType,
                ContentEncoding = basicProperties.ContentEncoding,
                CreationTime = AmqpEncoder.ConvertToDateTime(basicProperties.Timestamp)
            };

            if (basicProperties.Headers.TryGetValue(AmqpConstants.AMQP10_PROPERTIES_TO_KEY, out var toValue))
            {
                amqp10Properties.To = toValue.ToString();
                basicProperties.Headers.Remove(AmqpConstants.AMQP10_PROPERTIES_TO_KEY);
            }

            if (basicProperties.Headers.TryGetValue(AmqpConstants.AMQP10_PROPERTIES_SUBJECT_KEY, out var subjectValue))
            {
                amqp10Properties.Subject = subjectValue.ToString();
                basicProperties.Headers.Remove(AmqpConstants.AMQP10_PROPERTIES_SUBJECT_KEY);
            }

            if (basicProperties.Headers.TryGetValue(AmqpConstants.AMQP10_PROPERTIES_ABSOLUTEEXPIRTYTIME_KEY, out var expiryTimeValue))
            {
                amqp10Properties.AbsoluteExpiryTime = (DateTime)expiryTimeValue;
                basicProperties.Headers.Remove(AmqpConstants.AMQP10_PROPERTIES_ABSOLUTEEXPIRTYTIME_KEY);
            }

            AddToHeaders(basicProperties, AmqpConstants.AMQP10_PROPERTIES_KEY, amqp10Properties);
        }

        public static void MapToAmqp10AppPropertiesWhenSending(IBasicProperties basicProperties)
        {
            if (!basicProperties.IsHeadersPresent() || !basicProperties.Headers.Any()) return;

            var amqp10AppProperties = new ApplicationProperties();
            foreach (var kvHeader in basicProperties.Headers)
            {
                amqp10AppProperties.Map.Add(kvHeader.Key, kvHeader.Value);
            }

            AddToHeaders(basicProperties, AmqpConstants.AMQP10_APPPROPERTIES_KEY, amqp10AppProperties);
        }

        private static void AddToHeaders(IBasicProperties basicProperties, string headerKey, object obj)
        {
            basicProperties.Headers ??= new Dictionary<string, object>();

            var byteArray = AmqpEncoder.EncodeObject(obj);
            basicProperties.Headers.Add(headerKey, byteArray);
        }

        private static void AddToHeaderIfNotExistAlready(IBasicProperties basicProperties, string key, DateTime value)
        {
            if (value > DateTime.MinValue)
            {
                AddToHeaderIfNotExistAlready(basicProperties, key, value.ToString("o"));
            }
        }

        private static void AddToHeaderIfNotExistAlready(IBasicProperties basicProperties, object key, object value)
        {
            if (key == null || value == null) return;
            AddToHeaderIfNotExistAlready(basicProperties, key.ToString(), value.ToString());
        }

        private static void AddToHeaderIfNotExistAlready(IBasicProperties basicProperties, string key, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) return;

            basicProperties.Headers ??= new Dictionary<string, object>();
            if (basicProperties.Headers.ContainsKey(key)) return;

            basicProperties.Headers.Add(key, value);
        }
    }
}
