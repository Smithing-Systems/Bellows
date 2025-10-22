using Bellows.Abstractions;

namespace AspNetCoreSample.PipelineBehavior.Features.UserManagement;

public class CreateUserHandler : IRequestHandler<CreateUserRequest, CreateUserResponse>
{
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(ILogger<CreateUserHandler> logger)
    {
        _logger = logger;
    }

    public async Task<CreateUserResponse> Handle(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user: {Username}", request.Username);

        // Simulate database operation
        await Task.Delay(100, cancellationToken);

        var userId = Guid.NewGuid();

        return new CreateUserResponse(
            userId,
            request.Username,
            request.Email,
            DateTime.UtcNow
        );
    }
}
