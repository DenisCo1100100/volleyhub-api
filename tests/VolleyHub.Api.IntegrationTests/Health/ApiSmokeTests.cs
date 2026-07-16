using System.Net;
using FluentAssertions;
using VolleyHub.Api.IntegrationTests.Common;

namespace VolleyHub.Api.IntegrationTests.Health
{
    public sealed class ApiSmokeTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ApiSmokeTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetGameParticipants_ShouldReturnOk_WhenGameDoesNotExist()
        {
            var response = await _client.GetAsync(
                $"/api/games/{Guid.NewGuid()}/participants");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}