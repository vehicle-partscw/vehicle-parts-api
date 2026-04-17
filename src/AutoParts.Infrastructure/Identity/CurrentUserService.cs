using System.Security.Claims;
using AutoParts.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AutoParts.Infrastructure.Identity;

public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContext;

    public CurrentUserService(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    public string? UserId =>
        _httpContext.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email =>
        _httpContext.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated =>
        _httpContext.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) =>
        _httpContext.HttpContext?.User.IsInRole(role) ?? false;
}
