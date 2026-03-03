using JasperFx.Resources;
using Marten;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Kafka;
using Wolverine.Marten;
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
}

app.MapDefaultEndpoints();

app.Run();
