using System.Security.Cryptography;
using System.Text;
using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Domain.Common;
using AutoParts.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Auth;

internal static class CodeHasher
{
    public static string Hash(string code)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }
}

public static class ForgotPassword
{
    public class Command : IRequest<Unit>
    {
        public string Email { get; set; } = string.Empty;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IIdentityService _identity;
        private readonly IApplicationDbContext _db;
        private readonly IEmailSender _email;

        public Handler(IIdentityService identity, IApplicationDbContext db, IEmailSender email)
        {
            _identity = identity;
            _db = db;
            _email = email;
        }

        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            // Always return Unit.Value - never tell the caller whether the email exists.
            var user = await _identity.FindUserByEmailAsync(req.Email.Trim());
            if (user is null) return Unit.Value;

            // generate a 6-digit code, store its hash with a 15-minute expiry
            var code = Random.Shared.Next(100_000, 999_999).ToString();

            // invalidate any active codes for this user before issuing a new one
            var active = await _db.PasswordResetCodes
                .Where(c => c.UserId == user.UserId && c.ConsumedAt == null && c.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(ct);
            foreach (var c in active) c.ConsumedAt = DateTime.UtcNow;

            _db.PasswordResetCodes.Add(new PasswordResetCode
            {
                UserId = user.UserId,
                CodeHash = CodeHasher.Hash(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            });
            await _db.SaveChangesAsync(ct);

            // send the code via email
            var subject = "Your AutoParts password reset code";
            var html = $@"
<div style=""font-family: Arial, sans-serif; max-width:520px; margin:auto; color:#1A0F0C"">
  <h2 style=""color:#5C271F; margin:0 0 12px"">Reset your password</h2>
  <p>Hi {user.FullName},</p>
  <p>Use this code to reset your AutoParts password. It expires in <strong>15 minutes</strong>.</p>
  <p style=""font-size:32px; letter-spacing:6px; font-weight:700; background:#FFE7E0; padding:14px 18px; border-radius:10px; text-align:center; color:#E54D2E"">{code}</p>
  <p style=""color:#666; font-size:13px"">If you didn't request a reset, you can safely ignore this email.</p>
</div>";
            var text = $"Your AutoParts password reset code is {code}. It expires in 15 minutes.";

            try
            {
                await _email.SendAsync(user.Email, user.FullName, subject, html, text, ct: ct);
            }
            catch
            {
                // swallow - don't reveal delivery problems to the caller
            }

            return Unit.Value;
        }
    }
}

public static class VerifyResetCode
{
    public class Command : IRequest<string>
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$");
        }
    }

    public class Handler : IRequestHandler<Command, string>
    {
        private readonly IIdentityService _identity;
        private readonly IApplicationDbContext _db;
        public Handler(IIdentityService identity, IApplicationDbContext db) { _identity = identity; _db = db; }

        public async Task<string> Handle(Command req, CancellationToken ct)
        {
            var user = await _identity.FindUserByEmailAsync(req.Email.Trim());
            if (user is null) throw new DomainException("Invalid code.");

            var hash = CodeHasher.Hash(req.Code);
            var entry = await _db.PasswordResetCodes
                .Where(c => c.UserId == user.UserId
                            && c.ConsumedAt == null
                            && c.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (entry is null)
                throw new DomainException("Code has expired. Request a new one.");

            if (entry.CodeHash != hash)
            {
                entry.FailedAttempts += 1;
                if (entry.FailedAttempts >= 5)
                    entry.ConsumedAt = DateTime.UtcNow; // burn the code after too many tries
                await _db.SaveChangesAsync(ct);
                throw new DomainException("Invalid code.");
            }

            // generate a short-lived reset token (we'll just hand back the code-row id; staying server-side avoids
            // signing/key management). It's only valid until ConsumedAt is set in the next step.
            return entry.Id.ToString();
        }
    }
}

public static class ResetPasswordWithCode
{
    public class Command : IRequest<Unit>
    {
        public string Email { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;   // the entry id from VerifyResetCode
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.ResetToken).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(10);
            RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword)
                .WithMessage("Passwords do not match.");
        }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IIdentityService _identity;
        private readonly IApplicationDbContext _db;
        public Handler(IIdentityService identity, IApplicationDbContext db) { _identity = identity; _db = db; }

        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            if (!Guid.TryParse(req.ResetToken, out var entryId))
                throw new DomainException("Reset token is invalid.");

            var user = await _identity.FindUserByEmailAsync(req.Email.Trim());
            if (user is null) throw new DomainException("Reset token is invalid.");

            var entry = await _db.PasswordResetCodes
                .FirstOrDefaultAsync(c => c.Id == entryId && c.UserId == user.UserId, ct);

            if (entry is null || entry.ConsumedAt is not null || entry.ExpiresAt <= DateTime.UtcNow)
                throw new DomainException("Reset token has expired. Request a new code.");

            var (ok, errors) = await _identity.ResetPasswordAsync(user.UserId, req.NewPassword);
            if (!ok) throw new DomainException(string.Join(' ', errors));

            entry.ConsumedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            // sign the user out of every other device
            await _identity.RevokeAllRefreshTokensAsync(user.UserId);

            return Unit.Value;
        }
    }
}
