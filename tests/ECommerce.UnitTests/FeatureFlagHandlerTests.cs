using ECommerce.Domain.Flags;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Flags.Commands;
using ECommerce.UseCases.Flags.Handlers;
using ECommerce.UseCases.Flags.Queries;

namespace ECommerce.UnitTests;

public sealed class FeatureFlagHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeFeatureFlagRepository _flags = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly FakeAuditEntryRepository _auditEntries = new();

    private readonly FakeAuditContextProvider _auditContext = new();

    private SetFeatureFlagCommandHandler SetHandler => new(
        _flags,
        new AuditLogWriter(_auditEntries, _auditContext),
        _unitOfWork,
        _timeProvider);

    private ListFeatureFlagsQueryHandler ListHandler => new(_flags);

    private GetFeatureFlagQueryHandler GetHandler => new(_flags);

    [Fact]
    public async Task List_Returns_All_Flags()
    {
        _flags.Flags.Add(FeatureFlag.Create("catalog.search", "Search", true, UtcNow));
        _flags.Flags.Add(FeatureFlag.Create("catalog.legacy", "Legacy UI", false, UtcNow));

        var result = await ListHandler.Handle(new ListFeatureFlagsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, flag => flag.Key == "catalog.search" && flag.Enabled);
        Assert.Contains(result.Value, flag => flag.Key == "catalog.legacy" && !flag.Enabled);
    }

    [Fact]
    public async Task Get_Returns_Existing_Flag()
    {
        _flags.Flags.Add(FeatureFlag.Create("catalog.search", "Search", true, UtcNow));

        var result = await GetHandler.Handle(new GetFeatureFlagQuery("catalog.search"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("catalog.search", result.Value.Key);
        Assert.Equal("Search", result.Value.Description);
        Assert.True(result.Value.Enabled);
    }

    [Fact]
    public async Task Get_Unknown_Flag_Returns_NotFound()
    {
        var result = await GetHandler.Handle(new GetFeatureFlagQuery("missing"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Flags.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Set_Creates_Flag_When_Missing()
    {
        var result = await SetHandler.Handle(
            new SetFeatureFlagCommand("catalog.search", true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var flag = Assert.Single(_flags.Flags);
        Assert.Equal("catalog.search", flag.Key);
        Assert.True(flag.Enabled);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Contains(_auditEntries.Entries, entry => entry.Action == "platform.feature.flag.changed");
    }

    [Fact]
    public async Task Set_Updates_Existing_Flag()
    {
        _flags.Flags.Add(FeatureFlag.Create("catalog.search", "Search", true, UtcNow));

        var result = await SetHandler.Handle(
            new SetFeatureFlagCommand("catalog.search", false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var flag = Assert.Single(_flags.Flags);
        Assert.False(flag.Enabled);
    }
}
