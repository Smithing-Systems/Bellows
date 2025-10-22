using Bellows.Abstractions;

namespace AspNetCoreSample.PipelineBehavior.PipelineBehaviors;

/// <summary>
/// Validates requests before they reach the handler
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Validating {RequestName}...", requestName);

        // Check if request implements IValidatable
        if (request is IValidatable validatable)
        {
            var errors = validatable.Validate();

            if (errors.Any())
            {
                var errorMessage = string.Join("; ", errors);
                _logger.LogWarning("Validation failed for {RequestName}: {Errors}", requestName, errorMessage);
                throw new ValidationException(errorMessage, errors);
            }

            _logger.LogInformation("Validation passed for {RequestName}", requestName);
        }
        else
        {
            _logger.LogDebug("No validation required for {RequestName}", requestName);
        }

        return await next();
    }
}

/// <summary>
/// Interface for requests that can be validated
/// </summary>
public interface IValidatable
{
    List<string> Validate();
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public List<string> Errors { get; }

    public ValidationException(string message, List<string> errors) : base(message)
    {
        Errors = errors;
    }
}
