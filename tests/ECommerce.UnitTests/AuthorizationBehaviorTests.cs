using ECommerce.Domain.Identity;
using ECommerce.UseCases.Common;
using MediatR;

namespace ECommerce.UnitTests;

public sealed class AuthorizationBehaviorTests
{
    [Fact]
    public async Task Handle_Without_Required_Permission_Returns_Forbidden_Result()
    {
        var behavior = new AuthorizationBehavior<RequiresCatalogWrite, Result>(
            new FakeCurrentUser(isAuthenticated: true, permissions: []));

        var result = await behavior.Handle(
            new RequiresCatalogWrite(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error?.Type);
    }

    [Fact]
    public async Task Handle_With_Required_Permission_Executes_Next()
    {
        var behavior = new AuthorizationBehavior<RequiresCatalogWrite, Result>(
            new FakeCurrentUser(isAuthenticated: true, permissions: ["catalog.product.write"]));

        var result = await behavior.Handle(
            new RequiresCatalogWrite(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_When_Unauthenticated_Returns_Unauthorized_Result()
    {
        var behavior = new AuthorizationBehavior<RequiresCatalogWrite, Result>(
            new FakeCurrentUser(isAuthenticated: false, permissions: ["catalog.product.write"]));

        var result = await behavior.Handle(
            new RequiresCatalogWrite(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error?.Type);
    }

    [Fact]
    public async Task Handle_Without_Permission_Reports_Which_Permission_Was_Missing()
    {
        var behavior = new AuthorizationBehavior<RequiresCatalogWrite, Result>(
            new FakeCurrentUser(isAuthenticated: true, permissions: []));

        var result = await behavior.Handle(
            new RequiresCatalogWrite(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.Equal("catalog.product.write", result.Error?.Permission);
    }

    [Fact]
    public async Task Handle_For_Request_Without_Permission_Marker_Executes_Next()
    {
        var behavior = new AuthorizationBehavior<PlainRequest, Result>(
            new FakeCurrentUser(isAuthenticated: false));

        var result = await behavior.Handle(
            new PlainRequest(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_With_Generic_Result_Builds_Typed_Forbidden_Result()
    {
        var behavior = new AuthorizationBehavior<RequiresCatalogWrite, Result<Guid>>(
            new FakeCurrentUser(isAuthenticated: true, permissions: []));

        var result = await behavior.Handle(
            new RequiresCatalogWrite(),
            _ => Task.FromResult(Result<Guid>.Success(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error?.Type);
        Assert.Equal(AuthorizationErrors.PermissionDenied("catalog.product.write").Code, result.Error?.Code);
    }

    private sealed class RequiresCatalogWrite : IRequirePermission
    {
        public string Permission => "catalog.product.write";
    }

    private sealed class PlainRequest : IRequest<Result>;
}
