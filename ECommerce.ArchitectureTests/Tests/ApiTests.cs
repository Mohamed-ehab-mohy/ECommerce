using System.Reflection;
using ECommerce.API.Controllers;
using NetArchTest.Rules;

namespace ECommerce.ArchitectureTests.Tests;

public class ApiTests
{
    private static readonly Assembly ApiAssembly =
        typeof(ApiControllerBase).Assembly;

    [Fact]
    public void API_ShouldNotDependOnInfrastructureDirectly()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ResideInNamespaceContaining("Controllers")
            .ShouldNot()
            .HaveDependencyOn("ECommerce.Infrastructure.Data")
            .Or()
            .HaveDependencyOn("ECommerce.Infrastructure.Repositories")
            .Or()
            .HaveDependencyOn("ECommerce.Infrastructure.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void API_ShouldNotDependOnDomainDirectly()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ResideInNamespaceContaining("Controllers")
            .ShouldNot()
            .HaveDependencyOn("ECommerce.Domain.Entities")
            .Or()
            .HaveDependencyOn("ECommerce.Domain.Repositories")
            .Or()
            .HaveDependencyOn("ECommerce.Domain.Specifications")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Controllers_ShouldInheritFromApiControllerBase()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .Should()
            .Inherit(typeof(ApiControllerBase))
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Controllers_ShouldHaveApiControllerAttribute()
    {
        var controllerTypes = Types
            .InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .And()
            .Inherit(typeof(ApiControllerBase))
            .GetTypes();

        foreach (var type in controllerTypes)
        {
            var hasApiController = type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), true).Length > 0
                || type.BaseType?.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), true).Length > 0;

            hasApiController.Should().BeTrue($"{type.Name} should have [ApiController] attribute");
        }
    }

    [Fact]
    public void Controllers_ShouldHaveRouteAttribute()
    {
        var controllerTypes = Types
            .InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .And()
            .Inherit(typeof(ApiControllerBase))
            .GetTypes();

        foreach (var type in controllerTypes)
        {
            var hasRoute = type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), true).Length > 0
                || type.BaseType?.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), true).Length > 0;

            hasRoute.Should().BeTrue($"{type.Name} should have [Route] attribute");
        }
    }
}
