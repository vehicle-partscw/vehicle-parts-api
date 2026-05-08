using AutoParts.Application.Common.Interfaces;
using AutoParts.Infrastructure.Email;
using AutoParts.Infrastructure.Identity;
using AutoParts.Infrastructure.Persistence;
using AutoParts.Infrastructure.Persistence.Interceptors;
using AutoParts.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AutoParts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddScoped<AuditableEntityInterceptor>();

        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password requirements
            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;

            // Lockout settings
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication
        var jwtSecret = configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret not found in configuration.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // Services
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<IIdentityService, IdentityService>();

        // MediatR handlers that live in Infrastructure (e.g. GoogleSignInCommandHandler depending on Google.Apis.Auth)
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        // email sender. on render free we can't use smtp (ports 25/465/587 are blocked)
        // so when ResendSettings:ApiKey is configured we send via resend's https api;
        // otherwise we fall back to gmail smtp (works locally + on render paid tiers).
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.Configure<ResendSettings>(configuration.GetSection("ResendSettings"));

        var resendApiKey = configuration["ResendSettings:ApiKey"];
        if (!string.IsNullOrWhiteSpace(resendApiKey))
        {
            services.AddHttpClient();
            services.AddScoped<IEmailSender, ResendEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }

        // background service: scans every 24 hours for overdue On-Credit invoices and emails reminders
        services.AddHostedService<Services.OverdueInvoiceReminderService>();

        return services;
    }
}
