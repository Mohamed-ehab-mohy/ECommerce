using ECommerce.Domain.Notifications;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Notifications.Ports;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ECommerce.Infrastructure.Notifications;

public sealed class SmtpOptions
{
    public const string SectionName = "Notifications:Smtp";

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public bool UseSsl { get; init; } = true;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string From { get; init; } = "no-reply@ecommerce.dev";
}

public sealed class SmtpEmailProvider(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public string Key => "smtp";

    public async Task SendAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        var smtp = options.Value;

        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            logger.LogInformation(
                "Outbound email (stub): To {Recipient} Subject {Subject} BodyLength {BodyLength}",
                PiiMasker.MaskEmail(envelope.Recipient),
                envelope.Subject,
                envelope.Body.Length);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(smtp.From));
        message.To.Add(MailboxAddress.Parse(envelope.Recipient));
        message.Subject = envelope.Subject;
        message.Body = new BodyBuilder { HtmlBody = envelope.Body }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, smtp.UseSsl, cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation(
            "Email sent via SMTP to {Recipient} for {ReferenceId}.",
            PiiMasker.MaskEmail(envelope.Recipient),
            envelope.ReferenceId);
    }
}
