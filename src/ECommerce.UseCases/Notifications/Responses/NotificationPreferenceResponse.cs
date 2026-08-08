namespace ECommerce.UseCases.Notifications.Responses;

public sealed record NotificationPreferenceResponse(
    Guid Id,
    string Channel,
    string Kind,
    bool Enabled);
