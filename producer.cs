#:package WolverineFx.Kafka@5.17.0
#:package Microsoft.Extensions.Hosting@10.0.1
#:package Microsoft.Extensions.Logging.Console@10.0.1
#:package Spectre.Console@0.53.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.Kafka;
using Spectre.Console;

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
await host.StartAsync();

// Create the message bus for publishing
var bus = host.Services.GetRequiredService<IMessageBus>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Welcome banner
AnsiConsole.Write(
    new FigletText("Wolverine Producer")
        .Centered()
        .Color(Color.Cyan1));

AnsiConsole.MarkupLine("[bold green]✓ Producer Started[/]");
AnsiConsole.WriteLine();

var kafkaConn = Environment.GetEnvironmentVariable("KAFKA_CONNECTION_STRING") ?? "localhost:9092";
var table = new Table();
table.AddColumn("[bold cyan]Configuration[/]");
table.AddColumn("[bold cyan]Value[/]");
table.AddRow("Kafka Server", $"[yellow]{kafkaConn}[/]");
table.AddRow("Topic", "[yellow]wolverine-sagas[/]");
table.Title = new TableTitle("[bold]Connection Details[/]");
table.Border = TableBorder.Rounded;
AnsiConsole.Write(table);
AnsiConsole.WriteLine();

// Producer menu loop
while (true)
{
    AnsiConsole.MarkupLine("[bold cyan]═══════════════════════════════════════[/]");
    AnsiConsole.MarkupLine("[bold yellow]    🚀 Wolverine Kafka Message Menu[/]");
    AnsiConsole.MarkupLine("[bold cyan]═══════════════════════════════════════[/]");
    AnsiConsole.WriteLine();
    
    var options = new SelectionPrompt<string>()
        .Title("[bold]What would you like to do?[/]")
        .AddChoices(
            new[]
            {
                "[green]1. ✅ Send Success Message[/]",
                "[yellow]2. ⚠️  Send Failure Message[/]",
                "[cyan]3. 📤 Send Random Messages (10x)[/]",
                "[magenta]4. 🔄 Send Continuous Stream[/]",
                "[red]5. 🚪 Exit[/]"
            });

    var choice = AnsiConsole.Prompt(options);

    try
    {
        if (choice.StartsWith("[green]1"))
        {
            await SendSuccessMessage(bus, logger);
        }
        else if (choice.StartsWith("[yellow]2"))
        {
            await SendFailureMessage(bus, logger);
        }
        else if (choice.StartsWith("[cyan]3"))
        {
            await SendRandomMessages(bus, logger, count: 10);
        }
        else if (choice.StartsWith("[magenta]4"))
        {
            await SendContinuousStream(bus, logger);
        }
        else if (choice.StartsWith("[red]5"))
        {
            AnsiConsole.MarkupLine("[bold green]👋 Goodbye![/]");
            await host.StopAsync();
            return;
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[bold red]❌ Error:[/] {ex.Message}");
    }

    AnsiConsole.WriteLine();
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
    
    var panel = new Panel(
        $"[green]Message ID:[/] {id}\n[green]Content:[/] {message.Content}\n[green]Status:[/] Published to Kafka")
        .Header("[bold green]✅ Success Message Sent[/]")
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Green);
    
    AnsiConsole.Write(panel);
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
    
    var panel = new Panel(
        $"[yellow]Message ID:[/] {id}\n[yellow]Content:[/] {message.Content}\n[yellow]Status:[/] Published (will trigger saga failure)")
        .Header("[bold yellow]⚠️  Failure Message Sent[/]")
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Yellow);
    
    AnsiConsole.Write(panel);
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

    AnsiConsole.MarkupLine("[cyan]📤 Sending random messages...[/]");
    AnsiConsole.WriteLine();

    var table = new Table();
    table.AddColumn("[cyan]#[/]");
    table.AddColumn("[cyan]Message ID[/]");
    table.AddColumn("[cyan]Content[/]");
    table.AddColumn("[cyan]Status[/]");
    table.Title = new TableTitle("[bold cyan]Message Batch[/]");
    table.Border = TableBorder.Rounded;

    await AnsiConsole.Progress()
        .Columns(
            new ProgressColumn[] 
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn()
            })
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask($"[cyan]Sending {count} messages[/]", maxValue: count);

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
                
                var status = content.Contains("fail", StringComparison.OrdinalIgnoreCase) 
                    ? "[yellow]⚠️  Will Fail[/]" 
                    : "[green]✅ Will Succeed[/]";
                
                table.AddRow(
                    (i + 1).ToString(),
                    id.ToString("D").Substring(0, 8),
                    content.Length > 25 ? content.Substring(0, 22) + "..." : content,
                    status);

                task.Increment(1);
                await Task.Delay(100);
            }
        });

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
    
    var successPanel = new Panel($"[green]Successfully sent {count} messages[/]")
        .Header("[bold green]✅ Batch Complete[/]")
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Green);
    
    AnsiConsole.Write(successPanel);
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
    AnsiConsole.MarkupLine("[magenta]🔄 Starting continuous stream... Press [bold]Q[/] to stop[/]");
    AnsiConsole.WriteLine();

    var liveDisplay = new Table();
    liveDisplay.AddColumn("[cyan]Count[/]");
    liveDisplay.AddColumn("[cyan]Message ID[/]");
    liveDisplay.AddColumn("[cyan]Content[/]");
    liveDisplay.Title = new TableTitle("[bold magenta]Live Stream[/]");
    liveDisplay.Border = TableBorder.Rounded;

    var rows = new List<string[]>();

    await AnsiConsole.Live(liveDisplay)
        .StartAsync(async ctx =>
        {
            while (true)
            {
                // Check for stop signal (non-blocking)
                if (Console.KeyAvailable && Console.ReadKey(true).KeyChar.ToString().Equals("q", StringComparison.OrdinalIgnoreCase))
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

                rows.Insert(0, new[] { count.ToString(), id.ToString("D").Substring(0, 8), content });
                
                // Keep only last 10 rows
                if (rows.Count > 10)
                {
                    rows.RemoveAt(rows.Count - 1);
                }

                liveDisplay = new Table();
                liveDisplay.AddColumn("[cyan]Count[/]");
                liveDisplay.AddColumn("[cyan]Message ID[/]");
                liveDisplay.AddColumn("[cyan]Content[/]");
                liveDisplay.Title = new TableTitle($"[bold magenta]Live Stream - {count} messages sent[/]");
                liveDisplay.Border = TableBorder.Rounded;

                foreach (var row in rows)
                {
                    liveDisplay.AddRow(row[0], row[1], row[2]);
                }

                ctx.UpdateTarget(liveDisplay);

                await Task.Delay(500);
            }
        });

    AnsiConsole.WriteLine();
    
    var stopPanel = new Panel($"[magenta]Stream stopped. Sent {count} messages total[/]")
        .Header("[bold magenta]⏹️  Stream Stopped[/]")
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Magenta1);
    
    AnsiConsole.Write(stopPanel);
    logger.LogInformation("⏹️  Stopped. Sent {Count} messages in stream", count);
}

// Model classes - must be defined at file scope
public class KafkaMessage
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
}
