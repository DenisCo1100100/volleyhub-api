using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Domain.Auth;
using VolleyHub.Infrastructure.Persistence;

namespace VolleyHub.Api.IntegrationTests.Auth
{
    public sealed class RefreshTokenAuthenticationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string RefreshTokenCookieName = "volleyhub.refreshToken";
        private const string Password = "Password123!";

        private readonly CustomWebApplicationFactory _factory;

        public RefreshTokenAuthenticationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Login_ShouldReturnAccessTokenAndSecureHttpOnlyRefreshCookie()
        {
            var uniqueId = Guid.NewGuid().ToString("N");
            var email = $"refresh-login-{uniqueId}@test.com";
            var client = CreateClient();

            await RegisterAsync(client, email);

            var loginResponse = await client.PostAsApiJsonAsync(
                "/api/auth/login",
                new
                {
                    Email = email,
                    Password
                });

            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var authResponse = await loginResponse.Content
                .ReadFromApiJsonAsync<AuthResponse>();

            authResponse.Should().NotBeNull();
            authResponse!.UserId.Should().NotBeEmpty();
            authResponse.Email.Should().Be(email);
            authResponse.AccessToken.Should().NotBeNullOrWhiteSpace();

            var responseJson = await loginResponse.Content.ReadAsStringAsync();

            using var jsonDocument = JsonDocument.Parse(responseJson);

            jsonDocument.RootElement.TryGetProperty(
                "refreshToken",
                out _).Should().BeFalse();

            jsonDocument.RootElement.TryGetProperty(
                "refreshTokenExpiresAt",
                out _).Should().BeFalse();

            var setCookieHeader = GetRefreshTokenSetCookieHeader(loginResponse);
            var normalizedSetCookieHeader = setCookieHeader.ToLowerInvariant();

            normalizedSetCookieHeader.Should().Contain("httponly");
            normalizedSetCookieHeader.Should().Contain("secure");
            normalizedSetCookieHeader.Should().Contain("samesite=none");
            normalizedSetCookieHeader.Should().Contain("path=/api/auth");
            normalizedSetCookieHeader.Should().Contain("expires=");

            var refreshToken = GetRefreshTokenFromResponse(loginResponse);

            refreshToken.Should().NotBeNullOrWhiteSpace();
            responseJson.Should().NotContain(refreshToken);
        }

        [Fact]
        public async Task Refresh_ShouldRotateTokenAndRevokeReplacement_WhenOldTokenIsReused()
        {
            var uniqueId = Guid.NewGuid().ToString("N");
            var email = $"refresh-rotation-{uniqueId}@test.com";
            var client = CreateClient();

            var registerResult = await RegisterAsync(client, email);
            var originalRefreshToken = GetRefreshTokenFromResponse(registerResult.Response);

            var refreshResponse = await PostWithRefreshTokenAsync(
                client,
                "/api/auth/refresh",
                originalRefreshToken);

            refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var refreshedAuth = await refreshResponse.Content
                .ReadFromApiJsonAsync<AuthResponse>();

            refreshedAuth.Should().NotBeNull();
            refreshedAuth!.UserId.Should().Be(registerResult.Auth.UserId);
            refreshedAuth.Email.Should().Be(email);
            refreshedAuth.AccessToken.Should().NotBeNullOrWhiteSpace();

            var replacementRefreshToken = GetRefreshTokenFromResponse(refreshResponse);

            replacementRefreshToken.Should().NotBeNullOrWhiteSpace();
            replacementRefreshToken.Should().NotBe(originalRefreshToken);

            var reuseResponse = await PostWithRefreshTokenAsync(
                client,
                "/api/auth/refresh",
                originalRefreshToken);

            reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var reuseProblem = await reuseResponse.Content
                .ReadFromApiJsonAsync<ProblemDetailsResponse>();

            reuseProblem.Should().NotBeNull();
            reuseProblem!.Status.Should().Be((int)HttpStatusCode.Unauthorized);
            reuseProblem.Title.Should().Be("Unauthorized");
            reuseProblem.Detail.Should().Be("Refresh token reuse detected.");

            var replacementResponse = await PostWithRefreshTokenAsync(
                client,
                "/api/auth/refresh",
                replacementRefreshToken);

            replacementResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var replacementProblem = await replacementResponse.Content
                .ReadFromApiJsonAsync<ProblemDetailsResponse>();

            replacementProblem.Should().NotBeNull();
            replacementProblem!.Detail.Should().Be("Refresh token has been revoked.");
        }

