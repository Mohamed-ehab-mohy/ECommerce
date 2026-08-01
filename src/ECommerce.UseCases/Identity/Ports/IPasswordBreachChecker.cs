namespace ECommerce.UseCases.Identity.Ports;

public interface IPasswordBreachChecker
{
    Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken);
}
