using ECommerce.Infrastructure.Notifications;

namespace ECommerce.UnitTests;

public sealed class NotificationTemplateStoreTests
{
    private readonly InMemoryNotificationTemplateStore _store = new();

    [Theory]
    [InlineData("order.confirmation", "en")]
    [InlineData("order.confirmation", "ar")]
    [InlineData("order.shipped", "en")]
    [InlineData("order.shipped", "ar")]
    [InlineData("order.cancelled", "en")]
    [InlineData("order.cancelled", "ar")]
    [InlineData("low.stock.alert", "en")]
    [InlineData("integrations.webhook.suspended", "en")]
    public async Task All_Order_Notification_Templates_Render_Without_Error(
        string templateKey,
        string locale)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["OrderNumber"] = "E-20260807-000001",
            ["Total"] = "39.90",
            ["Currency"] = "USD",
            ["Reason"] = "customer-request",
            ["Carrier"] = "aramex",
            ["TrackingNumbers"] = "1Z999AA10123456784",
            ["Sku"] = "SKU-1",
            ["WarehouseId"] = "00000000000000000000000000000001",
            ["Available"] = "3",
            ["Threshold"] = "5",
            ["EndpointName"] = "Partner",
            ["Url"] = "https://partner.test/hook",
            ["SuspendedAt"] = "2026-08-15T12:00:00.0000000Z",
            ["EventType"] = "order.placed"
        };

        var content = await _store.RenderAsync(templateKey, locale, placeholders, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(content.Subject));
        Assert.False(string.IsNullOrWhiteSpace(content.Body));
    }

    [Fact]
    public async Task Render_Replaces_Placeholders_With_Template_Values()
    {
        var content = await _store.RenderAsync(
            "order.cancelled",
            "en",
            new Dictionary<string, string>
            {
                ["OrderNumber"] = "E-20260807-000042",
                ["Reason"] = "out-of-stock"
            },
            CancellationToken.None);

        Assert.Contains("E-20260807-000042", content.Subject);
        Assert.Contains("out-of-stock", content.Body);
    }
}
