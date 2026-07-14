using System.Reflection;
using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.Domain.Repositories;
using ECommerce.UseCases.Products;
using NetArchTest.Rules;

namespace ECommerce.ArchitectureTests.Tests;

public class InfrastructureTests
{
    private static readonly Assembly InfrastructureAssembly =
        typeof(StoreDbContext).Assembly;

    [Fact]
    public void Infrastructure_ShouldNotDependOnAPI()
    {
        var result = Types
            .InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("ECommerce.API")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void QueryServices_ShouldDependOnUseCases()
    {
        var result = Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("QueryService")
            .Should()
            .HaveDependencyOn("ECommerce.UseCases")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void DbContext_ShouldBePublic()
    {
        var result = Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .HaveName("StoreDbContext")
            .Should()
            .BePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Repository_ShouldImplementIRepositoryInterface()
    {
        var types = Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .HaveName("Repository")
            .GetTypes();

        foreach (var type in types)
        {
            var implements = type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRepository<>));

            implements.Should().BeTrue($"{type.Name} should implement IRepository<T>");
        }
    }

    [Fact]
    public void UnitOfWork_ShouldImplementIUnitOfWorkInterface()
    {
        var types = Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .HaveName("UnitOfWork")
            .GetTypes();

        foreach (var type in types)
        {
            var implements = type.GetInterfaces()
                .Any(i => i == typeof(IUnitOfWork));

            implements.Should().BeTrue($"{type.Name} should implement IUnitOfWork");
        }
    }
}
