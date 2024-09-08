using Confluent.Kafka;
using ReservationPlatform.CQRS;

namespace ReservationPlatform.Helpers
{
    public class KafkaConsumer
    {
        public static Func<string, string, string, Task> ConsumeEventsWithState<T>(Func<string, T, Task> messageHandler, Func<string, T> deserializer)
        {
            return async (topic, server, groupId) =>
            {
                var config = new ConsumerConfig
                {
                    GroupId = groupId, //"reservation-event-consumer-group"
                    BootstrapServers = server,
                    AutoOffsetReset = AutoOffsetReset.Earliest
                };

                using var consumer = new ConsumerBuilder<string, string>(config)
                    .SetKeyDeserializer(Deserializers.Utf8)
                    .SetValueDeserializer(Deserializers.Utf8)
                    .Build();

                consumer.Subscribe(topic);

                try
                {
                    while (true)
                    {
                        var consumeResult = consumer.Consume();
                        var userId = consumeResult.Message.Key; // Key is reservationId, should be userId
                        var message = consumeResult.Message.Value;

                        // Handle the event using the reservationId and the message (event)
                        await messageHandler(userId, deserializer(message));
                    }
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine($"Consume error: {e.Error.Reason}");
                }
                finally
                {
                    consumer.Close();
                }
            };
        }

        public static Func<Func<T, string>, Func<string, T, Task>> ProduceMessage<T>(string server, string topic) where T : IRecord //server: "localhost:9092"
        {
            return serializer => (userId, newEvent) =>
            {
                // Produce el nuevo evento en Kafka, garantizando que use el reservationId como key para mantener el orden.
                var producerConfig = new ProducerConfig { BootstrapServers = server };

                using var producer = new ProducerBuilder<string, string>(producerConfig)
                    .SetKeySerializer(Serializers.Utf8)
                    .SetValueSerializer(Serializers.Utf8)
                    .Build();

                return producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = userId,
                    Value = serializer(newEvent) // Implementa SerializeEvent
                });
            };
        }
    }
}