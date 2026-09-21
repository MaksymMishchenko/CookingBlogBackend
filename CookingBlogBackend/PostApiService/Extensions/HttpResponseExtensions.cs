using System.Text.Json;
using System.Text.Json.Serialization;

namespace PostApiService.Extensions
{
    public static class HttpResponseExtensions
    {
        private static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull            
        };

        public static Task WriteApiResponseAsync<T>(this HttpResponse response, T data, CancellationToken ct = default)
        {
            response.ContentType = "application/json";
            return response.WriteAsJsonAsync(data, DefaultJsonOptions, ct);
        }
    }
}
