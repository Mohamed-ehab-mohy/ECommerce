using System.Reflection;
using ECommerce.UseCases.Common.Messaging;
using NetArchTest.Rules;

namespace ECommerce.ArchitectureTests.Tests;

public class UseCasesTests
{
    private static readonly Assembly UseCasesAssembly = typeof(ISender).Assembly;

    [Fact]
    public void UseCases_ShouldNotDependOnInfrastructure()
    {
        var result = Types
            .InAssembly(UseCasesAssembly)
            .ShouldNot()
            .HaveDependencyOn("ECommerce.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void UseCases_ShouldNotDependOnAPI()
    {
        var result = Types
            .InAssembly(UseCasesAssembly)
            .ShouldNot()
            .HaveDependencyOn("ECommerce.API")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void UseCases_ShouldNotDependOnEFCore()
    {
        var result = Types
            .InAssembly(UseCasesAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void UseCases_Messaging_ShouldNotDependOnExternalFrameworks()
    {
        var result = Types
            .InAssembly(UseCasesAssembly)
            .That()
            .ResideInNamespaceContaining("Messaging")
            .ShouldNot()
            .HaveDependencyOnAny(
                "ECommerce.Infrastructure",
                "ECommerce.API",
                "Microsoft.EntityFrameworkCore",
                "Mapster")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void ConcreteCommandTypes_ShouldImplementICommandInterface()
    {
        var types = Types
            .InAssembly(UseCasesAssembly)
            .That()
            .HaveNameEndingWith("Command")
            .And()
            .DoNotHaveNameEndingWith("Handler")
            .And()
            .AreClasses()
            .GetTypes();

        foreach (var type in types)
        {
            var implementsCommand = type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommand) ||
                     i.GetGenericTypeDefinition() == typeof(ICommand<>)));

            implementsCommand.Should().BeTrue($"{type.Name} should implement ICommand or ICommand<T>");
        }
    }

    [Fact]
    public void ConcreteQueryTypes_ShouldImplementIQueryInterface()
    {
        var types = Types
            .InAssembly(UseCasesAssembly)
            .That()
            .HaveNameEndingWith("Query")
            .And()
            .DoNotHaveNameEndingWith("Handler")
            .And()
            .AreClasses()
            .GetTypes();

        foreach (var type in types)
        {
            var implementsQuery = type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

            implementsQuery.Should().BeTrue($"{type.Name} should implement IQuery<T>");
        }
    }

    [Fact]
    public void Handlers_ShouldImplementIRequestHandlerInterface()
    {
        var types = Types
            .InAssembly(UseCasesAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .GetTypes();

        foreach (var type in types)
        {
            var implementsHandler = type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            implementsHandler.Should().BeTrue($"{type.Name} should implement IRequestHandler<TReq, TRes>");
        }
    }
}
