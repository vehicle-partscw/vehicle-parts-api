using AutoParts.Domain.Entities;
using AutoParts.Domain.Enums;
using AutoParts.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Infrastructure.Persistence.Seeding;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        if (await db.Parts.AnyAsync()) return;

        var adminId = (await users.FindByEmailAsync("admin@autoparts.com"))?.Id ?? string.Empty;

        // vendors
        var bosch = new Vendor { Name = "Bosch Auto Parts", ContactPerson = "Ravi Shrestha", Phone = "+977 9801234567", Email = "ravi@bosch.example", Address = "Putalisadak, Kathmandu", IsActive = true };
        var toyota = new Vendor { Name = "Toyota Spares Nepal", ContactPerson = "Sita Maharjan", Phone = "+977 9842211222", Email = "sita@toyota-spares.example", Address = "Naxal, Kathmandu", IsActive = true };
        var hyundai = new Vendor { Name = "Hyundai Genuine Parts", ContactPerson = "Anil KC", Phone = "+977 9803001100", Email = "anil@hyundai-parts.example", Address = "Thapathali, Kathmandu", IsActive = true };
        db.Vendors.AddRange(bosch, toyota, hyundai);

        // categories
        var engine = new PartCategory { Name = "Engine", Description = "Engine internals and consumables" };
        var brakes = new PartCategory { Name = "Brakes", Description = "Brake pads, discs, fluid" };
        var electrical = new PartCategory { Name = "Electrical", Description = "Batteries, plugs, wiring" };
        var suspension = new PartCategory { Name = "Suspension", Description = "Shocks, struts, bushings" };
        db.PartCategories.AddRange(engine, brakes, electrical, suspension);

        await db.SaveChangesAsync();

        // parts (priced in NPR)
        var parts = new List<Part>
        {
            new() { Sku = "OIL-FLT-001",  Name = "Oil Filter (Toyota)",     CategoryId = engine.Id,     VendorId = toyota.Id, UnitPrice = 750,  StockQty = 0, ReorderLevel = 12 },
            new() { Sku = "AIR-FLT-002",  Name = "Air Filter (Hyundai)",    CategoryId = engine.Id,     VendorId = hyundai.Id,UnitPrice = 1200, StockQty = 0, ReorderLevel = 10 },
            new() { Sku = "SPK-PLG-003",  Name = "Spark Plug NGK",          CategoryId = electrical.Id, VendorId = bosch.Id,  UnitPrice = 480,  StockQty = 0, ReorderLevel = 30 },
            new() { Sku = "BRK-PAD-F04", Name = "Brake Pad Front (Civic)", CategoryId = brakes.Id,     VendorId = bosch.Id,  UnitPrice = 2100, StockQty = 0, ReorderLevel = 8 },
            new() { Sku = "BRK-DSC-R05", Name = "Brake Disc Rear",         CategoryId = brakes.Id,     VendorId = toyota.Id, UnitPrice = 4800, StockQty = 0, ReorderLevel = 6 },
            new() { Sku = "BAT-12V-006", Name = "Battery 12V 60Ah",        CategoryId = electrical.Id, VendorId = hyundai.Id,UnitPrice = 9500, StockQty = 0, ReorderLevel = 5 },
            new() { Sku = "SHK-FRT-007", Name = "Shock Absorber Front",    CategoryId = suspension.Id, VendorId = toyota.Id, UnitPrice = 5400, StockQty = 0, ReorderLevel = 4 },
            new() { Sku = "WPR-BLD-008", Name = "Wiper Blade Pair",        CategoryId = electrical.Id, VendorId = bosch.Id,  UnitPrice = 850,  StockQty = 0, ReorderLevel = 15 },
        };
        db.Parts.AddRange(parts);

        // loyalty tier matching the brief: 10% over 5000
        var tier = new LoyaltyTier
        {
            Name = "Default",
            MinSinglePurchase = 5000m,
            DiscountPercent = 10m,
            ValidFrom = new DateOnly(2026, 1, 1),
            IsActive = true
        };
        db.LoyaltyTiers.Add(tier);

        // service types
        db.ServiceTypes.AddRange(
            new ServiceType { Code = "OILCHG",  Name = "Oil Change",         BasePrice = 1500m, EstimatedMinutes = 30,  IsActive = true },
            new ServiceType { Code = "BRKINSP", Name = "Brake Inspection",   BasePrice = 1000m, EstimatedMinutes = 45,  IsActive = true },
            new ServiceType { Code = "TUNEUP",  Name = "Major Tune-up",      BasePrice = 6500m, EstimatedMinutes = 180, IsActive = true }
        );

        await db.SaveChangesAsync();

        // a couple of customers
        var customer1 = await EnsureCustomer(users, "alice@example.com", "Alice Sharma", "Customer@123");
        var customer2 = await EnsureCustomer(users, "ram@example.com",   "Ram Bahadur", "Customer@123");

        // a couple of vehicles per customer
        if (!await db.Vehicles.AnyAsync())
        {
            db.Vehicles.AddRange(
                new Vehicle { CustomerUserId = customer1, VehicleNumber = "BA-2-PA-1234", Make = "Honda",  Model = "Civic",   Year = 2018, Vin = "JHMFA16506S000001", Mileage = 64500 },
                new Vehicle { CustomerUserId = customer1, VehicleNumber = "BA-12-CHA-9088", Make = "Toyota", Model = "Yaris",   Year = 2020, Mileage = 28000 },
                new Vehicle { CustomerUserId = customer2, VehicleNumber = "BA-1-KHA-5512",  Make = "Hyundai", Model = "Creta", Year = 2021, Mileage = 18200 }
            );
            await db.SaveChangesAsync();
        }

        // purchase invoices stock the parts up across the past year
        var rnd = new Random(7);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pInvCounter = 1001;
        for (int monthsAgo = 11; monthsAgo >= 0; monthsAgo--)
        {
            var date = today.AddMonths(-monthsAgo);
            var vendor = monthsAgo % 3 == 0 ? toyota : monthsAgo % 3 == 1 ? bosch : hyundai;
            var lineCount = rnd.Next(2, 4);
            var lines = new List<PurchaseInvoiceItem>();
            for (int i = 0; i < lineCount; i++)
            {
                var part = parts[rnd.Next(parts.Count)];
                var qty = rnd.Next(5, 20);
                lines.Add(new PurchaseInvoiceItem
                {
                    PartId = part.Id,
                    Quantity = qty,
                    UnitCost = part.UnitPrice * 0.65m,
                    LineTotal = qty * part.UnitPrice * 0.65m
                });
                part.StockQty += qty;
            }

            db.PurchaseInvoices.Add(new PurchaseInvoice
            {
                InvoiceNumber = $"PUR-{pInvCounter++:0000}",
                VendorId = vendor.Id,
                CreatedByUserId = adminId,
                IssueDate = date,
                Items = lines,
                TotalAmount = lines.Sum(l => l.LineTotal)
            });
        }

        await db.SaveChangesAsync();

        // sales invoices spread across past 12 months - some triggering loyalty discount
        var sInvCounter = 5001;
        for (int monthsAgo = 11; monthsAgo >= 0; monthsAgo--)
        {
            var monthDate = today.AddMonths(-monthsAgo);
            int salesThisMonth = rnd.Next(2, 5);
            for (int i = 0; i < salesThisMonth; i++)
            {
                var date = monthDate.AddDays(-rnd.Next(0, 25));
                var customerId = rnd.Next(2) == 0 ? customer1 : customer2;
                var lineCount = rnd.Next(1, 4);
                var lines = new List<SalesInvoiceItem>();
                foreach (var _ in Enumerable.Range(0, lineCount))
                {
                    var part = parts[rnd.Next(parts.Count)];
                    if (part.StockQty <= 0) continue;
                    var qty = Math.Min(rnd.Next(1, 4), part.StockQty);
                    lines.Add(new SalesInvoiceItem
                    {
                        PartId = part.Id,
                        Quantity = qty,
                        UnitPrice = part.UnitPrice,
                        LineTotal = qty * part.UnitPrice
                    });
                    part.StockQty -= qty;
                }

                if (lines.Count == 0) continue;

                var subtotal = lines.Sum(l => l.LineTotal);
                var discount = subtotal >= tier.MinSinglePurchase
                    ? Math.Round(subtotal * tier.DiscountPercent / 100m, 2)
                    : 0m;
                var total = subtotal - discount;
                var paid = rnd.Next(3) == 0 ? 0m : total;
                var status = paid >= total
                    ? PaymentStatus.Paid
                    : paid > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Unpaid;

                db.SalesInvoices.Add(new SalesInvoice
                {
                    InvoiceNumber = $"SAL-{sInvCounter++:0000}",
                    CustomerUserId = customerId,
                    CreatedByUserId = adminId,
                    LoyaltyTierId = discount > 0 ? tier.Id : null,
                    IssueDate = date,
                    DueDate = date.AddDays(30),
                    Subtotal = subtotal,
                    DiscountAmount = discount,
                    TotalAmount = total,
                    AmountPaid = paid,
                    AmountDue = total - paid,
                    PaymentStatus = status,
                    Items = lines
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task<string> EnsureCustomer(UserManager<ApplicationUser> users, string email, string fullName, string password)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null) return existing.Id;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var res = await users.CreateAsync(user, password);
        if (res.Succeeded) await users.AddToRoleAsync(user, "Customer");
        return user.Id;
    }
}
