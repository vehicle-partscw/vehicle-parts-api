using AutoParts.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AutoParts.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _log;

    public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> log)
    {
        _settings = settings.Value;
        _log = log;
    }

    public async Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        string textBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            // dev fallback so the otp / invoice still shows up in console when smtp isn't configured
            _log.LogWarning(
                "SMTP host not configured; logging email instead.\nTO: {To} <{Email}>\nSUBJECT: {Subject}\n\n{Body}",
                toName, toEmail, subject, textBody);
            return;
        }

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        msg.To.Add(new MailboxAddress(toName, toEmail));
        msg.Subject = subject;

        var body = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody
        };

        if (attachments is not null)
        {
            foreach (var a in attachments)
            {
                body.Attachments.Add(a.FileName, a.Content, ContentType.Parse(a.ContentType));
            }
        }

        msg.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _settings.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.SslOnConnect;

        try
        {
            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, ct);

            if (!string.IsNullOrEmpty(_settings.Username))
                await client.AuthenticateAsync(_settings.Username, _settings.Password ?? string.Empty, ct);

            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to send email to {Email} via {Host}:{Port}", toEmail, _settings.Host, _settings.Port);
            throw;
        }
    }
}
