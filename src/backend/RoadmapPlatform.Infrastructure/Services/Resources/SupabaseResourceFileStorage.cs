using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Interfaces.Resources;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.Resources
{
    public sealed class SupabaseResourceFileStorage : IResourceFileStorage
    {
        private readonly HttpClient _httpClient;
        private readonly SupabaseStorageSettings _options;

        public SupabaseResourceFileStorage(HttpClient httpClient, IOptions<SupabaseStorageSettings> options)
        {
            _httpClient = httpClient;
            _options = options.Value;

            if (string.IsNullOrWhiteSpace(_options.Url))
            {
                throw new InvalidOperationException("SupabaseStorage:Url is required when FileStorage:Provider is Supabase.");
            }

            if (string.IsNullOrWhiteSpace(_options.ServiceRoleKey))
            {
                throw new InvalidOperationException("SupabaseStorage:ServiceRoleKey is required when FileStorage:Provider is Supabase.");
            }

            if (string.IsNullOrWhiteSpace(_options.Bucket))
            {
                throw new InvalidOperationException("SupabaseStorage:Bucket is required when FileStorage:Provider is Supabase.");
            }
        }

        public string ProviderName => "Supabase";

        public async Task<StoredResourceFile> SaveAsync(
            string objectPath,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var normalizedPath = NormalizeObjectPath(objectPath);
            using var request = CreateRequest(
                HttpMethod.Post,
                $"object/{Uri.EscapeDataString(_options.Bucket)}/{EncodeObjectPath(normalizedPath)}");

            request.Headers.TryAddWithoutValidation("x-upsert", "true");
            request.Content = new StreamContent(content);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, "upload resource file");

            return new StoredResourceFile(normalizedPath, normalizedPath);
        }

        public async Task<Stream> OpenReadAsync(string objectPath, CancellationToken cancellationToken = default)
        {
            var normalizedPath = NormalizeObjectPath(objectPath);
            using var request = CreateRequest(
                HttpMethod.Get,
                $"object/{Uri.EscapeDataString(_options.Bucket)}/{EncodeObjectPath(normalizedPath)}");

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await EnsureSuccessAsync(response, "read resource file");

            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            response.Dispose();

            return memoryStream;
        }

        public async Task DeleteAsync(string objectPath, CancellationToken cancellationToken = default)
        {
            var normalizedPath = NormalizeObjectPath(objectPath);
            using var request = CreateRequest(HttpMethod.Delete, $"object/{Uri.EscapeDataString(_options.Bucket)}");

            request.Content = new StringContent(
                JsonSerializer.Serialize(new { prefixes = new[] { normalizedPath } }),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, "delete resource file");
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
        {
            var baseUrl = _options.Url.TrimEnd('/');
            var request = new HttpRequestMessage(method, $"{baseUrl}/storage/v1/{relativePath}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
            request.Headers.TryAddWithoutValidation("apikey", _options.ServiceRoleKey);

            return request;
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string action)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Supabase Storage failed to {action}: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        private static string NormalizeObjectPath(string objectPath)
        {
            return objectPath.Replace("\\", "/").TrimStart('/');
        }

        private static string EncodeObjectPath(string objectPath)
        {
            return string.Join("/", objectPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
        }
    }
}
