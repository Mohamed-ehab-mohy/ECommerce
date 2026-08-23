using ECommerce.Domain.Identity;
using ECommerce.UseCases.Identity;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Handlers;

namespace ECommerce.UnitTests;

public sealed class VerifyEmailCommandHandlerTests
{
    private const string ValidEmail = "ahmed@example.com";
    private const string RawToken = "RAWTOKEN";

    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly VerifyEmailCommandValidator _validator = new();

    [Fact]
    public async Task Handle_With_Valid_Token_Verifies_Email()
    {
        _users.Add(CreateCustomer(DateTime.UtcNow.AddHours(1)));
        var handler = new VerifyEmailCommandHandler(
            _users,
            _unitOfWork,
            TimeProvider.System,
            _validator);

        var result = await handler.Handle(new VerifyEmailCommand(RawToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(_users.Customers[0].EmailVerified);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_With_Unknown_Token_Returns_Failure()
    {
        var handler = new VerifyEmailCommandHandler(
            _users,
            _unitOfWork,
            TimeProvider.System,
            _validator);

        var result = await handler.Handle(new VerifyEmailCommand("UNKNOWN"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.VerificationTokenInvalid, result.Error);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_With_Expired_Token_Returns_Failure()
    {
        _users.Add(CreateCustomer(DateTime.UtcNow.AddHours(-1)));
        var handler = new VerifyEmailCommandHandler(
            _users,
            _unitOfWork,
            TimeProvider.System,
            _validator);

        var result = await handler.Handle(new VerifyEmailCommand(RawToken), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.VerificationTokenExpired, result.Error);
        Assert.False(_users.Customers[0].EmailVerified);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_Second_Verify_With_Same_Token_Fails_After_First_Success()
    {
        _users.Add(CreateCustomer(DateTime.UtcNow.AddHours(1)));
        var handler = new VerifyEmailCommandHandler(
            _users,
            _unitOfWork,
            TimeProvider.System,
            _validator);

        var first = await handler.Handle(new VerifyEmailCommand(RawToken), CancellationToken.None);
        var second = await handler.Handle(new VerifyEmailCommand(RawToken), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal(CustomerErrors.VerificationTokenInvalid, second.Error);
    }

    private static Customer CreateCustomer(DateTime expiresAt) =>
        Customer.Register(ValidEmail, "Ahmed Hassan", "ar", "AED", "hash", VerificationTokens.Hash(RawToken), expiresAt, RawToken);
}
