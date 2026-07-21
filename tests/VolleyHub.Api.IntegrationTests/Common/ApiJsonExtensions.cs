using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VolleyHub.Api.IntegrationTests.Common
{
    internal static class ApiJsonExtensions
    {
        private static readonly JsonSerializerOptions SerializerOptions =
            CreateSerializerOptions();

        public static Task<HttpResponseMessage> PostAsApiJsonAsync<T>(
            this HttpClient client,
            string requestUri,
            T value,
            CancellationToken cancellationToken = default)
        {
            return client.PostAsJsonAsync(
                requestUri,
                value,
                SerializerOptions,
                cancellationToken);
        }

        public static Task<HttpResponseMessage> PutAsApiJsonAsync<T>(
            this HttpClient client,
            string requestUri,
            T value,
            CancellationToken cancellationToken = default)
        {
            return client.PutAsJsonAsync(
                requestUri,
                value,
                SerializerOptions,
                cancellationToken);
        }

        public static Task<T?> ReadFromApiJsonAsync<T>(
            this HttpContent content,
            CancellationToken cancellationToken = default)
        {
            return content.ReadFromJsonAsync<T>(
                SerializerOptions,
                cancellationToken);
        }

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            options.Converters.Add(
                new JsonStringEnumConverter(
                    allowIntegerValues: false));

            return options;
        }
    }
}