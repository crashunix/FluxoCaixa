using System.Diagnostics;
using MediatR;

namespace FluxoCaixa.Transactions.Application.Common.Behaviors;

public class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ActivitySource ActivitySource = new("FluxoCaixa.Transactions");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var activity = ActivitySource.StartActivity($"MediatR {requestName}");
        activity?.SetTag("mediatr.request_name", requestName);
        activity?.SetTag("mediatr.request_type", typeof(TRequest).FullName);

        try
        {
            var response = await next();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().FullName);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }
}
