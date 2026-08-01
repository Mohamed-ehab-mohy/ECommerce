using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
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

    [SkippableFact]
    public async Task Login_With_Verified_Customer_Returns_Token_Pair()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (userId, email) = await RegisterAndVerifyAsync($"login_{Guid.NewGuid():N}@example.com");

        var login = await LoginAsync(email, "device-1");

        Assert.False(string.IsNullOrWhiteSpace(login.GetProperty("accessToken").GetString()));
        var refreshToken = login.GetProperty("refreshToken").GetString();
        Assert.NotNull(refreshToken);
        Assert.StartsWith("r_", refreshToken);
        Assert.Equal("Bearer", login.GetProperty("tokenType").GetString());
        Assert.InRange(login.GetProperty("expiresIn").GetInt32(), 850, 900);
        Assert.Equal(userId, login.GetProperty("user").GetProperty("id").GetGuid());
        Assert.Equal(email, login.GetProperty("user").GetProperty("email").GetString());
        Assert.Equal("Customer", login.GetProperty("user").GetProperty("roles")[0].GetString());

        var accessToken = login.GetProperty("accessToken").GetString()!;
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Equal(userId.ToString(), jwt.Subject);
        Assert.Equal("ecommerce-api", jwt.Issuer);
        Assert.Contains("ecommerce-client", jwt.Audiences);
        Assert.Contains(jwt.Claims, claim => claim.Type == "roles" && claim.Value == "Customer");

        await using (var scope = _fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            var stored = await db.RefreshTokens.SingleAsync(token => token.UserId == userId);
            Assert.NotEqual(refreshToken, stored.TokenHash);
            Assert.Equal(RefreshTokens.Hash(refreshToken), stored.TokenHash);
            Assert.Equal("device-1", stored.DeviceId);
        }
    }

    [SkippableFact]
    public async Task Login_Unverified_Email_Returns_403_EmailNotVerified()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var email = $"unverified_{Guid.NewGuid():N}@example.com";
        await RegisterAsync(email);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email, password = "Str0ng!Passw0rd" })
        };
        var response = await _fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(WebJson);
        Assert.NotNull(problem);
        Assert.Equal("Customer.EmailNotVerified", problem!.Extensions["code"]!.ToString());
    }

    [SkippableFact]
    public async Task Login_Wrong_Password_Five_Times_Locks_Account_With_RetryAfter()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (_, email) = await RegisterAndVerifyAsync($"locked_{Guid.NewGuid():N}@example.com");

        HttpResponseMessage last = null!;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            last = await LoginRawAsync(email, "Wrong!Passw0rd");
        }

        Assert.Equal(HttpStatusCode.Locked, last.StatusCode);
        var problem = await last.Content.ReadFromJsonAsync<ProblemDetails>(WebJson);
        Assert.NotNull(problem);
        Assert.Equal("ERR_AUTH_003", problem!.Extensions["code"]!.ToString());
        var retryAfter = Assert.IsType<JsonElement>(problem.Extensions["retryAfter"]);
        Assert.True(retryAfter.GetInt32() > 0);

        var afterLockout = await LoginRawAsync(email, "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.Locked, afterLockout.StatusCode);
    }

    [SkippableFact]
    public async Task Refresh_Rotates_Tokens_And_Old_Is_Single_Use()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (_, email) = await RegisterAndVerifyAsync($"rotate_{Guid.NewGuid():N}@example.com");
        var login = await LoginAsync(email, "device-1");
        var oldRefreshToken = login.GetProperty("refreshToken").GetString()!;

        var refreshResponse = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = oldRefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await ReadJsonAsync(refreshResponse);
        Assert.False(string.IsNullOrWhiteSpace(refreshed.GetProperty("accessToken").GetString()));
        Assert.NotEqual(oldRefreshToken, refreshed.GetProperty("refreshToken").GetString());

        var reuseResponse = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = oldRefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
        var reuseProblem = await reuseResponse.Content.ReadFromJsonAsync<ProblemDetails>(WebJson);
        Assert.NotNull(reuseProblem);
        Assert.Equal("ERR_AUTH_002", reuseProblem!.Extensions["code"]!.ToString());
    }

    [SkippableFact]
    public async Task Refresh_Concurrent_Use_Revokes_Family()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (userId, email) = await RegisterAndVerifyAsync($"race_{Guid.NewGuid():N}@example.com");
        var login = await LoginAsync(email, "device-1");
        var refreshToken = login.GetProperty("refreshToken").GetString()!;

        var responses = await Task.WhenAll(
            _fixture.Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken }),
            _fixture.Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken }));

        Assert.Equal(2, responses.Length);
        var statuses = responses.Select(response => response.StatusCode).OrderBy(code => code).ToArray();
        Assert.Equal(HttpStatusCode.OK, statuses[0]);
        Assert.Equal(HttpStatusCode.Unauthorized, statuses[1]);

        var failure = responses.Single(response => response.StatusCode == HttpStatusCode.Unauthorized);
        var problem = await failure.Content.ReadFromJsonAsync<ProblemDetails>(WebJson);
        Assert.NotNull(problem);
        Assert.Equal("ERR_AUTH_002", problem!.Extensions["code"]!.ToString());

        await using (var scope = _fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            var tokens = await db.RefreshTokens.Where(token => token.UserId == userId).ToListAsync();
            Assert.NotEmpty(tokens);
            Assert.All(tokens, token => Assert.NotNull(token.RevokedAtUtc));
        }
    }

    [SkippableFact]
    public async Task Logout_Revokes_Device_Token()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (_, email) = await RegisterAndVerifyAsync($"logout_{Guid.NewGuid():N}@example.com");
        var login = await LoginAsync(email, "device-1");
        var refreshToken = login.GetProperty("refreshToken").GetString()!;
        var accessToken = login.GetProperty("accessToken").GetString()!;

        var logout = await LogoutAsync(refreshToken, accessToken);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refreshResponse = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [SkippableFact]
    public async Task Logout_All_Revokes_All_Device_Tokens()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (userId, email) = await RegisterAndVerifyAsync($"logoutall_{Guid.NewGuid():N}@example.com");
        var first = await LoginAsync(email, "device-1");
        var second = await LoginAsync(email, "device-2");
        var accessToken = first.GetProperty("accessToken").GetString()!;
        var firstRefresh = first.GetProperty("refreshToken").GetString()!;
        var secondRefresh = second.GetProperty("refreshToken").GetString()!;

        var logoutAll = await LogoutAllAsync(accessToken);
        Assert.Equal(HttpStatusCode.NoContent, logoutAll.StatusCode);

        var firstResponse = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = firstRefresh });
        var secondResponse = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = secondRefresh });
        Assert.Equal(HttpStatusCode.Unauthorized, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);

        await using (var scope = _fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            var tokens = await db.RefreshTokens.Where(token => token.UserId == userId).ToListAsync();
            Assert.NotEmpty(tokens);
            Assert.All(tokens, token => Assert.NotNull(token.RevokedAtUtc));
        }
    }

    private async Task<(Guid UserId, string Email)> RegisterAndVerifyAsync(string email)
    {
        var registerResponse = await RegisterAsync(email);
        var userId = (await ReadJsonAsync(registerResponse)).GetProperty("userId").GetGuid();
        var token = await GetVerificationTokenAsync(userId);

        var verify = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        return (userId, email);
    }

    [SkippableFact]
    public async Task ForgotPassword_Always_Returns_202()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var knownEmail = $"forgot_{Guid.NewGuid():N}@example.com";
        var (userId, _) = await RegisterAndVerifyAsync(knownEmail);

        var knownResponse = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = knownEmail });
        Assert.Equal(HttpStatusCode.Accepted, knownResponse.StatusCode);

        await WaitUntilAsync(() => _fixture.EmailSender.Messages.Any(message =>
            message.To == knownEmail && message.Subject == "Reset your password"));

        var resetMessage = _fixture.EmailSender.Messages.Last(message =>
            message.To == knownEmail && message.Subject == "Reset your password");
        Assert.Contains("/reset-password?token=", resetMessage.HtmlBody);

        var unknownEmail = $"ghost_{Guid.NewGuid():N}@example.com";
        var unknownResponse = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = unknownEmail });
        Assert.Equal(HttpStatusCode.Accepted, unknownResponse.StatusCode);

        await Task.Delay(500);
        Assert.DoesNotContain(_fixture.EmailSender.Messages, message => message.To == unknownEmail);
    }

    [SkippableFact]
    public async Task Password_Reset_Flow_Changes_Password_And_Revokes_Sessions()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (userId, email) = await RegisterAndVerifyAsync($"pwreset_{Guid.NewGuid():N}@example.com");
        var login = await LoginAsync(email, "device-1");
        var oldRefreshToken = login.GetProperty("refreshToken").GetString()!;

        var forgot = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);
        var resetToken = await GetPasswordResetTokenAsync(userId);

        var reset = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            token = resetToken,
            newPassword = "N3w!Passw0rd2026"
        });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal("passwordReset", (await ReadJsonAsync(reset)).GetProperty("status").GetString());

        var staleSession = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = oldRefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, staleSession.StatusCode);

        await WaitUntilAsync(() => _fixture.EmailSender.Messages.Any(message =>
            message.To == email && message.Subject == "Your password has been changed"));

        var reVerifyToken = await GetPasswordResetReVerifyTokenAsync(userId);
        var reVerify = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = reVerifyToken });
        Assert.Equal(HttpStatusCode.OK, reVerify.StatusCode);

        var loginWithNewPassword = await LoginRawAsync(email, "N3w!Passw0rd2026", "device-1");
        Assert.Equal(HttpStatusCode.OK, loginWithNewPassword.StatusCode);

        var loginWithOldPassword = await LoginRawAsync(email, "Str0ng!Passw0rd", "device-1");
        Assert.Equal(HttpStatusCode.Unauthorized, loginWithOldPassword.StatusCode);
    }

    [SkippableFact]
    public async Task Reset_Password_Token_Is_Single_Use()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (userId, email) = await RegisterAndVerifyAsync($"pwuse_{Guid.NewGuid():N}@example.com");

        await _fixture.Client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        var resetToken = await GetPasswordResetTokenAsync(userId);

        var first = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            token = resetToken,
            newPassword = "N3w!Passw0rd2026"
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _fixture.Client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            token = resetToken,
            newPassword = "An0ther!Passw0rd"
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(WebJson);
        Assert.NotNull(problem);
        Assert.Equal("Customer.PasswordResetTokenInvalid", problem!.Extensions["code"]!.ToString());
    }

    [SkippableFact]
    public async Task Get_Me_Returns_Authenticated_Customers_Profile()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (userId, email) = await RegisterAndVerifyAsync($"me_{Guid.NewGuid():N}@example.com");
        var accessToken = (await LoginAsync(email, "device-1")).GetProperty("accessToken").GetString()!;

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await ReadJsonAsync(response);
        Assert.Equal(userId, profile.GetProperty("id").GetGuid());
        Assert.Equal(email, profile.GetProperty("email").GetString());
        Assert.Equal("Ahmed Hassan", profile.GetProperty("displayName").GetString());
        Assert.Equal("ar", profile.GetProperty("locale").GetString());
        Assert.Equal("AED", profile.GetProperty("currency").GetString());
        Assert.True(profile.GetProperty("emailVerified").GetBoolean());
    }

    [SkippableFact]
    public async Task Patch_Me_Updates_Profile()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (_, email) = await RegisterAndVerifyAsync($"patch_{Guid.NewGuid():N}@example.com");
        var accessToken = (await LoginAsync(email, "device-1")).GetProperty("accessToken").GetString()!;

        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/me")
        {
            Content = JsonContent.Create(new
            {
                displayName = "Ahmed Ehab",
                phone = "+201001234567",
                locale = "en",
                currency = "USD"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await ReadJsonAsync(response);
        Assert.Equal("Ahmed Ehab", profile.GetProperty("displayName").GetString());
        Assert.Equal("+201001234567", profile.GetProperty("phone").GetString());
        Assert.Equal("en", profile.GetProperty("locale").GetString());
        Assert.Equal("USD", profile.GetProperty("currency").GetString());
    }

    [SkippableFact]
    public async Task Me_Without_Token_Returns_401()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var response = await _fixture.Client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task Address_Flow_Adds_Lists_And_Scopes_To_Owner()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (_, email) = await RegisterAndVerifyAsync($"addr_{Guid.NewGuid():N}@example.com");
        var accessToken = (await LoginAsync(email, "device-1")).GetProperty("accessToken").GetString()!;

        var add = await AddAddressAsync(accessToken, "Home", "1 Sheikh Zayed Rd", "Dubai", "AE");
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        var addressId = (await ReadJsonAsync(add)).GetProperty("id").GetGuid();

        var (otherUserId, _) = await RegisterAndVerifyAsync($"other_{Guid.NewGuid():N}@example.com");
        var foreignAddressId = Guid.Empty;
        await using (var scope = _fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            var foreign = CustomerAddress.Create(
                otherUserId,
                "Work",
                "2 Another St",
                "Cairo",
                null,
                "EG",
                null,
                DateTime.UtcNow);
            db.Set<CustomerAddress>().Add(foreign);
            await db.SaveChangesAsync();
            foreignAddressId = foreign.Id;
        }

        var list = await GetAddressesAsync(accessToken);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var addresses = await ReadJsonAsync(list);
        var items = addresses.EnumerateArray().ToList();
        var item = Assert.Single(items);
        Assert.Equal(addressId, item.GetProperty("id").GetGuid());
        Assert.Equal("Home", item.GetProperty("label").GetString());
        Assert.Equal("AE", item.GetProperty("country").GetString());
        Assert.DoesNotContain(foreignAddressId, items.Select(candidate => candidate.GetProperty("id").GetGuid()));
    }

    [SkippableFact]
    public async Task Delete_Address_Own_Address_Returns_204()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (_, email) = await RegisterAndVerifyAsync($"del_{Guid.NewGuid():N}@example.com");
        var accessToken = (await LoginAsync(email, "device-1")).GetProperty("accessToken").GetString()!;

        var add = await AddAddressAsync(accessToken, "Home", "1 Sheikh Zayed Rd", "Dubai", "AE");
        var addressId = (await ReadJsonAsync(add)).GetProperty("id").GetGuid();

        var delete = await DeleteAddressAsync(accessToken, addressId);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var list = await ReadJsonAsync(await GetAddressesAsync(accessToken));
        Assert.Empty(list.EnumerateArray());
    }

    [SkippableFact]
    public async Task Delete_Address_Not_Owned_Returns_404()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (userId, email) = await RegisterAndVerifyAsync($"nodel_{Guid.NewGuid():N}@example.com");
        var accessToken = (await LoginAsync(email, "device-1")).GetProperty("accessToken").GetString()!;

        var otherAddressId = Guid.NewGuid();
        await using (var scope = _fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            db.Set<CustomerAddress>().Add(CustomerAddress.Create(
                userId,
                null,
                "1 Sheikh Zayed Rd",
                "Dubai",
                null,
                "AE",
                null,
                DateTime.UtcNow));
            await db.SaveChangesAsync();
            otherAddressId = db.Set<CustomerAddress>().OrderBy(a => a.CreatedAt).Last().Id;
        }

        var otherEmail = $"foreign_{Guid.NewGuid():N}@example.com";
        await RegisterAndVerifyAsync(otherEmail);
        var otherToken = (await LoginAsync(otherEmail, "device-1")).GetProperty("accessToken").GetString()!;

        var delete = await DeleteAddressAsync(otherToken, otherAddressId);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var problem = await delete.Content.ReadFromJsonAsync<ProblemDetails>(WebJson);
        Assert.NotNull(problem);
        Assert.Equal("Address.AddressNotFound", problem!.Extensions["code"]!.ToString());
    }

    [SkippableFact]
    public async Task Add_Address_With_Invalid_Country_Returns_422()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var (_, email) = await RegisterAndVerifyAsync($"badaddr_{Guid.NewGuid():N}@example.com");
        var accessToken = (await LoginAsync(email, "device-1")).GetProperty("accessToken").GetString()!;

        var add = await AddAddressAsync(accessToken, null, "1 Sheikh Zayed Rd", "Dubai", "123");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, add.StatusCode);
        var problem = await add.Content.ReadFromJsonAsync<ProblemDetails>(WebJson);
        Assert.NotNull(problem);
        Assert.Equal("problems/validation-failed", problem!.Type);
    }

    private async Task<string> GetPasswordResetTokenAsync(Guid userId)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        var message = await db.OutboxMessages.SingleAsync(candidate =>
            candidate.AggregateId == userId &&
            candidate.EventType == typeof(PasswordResetRequested).FullName);

        using var json = JsonDocument.Parse(message.Content);
        return json.RootElement.GetProperty("ResetToken").GetString()!;
    }

    private async Task<string> GetPasswordResetReVerifyTokenAsync(Guid userId)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        var message = await db.OutboxMessages.SingleAsync(candidate =>
            candidate.AggregateId == userId &&
            candidate.EventType == typeof(PasswordReset).FullName);

        using var json = JsonDocument.Parse(message.Content);
        return json.RootElement.GetProperty("NewVerificationToken").GetString()!;
    }

    private async Task<JsonElement> LoginAsync(string email, string deviceId)
    {
        var response = await LoginRawAsync(email, "Str0ng!Passw0rd", deviceId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadJsonAsync(response);
    }

    private Task<HttpResponseMessage> LoginRawAsync(string email, string password, string? deviceId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email, password })
        };
        request.Headers.Add("X-Device-Id", deviceId ?? "test-device");
        return _fixture.Client.SendAsync(request);
    }

    private Task<HttpResponseMessage> LogoutAsync(string refreshToken, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new { refreshToken })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _fixture.Client.SendAsync(request);
    }

    private Task<HttpResponseMessage> LogoutAllAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout-all");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _fixture.Client.SendAsync(request);
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

    private Task<HttpResponseMessage> AddAddressAsync(string accessToken, string? label, string street, string city, string country)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/addresses")
        {
            Content = JsonContent.Create(new
            {
                label,
                street,
                city,
                region = "Dubai",
                country,
                postalCode = "00000"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _fixture.Client.SendAsync(request);
    }

    private Task<HttpResponseMessage> GetAddressesAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me/addresses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _fixture.Client.SendAsync(request);
    }

    private Task<HttpResponseMessage> DeleteAddressAsync(string accessToken, Guid addressId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/me/addresses/{addressId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _fixture.Client.SendAsync(request);
    }

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
