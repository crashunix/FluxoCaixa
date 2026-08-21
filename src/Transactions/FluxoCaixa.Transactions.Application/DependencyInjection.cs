using FluxoCaixa.Transactions.Application.Commands.CreateTransaction;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FluxoCaixa.Transactions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}