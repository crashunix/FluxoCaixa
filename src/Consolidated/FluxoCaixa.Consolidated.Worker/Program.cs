using FluxoCaixa.Consolidated.Domain.Repositories;
using FluxoCaixa.Consolidated.Infrastructure;
using FluxoCaixa.Consolidated.Worker.Consumers;
using FluxoCaixa.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddOpenTelemetryObservability("Consolidated.Worker");

builder.Services.AddConsolidatedInfrastructure(builder.Configuration);

builder.Services.AddHostedService<TransactionCreatedConsumer>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/consolidated", async ([FromQuery] DateOnly date, IDailyBalanceRepository repository, CancellationToken cancellationToken) =>
{
    var balance = await repository.GetByDateAsync(date, cancellationToken);

    if (balance is null)
    {
        return Results.NotFound(new { message = $"Nenhum saldo consolidado encontrado para a data {date:yyyy-MM-dd}." });
    }

    return Results.Ok(balance);
})
.WithName("GetConsolidatedBalanceByDate");

await app.ApplyConsolidatedMigrationsAsync();

app.Run();
