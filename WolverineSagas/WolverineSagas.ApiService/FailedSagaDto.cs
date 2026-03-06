namespace WolverineSagas.ApiService;

public record FailedSagaDto
{
    public Guid? SagaId { get; set; }
    public string? InitialMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
