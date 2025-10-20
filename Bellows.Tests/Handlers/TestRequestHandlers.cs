using Bellows.Abstractions;

namespace Bellows.Tests.Handlers;

// Test requests
public record GetUserQuery(int UserId) : IRequest<UserResponse>;

public record UserResponse(int Id, string Name, string Email);

public record CalculateCommand(int A, int B) : IRequest<int>;

// Test request handlers
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserResponse>
{
    public Task<UserResponse> Handle(GetUserQuery request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new UserResponse(request.UserId, $"User{request.UserId}", $"user{request.UserId}@example.com"));
    }
}

public class CalculateCommandHandler : IRequestHandler<CalculateCommand, int>
{
    public Task<int> Handle(CalculateCommand request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.A + request.B);
    }
}
