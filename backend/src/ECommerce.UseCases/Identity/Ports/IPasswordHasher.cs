namespace ECommerce.UseCases.Identity.Ports;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
