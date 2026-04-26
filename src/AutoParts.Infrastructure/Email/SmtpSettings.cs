namespace AutoParts.Infrastructure.Email;

public class SmtpSettings
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@autoparts.local";
    public string FromName { get; set; } = "AutoParts";
    public bool UseStartTls { get; set; } = true;
}
