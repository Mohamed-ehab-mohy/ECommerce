using ECommerce.Domain.Notifications;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Notifications.Commands;
using ECommerce.UseCases.Notifications.Handlers;
using ECommerce.UseCases.Notifications.Queries;

namespace ECommerce.UnitTests;

public sealed class NotificationPreferenceTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static readonly DateTime UtcNow = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeNotificationPreferenceRepository _preferences = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly FakeAuditEntryRepository _auditEntries = new();

    private readonly FakeAuditContextProvider _auditContext = new();

    private UpdateNotificationPreferenceCommandHandler UpdateHandler => new(
        _preferences,
        _unitOfWork,
        _timeProvider,
        new AuditLogWriter(_auditEntries, _auditContext));

    private ListNotificationPreferencesQueryHandler ListHandler => new(_preferences);

    [Fact]
    public async Task Update_Creates_Preference_When_Missing()
    {
        var result = await UpdateHandler.Handle(
            new UpdateNotificationPreferenceCommand(CustomerId, "email", "OrderConfirmation", false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var preference = Assert.Single(_preferences.Preferences);
        Assert.Equal(CustomerId, preference.CustomerId);
        Assert.Equal(NotificationChannel.Email, preference.Channel);
        Assert.Equal(NotificationKind.OrderConfirmation, preference.Kind);
        Assert.False(preference.Enabled);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Contains(_auditEntries.Entries, entry => entry.Action == "notifications.preference.updated");
    }

    [Fact]
    public async Task Update_Disables_Existing_Preference_Case_Insensitively()
    {
        _preferences.Preferences.Add(NotificationPreference.Create(
            CustomerId, NotificationChannel.Email, NotificationKind.OrderStatusUpdate, true, UtcNow));

        var result = await UpdateHandler.Handle(
            new UpdateNotificationPreferenceCommand(CustomerId, "Email", "OrderStatusUpdate", false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var preference = Assert.Single(_preferences.Preferences);
        Assert.False(preference.Enabled);
    }

    [Fact]
    public async Task Update_Rejects_Unknown_Channel()
    {
        var result = await UpdateHandler.Handle(
            new UpdateNotificationPreferenceCommand(CustomerId, "pigeon", "OrderStatusUpdate", true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Notifications.InvalidChannelOrKind", result.Error.Code);
        Assert.Empty(_preferences.Preferences);
    }

    [Fact]
    public async Task Update_Rejects_Unknown_Kind()
    {
        var result = await UpdateHandler.Handle(
            new UpdateNotificationPreferenceCommand(CustomerId, "email", "nonsense", true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Notifications.InvalidChannelOrKind", result.Error.Code);
        Assert.Empty(_preferences.Preferences);
    }

    [Fact]
    public async Task List_Returns_Only_The_Customer_Preferences()
    {
        _preferences.Preferences.Add(NotificationPreference.Create(
            CustomerId, NotificationChannel.Email, NotificationKind.OrderConfirmation, true, UtcNow));
        _preferences.Preferences.Add(NotificationPreference.Create(
            Guid.NewGuid(), NotificationChannel.Sms, NotificationKind.LowStockAlert, true, UtcNow));

        var result = await ListHandler.Handle(new ListNotificationPreferencesQuery(CustomerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal(NotificationKind.OrderConfirmation.ToString(), item.Kind);
        Assert.Equal(NotificationChannel.Email.ToString(), item.Channel);
        Assert.True(item.Enabled);
    }
}
