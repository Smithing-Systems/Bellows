using Bellows.Abstractions;
using AspNetCoreSample.PipelineBehavior.PipelineBehaviors;

namespace AspNetCoreSample.PipelineBehavior.Features.UserManagement;

/// <summary>
/// Request to create a new user (demonstrates validation)
/// </summary>
public record CreateUserRequest(string Username, string Email, int Age) : IRequest<CreateUserResponse>, IValidatable
{
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Username))
            errors.Add("Username is required");
        else if (Username.Length < 3)
            errors.Add("Username must be at least 3 characters");

        if (string.IsNullOrWhiteSpace(Email))
            errors.Add("Email is required");
        else if (!Email.Contains('@'))
            errors.Add("Email must be valid");

        if (Age < 18)
            errors.Add("User must be at least 18 years old");
        else if (Age > 120)
            errors.Add("Age must be realistic");

        return errors;
    }
}

public record CreateUserResponse(Guid UserId, string Username, string Email, DateTime CreatedAt);
