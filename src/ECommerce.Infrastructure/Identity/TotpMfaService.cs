using ECommerce.UseCases.Identity.Ports;
using OtpNet;
using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Infrastructure.Identity;

public sealed class TotpMfaService : IMfaService
{
    public string GenerateSecretKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Base32Encoding.ToString(bytes);
    }

    public string GetTotpUri(string secretKey, string email, string issuer)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
    }

    public bool VerifyTotp(string secretKey, string code)
    {
        var secretBytes = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(secretBytes);
        return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
    }

    public string GetCurrentCode(string secretKey)
    {
        var secretBytes = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(secretBytes);
        return totp.ComputeTotp();
    }
}
