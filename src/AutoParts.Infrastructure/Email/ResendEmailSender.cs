using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AutoParts.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoParts.Infrastructure.Email;

// resend's http api (https://resend.com) - used in production because render free
// blocks outbound smtp on ports 25/465/587 but allows https. local dev keeps using
// SmtpEmailSender against gmail because it works fine outside the render container.
public class ResendEmailSender : IEmailSender
{
    private readonly ResendSettings _settings;
    private readonly ILogger<ResendEmailSender> _log;
    private readonly HttpClient _http;

    public ResendEmailSender(
        IOptions<ResendSettings> settings,
        ILogger<ResendEmailSender> log,
        IHttpClientFactory httpFactory)
    {
        _settings = settings.Value;
        _log = log;
        _http = httpFactory.CreateClient();
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
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _log.LogWarning(
                "Resend API key not configured; logging email instead.\nTO: {To} <{Email}>\nSUBJECT: {Subject}\n\n{Body}",
                toName, toEmail, subject, textBody);
            return;
        }

        var fromHeader = string.IsNullOrWhiteSpace(_settings.FromName)
            ? _settings.FromAddress
            : $"{_settings.FromName} <{_settings.FromAddress}>";

        var payload = new ResendEmailPayload
        {
            From = fromHeader,
            To = new[] { toEmail },
            Subject = subject,
            Html = htmlBody,
            Text = textBody,
            Attachments = attachments?.Select(a => new ResendAttachment
            {
                Filename = a.FileName,
                Content = Convert.ToBase64String(a.Content),
                ContentType = a.ContentType,
            }).ToArray()
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        req.Content = JsonContent.Create(payload);

        try
        {
            var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                _log.LogError("Resend rejected email to {Email}: {Status} {Body}", toEmail, (int)res.StatusCode, body);
                throw new InvalidOperationException($"Resend rejected the email: {(int)res.StatusCode} {body}");
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to send email to {Email} via Resend", toEmail);
            throw;
        }
    }

    // resend's expected request body shape - keep these private and ignore unused JSON warnings
    private class ResendEmailPayload
    {
        [JsonPropertyName("from")] public string From { get; set; } = string.Empty;
        [JsonPropertyName("to")] public string[] To { get; set; } = Array.Empty<string>();
        [JsonPropertyName("subject")] public string Subject { get; set; } = string.Empty;
        [JsonPropertyName("html")] public string Html { get; set; } = string.Empty;
        [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;

        [JsonPropertyName("attachments")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResendAttachment[]? Attachments { get; set; }
    }

    private class ResendAttachment
    {
        [JsonPropertyName("filename")] public string Filename { get; set; } = string.Empty;
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
        [JsonPropertyName("content_type")] public string ContentType { get; set; } = "application/octet-stream";
    }
}

public class ResendSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "onboarding@resend.dev";
    public string FromName { get; set; } = "AutoParts";
}
