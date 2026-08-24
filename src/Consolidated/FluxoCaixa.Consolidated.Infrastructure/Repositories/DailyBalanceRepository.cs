using FluxoCaixa.Consolidated.Domain.Entities;
using FluxoCaixa.Consolidated.Domain.Repositories;
using FluxoCaixa.Consolidated.Infrastructure.Context;
using FluxoCaixa.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidated.Infrastructure.Repositories;

public class DailyBalanceRepository : IDailyBalanceRepository
{
    private readonly ConsolidatedDbContext _dbContext;

    public DailyBalanceRepository(ConsolidatedDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailyBalance?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DailyBalances
            .FirstOrDefaultAsync(x => x.Date == date, cancellationToken);
    }

    public async Task ProcessBatchAsync(IReadOnlyList<TransactionCreatedEvent> events, CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            return;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var dbTx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var candidateIds = events.Select(x => x.Id).ToList();

            var alreadyProcessedIds = await _dbContext.ProcessedTransactions
                .Where(x => candidateIds.Contains(x.TransactionId))
                .Select(x => x.TransactionId)
                .ToHashSetAsync(cancellationToken);

            var newItems = events
                .Where(x => !alreadyProcessedIds.Contains(x.Id))
                .ToList();

            if (newItems.Count > 0)
            {
                var groupedByDate = newItems
                    .GroupBy(x => DateOnly.FromDateTime(x.CreatedAt))
                    .ToList();

                var dates = groupedByDate.Select(g => g.Key).ToList();

                var existingBalances = await _dbContext.DailyBalances
                    .Where(x => dates.Contains(x.Date))
                    .ToDictionaryAsync(x => x.Date, cancellationToken);

                var now = DateTime.UtcNow;

                foreach (var dateGroup in groupedByDate)
                {
                    var date = dateGroup.Key;
                    decimal creditSum = 0;
                    decimal debitSum = 0;

                    foreach (var item in dateGroup)
                    {
                        if (item.TransactionType == 1) // Credito
                        {
                            creditSum += item.Amount;
                        }
                        else // Debito
                        {
                            debitSum += item.Amount;
                        }
                    }

                    if (!existingBalances.TryGetValue(date, out var dailyBalance))
                    {
                        dailyBalance = new DailyBalance
                        {
                            Date = date,
                            TotalCredit = 0,
                            TotalDebit = 0,
                            Balance = 0,
                            LastUpdatedUtc = now
                        };
                        _dbContext.DailyBalances.Add(dailyBalance);
                        existingBalances[date] = dailyBalance;
                    }

                    dailyBalance.TotalCredit += creditSum;
                    dailyBalance.TotalDebit += debitSum;
                    dailyBalance.Balance = dailyBalance.TotalCredit - dailyBalance.TotalDebit;
                    dailyBalance.LastUpdatedUtc = now;
                }

                _dbContext.ProcessedTransactions.AddRange(newItems.Select(x => new ProcessedTransaction
                {
                    TransactionId = x.Id,
                    ProcessedAtUtc = now
                }));

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await dbTx.CommitAsync(cancellationToken);
        });
    }
}
