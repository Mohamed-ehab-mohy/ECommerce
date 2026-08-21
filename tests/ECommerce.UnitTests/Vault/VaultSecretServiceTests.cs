using ECommerce.Infrastructure.Vault;
using ECommerce.UseCases.Common;
using Microsoft.Extensions.Options;

namespace ECommerce.UnitTests.Tests.Vault;

public sealed class VaultSecretServiceTests
{
    private static VaultOptions CreateOptions(bool enabled = false) => new()
    {
        Address = "http://localhost:8200",
        Token = "test-token",
        MountPath = "secret",
        CacheTtlSeconds = 300,
        Enabled = enabled
    };

    [Fact]
    public async Task GetSecretAsync_ReturnsNull_WhenVaultDisabled()
    {
        var options = Options.Create(CreateOptions(enabled: false));
        var factory = new MockHttpClientFactory();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<VaultSecretService>();
        var service = new VaultSecretService(factory, options, logger);

        var result = await service.GetSecretAsync("test/path");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSecretDataAsync_ReturnsEmpty_WhenVaultDisabled()
    {
        var options = Options.Create(CreateOptions(enabled: false));
        var factory = new MockHttpClientFactory();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<VaultSecretService>();
        var service = new VaultSecretService(factory, options, logger);

        var result = await service.GetSecretDataAsync("test/path");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SetSecretAsync_DoesNothing_WhenVaultDisabled()
    {
        var options = Options.Create(CreateOptions(enabled: false));
        var factory = new MockHttpClientFactory();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<VaultSecretService>();
        var service = new VaultSecretService(factory, options, logger);

        await service.SetSecretAsync("test/path", new Dictionary<string, string> { ["key"] = "value" });

        Assert.Empty(factory.CreatedClients);
    }

    [Fact]
    public void VaultOptions_DefaultValues_AreCorrect()
    {
        var options = new VaultOptions();
        Assert.Equal("http://localhost:8200", options.Address);
        Assert.Equal(string.Empty, options.Token);
        Assert.Equal("secret", options.MountPath);
        Assert.Equal(300, options.CacheTtlSeconds);
        Assert.False(options.Enabled);
    }

    [Fact]
    public void VaultOptions_SectionName_IsCorrect()
    {
        Assert.Equal("Vault", VaultOptions.SectionName);
    }

    [Fact]
    public async Task WithRenewableTokenAsync_DisposesCleanly()
    {
        var options = Options.Create(CreateOptions(enabled: false));
        var factory = new MockHttpClientFactory();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<VaultSecretService>();
        var service = new VaultSecretService(factory, options, logger);

        var handle = await service.WithRenewableTokenAsync("test/path", TimeSpan.FromSeconds(1));

        Assert.NotNull(handle);

        var disposeTask = Task.Run(() => handle.Dispose());
        await Task.WhenAny(disposeTask, Task.Delay(1000));
        Assert.True(disposeTask.IsCompleted);
    }

    private sealed class MockHttpClientFactory : IHttpClientFactory
    {
        public List<string> CreatedClients { get; } = [];

        public HttpClient CreateClient(string name)
        {
            CreatedClients.Add(name);
            return new HttpClient { BaseAddress = new Uri("http://localhost:8200") };
        }
    }
}
