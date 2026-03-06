using Wolverine;
using Wolverine.Attributes;
using Microsoft.EntityFrameworkCore;
using Ardalis.Result;

namespace WolverineSagas.ApiService;

[WolverineHandler]
public sealed class KafkaRetryHandler
{
    public async Task<Result> HandleAsync(KafkaRetryMessage message, ILogger<KafkaRetryHandler> logger, IMessageBus bus, KafkaSagaDbContext dbContext)
    {
        var saga = await dbContext.Sagas.FirstOrDefaultAsync(s => s.Id == message.SagaId);
        
        if (saga is null)
        {
            logger.LogWarning("Saga with ID {SagaId} not found for retry", message.SagaId);
            return Result.NotFound($"Saga with ID {message.SagaId} not found");
        }
        
        if (saga.State != KafkaSagaState.Failed)
        {
            logger.LogWarning("Attempted to retry saga {SagaId} which is in {SagaState} state", message.SagaId, saga.State);
            return Result.Error($"Saga is in {saga.State} state, not Failed. Only failed sagas can be retried.");
        }
        
        // Reset state back to Started and retry
        saga.State = KafkaSagaState.Started;
        saga.Message = null; // Clear previous error
        
        await dbContext.SaveChangesAsync();
        
        // Publish ProcessSaga message to retry
        await bus.PublishAsync(new ProcessSaga(message.SagaId));
        logger.LogInformation("Saga {message.SagaId} has been reset and will be retried", message.SagaId);
        return Result.SuccessWithMessage($"Saga {message.SagaId} has been reset and will be retried");
    }
}