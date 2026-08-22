using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FluxoCaixa.Shared.Extensions;

public static class ObservabilityExtensions
{
    public static IHostApplicationBuilder AddOpenTelemetryObservability(
        this IHostApplicationBuilder builder,
        string serviceName,
        Action<TracerProviderBuilder>? configureTracing = null,
        Action<MeterProviderBuilder>? configureMetrics = null)
    {
        var otelEndpoint = builder.Configuration["OpenTelemetry:Endpoint"]
            ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? "http://localhost:4317";

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: "1.0.0");

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resourceBuilder);
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
            logging.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otelEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(serviceName)
                    .AddSource("FluxoCaixa.*")
                    .AddSource("FluxoCaixa.MediatR")
                    .AddSource("FluxoCaixa.Transactions")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                configureTracing?.Invoke(tracing);

                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otelEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(serviceName)
                    .AddMeter("FluxoCaixa.*")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                configureMetrics?.Invoke(metrics);

                metrics.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otelEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                });
            });

        return builder;
    }
}
