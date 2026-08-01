using ECommerce.Domain.Events;
using ECommerce.Domain.Identity;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Identity;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Handlers;
using ECommerce.UseCases.Identity.Queries;

namespace ECommerce.UnitTests;

public sealed class ProfileCommandHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeAddressRepository _addresses = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private UpdateProfileCommandHandler UpdateProfileHandler =>
        new(_users, _unitOfWork, _timeProvider, new UpdateProfileCommandValidator());

    private AddAddressCommandHandler AddAddressHandler =>
        new(_users, _addresses, _unitOfWork, _timeProvider, new AddAddressCommandValidator());

    private DeleteAddressCommandHandler DeleteAddressHandler =>
        new(_addresses, _unitOfWork, new DeleteAddressCommandValidator());

    private GetProfileQueryHandler GetProfileHandler => new(_users);

    private GetAddressesQueryHandler GetAddressesHandler => new(_addresses);

    [Fact]
    public async Task UpdateProfile_Updates_Fields_And_Persists()
    {
        var customer = CreateCustomer();

        var result = await UpdateProfileHandler.Handle(
            new UpdateProfileCommand(customer.Id, "Ahmed H.", "+201001234567", "en", "usd"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ahmed H.", customer.DisplayName);
        Assert.Equal("+201001234567", customer.Phone);
        Assert.Equal("en", customer.Locale);
        Assert.Equal("USD", customer.Currency);
        Assert.Equal(1, _unitOfWork.SaveCount);

        var domainEvent = Assert.Single(customer.DomainEvents.OfType<ProfileUpdated>());
        Assert.Equal(customer.Id, domainEvent.CustomerId);
        Assert.Equal("Ahmed H.", domainEvent.DisplayName);
        Assert.Equal("+201001234567", domainEvent.Phone);
    }

    [Fact]
    public async Task UpdateProfile_Partial_Update_Only_Changes_Provided_Fields()
    {
        var customer = CreateCustomer();

        var result = await UpdateProfileHandler.Handle(
            new UpdateProfileCommand(customer.Id, null, "+971500000000", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ahmed Hassan", customer.DisplayName);
        Assert.Equal("+971500000000", customer.Phone);
        Assert.Equal("ar", customer.Locale);
        Assert.Equal("AED", customer.Currency);
    }

    [Fact]
    public async Task UpdateProfile_With_Empty_Phone_Clears_It()
    {
        var customer = CreateCustomer();
        customer.UpdateProfile(null, "+201001234567", null, null, DateTime.UtcNow);

        var result = await UpdateProfileHandler.Handle(
            new UpdateProfileCommand(customer.Id, null, "", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(customer.Phone);
    }

    [Fact]
    public async Task UpdateProfile_With_Unknown_Customer_Returns_NotFound()
    {
        var result = await UpdateProfileHandler.Handle(
            new UpdateProfileCommand(Guid.NewGuid(), "New Name", null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.CustomerNotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateProfile_With_No_Fields_Returns_Validation_Failure()
    {
        var customer = CreateCustomer();

        var result = await UpdateProfileHandler.Handle(
            new UpdateProfileCommand(customer.Id, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateProfile_With_Invalid_Phone_Returns_Validation_Failure()
    {
        var customer = CreateCustomer();

        var result = await UpdateProfileHandler.Handle(
            new UpdateProfileCommand(customer.Id, null, "not-a-phone", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Null(customer.Phone);
    }

    [Fact]
    public async Task AddAddress_Adds_Address_And_Returns_Id()
    {
        var customer = CreateCustomer();

        var result = await AddAddressHandler.Handle(
            new AddAddressCommand(customer.Id, "Home", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "ae", "00000"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var address = _addresses.Addresses.Single();
        Assert.Equal(result.Value, address.Id);
        Assert.Equal(customer.Id, address.CustomerId);
        Assert.Equal("Home", address.Label);
        Assert.Equal("AE", address.Country);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddAddress_With_Unknown_Customer_Returns_NotFound()
    {
        var result = await AddAddressHandler.Handle(
            new AddAddressCommand(Guid.NewGuid(), null, "Street", "City", null, "AE", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.CustomerNotFound, result.Error);
        Assert.Empty(_addresses.Addresses);
    }

    [Fact]
    public async Task AddAddress_With_Invalid_Country_Returns_Validation_Failure()
    {
        var customer = CreateCustomer();

        var result = await AddAddressHandler.Handle(
            new AddAddressCommand(customer.Id, null, "Street", "City", null, "123", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(_addresses.Addresses);
    }

    [Fact]
    public async Task DeleteAddress_Deletes_Own_Address()
    {
        var customer = CreateCustomer();
        var address = CustomerAddress.Create(customer.Id, null, "Street", "City", null, "AE", null, DateTime.UtcNow);
        _addresses.Add(address);

        var result = await DeleteAddressHandler.Handle(
            new DeleteAddressCommand(customer.Id, address.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_addresses.Addresses);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task DeleteAddress_With_Another_Customers_Address_Returns_NotFound()
    {
        var customer = CreateCustomer();
        var otherCustomer = CreateCustomer("other@example.com");
        var address = CustomerAddress.Create(otherCustomer.Id, null, "Street", "City", null, "AE", null, DateTime.UtcNow);
        _addresses.Add(address);

        var result = await DeleteAddressHandler.Handle(
            new DeleteAddressCommand(customer.Id, address.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AddressErrors.AddressNotFound, result.Error);
        Assert.Single(_addresses.Addresses);
    }

    [Fact]
    public async Task DeleteAddress_With_Unknown_Address_Returns_NotFound()
    {
        var customer = CreateCustomer();

        var result = await DeleteAddressHandler.Handle(
            new DeleteAddressCommand(customer.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AddressErrors.AddressNotFound, result.Error);
    }

    [Fact]
    public async Task GetAddresses_Returns_Own_Addresses_Only()
    {
        var customer = CreateCustomer();
        var otherCustomer = CreateCustomer("other@example.com");
        var own = CustomerAddress.Create(customer.Id, "Home", "Street A", "City", null, "AE", null, DateTime.UtcNow);
        var other = CustomerAddress.Create(otherCustomer.Id, null, "Street B", "City", null, "AE", null, DateTime.UtcNow);
        _addresses.Add(own);
        _addresses.Add(other);

        var result = await GetAddressesHandler.Handle(
            new GetAddressesQuery(customer.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal(own.Id, item.Id);
    }

    [Fact]
    public async Task GetProfile_Returns_Profile()
    {
        var customer = CreateCustomer();

        var result = await GetProfileHandler.Handle(
            new GetProfileQuery(customer.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.Id);
        Assert.Equal(customer.Email, result.Value.Email);
        Assert.Equal("Ahmed Hassan", result.Value.DisplayName);
        Assert.True(result.Value.EmailVerified);
    }

    [Fact]
    public async Task GetProfile_With_Unknown_Customer_Returns_NotFound()
    {
        var result = await GetProfileHandler.Handle(
            new GetProfileQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.CustomerNotFound, result.Error);
    }

    private Customer CreateCustomer(string email = "ahmed@example.com")
    {
        var rawToken = "verify-token";
        var customer = Customer.Register(
            email,
            "Ahmed Hassan",
            "ar",
            "AED",
            "hash",
            VerificationTokens.Hash(rawToken),
            DateTime.UtcNow.AddHours(24),
            rawToken);
        customer.VerifyEmail(VerificationTokens.Hash(rawToken), DateTime.UtcNow);

        _users.Customers.Add(customer);
        return customer;
    }
}
