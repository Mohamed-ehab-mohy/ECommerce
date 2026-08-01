using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Identity.Events;

public sealed class CustomerRegisteredEmailHandler(
    IEmailSender emailSender,
    ILogger<CustomerRegisteredEmailHandler> logger) : IEventHandler<CustomerRegistered>
{
    public async Task HandleAsync(CustomerRegistered domainEvent, CancellationToken cancellationToken)
    {
        var verificationLink = $"https://app.ecommerce.dev/verify-email?token={domainEvent.VerificationToken}";
        var body = $"<p>Welcome {domainEvent.DisplayName}.</p><p>Verify your email: <a href=\"{verificationLink}\">{verificationLink}</a></p>";

        await emailSender.SendAsync(new EmailMessage(
            domainEvent.Email,
            "Verify your email address",
            body), cancellationToken);

        logger.LogInformation(
            "Verification email requested for customer {CustomerId}, expires {ExpiresAtUtc:O}",
            domainEvent.CustomerId,
            domainEvent.ExpiresAtUtc);
    }
}
