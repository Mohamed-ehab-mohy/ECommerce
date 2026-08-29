namespace ECommerce.UseCases.Identity.Ports;

public sealed record AuthorizationCodeRecord(
    string Code,
    Guid UserId,
    string ClientId,
    string RedirectUri,
    IReadOnlyList<string> Scopes,
    string? CodeChallenge,
    string? CodeChallengeMethod);

public interface IAuthorizationCodeStore
{
    Task<string> CreateAsync(AuthorizationCodeRecord record, TimeSpan ttl, CancellationToken cancellationToken);

    Task<AuthorizationCodeRecord?> ConsumeAsync(string code, CancellationToken cancellationToken);
}
