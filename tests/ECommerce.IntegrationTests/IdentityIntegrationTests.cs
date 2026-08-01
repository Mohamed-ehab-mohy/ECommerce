using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Domain.Events;
using ECommerce.Domain.Identity;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Identity;
using ECommerce.UseCases.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public sealed class IdentityIntegrationTests : IClassFixture<IdentityApiFixture>
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private readonly IdentityApiFixture _fixture;

    public IdentityIntegrationTests(IdentityApiFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Register_Creates_Pending_Verification_Customer()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var email = $"ahmed_{Guid.NewGuid():N}@example.com";

        var response = await RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("pendingVerification", body.GetProperty("status").GetString());
        Assert.Equal("Verification email sent.", body.GetProperty("message").GetString());
        var userId = body.GetProperty("userId").GetGuid();

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        var customer = await db.Set<Customer>().SingleAsync(c => c.Id == userId);
        Assert.Equal(email, customer.Email);
        Assert.False(customer.EmailVerified);
        Assert.StartsWith("$2", customer.PasswordHash);
        Assert.True(new BcryptPasswordHasher().Verify("Str0ng!Passw0rd", customer.PasswordHash));
        Assert.NotNull(customer.VerificationTokenHash);
        Assert.True(customer.VerificationTokenExpiresAt > DateTime.UtcNow.AddHours(20));

        var outboxMessage = await db.OutboxMessages.SingleAsync(message => message.AggregateId == userId);
        Assert.Equal(typeof(CustomerRegistered).FullName, outboxMessage.EventType);
        Assert.Null(outboxMessage.ProcessedOn);
    }

    [SkippableFact]
    public async Task Register_Duplicate_Email_Returns_409_Conflict()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var email = $"dupe_{Guid.NewGuid():N}@example.com";
        await RegisterAsync(email);

        var response = await RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(WebJson);
        Assert.NotNull(problem);
        Assert.Equal("Customer.EmailAlreadyExists", problem!.Extensions["code"]!.ToString());
    }

    [SkippableFact]
    public async Task Register_Invalid_Input_Returns_422_Validation_Failed()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "not-an-email",
            password = "short",
            displayName = "",
            locale = "ar",
            currency = "AED"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(WebJson);
        Assert.NotNull(problem);
        Assert.Equal("problems/validation-failed", problem!.Type);
    }

    [SkippableFact]
    public async Task Register_Dispatches_Verification_Email_Via_Outbox()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var email = $"email_{Guid.NewGuid():N}@example.com";

        await RegisterAsync(email);

        await WaitUntilAsync(() => _fixture.EmailSender.Messages.Any(message => message.To == email));

        var message = _fixture.EmailSender.Messages.Single(message => message.To == email);
        Assert.Equal("Verify your email address", message.Subject);
        Assert.Contains("/verify-email?token=", message.HtmlBody);
    }

    [SkippableFact]
    public async Task Verify_Email_With_Real_Token_Succeeds_And_Is_Single_Use()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var email = $"verify_{Guid.NewGuid():N}@example.com";
        var registerResponse = await RegisterAsync(email);
        var userId = (await ReadJsonAsync(registerResponse)).GetProperty("userId").GetGuid();
        var token = await GetVerificationTokenAsync(userId);

        var first = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("verified", (await ReadJsonAsync(first)).GetProperty("status").GetString());

        await using (var scope = _fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            var customer = await db.Set<Customer>().SingleAsync(c => c.Id == userId);
            Assert.True(customer.EmailVerified);
            Assert.NotNull(customer.EmailVerifiedAt);
            Assert.Null(customer.VerificationTokenHash);
        }

        var second = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
    }

    [SkippableFact]
    public async Task Verify_Email_With_Expired_Token_Returns_422()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var rawToken = $"expired-{Guid.NewGuid():N}";
        var tokenHash = VerificationTokens.Hash(rawToken);
        var customer = Customer.Register(
            $"expired_{Guid.NewGuid():N}@example.com",
            "Expired User",
            "ar",
            "AED",
            "hash",
            tokenHash,
            DateTime.UtcNow.AddHours(-1),
            rawToken);

        await using (var scope = _fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            db.Set<Customer>().Add(customer);
            await db.SaveChangesAsync();
        }

        var response = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = rawToken });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(WebJson);
        Assert.NotNull(problem);
        Assert.Equal("Customer.VerificationTokenExpired", problem!.Extensions["code"]!.ToString());
    }

    private Task<HttpResponseMessage> RegisterAsync(string email) =>
        _fixture.Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Str0ng!Passw0rd",
            displayName = "Ahmed Hassan",
            locale = "ar",
            currency = "AED"
        });

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(WebJson);

    private async Task<string> GetVerificationTokenAsync(Guid userId)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        var outboxMessage = await db.OutboxMessages.SingleAsync(message => message.AggregateId == userId);

        using var json = JsonDocument.Parse(outboxMessage.Content);
        return json.RootElement.GetProperty("VerificationToken").GetString()!;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutSeconds = 20)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail("Timed out waiting for condition.");
            }

            await Task.Delay(250);
        }
    }
}
