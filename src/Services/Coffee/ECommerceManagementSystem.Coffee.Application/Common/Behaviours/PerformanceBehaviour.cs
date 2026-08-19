using System.Diagnostics;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Common.Behaviours;

/// <summary>
/// Represents a pipeline behavior for measuring and logging the performance
/// of request handling operations in the application.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class PerformanceBehaviour<TRequest, TResponse> :
    IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly Stopwatch _timer;
    private readonly ILogger _logger;
    
    public PerformanceBehaviour(ILogger logger)
    {
        _timer = new Stopwatch();
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken, MessageHandlerDelegate<TRequest, TResponse> next)
    {
        _timer.Start();
        var response = await next(request, cancellationToken);
        _timer.Stop();
        
        var elapsedMilliseconds = _timer.ElapsedMilliseconds;
        
        if(elapsedMilliseconds <= 500) return response;

        var requestName = typeof(TRequest).Name;
        _logger.Warning("Application Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@Request}",
            requestName, elapsedMilliseconds, request);

        return response;
    }
}