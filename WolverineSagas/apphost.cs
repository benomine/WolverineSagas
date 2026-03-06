#:package Aspire.Hosting.Kafka@13.1.2
#:package Aspire.Hosting.PostgreSQL@13.1.2
#:sdk Aspire.AppHost.Sdk@13.1.2

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka", port: 9092).WithKafkaUI();

var db = builder
    .AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("wolverine");

var api = builder.AddProject("api", "WolverineSagas.ApiService/WolverineSagas.ApiService.csproj")
    .WithReference(kafka)
    .WithReference(db)
    .WaitFor(kafka)
    .WaitFor(db)
    .WithUrl("/scalar", "Scalar");

#pragma warning disable ASPIRECSHARPAPPS001
var producer = builder.AddCSharpApp("producer", "producer.cs")
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithExplicitStart();
#pragma warning restore ASPIRECSHARPAPPS001

builder.Build().Run();
