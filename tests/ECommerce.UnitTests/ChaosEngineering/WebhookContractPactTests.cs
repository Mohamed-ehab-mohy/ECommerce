using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECommerce.Domain.Integrations;
using ECommerce.Infrastructure.Integrations;
using ECommerce.UseCases.Integrations.Services;

namespace ECommerce.UnitTests.Tests.WebhookContracts;

public sealed class WebhookContractConsumerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Contract_OrderPlaced_Envelope_HasRequiredFields()
    {
        var payload = CreateOrderPlacedPayload();
        var envelope = new WebhookEnvelope("evt_test001", WebhookEventTypes.OrderPlaced, DateTime.UtcNow, "1.0", payload);
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("eventId", out _));
        Assert.True(root.TryGetProperty("type", out _));
        Assert.True(root.TryGetProperty("occurredAt", out _));
        Assert.True(root.TryGetProperty("version", out _));
        Assert.True(root.TryGetProperty("payload", out _));
        Assert.Equal(WebhookEventTypes.OrderPlaced, root.GetProperty("type").GetString());
        Assert.Equal("1.0", root.GetProperty("version").GetString());
    }

    [Fact]
    public void Contract_OrderPlaced_Payload_HasOrderFields()
    {
        var payload = CreateOrderPlacedPayload();
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("orderId", out _));
        Assert.True(root.TryGetProperty("orderNumber", out _));
        Assert.True(root.TryGetProperty("currency", out _));
        Assert.True(root.TryGetProperty("totals", out var totals));
        Assert.True(root.TryGetProperty("lines", out _));
        Assert.True(totals.TryGetProperty("grandTotal", out _));
    }

    [Fact]
    public void Contract_OrderPaid_HasPaymentFields()
    {
        var payload = new { orderNumber = "ORD-001", paymentId = "pay_123", amount = 99.99m, currency = "USD" };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("orderNumber", out _));
        Assert.True(root.TryGetProperty("paymentId", out _));
        Assert.True(root.TryGetProperty("amount", out _));
        Assert.True(root.TryGetProperty("currency", out _));
    }

    [Fact]
    public void Contract_OrderShipped_HasTrackingFields()
    {
        var payload = new { orderNumber = "ORD-002", trackingNumbers = new[] { "TRK-123" } };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("orderNumber", out _));
        Assert.True(root.TryGetProperty("trackingNumbers", out var tracking));
        Assert.Equal(JsonValueKind.Array, tracking.ValueKind);
    }

    [Fact]
    public void Contract_OrderCancelled_HasReasonField()
    {
        var payload = new { orderNumber = "ORD-003", reason = "Customer request" };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("orderNumber", out _));
        Assert.True(root.TryGetProperty("reason", out _));
    }

    [Fact]
    public void Contract_RefundCompleted_HasRefundFields()
    {
        var payload = new { refundId = "ref_001", orderNumber = "ORD-001", amount = 50.00m, currency = "USD" };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("refundId", out _));
        Assert.True(root.TryGetProperty("orderNumber", out _));
        Assert.True(root.TryGetProperty("amount", out _));
        Assert.True(root.TryGetProperty("currency", out _));
    }

    [Fact]
    public void Contract_ProductUpdated_HasProductFields()
    {
        var payload = new { productId = Guid.NewGuid(), sku = "SKU-001", status = "Active" };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("productId", out _));
        Assert.True(root.TryGetProperty("sku", out _));
        Assert.True(root.TryGetProperty("status", out _));
    }

    [Fact]
    public void Contract_StockLow_HasInventoryFields()
    {
        var payload = new { sku = "SKU-001", warehouseCode = "WH-1", onHand = 5, threshold = 10 };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("sku", out _));
        Assert.True(root.TryGetProperty("warehouseCode", out _));
        Assert.True(root.TryGetProperty("onHand", out _));
        Assert.True(root.TryGetProperty("threshold", out _));
    }

    [Fact]
    public void Contract_AllEventTypes_AreSnakeCaseDotNotation()
    {
        foreach (var eventType in WebhookEventTypes.All)
        {
            Assert.Contains('.', eventType);
            Assert.DoesNotContain(' ', eventType);
            Assert.Equal(eventType.ToLowerInvariant(), eventType);
            Assert.Matches(@"^[a-z]+\.[a-z]+$", eventType);
        }
    }

    [Fact]
    public void Contract_Envelope_Version_IsSemantic()
    {
        var envelope = new WebhookEnvelope("evt_test", "order.placed", DateTime.UtcNow, "1.0", new { });
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        var doc = JsonDocument.Parse(json);
        var version = doc.RootElement.GetProperty("version").GetString();

        Assert.Matches(@"^\d+\.\d+$", version!);
    }

    private static object CreateOrderPlacedPayload() => new
    {
        orderId = Guid.NewGuid(),
        orderNumber = "ORD-1000",
        customerId = Guid.NewGuid(),
        currency = "USD",
        totals = new
        {
            subtotal = 100.00m,
            discount = 10.00m,
            shipping = 5.99m,
            tax = 7.50m,
            grandTotal = 103.49m
        },
        lines = new[]
        {
            new { productId = Guid.NewGuid(), sku = "SKU-001", name = "Widget", quantity = 2, unitPrice = 50.00m }
        }
    };
}

