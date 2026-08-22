using FluxoCaixa.Transactions.Domain.Interfaces.Repositories;
using FluxoCaixa.Transactions.Infrastructure.Context;
using FluxoCaixa.Transactions.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

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

        services.AddTransactionsObservability(configuration);

        return services;
    }

    private static IServiceCollection AddTransactionsObservability(this IServiceCollection services, IConfiguration configuration, string serviceName = "Transactions-Api")
    {
        var otelEndpoint = configuration["OpenTelemetry:Endpoint"]
            ?? configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? "http://localhost:4317";

        services.AddSerilog((serviceProvider, loggerConfig) =>
        {
            loggerConfig
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otelEndpoint;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = serviceName
                    };
                });
        });

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource("FluxoCaixa.*")
                    .AddSource("MediatR")
                    .AddSource("FluxoCaixa.Transactions")
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsql()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otelEndpoint);
                    });
            });

        return services;
    }
}