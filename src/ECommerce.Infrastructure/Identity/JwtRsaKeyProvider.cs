using System.Security.Cryptography;

namespace ECommerce.Infrastructure.Identity;

public sealed class JwtRsaKeyProvider : IDisposable
{
    public JwtRsaKeyProvider(JwtOptions options)
    {
        Key = LoadOrCreateKey(options);
    }

    public RSA Key { get; }

    public void Dispose() => Key.Dispose();

    private static RSA LoadOrCreateKey(JwtOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PrivateKeyPem))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(options.PrivateKeyPem);
            return rsa;
        }

        var keyFile = options.KeyFile ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ECommerce",
            "jwt-dev.pem");

        if (File.Exists(keyFile))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(keyFile));
            return rsa;
        }

        var generated = RSA.Create(2048);
        Directory.CreateDirectory(Path.GetDirectoryName(keyFile)!);
        File.WriteAllText(keyFile, generated.ExportPkcs8PrivateKeyPem());
        return generated;
    }
}
