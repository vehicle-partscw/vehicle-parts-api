using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Domain.Common;
using FluentValidation;
using MediatR;

namespace AutoParts.Application.Features.Me;

public static class GetMyProfile
{
    public class Query : IRequest<CustomerDto> { }

    public class Handler : IRequestHandler<Query, CustomerDto>
    {
        private readonly IIdentityService _identity;
        private readonly ICurrentUser _user;
        public Handler(IIdentityService identity, ICurrentUser user)
        {
            _identity = identity;
            _user = user;
        }

        public async Task<CustomerDto> Handle(Query req, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_user.UserId))
                throw new ForbiddenException();

            var dto = await _identity.GetMyProfileAsync(_user.UserId);
            if (dto is null)
                throw new NotFoundException("Profile", _user.UserId);

            return dto;
        }
    }
}

public static class ChangeMyEmail
{
    public class Command : IRequest<Unit>
    {
        public string NewEmail { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.NewEmail).NotEmpty().EmailAddress();
            RuleFor(x => x.CurrentPassword).NotEmpty()
                .WithMessage("Enter your current password to confirm the change.");
        }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IIdentityService _identity;
        private readonly ICurrentUser _user;
        public Handler(IIdentityService identity, ICurrentUser user) { _identity = identity; _user = user; }

        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_user.UserId))
                throw new ForbiddenException();

            var ok = await _identity.CheckPasswordAsync(_user.UserId, req.CurrentPassword);
            if (!ok)
                throw new DomainException("Current password is incorrect.");

            var (succeeded, errors) = await _identity.ChangeEmailAsync(_user.UserId, req.NewEmail.Trim());
            if (!succeeded)
                throw new DomainException(string.Join(' ', errors));

            // safest to invalidate all refresh tokens after email change so the user re-authenticates
            await _identity.RevokeAllRefreshTokensAsync(_user.UserId);

            return Unit.Value;
        }
    }
}

public static class ChangeMyPassword
{
    public class Command : IRequest<Unit>
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(10);
            RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword)
                .WithMessage("Passwords do not match.");
        }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IIdentityService _identity;
        private readonly ICurrentUser _user;
        public Handler(IIdentityService identity, ICurrentUser user) { _identity = identity; _user = user; }

        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_user.UserId))
                throw new ForbiddenException();

            var ok = await _identity.CheckPasswordAsync(_user.UserId, req.CurrentPassword);
            if (!ok)
                throw new DomainException("Current password is incorrect.");

            var (succeeded, errors) = await _identity.ResetPasswordAsync(_user.UserId, req.NewPassword);
            if (!succeeded)
                throw new DomainException(string.Join(' ', errors));

            await _identity.RevokeAllRefreshTokensAsync(_user.UserId);
            return Unit.Value;
        }
    }
}

public static class UpdateMyProfile
{
    public class Command : IRequest<Unit>
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FullName).NotEmpty().MinimumLength(2).MaximumLength(120);
            RuleFor(x => x.Phone).MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        private readonly IIdentityService _identity;
        private readonly ICurrentUser _user;
        public Handler(IIdentityService identity, ICurrentUser user)
        {
            _identity = identity;
            _user = user;
        }

        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_user.UserId))
                throw new ForbiddenException();

            var ok = await _identity.UpdateMyProfileAsync(_user.UserId, req.FullName.Trim(), req.Phone);
            if (!ok)
                throw new NotFoundException("Profile", _user.UserId);

            return Unit.Value;
        }
    }
}
