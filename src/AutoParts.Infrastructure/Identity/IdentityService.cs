using System.Security.Cryptography;
using System.Text;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Domain.Entities;
using AutoParts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AutoParts.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private const int RefreshTokenExpiryDays = 7;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        JwtTokenGenerator jwtTokenGenerator,
        ApplicationDbContext dbContext,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<AuthResult> RegisterCustomerAsync(string fullName, string email, string password)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            return AuthResult.Failure(errors);
        }

        var customerRoleExists = await _roleManager.RoleExistsAsync("Customer");
        if (!customerRoleExists)
        {
            await _roleManager.CreateAsync(new IdentityRole("Customer"));
        }

        await _userManager.AddToRoleAsync(user, "Customer");

        var tokens = await GenerateTokensAsync(user);
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60");

        return AuthResult.Success(user.Id, tokens.AccessToken, tokens.RefreshToken, DateTime.UtcNow.AddMinutes(expiryMinutes));
    }

    public async Task<AuthResult> RegisterStaffAsync(string fullName, string email, string password, string role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            return AuthResult.Failure(errors);
        }

        var roleExists = await _roleManager.RoleExistsAsync(role);
        if (!roleExists)
        {
            return AuthResult.Failure($"Role '{role}' does not exist");
        }

        await _userManager.AddToRoleAsync(user, role);

        // Create StaffProfile entity
        var staffProfile = new StaffProfile
        {
            UserId = user.Id,
            FullName = fullName,
            Email = email,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.StaffProfiles.Add(staffProfile);
        await _dbContext.SaveChangesAsync();

        return AuthResult.Success(user.Id, string.Empty, string.Empty, DateTime.UtcNow);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null || !user.IsActive)
        {
            return AuthResult.Failure("Invalid email or password");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);

        if (!passwordValid)
        {
            return AuthResult.Failure("Invalid email or password");
        }

        var tokens = await GenerateTokensAsync(user);
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60");

        return AuthResult.Success(user.Id, tokens.AccessToken, tokens.RefreshToken, DateTime.UtcNow.AddMinutes(expiryMinutes));
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash
                                       && rt.RevokedAt == null
                                       && rt.ExpiresAt > DateTime.UtcNow);

        if (storedToken == null)
        {
            return AuthResult.Failure("Invalid or expired refresh token");
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId);

        if (user == null || !user.IsActive)
        {
            return AuthResult.Failure("User not found or inactive");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles);
        var newRefreshToken = GenerateRandomToken();
        var newRefreshTokenHash = HashToken(newRefreshToken);

        // Revoke old token
        storedToken.RevokedAt = DateTime.UtcNow;
        _dbContext.RefreshTokens.Update(storedToken);

        // Add new token
        var newStoredToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(newStoredToken);
        await _dbContext.SaveChangesAsync();

        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60");
        return AuthResult.Success(user.Id, newAccessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(expiryMinutes));
    }

    public async Task<IReadOnlyList<StaffDto>> GetAllStaffAsync()
    {
        var staffProfiles = await _dbContext.StaffProfiles
            .Where(sp => !sp.IsDeleted)
            .ToListAsync();

        return staffProfiles
            .Select(sp => new StaffDto
            {
                UserId = sp.UserId,
                FullName = sp.FullName,
                Email = sp.Email,
                Phone = sp.Phone,
                Role = sp.Role,
                IsActive = sp.IsActive,
                CreatedAt = sp.CreatedAt
            })
            .ToList()
            .AsReadOnly();
    }

    public async Task<bool> UpdateStaffRoleAsync(string userId, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var roleExists = await _roleManager.RoleExistsAsync(newRole);
        if (!roleExists)
        {
            return false;
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        var result = await _userManager.AddToRoleAsync(user, newRole);

        if (!result.Succeeded)
        {
            return false;
        }

        var staffProfile = await _dbContext.StaffProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId && !sp.IsDeleted);

        if (staffProfile != null)
        {
            staffProfile.Role = newRole;
            staffProfile.UpdatedAt = DateTime.UtcNow;
            _dbContext.StaffProfiles.Update(staffProfile);
            await _dbContext.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> ToggleStaffActiveAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return false;
        }

        var staffProfile = await _dbContext.StaffProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId && !sp.IsDeleted);

        if (staffProfile != null)
        {
            staffProfile.IsActive = user.IsActive;
            staffProfile.UpdatedAt = DateTime.UtcNow;
            _dbContext.StaffProfiles.Update(staffProfile);
            await _dbContext.SaveChangesAsync();
        }

        return true;
    }

    private async Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles);

        var refreshToken = GenerateRandomToken();
        var refreshTokenHash = HashToken(refreshToken);

        var storedToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(storedToken);
        await _dbContext.SaveChangesAsync();

        return (accessToken, refreshToken);
    }

    private static string GenerateRandomToken()
    {
        var randomNumber = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    private static string HashToken(string token)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllCustomersAsync()
    {
        var customers = await _userManager.GetUsersInRoleAsync("Customer");
        return customers.Select(u => new CustomerDto
        {
            UserId = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? string.Empty,
            Phone = u.PhoneNumber,
            IsActive = u.IsActive,
            CreditLimit = u.CreditLimit,
            CreatedAt = u.CreatedAt
        }).ToList().AsReadOnly();
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;
        if (!await _userManager.IsInRoleAsync(user, "Customer")) return null;
        return new CustomerDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumber,
            IsActive = user.IsActive,
            CreditLimit = user.CreditLimit,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> ToggleCustomerActiveAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;
        if (!await _userManager.IsInRoleAsync(user, "Customer")) return false;
        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> UpdateCustomerCreditLimitAsync(string userId, decimal? creditLimit)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;
        if (!await _userManager.IsInRoleAsync(user, "Customer")) return false;
        user.CreditLimit = creditLimit;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<UserLookup?> FindUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return null;
        return new UserLookup(user.Id, user.FullName, user.Email ?? string.Empty);
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> ResetPasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return (false, new[] { "User not found." });
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task RevokeAllRefreshTokensAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var tokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();
        foreach (var t in tokens) t.RevokedAt = now;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<AuthResult> SignInWithExternalAsync(string email, string fullName, string provider)
    {
        var user = await _userManager.FindByEmailAsync(email);

        // not registered yet -> auto-create as a Customer with a random secure password
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = string.IsNullOrWhiteSpace(fullName) ? email.Split('@')[0] : fullName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "Aa1!";
            var result = await _userManager.CreateAsync(user, randomPassword);
            if (!result.Succeeded)
                return AuthResult.Failure(result.Errors.Select(e => e.Description).ToArray());

            if (!await _roleManager.RoleExistsAsync("Customer"))
                await _roleManager.CreateAsync(new IdentityRole("Customer"));
            await _userManager.AddToRoleAsync(user, "Customer");
        }

        if (!user.IsActive)
            return AuthResult.Failure("This account has been deactivated.");

        var tokens = await GenerateTokensAsync(user);
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60");
        return AuthResult.Success(user.Id, tokens.AccessToken, tokens.RefreshToken, DateTime.UtcNow.AddMinutes(expiryMinutes));
    }

    public async Task<CustomerDto?> GetMyProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;
        return new CustomerDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumber,
            IsActive = user.IsActive,
            CreditLimit = user.CreditLimit,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> UpdateMyProfileAsync(string userId, string fullName, string? phone)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;
        user.FullName = fullName;
        user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<IReadOnlyDictionary<string, UserSummary>> GetUserSummariesAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, UserSummary>();

        var summaries = await _userManager.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserSummary
            {
                UserId = u.Id,
                FullName = u.FullName,
                Phone = u.PhoneNumber
            })
            .ToListAsync();

        return summaries.ToDictionary(s => s.UserId);
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> ChangeEmailAsync(string userId, string newEmail)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return (false, new[] { "User not found." });

        var existing = await _userManager.FindByEmailAsync(newEmail);
        if (existing is not null && existing.Id != userId)
            return (false, new[] { "That email is already in use by another account." });

        var setEmail = await _userManager.SetEmailAsync(user, newEmail);
        if (!setEmail.Succeeded) return (false, setEmail.Errors.Select(e => e.Description));

        var setUserName = await _userManager.SetUserNameAsync(user, newEmail);
        if (!setUserName.Succeeded) return (false, setUserName.Errors.Select(e => e.Description));

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        await _userManager.ChangeEmailAsync(user, newEmail, token);

        return (true, Array.Empty<string>());
    }
}
