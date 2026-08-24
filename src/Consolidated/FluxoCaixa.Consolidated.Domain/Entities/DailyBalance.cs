namespace FluxoCaixa.Consolidated.Domain.Entities;

public class DailyBalance
{
    public DateOnly Date { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal Balance { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}
