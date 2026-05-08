using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using MediatR;

namespace AutoParts.Application.Features.Customers;

public static class GetCustomers
{
    public class Query : IRequest<IReadOnlyList<CustomerDto>> { }

    public class Handler : IRequestHandler<Query, IReadOnlyList<CustomerDto>>
    {
        private readonly IIdentityService _identity;
        public Handler(IIdentityService identity) { _identity = identity; }
        public Task<IReadOnlyList<CustomerDto>> Handle(Query req, CancellationToken ct) =>
            _identity.GetAllCustomersAsync();
    }
}

public static class GetCustomerById
{
    public class Query : IRequest<CustomerDto?> { public string UserId { get; set; } = string.Empty; }

    public class Handler : IRequestHandler<Query, CustomerDto?>
    {
        private readonly IIdentityService _identity;
        public Handler(IIdentityService identity) { _identity = identity; }
        public Task<CustomerDto?> Handle(Query req, CancellationToken ct) =>
            _identity.GetCustomerByIdAsync(req.UserId);
    }
}

public static class ToggleCustomerActive
{
    public class Command : IRequest<bool> { public string UserId { get; set; } = string.Empty; }

    public class Handler : IRequestHandler<Command, bool>
    {
        private readonly IIdentityService _identity;
        public Handler(IIdentityService identity) { _identity = identity; }
        public Task<bool> Handle(Command req, CancellationToken ct) =>
            _identity.ToggleCustomerActiveAsync(req.UserId);
    }
}

public static class UpdateCustomerCreditLimit
{
    public class Command : IRequest<bool>
    {
        public string UserId { get; set; } = string.Empty;
        public decimal? CreditLimit { get; set; }
    }

    public class Handler : IRequestHandler<Command, bool>
    {
        private readonly IIdentityService _identity;
        public Handler(IIdentityService identity) { _identity = identity; }
        public Task<bool> Handle(Command req, CancellationToken ct) =>
            _identity.UpdateCustomerCreditLimitAsync(req.UserId, req.CreditLimit);
    }
}

// admin-side profile edit so staff can fix a customer's name + phone without
// needing the customer to log in and edit their own profile.
public static class UpdateCustomerProfile
{
    public class Command : IRequest<bool>
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class Handler : IRequestHandler<Command, bool>
    {
        private readonly IIdentityService _identity;
        public Handler(IIdentityService identity) { _identity = identity; }
        public Task<bool> Handle(Command req, CancellationToken ct) =>
            _identity.UpdateMyProfileAsync(req.UserId, req.FullName, req.Phone);
    }
}
