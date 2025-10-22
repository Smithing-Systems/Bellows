using Bellows.Abstractions;
using AspNetCoreSample.PipelineBehavior.PipelineBehaviors;

namespace AspNetCoreSample.PipelineBehavior.Features.UserManagement;

/// <summary>
/// Request to get user by ID (demonstrates caching)
/// </summary>
public record GetUserRequest(Guid UserId) : IRequest<UserDto>, ICacheable
{
    public string GetCacheKey() => $"user_{UserId}";
    public int CacheDurationSeconds => 30;
}

public record UserDto(Guid Id, string Username, string Email, DateTime CreatedAt);
