using ECommerce.Domain.Integrations;

namespace ECommerce.UnitTests;

public sealed class WebhookEndpointTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private static WebhookEndpoint CreateEndpoint(params string[] eventTypes) =>
        WebhookEndpoint.Create("Partner", "https://partner.test/hook", "secret", eventTypes, UtcNow);

    [Fact]
    public void Create_Sets_Active_Endpoint_With_Distinct_Event_Types()
    {
        var endpoint = CreateEndpoint(WebhookEventTypes.OrderPlaced, WebhookEventTypes.OrderPlaced);

        Assert.True(endpoint.IsActive);
        Assert.Equal("Partner", endpoint.Name);
        Assert.Equal("https://partner.test/hook", endpoint.Url);
        Assert.Equal("secret", endpoint.Secret);
        Assert.Equal(UtcNow, endpoint.CreatedAt);
        Assert.Equal([WebhookEventTypes.OrderPlaced], endpoint.EventTypes);
    }

    [Fact]
    public void IsSubscribedTo_Returns_False_When_Inactive()
    {
        var endpoint = CreateEndpoint(WebhookEventTypes.OrderPlaced);
        endpoint.Deactivate(UtcNow);

        Assert.False(endpoint.IsSubscribedTo(WebhookEventTypes.OrderPlaced));
    }

    [Fact]
    public void IsSubscribedTo_Returns_False_For_Unsubscribed_Event()
    {
        var endpoint = CreateEndpoint(WebhookEventTypes.OrderPlaced);

        Assert.False(endpoint.IsSubscribedTo(WebhookEventTypes.OrderShipped));
    }

    [Fact]
    public void RotateSecret_Updates_Secret_And_Rotation_Timestamp()
    {
        var endpoint = CreateEndpoint();
        var rotatedAt = UtcNow.AddMinutes(5);

        endpoint.RotateSecret("new-secret", rotatedAt);

        Assert.Equal("new-secret", endpoint.Secret);
        Assert.Equal(rotatedAt, endpoint.SecretRotatedAtUtc);
        Assert.Equal(rotatedAt, endpoint.UpdatedAt);
    }

    [Fact]
    public void Suspend_Marks_Endpoint_For_One_Hour()
    {
        var endpoint = CreateEndpoint();
        var suspendedAt = UtcNow.AddMinutes(10);

        endpoint.Suspend(suspendedAt);

        Assert.Equal(suspendedAt.AddHours(1), endpoint.SuspendedUntilUtc);
        Assert.True(endpoint.IsSuspended(suspendedAt));
        Assert.True(endpoint.IsSuspended(suspendedAt.AddMinutes(59)));
        Assert.False(endpoint.IsSuspended(suspendedAt.AddHours(1)));
    }

    [Fact]
    public void Suspend_Is_Idempotent_While_Already_Suspended()
    {
        var endpoint = CreateEndpoint();
        endpoint.Suspend(UtcNow);

        var firstUntil = endpoint.SuspendedUntilUtc;
        endpoint.Suspend(UtcNow.AddMinutes(1));

        Assert.Equal(firstUntil, endpoint.SuspendedUntilUtc);
    }

    [Fact]
    public void Resume_Reactivates_Endpoint()
    {
        var endpoint = CreateEndpoint(WebhookEventTypes.OrderPlaced);
        endpoint.Suspend(UtcNow);
        var resumedAt = UtcNow.AddMinutes(30);

        endpoint.Resume(resumedAt);

        Assert.Null(endpoint.SuspendedUntilUtc);
        Assert.True(endpoint.IsActive);
        Assert.True(endpoint.IsSubscribedTo(WebhookEventTypes.OrderPlaced));
    }

    [Fact]
    public void Deactivate_Stops_Delivery()
    {
        var endpoint = CreateEndpoint(WebhookEventTypes.OrderPlaced);

        endpoint.Deactivate(UtcNow);

        Assert.False(endpoint.IsActive);
        Assert.False(endpoint.IsSubscribedTo(WebhookEventTypes.OrderPlaced));
    }
}
