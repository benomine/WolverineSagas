using Wolverine;
using Wolverine.Attributes;

namespace WolverineSagas.ApiService;

[WolverineHandler]
public sealed class KafkaHandler
{
    public void Handle(KafkaMessage message, ILogger<KafkaHandler> logger, IMessageBus bus)
    {
        logger.LogInformation("Handling Kafka message with ID: {MessageId}", message.Id);
        var saga = new StartSaga(message);
        bus.InvokeAsync(saga);
    }
}
