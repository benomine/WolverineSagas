using JasperFx.Resources;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Kafka;
using Wolverine.Postgresql;
using WolverineSagas.ApiService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// builder.Services.AddMarten(options =>
//     {
//         options.DisableNpgsqlLogging = true;
//         var connectionString = builder.Configuration.GetConnectionString("wolverine");
//         options.Connection(connectionString!);
//     })
//     .IntegrateWithWolverine();

builder.Services.AddNpgsqlDataSource(builder.Configuration.GetConnectionString("wolverine")!);
builder.Services.AddDbContext<KafkaSagaDbContext>((sp, options) =>
{
    var datasource = sp.GetRequiredService<NpgsqlDataSource>();
    options.UseNpgsql(datasource);
});

builder.Host.UseWolverine(options =>
{
    options.UseKafka(builder.Configuration.GetConnectionString("kafka")!);
    options.PersistMessagesWithPostgresql(builder.Configuration.GetConnectionString("wolverine")!, schemaName: "public");
    options.Services.AddDbContextWithWolverineIntegration<KafkaSagaDbContext>(
        x => x.UseNpgsql(builder.Configuration.GetConnectionString("wolverine")!), wolverineDatabaseSchema: "public");

    options.UseEntityFrameworkCoreWolverineManagedMigrations();
    options
        .ListenToKafkaTopic("wolverine-sagas")
        .ConfigureConsumer(c => c.GroupId = "wolverine-sagas-group")
        .ReceiveRawJson<KafkaMessage>()
        .UseDurableInbox();
});

builder.Services.AddResourceSetupOnStartup();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();

// Get failed sagas endpoint
app.MapGet("/sagas/failed", async (KafkaSagaDbContext dbContext) =>
{
    var failedSagas = await dbContext.Sagas
        .Where(s => s.State == KafkaSagaState.Failed)
        .Select(s => new FailedSagaDto
        {
            SagaId = s.Id!,
            InitialMessage = s.Content,
            ErrorMessage = s.Message
        })
        .OrderByDescending(s => s.SagaId)
        .ToListAsync();
    
    return Results.Ok(failedSagas);
})
.WithName("GetFailedSagas")
.WithDescription("Get all failed sagas with their details")
.WithDisplayName("Get Failed Sagas")
.WithSummary("Returns a list of all failed sagas including their initial message and error details")
.Produces<List<FailedSagaDto>>(200);

// Retry endpoint for failed sagas
app.MapPost("/sagas/{id:guid}/retry", async (Guid id, KafkaSagaDbContext dbContext, IMessageBus bus) =>
{
    var saga = await dbContext.Sagas.FirstOrDefaultAsync(s => s.Id == id.ToString());
    
    if (saga is null)
        return Results.NotFound($"Saga with ID {id} not found");
    
    if (saga.State != KafkaSagaState.Failed)
        return Results.BadRequest($"Saga is in {(KafkaSagaState)saga.State} state, not Failed. Only failed sagas can be retried.");
    
    // Reset state back to Started and retry
    saga.State = (int)KafkaSagaState.Started;
    saga.Message = null; // Clear previous error
    
    await dbContext.SaveChangesAsync();
    
    // Publish ProcessSaga message to retry
    await bus.PublishAsync(new ProcessSaga(id.ToString()));
    
    return Results.Ok(new { message = $"Saga {id} has been reset and will be retried", sagaId = id });
})
.WithName("RetrySaga")
.WithDescription("Retry a failed saga by ID")
.WithDisplayName("Retry Failed Saga")
.WithSummary("Retries a failed saga by resetting its state and re-publishing the processing message")
.Produces(200)
.Produces(404)
.Produces(400);

app.Run();
