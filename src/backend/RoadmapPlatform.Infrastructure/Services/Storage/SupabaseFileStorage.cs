using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.Storage;
using RoadmapPlatform.Application.Interfaces.Storage;
using RoadmapPlatform.Infrastructure.Configurations;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RoadmapPlatform.Infrastructure.Services.Storage;

public sealed class SupabaseFileStorage : IFileStorage
{
    private readonly HttpClient _httpClient;
    private readonly FileStorageSettings _settings;
    private readonly SupabaseFileStorageSettings _supabaseSettings;

    public SupabaseFileStorage(
        HttpClient httpClient,
        IOptions<FileStorageSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _supabaseSettings = _settings.Supabase;

        if (string.IsNullOrWhiteSpace(_supabaseSettings.Url))
        {
            throw new InvalidOperationException("FileStorage:Supabase:Url is required when FileStorage:Provider is Supabase.");
        }

        if (string.IsNullOrWhiteSpace(_supabaseSettings.ServiceRoleKey))
        {
            throw new InvalidOperationException("FileStorage:Supabase:ServiceRoleKey is required when FileStorage:Provider is Supabase.");
        }

        if (string.IsNullOrWhiteSpace(_supabaseSettings.Bucket))
        {
            throw new InvalidOperationException("FileStorage:Supabase:Bucket is required when FileStorage:Provider is Supabase.");
        }
    }

    public string ProviderName => "Supabase";

    public async Task<StoredFileDto> SaveAsync(
        string objectPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizeObjectPath(objectPath);
        var bufferedContent = await BufferContentAsync(content, cancellationToken);
        var contentHash = await CalculateSha256Async(bufferedContent, cancellationToken);

        using var request = CreateRequest(
            HttpMethod.Post,
            $"object/{Uri.EscapeDataString(_supabaseSettings.Bucket)}/{EncodeObjectPath(normalizedPath)}");

        request.Headers.TryAddWithoutValidation("x-upsert", "true");

        bufferedContent.Position = 0;
        request.Content = new StreamContent(bufferedContent);
        request.Content.Headers.ContentType = CreateContentTypeHeader(contentType);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "upload file", cancellationToken);

        return new StoredFileDto(
            normalizedPath,
            CreatePublicUrl(normalizedPath),
            bufferedContent.Length,
            contentHash);
    }

    public async Task<Stream> OpenReadAsync(
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizeObjectPath(objectPath);

        using var request = CreateRequest(
            HttpMethod.Get,
            $"object/{Uri.EscapeDataString(_supabaseSettings.Bucket)}/{EncodeObjectPath(normalizedPath)}");

        var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await EnsureSuccessAsync(response, "read file", cancellationToken);

        var memoryStream = new MemoryStream();

        await response.Content.CopyToAsync(memoryStream, cancellationToken);
        response.Dispose();

        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizeObjectPath(objectPath);

        using var request = CreateRequest(
            HttpMethod.Delete,
            $"object/{Uri.EscapeDataString(_supabaseSettings.Bucket)}");

        request.Content = new StringContent(
            JsonSerializer.Serialize(new { prefixes = new[] { normalizedPath } }),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        await EnsureSuccessAsync(response, "delete file", cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var baseUrl = _supabaseSettings.Url.TrimEnd('/');
        var request = new HttpRequestMessage(method, $"{baseUrl}/storage/v1/{relativePath}");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseSettings.ServiceRoleKey);
        request.Headers.TryAddWithoutValidation("apikey", _supabaseSettings.ServiceRoleKey);

        return request;
    }

    private string? CreatePublicUrl(string normalizedPath)
    {
        if (!_supabaseSettings.UsePublicUrls)
        {
            return null;
        }

        var baseUrl = _supabaseSettings.Url.TrimEnd('/');

        return $"{baseUrl}/storage/v1/object/public/{Uri.EscapeDataString(_supabaseSettings.Bucket)}/{EncodeObjectPath(normalizedPath)}";
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string action,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new InvalidOperationException(
            $"Supabase Storage failed to {action}: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
    }

    private static async Task<MemoryStream> BufferContentAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var memoryStream = new MemoryStream();

        await content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return memoryStream;
    }

    private static async Task<string> CalculateSha256Async(
        Stream content,
        CancellationToken cancellationToken)
    {
        content.Position = 0;

        var hashBytes = await SHA256.HashDataAsync(content, cancellationToken);

        content.Position = 0;

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static MediaTypeHeaderValue CreateContentTypeHeader(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return new MediaTypeHeaderValue("application/octet-stream");
        }

        if (MediaTypeHeaderValue.TryParse(contentType, out var parsedContentType))
        {
            return parsedContentType;
        }

        return new MediaTypeHeaderValue("application/octet-stream");
    }

    private static string NormalizeObjectPath(string objectPath)
    {
        if (string.IsNullOrWhiteSpace(objectPath))
        {
            throw new ArgumentException("Object path is required.", nameof(objectPath));
        }

        var normalized = objectPath
            .Replace('\\', '/')
            .Trim()
            .TrimStart('/');

        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Object path cannot contain parent directory traversal.");
        }

        return normalized;
    }

    private static string EncodeObjectPath(string objectPath)
    {
        return string.Join(
            "/",
            objectPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
    }
}
