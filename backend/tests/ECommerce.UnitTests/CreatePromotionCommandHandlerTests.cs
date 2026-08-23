using ECommerce.Domain.Audit;
using ECommerce.Domain.Pricing;
using ECommerce.UseCases.Promotions.Commands;
using ECommerce.UseCases.Promotions.Handlers;

namespace ECommerce.UnitTests;

public sealed class CreatePromotionCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private readonly FakePromotionRepository _promotions = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly FakeAuditLogWriter _auditLog = new();

    private static CreatePromotionCommand ValidCommand() =>
        new(
            "Summer Sale",
            [new PromotionConditionInput("min_amount", MinAmount: 50m)],
            [new DiscountRuleInput(DiscountType.Product, DiscountBasis.Percent, 20m, null)],
            AllowStack: false,
            AllowStackWith: [],
            EligibleCountries: ["EG"],
            EligibleCurrencies: ["EGP"],
            StartsAt: UtcNow.AddDays(-1),
            EndsAt: UtcNow.AddDays(30));

    private CreatePromotionCommandHandler CreateHandler() =>
        new(
            _promotions,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new CreatePromotionCommandValidator(),
            _auditLog);

    [Fact]
    public async Task Create_Valid_Adds_Promotion_And_Writes_Audit()
    {
        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal("Summer Sale", result.Value.Name);
        Assert.Equal(PromotionState.Draft.ToString(), result.Value.State);
        Assert.Equal(PromotionState.Draft, _promotions.Promotions.Single().State);
        var operation = Assert.Single(_auditLog.Operations);
        Assert.Equal(AuditActions.PromotionCreated, operation.Action);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_Invalid_Name_Returns_Validation_Error()
    {
        var command = ValidCommand() with { Name = string.Empty };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(_promotions.Promotions);
    }

    [Fact]
    public async Task Create_Without_Actions_Returns_Validation_Error()
    {
        var command = ValidCommand() with { Actions = [] };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(_promotions.Promotions);
    }

    [Fact]
    public async Task Create_Percent_Over_One_Hundred_Returns_Validation_Error()
    {
        var command = ValidCommand() with
        {
            Actions = [new DiscountRuleInput(DiscountType.Product, DiscountBasis.Percent, 150m, null)]
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PromotionErrors.InvalidDiscountValue, result.Error);
    }

    [Fact]
    public async Task Create_Invalid_Schedule_Returns_Validation_Error()
    {
        var command = ValidCommand() with { StartsAt = UtcNow, EndsAt = UtcNow.AddDays(-1) };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
