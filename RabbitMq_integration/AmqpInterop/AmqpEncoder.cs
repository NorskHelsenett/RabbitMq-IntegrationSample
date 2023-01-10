using Amqp;
using Amqp.Types;
using RabbitMQ.Client;

namespace RabbitMq_integration.AmqpInterop
{
    public static class AmqpEncoder
    {
        public static T DecodeObject<T>(byte[] props) where T : class
        {
            var buffer = new ByteBuffer(props, 0, props.Length, props.Length);
            var formatCode = Encoder.ReadFormatCode(buffer);
            return Encoder.ReadDescribed(buffer, formatCode) as T;
        }

        public static byte[] EncodeObject(object obj)
        {
            var byteBuffer = new ByteBuffer(64, true);
            Encoder.WriteObject(byteBuffer, obj);
            var byteArray = new byte[byteBuffer.Length];
            Buffer.BlockCopy(byteBuffer.Buffer, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }

        public static DateTime ConvertToDateTime(AmqpTimestamp amqpTimestamp)
        {
            return amqpTimestamp.UnixTime > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(amqpTimestamp.UnixTime).ToUniversalTime().DateTime
                : DateTime.MinValue;
        }

        public static byte[] ConvertToBase64(string value)
        {
            return !string.IsNullOrEmpty(value) ? System.Text.Encoding.UTF8.GetBytes(value) : null;
        }
    }
}
