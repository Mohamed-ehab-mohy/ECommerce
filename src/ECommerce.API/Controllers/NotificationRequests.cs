namespace ECommerce.API.Controllers;

public sealed record SetFeatureFlagRequest(bool Enabled);

public sealed record UpdateNotificationPreferenceRequest(
    string Channel,
    string Kind,
    bool Enabled);
