using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using ShipmentTracker.Core.Interfaces;

namespace ShipmentTracker.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpHost = _configuration["Email:SmtpHost"] ?? throw new InvalidOperationException("SMTP Host not configured");
        _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        _smtpUsername = _configuration["Email:SmtpUsername"] ?? throw new InvalidOperationException("SMTP Username not configured");
        _smtpPassword = _configuration["Email:SmtpPassword"] ?? throw new InvalidOperationException("SMTP Password not configured");
        _fromEmail = _configuration["Email:FromEmail"] ?? throw new InvalidOperationException("From Email not configured");
        _fromName = _configuration["Email:FromName"] ?? "Shipment Tracker";
    }

    public async Task SendEmailVerificationAsync(string email, string userName, string verificationToken)
    {
        var subject = "Verify Your Email Address";
        var verificationUrl = $"{_configuration["App:BaseUrl"]}/verify-email?token={verificationToken}";
        
        var body = $@"
            <html>
            <body>
                <h2>Welcome to Shipment Tracker!</h2>
                <p>Hello {userName},</p>
                <p>Thank you for registering with Shipment Tracker. Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationUrl}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email Address</a></p>
                <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                <p>{verificationUrl}</p>
                <p>This link will expire in 24 hours.</p>
                <p>If you didn't create an account, please ignore this email.</p>
                <br>
                <p>Best regards,<br>Shipment Tracker Team</p>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string userName, string resetToken)
    {
        var subject = "Reset Your Password";
        var resetUrl = $"{_configuration["App:BaseUrl"]}/reset-password?token={resetToken}";
        
        var body = $@"
            <html>
            <body>
                <h2>Password Reset Request</h2>
                <p>Hello {userName},</p>
                <p>We received a request to reset your password. Click the link below to reset your password:</p>
                <p><a href='{resetUrl}' style='background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                <p>{resetUrl}</p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request a password reset, please ignore this email.</p>
                <br>
                <p>Best regards,<br>Shipment Tracker Team</p>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string userName)
    {
        var subject = "Welcome to Shipment Tracker!";
        
        var body = $@"
            <html>
            <body>
                <h2>Welcome to Shipment Tracker!</h2>
                <p>Hello {userName},</p>
                <p>Your email has been successfully verified. You can now log in to your account and start tracking your shipments.</p>
                <p>Here are some things you can do:</p>
                <ul>
                    <li>Track your shipments in real-time</li>
                    <li>View shipment history and events</li>
                    <li>Receive notifications about your shipments</li>
                    <li>Access announcements and updates</li>
                </ul>
                <p>If you have any questions, please don't hesitate to contact our support team.</p>
                <br>
                <p>Best regards,<br>Shipment Tracker Team</p>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        if (isHtml)
        {
            bodyBuilder.HtmlBody = body;
        }
        else
        {
            bodyBuilder.TextBody = body;
        }
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_smtpHost, _smtpPort, false);
            await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
            await client.SendAsync(message);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}
