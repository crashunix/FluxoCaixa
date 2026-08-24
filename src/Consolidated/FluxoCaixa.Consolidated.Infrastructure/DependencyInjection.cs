using FluxoCaixa.Consolidated.Domain.Repositories;
using FluxoCaixa.Consolidated.Infrastructure.Context;
using FluxoCaixa.Consolidated.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluxoCaixa.Consolidated.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConsolidatedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5433;Database=fluxocaixa_consolidated;Username=postgres;Password=postgres";

        services.AddDbContext<ConsolidatedDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ConsolidatedDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure();
            }));

        services.AddScoped<IDailyBalanceRepository, DailyBalanceRepository>();

        return services;
    }

    public static async Task ApplyConsolidatedMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConsolidatedDbContext>();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.MigrateAsync();
        });
    }
}
