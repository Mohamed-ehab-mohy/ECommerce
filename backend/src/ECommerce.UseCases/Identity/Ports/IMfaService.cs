namespace ECommerce.UseCases.Identity.Ports;

public interface IMfaService
{
    string GenerateSecretKey();
    string GetTotpUri(string secretKey, string email, string issuer);
    bool VerifyTotp(string secretKey, string code);
    string GetCurrentCode(string secretKey);
}
