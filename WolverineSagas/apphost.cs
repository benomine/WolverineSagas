#:package Aspire.Hosting.Kafka@13.1.2
#:package Aspire.Hosting.PostgreSQL@13.1.2
#:sdk Aspire.AppHost.Sdk@13.1.2

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka").WithKafkaUI();

var db = builder
    .AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("wolverine");

var api = builder.AddProject("api", "WolverineSagas.ApiService/WolverineSagas.ApiService.csproj")
    .WithReference(kafka)
    .WithReference(db)
    .WaitFor(kafka)
    .WaitFor(db);

builder.Build().Run();
