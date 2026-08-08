using ECommerce.UseCases.Notifications.Ports;

namespace ECommerce.Infrastructure.Notifications;

public sealed class InMemoryNotificationTemplateStore : INotificationTemplateStore
{
    private const string DefaultLocale = "en";

    private static readonly IReadOnlyList<string> FallbackChain = ["en", "ar"];

    private readonly IReadOnlyDictionary<string, TemplateDefinition> _templates = BuildTemplates();

    public Task<NotificationTemplateContent> RenderAsync(
        string templateKey,
        string locale,
        IReadOnlyDictionary<string, string> placeholders,
        CancellationToken cancellationToken)
    {
        var template = Resolve(templateKey, locale);
        return Task.FromResult(new NotificationTemplateContent(
            Render(template.Subject, placeholders),
            Render(template.Body, placeholders)));
    }

    private TemplateDefinition Resolve(string templateKey, string locale)
    {
        if (_templates.TryGetValue($"{templateKey}.{locale}", out var localized))
        {
            return localized;
        }

        foreach (var fallback in FallbackChain)
        {
            if (_templates.TryGetValue($"{templateKey}.{fallback}", out var candidate))
            {
                return candidate;
            }
        }

        throw new KeyNotFoundException($"No notification template '{templateKey}' is registered.");
    }

    private static string Render(string template, IReadOnlyDictionary<string, string> placeholders)
    {
        var result = template;

        foreach (var (key, value) in placeholders)
        {
            result = result.Replace("{{" + key + "}}", value, StringComparison.Ordinal);
        }

        return result;
    }

    private static IReadOnlyDictionary<string, TemplateDefinition> BuildTemplates()
    {
        return new Dictionary<string, TemplateDefinition>
        {
            ["order.confirmation.en"] = new(
                "Order {OrderNumber} confirmed",
                "<p>Thank you for your order <strong>{OrderNumber}</strong>.</p><p>Total: {Total} {Currency}</p>"),
            ["order.confirmation.ar"] = new(
                "تأكيد الطلب {OrderNumber}",
                "<p>شكراً لطلبك <strong>{OrderNumber}</strong>.</p><p>الإجمالي: {Total} {Currency}</p>"),
            ["order.shipped.en"] = new(
                "Order {OrderNumber} shipped",
                "<p>Your order <strong>{OrderNumber}</strong> is on its way.</p>"),
            ["low.stock.alert.en"] = new(
                "Low stock alert: {Sku}",
                "<p>Stock item <strong>{Sku}</strong> (warehouse {WarehouseId}) has {Available} units available, below the threshold of {Threshold}.</p>")
        };
    }

    private sealed record TemplateDefinition(string Subject, string Body);
}
