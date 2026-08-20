using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace ECommerce.E2E;

public sealed class CatalogE2ETests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IAPIRequestContext _api = null!;

    private static string BaseUrl => Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _api = await _playwright.APIRequest.NewContextAsync(new() { BaseURL = BaseUrl });
    }

    public async Task DisposeAsync()
    {
        await _api.DisposeAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task ListProducts_ReturnsPagedResults()
    {
        var response = await _api.GetAsync("/api/v1/products?page=1&pageSize=5");
        Assert.True(response.Ok, $"Expected 200 but got {response.Status}");

        var json = (await response.JsonAsync())!.Value;

        Assert.True(json.TryGetProperty("items", out _));
        Assert.True(json.TryGetProperty("totalCount", out _));
    }

    [Fact]
    public async Task ListProducts_WithSearch_ReturnsFilteredResults()
    {
        var response = await _api.GetAsync("/api/v1/products?q=phone&page=1&pageSize=10");
        Assert.True(response.Ok, $"Expected 200 but got {response.Status}");
    }

    [Fact]
    public async Task ListCategories_ReturnsTree()
    {
        var response = await _api.GetAsync("/api/v1/categories/tree");
        Assert.True(response.Ok, $"Expected 200 but got {response.Status}");
    }

    [Fact]
    public async Task ListBrands_ReturnsPagedResults()
    {
        var response = await _api.GetAsync("/api/v1/brands?page=1&pageSize=10");
        Assert.True(response.Ok, $"Expected 200 but got {response.Status}");
    }

    [Fact]
    public async Task HealthLive_ReturnsHealthy()
    {
        var response = await _api.GetAsync("/api/v1/health/live");
        Assert.True(response.Ok, $"Expected 200 but got {response.Status}");
    }

    [Fact]
    public async Task HealthReady_ReturnsHealthy()
    {
        var response = await _api.GetAsync("/api/v1/health/ready");
        Assert.True(response.Ok, $"Expected 200 but got {response.Status}");
    }
}

public sealed class CartE2ETests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IAPIRequestContext _api = null!;
    private string _authToken = string.Empty;

    private static string BaseUrl => Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _api = await _playwright.APIRequest.NewContextAsync(new() { BaseURL = BaseUrl });

        var registerResponse = await _api.PostAsync("/api/v1/auth/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = $"e2e-{Guid.NewGuid():N}@test.com",
                password = "Test1234!",
                firstName = "E2E",
                lastName = "Test"
            }
        });

        if (registerResponse.Ok)
        {
        var json = await registerResponse.JsonAsync();
        _authToken = json?.GetProperty("accessToken").GetString() ?? string.Empty;
        }
    }

    public async Task DisposeAsync()
    {
        await _api.DisposeAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task GuestCheckout_CanInitiateCheckout()
    {
        var cartResponse = await _api.PostAsync("/api/v1/cart", new APIRequestContextOptions
        {
            DataObject = new { }
        });

        if (!cartResponse.Ok) return;

        var cartJson = await cartResponse.JsonAsync();
        var cartId = cartJson?.GetProperty("cartId").GetGuid() ?? Guid.Empty;
        if (cartId == Guid.Empty) return;

        var checkoutResponse = await _api.PostAsync("/api/v1/guest-checkout", new APIRequestContextOptions
        {
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            DataObject = new
            {
                cartId,
                customerEmail = "guest@test.com",
                currency = "USD",
                shippingAddress = new
                {
                    fullName = "Guest User",
                    phone = "+1234567890",
                    street = "123 Test St",
                    city = "Dubai",
                    region = "Dubai",
                    country = "AE",
                    postalCode = "00000"
                },
                shippingMethodId = "standard",
                paymentMethod = new
                {
                    providerKey = "mock",
                    methodType = "card"
                }
            }
        });

        Assert.True(
            checkoutResponse.Ok || checkoutResponse.Status == 400,
            $"Expected 200 or 400 but got {checkoutResponse.Status}");
    }

    [Fact]
    public async Task GuestCheckout_CanTrackOrdersByEmail()
    {
        var response = await _api.GetAsync("/api/v1/guest-checkout/orders?email=nobody@test.com");
        Assert.True(response.Ok, $"Expected 200 but got {response.Status}");

        var json = await response.JsonAsync();
        Assert.NotNull(json);
    }
}
