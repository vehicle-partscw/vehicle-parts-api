using AutoParts.Application.Common.Exceptions;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Application.Common.Security;
using AutoParts.Domain.Common;
using AutoParts.Domain.Entities;
using AutoParts.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Features.SalesInvoices;

public class SalesInvoiceLineDto
{
    public Guid PartId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class SalesInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerUserId { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public Guid? LoyaltyTierId { get; set; }
    public string? LoyaltyTierName { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? EmailSentAt { get; set; }
    public DateTime? LastOverdueReminderAt { get; set; }
    public List<SalesInvoiceLineDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public static class CreateSalesInvoice
{
    public class LineInput
    {
        public Guid PartId { get; set; }
        public int Quantity { get; set; }
    }

    public class Command : IRequest<Guid>
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerUserId { get; set; } = string.Empty;
        public DateOnly IssueDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public Guid? RelatedAppointmentId { get; set; }   // optional - links the invoice back to a Done appointment
        public List<LineInput> Items { get; set; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(32);
            RuleFor(x => x.CustomerUserId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).ChildRules(line =>
            {
                line.RuleFor(l => l.PartId).NotEmpty();
                line.RuleFor(l => l.Quantity).GreaterThan(0);
            });
        }
    }

    public class Handler : IRequestHandler<Command, Guid>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        private readonly IIdentityService _identity;
        public Handler(IApplicationDbContext db, ICurrentUser user, IIdentityService identity)
        {
            _db = db;
            _user = user;
            _identity = identity;
        }

        public async Task<Guid> Handle(Command req, CancellationToken ct)
        {
            var number = req.InvoiceNumber.Trim();
            if (await _db.SalesInvoices.AnyAsync(s => s.InvoiceNumber == number, ct))
                throw new DomainException($"Invoice number '{number}' already exists.");

            var partIds = req.Items.Select(i => i.PartId).Distinct().ToList();
            var parts = await _db.Parts
                .Include(p => p.Vendor)
                .Where(p => partIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            var lines = new List<SalesInvoiceItem>();
            // parts whose stock crossed below the reorder level after this sale; we'll alert admins after save
            var partsNowLow = new List<AutoParts.Domain.Entities.Part>();
            foreach (var line in req.Items)
            {
                if (!parts.TryGetValue(line.PartId, out var part))
                    throw new NotFoundException("Part", line.PartId);

                if (part.StockQty < line.Quantity)
                    throw new DomainException($"Insufficient stock for SKU '{part.Sku}'. Available: {part.StockQty}.");

                var wasAbove = part.StockQty > part.ReorderLevel;
                part.StockQty -= line.Quantity;
                var nowAtOrBelow = part.StockQty <= part.ReorderLevel;
                if (wasAbove && nowAtOrBelow && part.ReorderLevel > 0)
                    partsNowLow.Add(part);

                lines.Add(new SalesInvoiceItem
                {
                    PartId = part.Id,
                    Quantity = line.Quantity,
                    UnitPrice = part.UnitPrice,
                    LineTotal = part.UnitPrice * line.Quantity
                });
            }

            var subtotal = lines.Sum(l => l.LineTotal);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var tier = await _db.LoyaltyTiers
                .Where(t => t.IsActive
                            && t.MinSinglePurchase <= subtotal
                            && t.ValidFrom <= today
                            && (t.ValidTo == null || t.ValidTo >= today))
                .OrderByDescending(t => t.MinSinglePurchase)
                .FirstOrDefaultAsync(ct);

            decimal discount = 0;
            if (tier is not null)
                discount = Math.Round(subtotal * tier.DiscountPercent / 100m, 2);

            var total = subtotal - discount;

            // if linked to an appointment, validate it belongs to the same customer and is Done
            if (req.RelatedAppointmentId.HasValue)
            {
                var appt = await _db.Appointments
                    .FirstOrDefaultAsync(a => a.Id == req.RelatedAppointmentId.Value, ct);
                if (appt is null)
                    throw new NotFoundException("Appointment", req.RelatedAppointmentId.Value);
                if (appt.CustomerUserId != req.CustomerUserId)
                    throw new DomainException("The appointment belongs to a different customer.");
            }

            var invoice = new SalesInvoice
            {
                InvoiceNumber = number,
                CustomerUserId = req.CustomerUserId,
                CreatedByUserId = _user.UserId ?? string.Empty,
                LoyaltyTierId = tier?.Id,
                RelatedAppointmentId = req.RelatedAppointmentId,
                IssueDate = req.IssueDate,
                DueDate = req.DueDate,
                Subtotal = subtotal,
                DiscountAmount = discount,
                TotalAmount = total,
                AmountPaid = 0,
                AmountDue = total,
                PaymentStatus = PaymentStatus.Unpaid,
                Items = lines
            };

            _db.SalesInvoices.Add(invoice);

            // queue low-stock notifications for any part that just dipped to/below its reorder level.
            // we send one notification per admin user, but skip parts that already have an unread alert
            // so admins don't get spammed with duplicates.
            if (partsNowLow.Count > 0)
            {
                var adminIds = await _identity.GetAdminUserIdsAsync();
                if (adminIds.Count > 0)
                {
                    foreach (var part in partsNowLow)
                    {
                        // we store the SKU (not the guid) so the inventory page's existing search filter
                        // can land on the right row when the admin clicks the notification
                        var alreadyHasUnread = await _db.Notifications.AnyAsync(
                            n => n.Type == NotificationType.LowStock
                                 && n.RelatedEntityName == "Part"
                                 && n.RelatedEntityId == part.Sku
                                 && n.ReadAt == null,
                            ct);
                        if (alreadyHasUnread) continue;

                        var vendorBit = string.IsNullOrEmpty(part.Vendor?.Name) ? "" : $" Vendor: {part.Vendor!.Name}.";
                        foreach (var adminId in adminIds)
                        {
                            _db.Notifications.Add(new Notification
                            {
                                RecipientUserId = adminId,
                                Type = NotificationType.LowStock,
                                Title = $"Low stock: {part.Name}",
                                Body = $"Only {part.StockQty} left in stock - reorder level is {part.ReorderLevel}.{vendorBit}",
                                RelatedEntityName = "Part",
                                RelatedEntityId = part.Sku
                            });
                        }
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
            return invoice.Id;
        }
    }
}

public static class GetSalesInvoiceById
{
    public class Query : IRequest<SalesInvoiceDto?> { public Guid Id { get; set; } }

    public class Handler : IRequestHandler<Query, SalesInvoiceDto?>
    {
        private readonly IApplicationDbContext _db;
        public Handler(IApplicationDbContext db) { _db = db; }
        public Task<SalesInvoiceDto?> Handle(Query req, CancellationToken ct) =>
            _db.SalesInvoices
                .Include(s => s.LoyaltyTier)
                .Include(s => s.Items).ThenInclude(i => i.Part)
                .Where(s => s.Id == req.Id)
                .Select(s => new SalesInvoiceDto
                {
                    Id = s.Id,
                    InvoiceNumber = s.InvoiceNumber,
                    CustomerUserId = s.CustomerUserId,
                    CreatedByUserId = s.CreatedByUserId,
                    LoyaltyTierId = s.LoyaltyTierId,
                    LoyaltyTierName = s.LoyaltyTier != null ? s.LoyaltyTier.Name : null,
                    IssueDate = s.IssueDate,
                    DueDate = s.DueDate,
                    Subtotal = s.Subtotal,
                    DiscountAmount = s.DiscountAmount,
                    TotalAmount = s.TotalAmount,
                    AmountPaid = s.AmountPaid,
                    AmountDue = s.AmountDue,
                    PaymentStatus = s.PaymentStatus.ToString(),
                    EmailSentAt = s.EmailSentAt,
                    LastOverdueReminderAt = s.LastOverdueReminderAt,
                    Items = s.Items.Select(i => new SalesInvoiceLineDto
                    {
                        PartId = i.PartId,
                        Sku = i.Part != null ? i.Part.Sku : string.Empty,
                        PartName = i.Part != null ? i.Part.Name : string.Empty,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        LineTotal = i.LineTotal
                    }).ToList(),
                    CreatedAt = s.CreatedAt
                })
                .FirstOrDefaultAsync(ct);
    }
}

public static class GetSalesInvoices
{
    public class Query : PaginationParams, IRequest<PaginatedList<SalesInvoiceDto>>
    {
        public string? CustomerUserId { get; set; }
        public PaymentStatus? Status { get; set; }
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
        public bool? Overdue { get; set; }
    }

    public class Handler : IRequestHandler<Query, PaginatedList<SalesInvoiceDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _user;
        public Handler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

        public Task<PaginatedList<SalesInvoiceDto>> Handle(Query req, CancellationToken ct)
        {
            var q = _db.SalesInvoices.Include(s => s.LoyaltyTier).AsNoTracking();

            if (_user.IsInRole(Roles.Customer))
                q = q.Where(s => s.CustomerUserId == _user.UserId);
            else if (!string.IsNullOrWhiteSpace(req.CustomerUserId))
                q = q.Where(s => s.CustomerUserId == req.CustomerUserId);

            if (req.Status.HasValue) q = q.Where(s => s.PaymentStatus == req.Status.Value);
            if (req.From.HasValue) q = q.Where(s => s.IssueDate >= req.From.Value);
            if (req.To.HasValue) q = q.Where(s => s.IssueDate <= req.To.Value);
            if (req.Overdue == true)
            {
                var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);
                q = q.Where(s => s.AmountDue > 0 && s.IssueDate < cutoff);
            }

            var dtos = q.OrderByDescending(s => s.IssueDate).Select(s => new SalesInvoiceDto
            {
                Id = s.Id,
                InvoiceNumber = s.InvoiceNumber,
                CustomerUserId = s.CustomerUserId,
                CreatedByUserId = s.CreatedByUserId,
                LoyaltyTierId = s.LoyaltyTierId,
                LoyaltyTierName = s.LoyaltyTier != null ? s.LoyaltyTier.Name : null,
                IssueDate = s.IssueDate,
                DueDate = s.DueDate,
                Subtotal = s.Subtotal,
                DiscountAmount = s.DiscountAmount,
                TotalAmount = s.TotalAmount,
                AmountPaid = s.AmountPaid,
                AmountDue = s.AmountDue,
                PaymentStatus = s.PaymentStatus.ToString(),
                EmailSentAt = s.EmailSentAt,
                LastOverdueReminderAt = s.LastOverdueReminderAt,
                CreatedAt = s.CreatedAt
            });
            return PaginatedList<SalesInvoiceDto>.CreateAsync(dtos, req.Page, req.PageSize, ct);
        }
    }
}

public static class EmailInvoice
{
    public class Command : IRequest<Unit>
    {
        public Guid Id { get; set; }
    }

    public class Handler : IRequestHandler<Command, Unit>
    {
        // hardcoded site url is fine - we only ever deploy to one frontend.
        // can be moved to settings later if we need preview/staging links.
        private const string SiteUrl = "https://autoparts-ebon.vercel.app";

        private readonly IApplicationDbContext _db;
        private readonly IEmailSender _email;
        private readonly IIdentityService _identity;

        public Handler(IApplicationDbContext db, IEmailSender email, IIdentityService identity)
        {
            _db = db;
            _email = email;
            _identity = identity;
        }

        public async Task<Unit> Handle(Command req, CancellationToken ct)
        {
            // load the invoice with everything we need to render it
            var invoice = await _db.SalesInvoices
                .Include(s => s.LoyaltyTier)
                .Include(s => s.Items).ThenInclude(i => i.Part)
                .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
                ?? throw new NotFoundException("SalesInvoice", req.Id);

            // look up customer email and full name (these are not on the invoice row)
            var customer = await _identity.GetCustomerByIdAsync(invoice.CustomerUserId)
                ?? throw new DomainException("Customer for this invoice was not found.");

            if (string.IsNullOrWhiteSpace(customer.Email))
                throw new DomainException("Customer does not have an email on file.");

            // build the dto we hand to the email + pdf builders
            var dto = new SalesInvoiceDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerUserId = invoice.CustomerUserId,
                CreatedByUserId = invoice.CreatedByUserId,
                LoyaltyTierId = invoice.LoyaltyTierId,
                LoyaltyTierName = invoice.LoyaltyTier?.Name,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                Subtotal = invoice.Subtotal,
                DiscountAmount = invoice.DiscountAmount,
                TotalAmount = invoice.TotalAmount,
                AmountPaid = invoice.AmountPaid,
                AmountDue = invoice.AmountDue,
                PaymentStatus = invoice.PaymentStatus.ToString(),
                EmailSentAt = invoice.EmailSentAt,
                Items = invoice.Items.Select(i => new SalesInvoiceLineDto
                {
                    PartId = i.PartId,
                    Sku = i.Part?.Sku ?? string.Empty,
                    PartName = i.Part?.Name ?? string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList(),
                CreatedAt = invoice.CreatedAt
            };

            var content = InvoiceEmailBuilder.Build(dto, customer.FullName, SiteUrl);
            var pdfBytes = InvoicePdfBuilder.Build(dto, customer.FullName);

            var attachment = new EmailAttachment(
                FileName: $"invoice-{invoice.InvoiceNumber}.pdf",
                Content: pdfBytes,
                ContentType: "application/pdf");

            await _email.SendAsync(
                toEmail: customer.Email,
                toName: customer.FullName,
                subject: content.Subject,
                htmlBody: content.HtmlBody,
                textBody: content.TextBody,
                attachments: new[] { attachment },
                ct: ct);

            // record the timestamp on the invoice (overwrites on every re-send)
            invoice.EmailSentAt = DateTime.UtcNow;

            // notify the customer in-app so they see a bell badge next time they log in
            _db.Notifications.Add(new Notification
            {
                RecipientUserId = invoice.CustomerUserId,
                Type = NotificationType.InvoiceEmailed,
                Title = $"Invoice {invoice.InvoiceNumber} sent",
                Body = $"We have emailed your invoice {invoice.InvoiceNumber} to {customer.Email}.",
                RelatedEntityName = "SalesInvoice",
                RelatedEntityId = invoice.Id.ToString()
            });

            await _db.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}

public static class ScanOverdueInvoices
{
    public class Result
    {
        public int Found { get; set; }      // invoices that matched the overdue criteria
        public int Sent { get; set; }       // reminders successfully emailed
        public int Skipped { get; set; }    // skipped due to missing email or rate-limit edge cases
        public int Failed { get; set; }     // smtp errors
    }

    public class Command : IRequest<Result> { }

    public class Handler : IRequestHandler<Command, Result>
    {
        // hardcoded site url is fine - we only ever deploy to one frontend
        private const string SiteUrl = "https://autoparts-ebon.vercel.app";

        // overdue means the invoice is at least this many days past its issue date
        private static readonly TimeSpan OverdueAfter = TimeSpan.FromDays(30);
        // don't re-send a reminder more often than this per invoice
        private static readonly TimeSpan ReminderCooldown = TimeSpan.FromDays(7);

        private readonly IApplicationDbContext _db;
        private readonly IEmailSender _email;
        private readonly IIdentityService _identity;

        public Handler(IApplicationDbContext db, IEmailSender email, IIdentityService identity)
        {
            _db = db;
            _email = email;
            _identity = identity;
        }

        public async Task<Result> Handle(Command req, CancellationToken ct)
        {
            var result = new Result();
            var nowUtc = DateTime.UtcNow;
            var overdueCutoff = DateOnly.FromDateTime(nowUtc - OverdueAfter);
            var cooldownCutoff = nowUtc - ReminderCooldown;

            // find candidates: OnCredit, money still owed, issued > 30 days ago,
            // and either never reminded or last reminder was >= 7 days ago
            var candidates = await _db.SalesInvoices
                .Include(s => s.LoyaltyTier)
                .Include(s => s.Items).ThenInclude(i => i.Part)
                .Where(s => s.PaymentStatus == PaymentStatus.OnCredit
                            && s.AmountDue > 0
                            && s.IssueDate < overdueCutoff
                            && (s.LastOverdueReminderAt == null || s.LastOverdueReminderAt < cooldownCutoff))
                .ToListAsync(ct);

            result.Found = candidates.Count;
            if (candidates.Count == 0) return result;

            // batch-load customer emails so we don't hit identity per row
            var customerIds = candidates.Select(c => c.CustomerUserId).Distinct().ToList();
            var customers = new Dictionary<string, CustomerDto>();
            foreach (var cid in customerIds)
            {
                var dto = await _identity.GetCustomerByIdAsync(cid);
                if (dto is not null) customers[cid] = dto;
            }

            foreach (var invoice in candidates)
            {
                if (!customers.TryGetValue(invoice.CustomerUserId, out var customer)
                    || string.IsNullOrWhiteSpace(customer.Email))
                {
                    result.Skipped++;
                    continue;
                }

                var dto = ToDto(invoice);
                var daysOverdue = nowUtc.Date.Subtract(invoice.IssueDate.ToDateTime(TimeOnly.MinValue)).Days;
                var content = OverdueReminderEmailBuilder.Build(dto, customer.FullName, daysOverdue, SiteUrl);
                var pdfBytes = InvoicePdfBuilder.Build(dto, customer.FullName);

                var attachment = new EmailAttachment(
                    FileName: $"invoice-{invoice.InvoiceNumber}.pdf",
                    Content: pdfBytes,
                    ContentType: "application/pdf");

                try
                {
                    await _email.SendAsync(
                        toEmail: customer.Email,
                        toName: customer.FullName,
                        subject: content.Subject,
                        htmlBody: content.HtmlBody,
                        textBody: content.TextBody,
                        attachments: new[] { attachment },
                        ct: ct);

                    invoice.LastOverdueReminderAt = nowUtc;
                    result.Sent++;
                }
                catch
                {
                    // smtp blew up - leave LastOverdueReminderAt alone so next cycle picks it up again
                    result.Failed++;
                }
            }

            await _db.SaveChangesAsync(ct);
            return result;
        }

        private static SalesInvoiceDto ToDto(SalesInvoice invoice) => new()
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerUserId = invoice.CustomerUserId,
            CreatedByUserId = invoice.CreatedByUserId,
            LoyaltyTierId = invoice.LoyaltyTierId,
            LoyaltyTierName = invoice.LoyaltyTier?.Name,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Subtotal = invoice.Subtotal,
            DiscountAmount = invoice.DiscountAmount,
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            AmountDue = invoice.AmountDue,
            PaymentStatus = invoice.PaymentStatus.ToString(),
            EmailSentAt = invoice.EmailSentAt,
            LastOverdueReminderAt = invoice.LastOverdueReminderAt,
            Items = invoice.Items.Select(i => new SalesInvoiceLineDto
            {
                PartId = i.PartId,
                Sku = i.Part?.Sku ?? string.Empty,
                PartName = i.Part?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList(),
            CreatedAt = invoice.CreatedAt
        };
    }
}
