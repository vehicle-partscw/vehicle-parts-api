namespace AutoParts.Application.Common.Models;

public class AuthResult
{
    public bool Succeeded { get; set; }
    public string? UserId { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string[] Errors { get; set; } = [];

    public static AuthResult Success(string userId, string accessToken, string refreshToken, DateTime expiresAt)
    {
        return new AuthResult
        {
            Succeeded = true,
            UserId = userId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }

    public static AuthResult Failure(params string[] errors)
    {
        return new AuthResult
        {
            Succeeded = false,
            Errors = errors
        };
    }
}
