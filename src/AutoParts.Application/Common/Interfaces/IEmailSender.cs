namespace AutoParts.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        string textBody,
        IEnumerable<EmailAttachment>? attachments = null,
        CancellationToken ct = default);
}

public record EmailAttachment(string FileName, byte[] Content, string ContentType);
