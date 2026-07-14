using ECommerce.Domain;
using ECommerce.UseCases.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.UnitTests.Messaging;

public record TestQuery(string Name) : IRequest<string>;

public class TestQueryHandler : IRequestHandler<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery request, CancellationToken ct = default)
    {
        return Task.FromResult($"Hello {request.Name}");
    }
}

public record TestCommand : ICommand;

public class TestCommandHandler : ICommandHandler<TestCommand>
{
    public Task<Result> HandleAsync(TestCommand request, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }
}

public record TestCommandWithResponse(string Value) : ICommand<string>;

public class TestCommandWithResponseHandler : ICommandHandler<TestCommandWithResponse, string>
{
    public Task<Result<string>> HandleAsync(TestCommandWithResponse request, CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success(request.Value));
    }
}

public record UnregisteredQuery : IRequest<string>;

public class SenderTests
{
    private readonly Sender _sender;

    public SenderTests()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestQuery, string>, TestQueryHandler>();
        services.AddScoped<IRequestHandler<TestCommand, Result>, TestCommandHandler>();
        services.AddScoped<IRequestHandler<TestCommandWithResponse, Result<string>>, TestCommandWithResponseHandler>();
        var provider = services.BuildServiceProvider();
        _sender = new Sender(provider);
    }

    [Fact]
    public async Task Send_Query_ShouldResolveHandlerAndReturnResult()
    {
        var query = new TestQuery("World");

        var result = await _sender.Send(query);

        result.Should().Be("Hello World");
    }

    [Fact]
    public async Task Send_Command_ShouldResolveHandlerAndReturnSuccess()
    {
        var command = new TestCommand();

        var result = await _sender.Send(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Send_CommandWithResponse_ShouldReturnData()
    {
        var command = new TestCommandWithResponse("test-value");

        var result = await _sender.Send(command);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("test-value");
    }

    [Fact]
    public async Task Send_UnregisteredHandler_ShouldThrow()
    {
        var query = new UnregisteredQuery();

        var act = () => _sender.Send(query);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Send_ShouldPassCancellationTokenToHandler()
    {
        var cts = new CancellationTokenSource();
        var query = new TestQuery("Test");

        var result = await _sender.Send(query, cts.Token);

        result.Should().Be("Hello Test");
    }

    [Fact]
    public async Task Send_Command_ShouldReturnFailure_WhenHandlerReturnsFailure()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestCommand, Result>, FailingCommandHandler>();
        var provider = services.BuildServiceProvider();
        var sender = new Sender(provider);

        var result = await sender.Send(new TestCommand());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Test.Error");
    }
}

public class FailingCommandHandler : ICommandHandler<TestCommand>
{
    public Task<Result> HandleAsync(TestCommand request, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Failure(new Error("Test.Error", "Test error", ErrorType.InternalServerError)));
    }
}