        [Fact]
        public async Task Logout_ShouldRevokeRefreshTokenAndDeleteCookie()
        {
            var uniqueId = Guid.NewGuid().ToString("N");
            var email = $"refresh-logout-{uniqueId}@test.com";
            var client = CreateClient();

            var registerResult = await RegisterAsync(client, email);
            var refreshToken = GetRefreshTokenFromResponse(registerResult.Response);

            var logoutResponse = await PostWithRefreshTokenAsync(
                client,
                "/api/auth/logout",
                refreshToken);

            logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var deleteCookieHeader = GetRefreshTokenSetCookieHeader(logoutResponse);
            var normalizedDeleteCookieHeader = deleteCookieHeader.ToLowerInvariant();

            normalizedDeleteCookieHeader.Should().Contain($"{RefreshTokenCookieName.ToLowerInvariant()}=");
            normalizedDeleteCookieHeader.Should().Contain("path=/api/auth");
            normalizedDeleteCookieHeader.Should().Contain("expires=");

            var refreshResponse = await PostWithRefreshTokenAsync(
                client,
                "/api/auth/refresh",
                refreshToken);

            refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var problem = await refreshResponse.Content
                .ReadFromApiJsonAsync<ProblemDetailsResponse>();

            problem.Should().NotBeNull();
            problem!.Detail.Should().Be("Refresh token has been revoked.");

            var secondLogoutResponse = await client.PostAsync(
                "/api/auth/logout",
                content: null);

            secondLogoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Refresh_ShouldReturnUnauthorized_WhenCookieIsMissing()
        {
            var client = CreateClient();

            var response = await client.PostAsync(
                "/api/auth/refresh",
                content: null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var problem = await response.Content
                .ReadFromApiJsonAsync<ProblemDetailsResponse>();

            problem.Should().NotBeNull();
            problem!.Status.Should().Be((int)HttpStatusCode.Unauthorized);
            problem.Title.Should().Be("Unauthorized");
            problem.Detail.Should().Be("Refresh token cookie is missing.");
        }

        [Fact]
        public async Task Refresh_ShouldReturnUnauthorized_WhenTokenIsInvalid()
        {
            var client = CreateClient();

            var response = await PostWithRefreshTokenAsync(
                client,
                "/api/auth/refresh",
                "invalid-refresh-token");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var problem = await response.Content
                .ReadFromApiJsonAsync<ProblemDetailsResponse>();

            problem.Should().NotBeNull();
            problem!.Detail.Should().Be("Refresh token is invalid.");
        }

        [Fact]
        public async Task Refresh_ShouldReturnUnauthorized_WhenTokenIsExpired()
        {
            var uniqueId = Guid.NewGuid().ToString("N");
            var email = $"refresh-expired-{uniqueId}@test.com";
            var client = CreateClient();

            var registerResult = await RegisterAsync(client, email);

            var expiredRefreshToken = $"expired-{Guid.NewGuid():N}";

            await SeedExpiredRefreshTokenAsync(
                registerResult.Auth.UserId,
                expiredRefreshToken);

            var response = await PostWithRefreshTokenAsync(
                client,
                "/api/auth/refresh",
                expiredRefreshToken);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var problem = await response.Content
                .ReadFromApiJsonAsync<ProblemDetailsResponse>();

            problem.Should().NotBeNull();
            problem!.Detail.Should().Be("Refresh token has expired.");
        }

        [Fact]
        public async Task Login_ShouldRemoveExpiredRefreshTokens()
        {
            var uniqueId = Guid.NewGuid().ToString("N");
            var email = $"refresh-cleanup-{uniqueId}@test.com";
            var client = CreateClient();

            var registerResult = await RegisterAsync(client, email);
            var expiredRefreshToken = $"cleanup-expired-{Guid.NewGuid():N}";
            var expiredRefreshTokenHash = HashToken(expiredRefreshToken);

            await SeedExpiredRefreshTokenAsync(
                registerResult.Auth.UserId,
                expiredRefreshToken);

            var existsBeforeLogin = await RefreshTokenExistsAsync(
                expiredRefreshTokenHash);

            existsBeforeLogin.Should().BeTrue();

            var loginResponse = await client.PostAsApiJsonAsync(
                "/api/auth/login",
                new
                {
                    Email = email,
                    Password
                });

            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var existsAfterLogin = await RefreshTokenExistsAsync(
                expiredRefreshTokenHash);

            existsAfterLogin.Should().BeFalse();
        }

        private HttpClient CreateClient()
        {
            return _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false,
                    HandleCookies = false
                });
        }

