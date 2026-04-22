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
}
