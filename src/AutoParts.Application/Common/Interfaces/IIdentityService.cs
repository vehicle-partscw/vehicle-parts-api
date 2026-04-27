using AutoParts.Application.Common.Models;

namespace AutoParts.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<AuthResult> RegisterCustomerAsync(string fullName, string email, string password);
    Task<AuthResult> RegisterStaffAsync(string fullName, string email, string password, string role);
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);

    Task<IReadOnlyList<StaffDto>> GetAllStaffAsync();
    Task<bool> UpdateStaffRoleAsync(string userId, string newRole);
    Task<bool> ToggleStaffActiveAsync(string userId);

    // customers
    Task<IReadOnlyList<CustomerDto>> GetAllCustomersAsync();
    Task<CustomerDto?> GetCustomerByIdAsync(string userId);
    Task<bool> ToggleCustomerActiveAsync(string userId);
    Task<bool> UpdateCustomerCreditLimitAsync(string userId, decimal? creditLimit);

    // sign in with Google or another verified external provider
    Task<AuthResult> SignInWithExternalAsync(string email, string fullName, string provider);

    // self-service profile (any role)
    Task<CustomerDto?> GetMyProfileAsync(string userId);
    Task<bool> UpdateMyProfileAsync(string userId, string fullName, string? phone);

    // batch lookup of name + phone for joining onto DTOs
    Task<IReadOnlyDictionary<string, UserSummary>> GetUserSummariesAsync(IEnumerable<string> userIds);

    // password reset / email change
    Task<bool> CheckPasswordAsync(string userId, string password);
    Task<(bool Succeeded, IEnumerable<string> Errors)> ChangeEmailAsync(string userId, string newEmail);

    // password reset
    Task<UserLookup?> FindUserByEmailAsync(string email);
    Task<(bool Succeeded, IEnumerable<string> Errors)> ResetPasswordAsync(string userId, string newPassword);
    Task RevokeAllRefreshTokensAsync(string userId);
}

public record UserLookup(string UserId, string FullName, string Email);
