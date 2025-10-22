using Bellows.Abstractions;

namespace AspNetCoreSample.PipelineBehavior.Features.UserManagement;

public class GetUserHandler : IRequestHandler<GetUserRequest, UserDto>
{
    private readonly ILogger<GetUserHandler> _logger;

    public GetUserHandler(ILogger<GetUserHandler> logger)
    {
        _logger = logger;
    }

    public async Task<UserDto> Handle(GetUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching user: {UserId}", request.UserId);

        // Simulate database query
        await Task.Delay(200, cancellationToken);

        return new UserDto(
            request.UserId,
            "john_doe",
            "john@example.com",
            DateTime.UtcNow.AddDays(-30)
        );
    }
}
