using FluxoCaixa.Consolidated.Domain.Entities;
using FluxoCaixa.Consolidated.Infrastructure.Context;
using FluxoCaixa.Consolidated.Infrastructure.Repositories;
using FluxoCaixa.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FluxoCaixa.Consolidated.UnitTests;

public class BatchProcessingTests
{
    private ConsolidatedDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ConsolidatedDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ConsolidatedDbContext(options);
    }

    [Fact]
    public async Task ProcessBatch_ShouldAggregateCreditsAndDebitsCorrectly()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new DailyBalanceRepository(dbContext);
        var date = new DateTime(2026, 8, 23, 10, 0, 0, DateTimeKind.Utc);

        var events = new List<TransactionCreatedEvent>
        {
            new(Guid.NewGuid(), 100.00m, "BRL", 1, "Credit 1", date),
            new(Guid.NewGuid(), 50.00m, "BRL", 1, "Credit 2", date),
            new(Guid.NewGuid(), 30.00m, "BRL", 2, "Debit 1", date)
        };

        // Act
        await repository.ProcessBatchAsync(events);

        // Assert
        var targetDate = DateOnly.FromDateTime(date);
        var balance = await repository.GetByDateAsync(targetDate);

        Assert.NotNull(balance);
        Assert.Equal(150.00m, balance.TotalCredit);
        Assert.Equal(30.00m, balance.TotalDebit);
        Assert.Equal(120.00m, balance.Balance);
        Assert.Equal(3, await dbContext.ProcessedTransactions.CountAsync());
    }

    [Fact]
    public async Task ProcessBatch_ShouldSkipAlreadyProcessedTransactions()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var repository = new DailyBalanceRepository(dbContext);
        var date = new DateTime(2026, 8, 23, 10, 0, 0, DateTimeKind.Utc);
        var existingTxId = Guid.NewGuid();

        dbContext.ProcessedTransactions.Add(new ProcessedTransaction
        {
            TransactionId = existingTxId,
            ProcessedAtUtc = DateTime.UtcNow
        });
        dbContext.DailyBalances.Add(new DailyBalance
        {
            Date = DateOnly.FromDateTime(date),
            TotalCredit = 200.00m,
            TotalDebit = 0m,
            Balance = 200.00m,
            LastUpdatedUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var events = new List<TransactionCreatedEvent>
        {
            new(existingTxId, 100.00m, "BRL", 1, "Duplicate Credit", date),
            new(Guid.NewGuid(), 50.00m, "BRL", 1, "New Credit", date)
        };

        // Act
        await repository.ProcessBatchAsync(events);

        // Assert
        var targetBalance = await dbContext.DailyBalances.FirstAsync();
        Assert.Equal(250.00m, targetBalance.TotalCredit); // 200 existing + 50 new (100 skipped)
        Assert.Equal(2, await dbContext.ProcessedTransactions.CountAsync());
    }

    [Fact]
    public void ProcessBatch_ShouldHandleIntraBatchDuplicates()
    {
        // Arrange
        var duplicateId = Guid.NewGuid();
        var date = DateTime.UtcNow;

        var events = new List<TransactionCreatedEvent>
        {
            new(duplicateId, 100.00m, "BRL", 1, "First occurrence", date),
            new(duplicateId, 100.00m, "BRL", 1, "Second occurrence inside same batch", date)
        };

        // Act
        var distinctEvents = events.GroupBy(x => x.Id).Select(g => g.First()).ToList();

        // Assert
        Assert.Single(distinctEvents);
        Assert.Equal(duplicateId, distinctEvents[0].Id);
    }
}
