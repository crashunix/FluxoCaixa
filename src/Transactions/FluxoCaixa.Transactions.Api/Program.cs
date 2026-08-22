using FluxoCaixa.Transactions.Api.Middlewares;
using FluxoCaixa.Transactions.Application.Commands.CreateTransaction;
using FluxoCaixa.Transactions.Application;
using FluxoCaixa.Transactions.Infrastructure;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();
builder.Services.AddTransactionsApplication();
builder.Services.AddTransactionsInfrastructure(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline.
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