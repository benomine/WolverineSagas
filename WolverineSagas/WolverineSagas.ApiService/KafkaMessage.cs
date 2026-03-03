namespace WolverineSagas.ApiService;

public class KafkaMessage
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
}
