namespace ECommerce.UseCases.Notifications.Ports;

public sealed record NotificationTemplateContent(string Subject, string Body);

public interface INotificationTemplateStore
{
    Task<NotificationTemplateContent> RenderAsync(
        string templateKey,
        string locale,
        IReadOnlyDictionary<string, string> placeholders,
        CancellationToken cancellationToken);
}
