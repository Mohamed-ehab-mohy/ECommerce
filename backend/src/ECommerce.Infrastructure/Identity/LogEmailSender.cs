using ECommerce.UseCases.Identity.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Identity;

public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Outbound email (stub): To {Recipient} Subject {Subject} BodyLength {BodyLength}",
            MaskEmail(message.To),
            message.Subject,
            message.HtmlBody.Length);

        return Task.CompletedTask;
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? "***" : $"{email[..1]}***{email[at..]}";
    }
}
