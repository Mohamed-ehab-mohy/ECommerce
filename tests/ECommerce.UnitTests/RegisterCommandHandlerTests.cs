using ECommerce.Shared.Errors;
using ECommerce.Domain.Identity;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Handlers;

namespace ECommerce.UnitTests;

public sealed class RegisterCommandHandlerTests
{
    private const string ValidEmail = "ahmed@example.com";
    private const string ValidPassword = "Str0ng!Passw0rd";
    private const string DuplicateEmail = "taken@example.com";

    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public async Task Handle_With_Valid_Request_Registers_Customer()
    {
        var handler = new RegisterCommandHandler(
            _users,
            _passwordHasher,
            new FakeBreachChecker(breached: false),
            _unitOfWork,
            TimeProvider.System,
            _validator);

        var result = await handler.Handle(new RegisterCommand(
            ValidEmail,
            ValidPassword,
            "Ahmed Hassan",
            "ar",
            "AED"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var customer = Assert.Single(_users.Customers);
        Assert.Equal(result.Value, customer.Id);
        Assert.Equal(ValidEmail, customer.Email);
        Assert.Equal("ahmed@example.com", customer.Email);
        Assert.Equal("hash:" + ValidPassword, customer.PasswordHash);
        Assert.False(customer.EmailVerified);
        Assert.NotNull(customer.VerificationTokenHash);
        Assert.Single(customer.DomainEvents.OfType<ECommerce.Domain.Events.CustomerRegistered>());
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_With_Duplicate_Email_Returns_Conflict()
    {
        _users.ExistingByEmail = Customer.Register(
            DuplicateEmail,
            "Existing",
            "ar",
            "AED",
            "hash",
            "t",
            DateTime.UtcNow.AddHours(24),
            "raw");
        var handler = new RegisterCommandHandler(
            _users,
            _passwordHasher,
            new FakeBreachChecker(breached: false),
            _unitOfWork,
            TimeProvider.System,
            _validator);

        var result = await handler.Handle(new RegisterCommand(
            DuplicateEmail,
            ValidPassword,
            "Ahmed Hassan",
            "ar",
            "AED"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.EmailAlreadyExists, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Empty(_users.Customers);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_With_Breached_Password_Returns_Failure()
    {
        var handler = new RegisterCommandHandler(
            _users,
            _passwordHasher,
            new FakeBreachChecker(breached: true),
            _unitOfWork,
            TimeProvider.System,
            _validator);

        var result = await handler.Handle(new RegisterCommand(
            ValidEmail,
            ValidPassword,
            "Ahmed Hassan",
            "ar",
            "AED"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.BreachedPassword, result.Error);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(_users.Customers);
    }

    [Theory]
    [InlineData("not-an-email", "Str0ng!Passw0rd")]
    [InlineData("ahmed@example.com", "short")]
    [InlineData("", "Str0ng!Passw0rd")]
    public async Task Handle_With_Invalid_Input_Returns_Validation_Failure(string email, string password)
    {
        var handler = new RegisterCommandHandler(
            _users,
            _passwordHasher,
            new FakeBreachChecker(breached: false),
            _unitOfWork,
            TimeProvider.System,
            _validator);

        var result = await handler.Handle(new RegisterCommand(
            email,
            password,
            "Ahmed Hassan",
            "ar",
            "AED"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(_users.Customers);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_Normalizes_Email_And_Currency()
    {
        var handler = new RegisterCommandHandler(
            _users,
            _passwordHasher,
            new FakeBreachChecker(breached: false),
            _unitOfWork,
            TimeProvider.System,
            _validator);

        var result = await handler.Handle(new RegisterCommand(
            "  Ahmed@Example.COM ",
            ValidPassword,
            "Ahmed Hassan",
            "  ar ",
            "aed"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var customer = Assert.Single(_users.Customers);
        Assert.Equal("ahmed@example.com", customer.Email);
        Assert.Equal("AED", customer.Currency);
    }
}
