using System.Reflection;
using ECommerce.Domain.Entities;
using ECommerce.UseCases.Common.Messaging;
using NetArchTest.Rules;

namespace ECommerce.ArchitectureTests.Tests;

public class DomainTests
{
    private static readonly Assembly DomainAssembly = typeof(BaseEntity).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnAnyOtherProject()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "ECommerce.UseCases",
                "ECommerce.Infrastructure",
                "ECommerce.API")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Entities_ShouldNotDependOnEntityFramework()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceContaining("Entities")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_ShouldNotDependOnDependencyInjection()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Extensions.DependencyInjection")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_ShouldNotDependOnMappingLibraries()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Mapster", "AutoMapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Errors_ShouldBeSealedRecords()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("Errors")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Entities_ShouldHavePrivateParameterlessConstructor()
    {
        var entityTypes = Types
            .InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(BaseEntity))
            .GetTypes();

        foreach (var type in entityTypes)
        {
            var ctor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null, Type.EmptyTypes, null);

            ctor.Should().BeNull($"{type.Name} should not have a public parameterless constructor");
        }
    }
}
