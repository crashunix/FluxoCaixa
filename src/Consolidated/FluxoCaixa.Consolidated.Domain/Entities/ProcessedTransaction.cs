namespace FluxoCaixa.Consolidated.Domain.Entities;

public class ProcessedTransaction
{
    public Guid TransactionId { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}
