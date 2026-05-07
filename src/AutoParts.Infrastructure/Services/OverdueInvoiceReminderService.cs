using AutoParts.Application.Features.SalesInvoices;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoParts.Infrastructure.Services;

// Background service that wakes up every 24 hours and sweeps the database for overdue
// On-Credit invoices, emailing the customer a reminder. Each cycle creates its own DI
// scope so we get a fresh DbContext + Mediator that can be safely disposed afterwards.
//
// Note: on Render's free tier the web service sleeps after ~15 minutes of no traffic.
// While asleep this timer cannot fire, so we also expose a manual /scan-overdue endpoint
// for admins (and an external uptime pinger keeps the service awake in production).
public class OverdueInvoiceReminderService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    // wait a little after startup so migrations + seeding finish first
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OverdueInvoiceReminderService> _log;

    public OverdueInvoiceReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<OverdueInvoiceReminderService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new ScanOverdueInvoices.Command(), stoppingToken);
                _log.LogInformation(
                    "Overdue invoice scan: found {Found}, sent {Sent}, skipped {Skipped}, failed {Failed}",
                    result.Found, result.Sent, result.Skipped, result.Failed);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Overdue invoice scan threw an exception");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (TaskCanceledException) { break; }
        }
    }
}
