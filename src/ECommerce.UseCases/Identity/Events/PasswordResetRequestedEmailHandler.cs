using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Identity.Events;

public sealed class PasswordResetRequestedEmailHandler(
    IEmailSender emailSender,
    ILogger<PasswordResetRequestedEmailHandler> logger) : IEventHandler<PasswordResetRequested>
{
    public async Task HandleAsync(PasswordResetRequested domainEvent, CancellationToken cancellationToken)
    {
        var resetLink = $"https://app.ecommerce.dev/reset-password?token={domainEvent.ResetToken}";
        var body = $"<p>Hi {domainEvent.DisplayName},</p><p>Reset your password: <a href=\"{resetLink}\">{resetLink}</a></p><p>This link expires in 30 minutes.</p>";

        await emailSender.SendAsync(new EmailMessage(
            domainEvent.Email,
            "Reset your password",
            body), cancellationToken);

        logger.LogInformation(
            "Password reset requested for customer {CustomerId}, expires {ExpiresAtUtc:O}",
            domainEvent.CustomerId,
            domainEvent.ExpiresAtUtc);
    }
}
