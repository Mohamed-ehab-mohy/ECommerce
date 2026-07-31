using System.Reflection;
using NetArchTest.Rules;

namespace ECommerce.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly Assembly Api = typeof(ECommerce.API.DependencyInjection).Assembly;
    private static readonly Assembly UseCases = typeof(ECommerce.UseCases.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(ECommerce.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Domain = typeof(ECommerce.Domain.AssemblyMarker).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnOtherLayers()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOn("ECommerce.API")
            .And()
            .NotHaveDependencyOn("ECommerce.UseCases")
            .And()
            .NotHaveDependencyOn("ECommerce.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailureMessage(result));
    }

    [Fact]
    public void UseCases_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(UseCases)
            .Should()
            .NotHaveDependencyOn("ECommerce.Infrastructure")
            .And()
            .NotHaveDependencyOn("ECommerce.API")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailureMessage(result));
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnApi()
    {
        var result = Types.InAssembly(Infrastructure)
            .Should()
            .NotHaveDependencyOn("ECommerce.API")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailureMessage(result));
    }

    [Fact]
    public void Api_ShouldNotDependOnDomainDirectly()
    {
        var result = Types.InAssembly(Api)
            .Should()
            .NotHaveDependencyOn("ECommerce.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailureMessage(result));
    }

    [Fact]
    public void NoProject_ShouldDependOnTestAssemblies()
    {
        foreach (var assembly in new[] { Api, UseCases, Infrastructure, Domain })
        {
            var result = Types.InAssembly(assembly)
                .Should()
                .NotHaveDependencyOn("ECommerce.UnitTests")
                .And()
                .NotHaveDependencyOn("ECommerce.ArchitectureTests")
                .GetResult();

            Assert.True(result.IsSuccessful, GetFailureMessage(result));
        }
    }

    private static string GetFailureMessage(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : string.Join(Environment.NewLine, result.FailingTypeNames ?? []);
}
