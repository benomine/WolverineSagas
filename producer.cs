#:package WolverineFx.Kafka@5.17.0
#:package Microsoft.Extensions.Hosting@10.0.1
#:package Microsoft.Extensions.Logging.Console@10.0.1

using Wolverine;
using Wolverine.Kafka;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    });
});

builder.UseWolverine(options =>
{
    var kafkaConnectionString = Environment.GetEnvironmentVariable("KAFKA_CONNECTION_STRING") 
        ?? "localhost:9092";
    options.UseKafka(kafkaConnectionString);
});

var host = builder.Build();

// Create the message bus for publishing
var bus = host.Services.GetRequiredService<IMessageBus>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("🚀 Wolverine Kafka Producer Started");
logger.LogInformation("📍 Kafka: {KafkaConnection}", 
    Environment.GetEnvironmentVariable("KAFKA_CONNECTION_STRING") ?? "localhost:9092");
logger.LogInformation("📤 Topic: wolverine-sagas");
logger.LogInformation("");

// Producer menu
while (true)
{
    Console.WriteLine("\n╔════════════════════════════════════╗");
    Console.WriteLine("║   Wolverine Kafka Message Producer  ║");
    Console.WriteLine("╚════════════════════════════════════╝");
    Console.WriteLine("\n1. Send Success Message");
    Console.WriteLine("2. Send Failure Message");
    Console.WriteLine("3. Send Random Messages (10x)");
    Console.WriteLine("4. Send Continuous Stream (press 'q' to stop)");
    Console.WriteLine("5. Exit");
    Console.Write("\nSelect option (1-5): ");

    var choice = Console.ReadLine()?.Trim();

    try
    {
        switch (choice)
        {
            case "1":
                await SendSuccessMessage(bus, logger);
                break;

            case "2":
                await SendFailureMessage(bus, logger);
                break;

            case "3":
                await SendRandomMessages(bus, logger, count: 10);
                break;

            case "4":
                await SendContinuousStream(bus, logger);
                break;

            case "5":
                logger.LogInformation("👋 Goodbye!");
                return;

            default:
                Console.WriteLine("❌ Invalid option. Please try again.");
                break;
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error sending message");
    }
}

async Task SendSuccessMessage(IMessageBus bus, ILogger logger)
{
    var id = Guid.NewGuid();
    var message = new KafkaMessage
    {
        Id = id,
        Content = "This message will succeed"
    };

    await bus.PublishAsync(message);
    logger.LogInformation("✅ Sent SUCCESS message: {MessageId}", id);
}

async Task SendFailureMessage(IMessageBus bus, ILogger logger)
{
    var id = Guid.NewGuid();
    var message = new KafkaMessage
    {
        Id = id,
        Content = "This message will FAIL - contains 'fail'"
    };

    await bus.PublishAsync(message);
    logger.LogInformation("⚠️  Sent FAILURE message: {MessageId}", id);
}

async Task SendRandomMessages(IMessageBus bus, ILogger logger, int count)
{
    var random = new Random();
    var contents = new[]
    {
        "Process order #12345",
        "Update user profile",
        "Calculate analytics",
        "Send notification email",
        "Generate report",
        "Process payment",
        "Sync inventory",
        "Index search results",
        "Validate customer data",
        "This will FAIL due to error",
        "FAIL - simulated error",
        "Complete successfully",
        "Handle async operation",
        "Transform data",
        "Archive old records"
    };

    logger.LogInformation("📤 Sending {Count} random messages...", count);

    for (int i = 0; i < count; i++)
    {
        var id = Guid.NewGuid();
        var content = contents[random.Next(contents.Length)];
        var message = new KafkaMessage
        {
            Id = id,
            Content = content
        };

        await bus.PublishAsync(message);
        logger.LogInformation("  [{Current}/{Total}] {MessageId} - {Content}", 
            i + 1, count, id, content);

        // Small delay to avoid overwhelming the system
        await Task.Delay(100);
    }

    logger.LogInformation("✅ Sent {Count} messages successfully!", count);
}

async Task SendContinuousStream(IMessageBus bus, ILogger logger)
{
    var random = new Random();
    var contents = new[]
    {
        "Processing transaction",
        "Updating database",
        "Calling external API",
        "Validating input",
        "Transforming data",
        "This will FAIL",
        "Check inventory",
        "Send alert",
        "Archive data",
        "Generate metrics"
    };

    var count = 0;
    logger.LogInformation("📤 Starting continuous stream... Press 'q' to stop");

    while (true)
    {
        // Check for stop signal (non-blocking)
        if (Console.KeyAvailable && Console.ReadKey(true).KeyChar == 'q')
        {
            break;
        }

        var id = Guid.NewGuid();
        var content = contents[random.Next(contents.Length)];
        var message = new KafkaMessage
        {
            Id = id,
            Content = content
        };

        await bus.PublishAsync(message);
        count++;

        logger.LogInformation("  [Stream #{Count}] {MessageId} - {Content}", 
            count, id, content);

        // Interval between messages
        await Task.Delay(500);
    }

    logger.LogInformation("⏹️  Stopped. Sent {Count} messages in stream", count);
}

// Model classes - must be defined at file scope
public class KafkaMessage
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
}
