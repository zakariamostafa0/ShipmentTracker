namespace ShipmentTracker.Core.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string email, string userName, string verificationToken);
    Task SendPasswordResetAsync(string email, string userName, string resetToken);
    Task SendWelcomeEmailAsync(string email, string userName);
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
}
