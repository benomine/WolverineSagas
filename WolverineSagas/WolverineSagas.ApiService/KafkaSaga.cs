using System;
using Wolverine;

namespace WolverineSagas.ApiService;

public enum KafkaSagaState
{
    Started = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public record StartSaga(KafkaMessage Message);
public record ProcessSaga(Guid Id);
public record CompleteSaga(Guid Id);
public record FailSaga(Guid Id, string? ErrorMessage = null);

public class KafkaSaga : Saga
{
    public Guid? Id { get; set; }
    public string? Content { get; set; }
    public new int Version { get; set; }
    public KafkaSagaState State { get; set; } = KafkaSagaState.Started;
    public string? Message { get; set; }

    public static (KafkaSaga, ProcessSaga) Start(StartSaga saga, ILogger<KafkaSaga> logger)
    {
        logger.LogInformation("Starting Kafka saga with message ID: {MessageId}", saga.Message.Id);
        return (new KafkaSaga
        {
            Id = saga.Message.Id,
            Content = saga.Message.Content,
            Version = 0,
            State = KafkaSagaState.Started,
            Message = null
        }, new ProcessSaga(saga.Message.Id));
    }

    public void Handle(ProcessSaga saga, ILogger<KafkaSaga> logger, IMessageBus bus)
    {
        try
        {
            State = KafkaSagaState.Processing;
            logger.LogInformation("Processing Kafka saga with ID: {SagaId}", saga.Id);

            if (Content!.Contains("fail", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Simulated failure in processing Kafka saga.");
            }

            bus.PublishAsync(new CompleteSaga(saga.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Kafka saga with ID: {SagaId}", saga.Id);
            Message = ex.Message;
            bus.PublishAsync(new FailSaga(saga.Id, ex.Message));
        }
    }

    public void Handle(CompleteSaga saga, ILogger<KafkaSaga> logger)
    {
        logger.LogInformation("Completing Kafka saga with ID: {SagaId}", saga.Id);
        State = KafkaSagaState.Completed;
        MarkCompleted();
    }

    public void Handle(FailSaga saga, ILogger<KafkaSaga> logger)
    {
        logger.LogError("Failed to process Kafka saga with ID: {SagaId}, Error: {ErrorMessage}", saga.Id, saga.ErrorMessage);
        State = KafkaSagaState.Failed;
        Message = saga.ErrorMessage;
    }

    public static void NotFound(CompleteSaga complete, ILogger<KafkaSaga> logger)
    {
        logger.LogInformation("Tried to complete saga {Id}, but it cannot be found", complete.Id);
    }
}
