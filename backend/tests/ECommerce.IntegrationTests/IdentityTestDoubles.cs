using ECommerce.UseCases.Identity.Ports;
using ECommerce.UseCases.Common;

namespace ECommerce.IntegrationTests;

internal sealed class FakeCurrentUser(
    bool isAuthenticated = false,
    Guid? userId = null) : ICurrentUser
{
    public Guid? UserId { get; } = userId;

    public bool IsAuthenticated { get; } = isAuthenticated;

    public IReadOnlyList<string> Roles { get; } = [];

    public IReadOnlyList<string> Permissions { get; } = [];
}

internal sealed class NonBreachedPasswordChecker : IPasswordBreachChecker
{
    public Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}

public sealed class CapturingEmailSender : IEmailSender
{
    public List<EmailMessage> Messages { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}
