using ECommerce.Infrastructure.Payments;
using Microsoft.Extensions.Options;

namespace ECommerce.UnitTests;

public sealed class PaymentProviderFactoryTests
{
    private readonly FakePaymentProvider _primary = new("psp-a");

    private readonly FakePaymentProvider _backup = new("psp-b");

    private readonly FakePaymentProviderHealth _health = new();

    private readonly PaymentProviderFactory _factory;

    public PaymentProviderFactoryTests()
    {
        _factory = new PaymentProviderFactory(
            [_primary, _backup],
            _health,
            Options.Create(new PaymentProviderOptions
            {
                DefaultProvider = "psp-a",
                FailoverProvider = "psp-b"
            }));
    }

    [Fact]
    public async Task Routes_To_Primary_When_Healthy()
    {
        var provider = await _factory.GetAsync("psp-a", CancellationToken.None);

        Assert.Same(_primary, provider);
    }

    [Fact]
    public async Task Fails_Over_To_Backup_When_Primary_Unavailable()
    {
        _health.SetUnavailable("psp-a");

        var provider = await _factory.GetAsync("psp-a", CancellationToken.None);

        Assert.Same(_backup, provider);
    }

    [Fact]
    public async Task RouteAsync_Uses_Default_And_Fails_Over()
    {
        var provider = await _factory.RouteAsync("AED", "AE", CancellationToken.None);
        Assert.Same(_primary, provider);

        _health.SetUnavailable("psp-a");

        var failedOver = await _factory.RouteAsync("AED", "AE", CancellationToken.None);
        Assert.Same(_backup, failedOver);
    }

    [Fact]
    public async Task Throws_When_Primary_And_Backup_Unavailable()
    {
        _health.SetUnavailable("psp-a");
        _health.SetUnavailable("psp-b");

        await Assert.ThrowsAsync<PaymentProvidersUnavailableException>(
            () => _factory.GetAsync("psp-a", CancellationToken.None));
    }

    [Fact]
    public async Task Falls_Back_To_Backup_When_Primary_Not_Registered()
    {
        var factory = new PaymentProviderFactory(
            [_backup],
            _health,
            Options.Create(new PaymentProviderOptions
            {
                DefaultProvider = "psp-a",
                FailoverProvider = "psp-b"
            }));

        var provider = await factory.GetAsync("psp-a", CancellationToken.None);

        Assert.Same(_backup, provider);
    }
}
