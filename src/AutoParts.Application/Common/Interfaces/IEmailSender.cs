namespace AutoParts.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, string textBody, CancellationToken ct = default);
}
