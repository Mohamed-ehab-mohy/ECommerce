using ECommerce.Domain.Identity;
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Identity;
using ECommerce.UseCases.Identity.Handlers;
using ECommerce.UseCases.Identity.Queries;

namespace ECommerce.UnitTests;

public sealed class CustomerLookupQueryHandlerTests
{
    private readonly FakeUserRepository _users = new();

    private SearchCustomersQueryHandler SearchHandler(FakeCurrentUser currentUser) =>
        new(_users, new SearchCustomersQueryValidator(), currentUser);

    private GetCustomerQueryHandler GetHandler(FakeCurrentUser currentUser) =>
        new(_users, currentUser);

    [Fact]
    public async Task Search_Masks_Pii_When_Caller_Lacks_Pii_Permission()
    {
        var customer = CreateCustomer("ahmed@example.com", phone: "+201001234567");
        _users.Customers.Add(customer);

        var result = await SearchHandler(new FakeCurrentUser(permissions: [Permissions.CustomersRead]))
            .Handle(new SearchCustomersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("a***@example.com", item.Email);
        Assert.Equal("*********4567", item.Phone);
        Assert.Equal("Ahmed Hassan", item.DisplayName);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task Search_Returns_Full_Pii_When_Caller_Has_Pii_Permission()
    {
        var customer = CreateCustomer("ahmed@example.com", phone: "+201001234567");
        _users.Customers.Add(customer);

        var result = await SearchHandler(
                new FakeCurrentUser(permissions: [Permissions.CustomersRead, Permissions.CustomersPiiRead]))
            .Handle(new SearchCustomersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("ahmed@example.com", item.Email);
        Assert.Equal("+201001234567", item.Phone);
    }

    [Fact]
    public async Task Search_Filters_By_Email_And_Returns_Matching_Total()
    {
        _users.Customers.Add(CreateCustomer("sarah@example.com"));
        _users.Customers.Add(CreateCustomer("sam@example.org"));
        _users.Customers.Add(CreateCustomer("other@example.com"));

        var result = await SearchHandler(new FakeCurrentUser(permissions: [Permissions.CustomersRead]))
            .Handle(new SearchCustomersQuery(Email: "example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.All(result.Value.Items, item => Assert.EndsWith("example.com", item.Email));
    }

    [Fact]
    public async Task Search_Rejects_Invalid_PageSize()
    {
        var result = await SearchHandler(new FakeCurrentUser(permissions: [Permissions.CustomersRead]))
            .Handle(new SearchCustomersQuery(PageSize: 101), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Get_Returns_Customer_With_Full_Pii_For_Privileged_Caller()
    {
        var customer = CreateCustomer("ahmed@example.com", phone: "+201001234567");
        _users.Customers.Add(customer);

        var result = await GetHandler(
                new FakeCurrentUser(permissions: [Permissions.CustomersRead, Permissions.CustomersPiiRead]))
            .Handle(new GetCustomerQuery(customer.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ahmed@example.com", result.Value.Email);
        Assert.Equal("+201001234567", result.Value.Phone);
    }

    [Fact]
    public async Task Get_Masks_Pii_For_Non_Privileged_Caller()
    {
        var customer = CreateCustomer("ahmed@example.com", phone: "+201001234567");
        _users.Customers.Add(customer);

        var result = await GetHandler(new FakeCurrentUser(permissions: [Permissions.CustomersRead]))
            .Handle(new GetCustomerQuery(customer.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("a***@example.com", result.Value.Email);
        Assert.Equal("*********4567", result.Value.Phone);
    }

    [Fact]
    public async Task Get_Returns_NotFound_When_Customer_Missing()
    {
        var result = await GetHandler(new FakeCurrentUser(permissions: [Permissions.CustomersRead]))
            .Handle(new GetCustomerQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.CustomerNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    private static Customer CreateCustomer(string email, string? phone = null)
    {
        var rawToken = "verify-token";
        var customer = Customer.Register(
            email,
            "Ahmed Hassan",
            "en",
            "USD",
            "hash",
            VerificationTokens.Hash(rawToken),
            DateTime.UtcNow.AddHours(24),
            rawToken);
        customer.VerifyEmail(VerificationTokens.Hash(rawToken), DateTime.UtcNow);
        customer.UpdateProfile(null, phone, null, null, DateTime.UtcNow);
        return customer;
    }
}