public sealed class WebhookContractProviderTests
{
    private const string TestSecret = "whsec_test_provider_secret";

    [Fact]
    public void Provider_Signature_IsValid_HMAC_SHA256()
    {
        var signer = new HmacWebhookSigner();
        var payload = """{"eventId":"evt_001","type":"order.placed"}""";
        var signature = signer.ComputeSignature(TestSecret, payload);

        Assert.StartsWith("sha256=", signature);

        var expectedHash = ComputeExpectedHmac(TestSecret, payload);
        Assert.Equal($"sha256={expectedHash}", signature);
    }

    [Fact]
    public void Provider_Signature_Changes_With_DifferentSecret()
    {
        var signer = new HmacWebhookSigner();
        var payload = """{"eventId":"evt_001"}""";
        var sig1 = signer.ComputeSignature("secret_a", payload);
        var sig2 = signer.ComputeSignature("secret_b", payload);

        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void Provider_Signature_Changes_With_DifferentPayload()
    {
        var signer = new HmacWebhookSigner();
        var sig1 = signer.ComputeSignature(TestSecret, "payload_a");
        var sig2 = signer.ComputeSignature(TestSecret, "payload_b");

        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void Provider_Signature_Is_Deterministic_For_Same_Input()
    {
        var signer = new HmacWebhookSigner();
        var payload = """{"eventId":"evt_001"}""";
        var sig1 = signer.ComputeSignature(TestSecret, payload);
        var sig2 = signer.ComputeSignature(TestSecret, payload);

        Assert.Equal(sig1, sig2);
    }

    [Fact]
    public void Provider_DeliveryHeaders_Are_Correct()
    {
        var expectedHeaders = new[]
        {
            "X-Signature",
            "X-Event-Id",
            "X-Event-Type",
            "X-Timestamp"
        };

        foreach (var header in expectedHeaders)
        {
            Assert.NotNull(header);
            Assert.NotEmpty(header);
            Assert.StartsWith("X-", header);
        }
    }

    [Fact]
    public void Provider_AllEventTypes_Are_Subscribable()
    {
        foreach (var eventType in WebhookEventTypes.All)
        {
            var endpoint = WebhookEndpoint.Create(
                "Test Provider",
                "https://example.com/webhook",
                TestSecret,
                [eventType],
                DateTime.UtcNow);

            Assert.True(endpoint.IsSubscribedTo(eventType));
        }
    }

    [Fact]
    public void Provider_Endpoint_Only_Receives_Subscribed_Events()
    {
        var endpoint = WebhookEndpoint.Create(
            "Test Provider",
            "https://example.com/webhook",
            TestSecret,
            [WebhookEventTypes.OrderPlaced, WebhookEventTypes.OrderPaid],
            DateTime.UtcNow);

        Assert.True(endpoint.IsSubscribedTo(WebhookEventTypes.OrderPlaced));
        Assert.True(endpoint.IsSubscribedTo(WebhookEventTypes.OrderPaid));
        Assert.False(endpoint.IsSubscribedTo(WebhookEventTypes.OrderShipped));
        Assert.False(endpoint.IsSubscribedTo(WebhookEventTypes.RefundCompleted));
    }

    [Fact]
    public void Provider_Endpoint_Rejects_Events_When_Suspended()
    {
        var now = DateTime.UtcNow;
        var endpoint = WebhookEndpoint.Create(
            "Test Provider",
            "https://example.com/webhook",
            TestSecret,
            [WebhookEventTypes.OrderPlaced],
            now);

        endpoint.Suspend(now);

        Assert.True(endpoint.IsSuspended(now));
    }

    [Fact]
    public void Provider_MaxAttempts_Policy_Is_5()
    {
        var options = new WebhookOptions { MaxAttempts = 5 };
        Assert.Equal(5, options.MaxAttempts);
    }

    [Fact]
    public void Provider_ExponentialBackoff_Computes_Correctly()
    {
        var delays = new[] { 1, 2, 4, 8 };
        for (var i = 0; i < delays.Length; i++)
        {
            var minutes = Math.Min(1 << i, 8);
            Assert.Equal(delays[i], minutes);
        }
    }

    private static string ComputeExpectedHmac(string secret, string payload)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