        private static async Task<RegisterResult> RegisterAsync(
            HttpClient client,
            string email)
        {
            var response = await client.PostAsApiJsonAsync(
                "/api/auth/register",
                new
                {
                    Email = email,
                    Password
                });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var authResponse = await response.Content
                .ReadFromApiJsonAsync<AuthResponse>();

            authResponse.Should().NotBeNull();
            authResponse!.UserId.Should().NotBeEmpty();
            authResponse.Email.Should().Be(email);
            authResponse.AccessToken.Should().NotBeNullOrWhiteSpace();

            return new RegisterResult(
                response,
                authResponse);
        }

        private static async Task<HttpResponseMessage> PostWithRefreshTokenAsync(
            HttpClient client,
            string requestUri,
            string refreshToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                requestUri);

            request.Headers.Add(
                "Cookie",
                $"{RefreshTokenCookieName}={refreshToken}");

            return await client.SendAsync(request);
        }

        private async Task SeedExpiredRefreshTokenAsync(
            Guid userId,
            string rawRefreshToken)
        {
            using var scope = _factory.Services.CreateScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var refreshToken = RefreshToken.Create(
                userId,
                HashToken(rawRefreshToken),
                DateTimeOffset.UtcNow.AddMinutes(-1));

            await dbContext.RefreshTokens.AddAsync(refreshToken);
            await dbContext.SaveChangesAsync();
        }

        private async Task<bool> RefreshTokenExistsAsync(
            string tokenHash)
        {
            using var scope = _factory.Services.CreateScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            return await dbContext.RefreshTokens
                .AnyAsync(
                    refreshToken => refreshToken.TokenHash == tokenHash);
        }

        private static string GetRefreshTokenFromResponse(
            HttpResponseMessage response)
        {
            var setCookieHeader = GetRefreshTokenSetCookieHeader(response);
            var prefix = $"{RefreshTokenCookieName}=";

            var valueStart = setCookieHeader.IndexOf(
                prefix,
                StringComparison.OrdinalIgnoreCase);

            valueStart.Should().BeGreaterThanOrEqualTo(0);

            valueStart += prefix.Length;

            var valueEnd = setCookieHeader.IndexOf(
                ';',
                valueStart);

            if (valueEnd < 0)
            {
                valueEnd = setCookieHeader.Length;
            }

            return setCookieHeader[valueStart..valueEnd];
        }

        private static string GetRefreshTokenSetCookieHeader(
            HttpResponseMessage response)
        {
            response.Headers.TryGetValues(
                "Set-Cookie",
                out var setCookieHeaders).Should().BeTrue();

            setCookieHeaders.Should().NotBeNull();

            var setCookieHeader = setCookieHeaders!
                .SingleOrDefault(
                    value => value.StartsWith(
                        $"{RefreshTokenCookieName}=",
                        StringComparison.OrdinalIgnoreCase));

            setCookieHeader.Should().NotBeNull();

            return setCookieHeader!;
        }

        private static string HashToken(string rawRefreshToken)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(rawRefreshToken);
            var hashBytes = SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hashBytes);
        }

        private sealed record RegisterResult(
            HttpResponseMessage Response,
            AuthResponse Auth);

        private sealed record AuthResponse(
            Guid UserId,
            string Email,
            string AccessToken);

        private sealed record ProblemDetailsResponse(
            int? Status,
            string? Title,
            string? Detail,
            string? Instance);
    }
}