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

    // self-service profile (any role)
    Task<CustomerDto?> GetMyProfileAsync(string userId);
    Task<bool> UpdateMyProfileAsync(string userId, string fullName, string? phone);

    // batch lookup of name + phone for joining onto DTOs
    Task<IReadOnlyDictionary<string, UserSummary>> GetUserSummariesAsync(IEnumerable<string> userIds);

    // sign in with Google or another verified external provider - finds or auto-creates a Customer and issues tokens
    Task<AuthResult> SignInWithExternalAsync(string email, string fullName, string provider);

    // password reset / email change support
    Task<UserLookup?> FindUserByEmailAsync(string email);
    Task<bool> CheckPasswordAsync(string userId, string password);
    Task<(bool Succeeded, IEnumerable<string> Errors)> ResetPasswordAsync(string userId, string newPassword);
    Task<(bool Succeeded, IEnumerable<string> Errors)> ChangeEmailAsync(string userId, string newEmail);
    Task RevokeAllRefreshTokensAsync(string userId);

    // returns user ids of every active admin - used to fan-out in-app notifications like low-stock alerts
    Task<IReadOnlyList<string>> GetAdminUserIdsAsync();
}

public record UserLookup(string UserId, string FullName, string Email);
