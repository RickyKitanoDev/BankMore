namespace Transfer.API.Infrastructure.Kafka;

public interface IKafkaProducer
{
    Task PublishAsync<T>(string topic, T message) where T : class;
}
