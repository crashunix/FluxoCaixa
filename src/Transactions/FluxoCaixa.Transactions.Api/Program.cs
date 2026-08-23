using FluxoCaixa.Shared.Extensions;
using FluxoCaixa.Transactions.Api.Middlewares;
using FluxoCaixa.Transactions.Application;
using FluxoCaixa.Transactions.Application.Commands.CreateTransaction;
using FluxoCaixa.Transactions.Infrastructure;
using MediatR;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddOpenTelemetryObservability("Transactions.Api", tracing =>
{
    tracing.AddNpgsql();
});

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddTransactionsApplication();
builder.Services.AddTransactionsInfrastructure(builder.Configuration);

var app = builder.Build();

await app.ApplyTransactionsMigrationsAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/transactions", async (CreateTransactionCommand command, ISender sender, CancellationToken cancellationToken) =>
{
    var result = await sender.Send(command, cancellationToken);
    return Results.Created($"/transactions/{result.Id}", result);
});

app.Run();