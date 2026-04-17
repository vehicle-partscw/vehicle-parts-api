using AutoParts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<StaffProfile> StaffProfiles { get; }
    DbSet<Vendor> Vendors { get; }
    DbSet<PartCategory> PartCategories { get; }
    DbSet<Part> Parts { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<ServiceType> ServiceTypes { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<PurchaseInvoice> PurchaseInvoices { get; }
    DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; }
    DbSet<LoyaltyTier> LoyaltyTiers { get; }
    DbSet<SalesInvoice> SalesInvoices { get; }
    DbSet<SalesInvoiceItem> SalesInvoiceItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PartRequest> PartRequests { get; }
    DbSet<Review> Reviews { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AiPrediction> AiPredictions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
