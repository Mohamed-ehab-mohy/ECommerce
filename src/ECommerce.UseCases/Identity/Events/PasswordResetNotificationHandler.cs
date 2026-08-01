using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Identity.Events;

public sealed class PasswordResetNotificationHandler(
    IEmailSender emailSender,
    ILogger<PasswordResetNotificationHandler> logger) : IEventHandler<PasswordReset>
{
    public async Task HandleAsync(PasswordReset domainEvent, CancellationToken cancellationToken)
    {
        var body = $"<p>Hi {domainEvent.DisplayName},</p><p>Your password was changed. If this wasn't you, contact support immediately.</p>";

        if (domainEvent.NewVerificationToken is not null)
        {
            var verifyLink = $"https://app.ecommerce.dev/verify-email?token={domainEvent.NewVerificationToken}";
            body += $"<p>To keep your account secure, please re-verify your email: <a href=\"{verifyLink}\">{verifyLink}</a></p>";
        }

        await emailSender.SendAsync(new EmailMessage(
            domainEvent.Email,
            "Your password has been changed",
            body), cancellationToken);

        logger.LogInformation(
            "Password reset completed for customer {CustomerId}",
            domainEvent.CustomerId);
    }
}
