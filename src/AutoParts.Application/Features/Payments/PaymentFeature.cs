using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Domain.Common;
using AutoParts.Domain.Entities;
using AutoParts.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.Payments;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid SalesInvoiceId { get; set; }
    public string ReceivedByUserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }
}

public static class RecordPayment
{
    public class Command : IRequest<Guid>
    {
        public Guid SalesInvoiceId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Notes { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SalesInvoiceId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
        }
    }

    public class Handler : IRequestHandler<Command, Guid>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

        public async Task<Guid> Handle(Command req, CancellationToken ct)
        {
            var invoice = await _db.SalesInvoices.FirstOrDefaultAsync(s => s.Id == req.SalesInvoiceId, ct)
                ?? throw new NotFoundException("SalesInvoice", req.SalesInvoiceId);

            if (req.Amount > invoice.AmountDue + 0.01m)
                throw new DomainException($"Payment ({req.Amount}) exceeds amount due ({invoice.AmountDue}).");

            var payment = new Payment
            {
                SalesInvoiceId = invoice.Id,
                ReceivedByUserId = _user.UserId ?? string.Empty,
                Amount = req.Amount,
                Method = req.Method,
                ReferenceNo = req.ReferenceNo,
                Notes = req.Notes,
                PaidAt = DateTime.UtcNow
            };
            _db.Payments.Add(payment);

            invoice.AmountPaid += req.Amount;
            invoice.AmountDue = invoice.TotalAmount - invoice.AmountPaid;

            if (invoice.AmountDue <= 0)
            {
                invoice.PaymentStatus = PaymentStatus.Paid;
            }
            else if (invoice.AmountPaid > 0)
            {
                var overdue = invoice.DueDate.HasValue && invoice.DueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);
                invoice.PaymentStatus = overdue ? PaymentStatus.OnCredit : PaymentStatus.PartiallyPaid;
            }

            await _db.SaveChangesAsync(ct);
            return payment.Id;
        }
    }
}

public static class GetPaymentsForInvoice
{
    public class Query : IRequest<IReadOnlyList<PaymentDto>>
    {
        public Guid SalesInvoiceId { get; set; }
    }

    public class Handler : IRequestHandler<Query, IReadOnlyList<PaymentDto>>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public async Task<IReadOnlyList<PaymentDto>> Handle(Query req, CancellationToken ct) =>
            await _db.Payments
                .Where(p => p.SalesInvoiceId == req.SalesInvoiceId)
                .OrderByDescending(p => p.PaidAt)
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    SalesInvoiceId = p.SalesInvoiceId,
                    ReceivedByUserId = p.ReceivedByUserId,
                    Amount = p.Amount,
                    Method = p.Method.ToString(),
                    ReferenceNo = p.ReferenceNo,
                    PaidAt = p.PaidAt,
                    Notes = p.Notes
                })
                .ToListAsync(ct);
    }
}
