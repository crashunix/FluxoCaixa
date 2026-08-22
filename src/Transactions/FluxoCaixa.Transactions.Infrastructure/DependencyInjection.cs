using FluxoCaixa.Transactions.Domain.Interfaces.Repositories;
using FluxoCaixa.Transactions.Infrastructure.Context;
using FluxoCaixa.Transactions.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FluxoCaixa.Transactions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<TransactionsDbContext>(options =>
            options.UseNpgsql(connectionString, options => options.MigrationsAssembly(typeof(TransactionsDbContext).Assembly.FullName)));

        services.AddScoped<ITransactionRepository, TransactionRepository>();

        return services;
    }
}