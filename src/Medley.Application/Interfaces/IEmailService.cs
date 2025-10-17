namespace Medley.Application.Interfaces;

/// <summary>
/// Service interface for sending emails (transport only)
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email with the specified subject and body
    /// </summary>
    Task SendEmailAsync(string to, string subject, string htmlBody);
}
