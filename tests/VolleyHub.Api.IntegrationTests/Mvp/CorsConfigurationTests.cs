using System.Net;
using FluentAssertions;
using VolleyHub.Api.IntegrationTests.Common;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class CorsConfigurationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CorsConfigurationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PreflightRequest_ShouldAllowLocalFrontendOriginAndAuthorizationHeader()
        {
            var client = _factory.CreateClient();

            using var request = new HttpRequestMessage(
                HttpMethod.Options,
                "/api/games");

            request.Headers.Add("Origin", "http://localhost:5173");
            request.Headers.Add("Access-Control-Request-Method", "GET");
            request.Headers.Add("Access-Control-Request-Headers", "Authorization, Content-Type");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            response.Headers.TryGetValues(
                    "Access-Control-Allow-Origin",
                    out var allowedOrigins)
                .Should()
                .BeTrue();

            allowedOrigins.Should().Contain("http://localhost:5173");

            response.Headers.TryGetValues(
                    "Access-Control-Allow-Headers",
                    out var allowedHeaders)
                .Should()
                .BeTrue();

            var allowedHeadersValue = string.Join(",", allowedHeaders!);

            allowedHeadersValue.Should().Contain("Authorization");
            allowedHeadersValue.Should().Contain("Content-Type");

            response.Headers.TryGetValues(
                    "Access-Control-Allow-Methods",
                    out var allowedMethods)
                .Should()
                .BeTrue();

            var allowedMethodsValue = string.Join(",", allowedMethods!);

            allowedMethodsValue.Should().Contain("GET");
        }

        [Fact]
        public async Task PreflightRequest_ShouldNotAllowUnknownOrigin()
        {
            var client = _factory.CreateClient();

            using var request = new HttpRequestMessage(
                HttpMethod.Options,
                "/api/games");

            request.Headers.Add("Origin", "http://evil.localhost:5173");
            request.Headers.Add("Access-Control-Request-Method", "GET");
            request.Headers.Add("Access-Control-Request-Headers", "Authorization, Content-Type");

            var response = await client.SendAsync(request);

            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        }
    }
}