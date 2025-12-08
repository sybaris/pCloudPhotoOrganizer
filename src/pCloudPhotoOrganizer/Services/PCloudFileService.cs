/*
WEB-DAV LIMITATIONS OF PCLOUD (MANDATORY FOR ALL CODE):
- PROPFIND is NOT supported
- MKCOL is NOT supported
- COPY is NOT supported
- MOVE is NOT supported
- PROPPATCH is NOT supported
- Directory existence CANNOT be checked via WebDAV
- Folder listing CANNOT be done via WebDAV
- The ONLY supported WebDAV verbs are:
  OPTIONS, GET, HEAD, POST, PUT, DELETE, TRACE, PATCH
- Upload MUST be done using PUT or POST only
- Directory creation MUST be done via pCloud public API (absolute path required, starting with "/")
- Folder existence MUST be checked using pCloud API (listfolder -> folderid; createfolder(folderid,name) for nesting)
- The final file upload should use direct WebDAV PUT:
     https://ewebdav.pcloud.com/<remote-path>/<filename>
------------------------------------------------------------
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using pCloudPhotoOrganizer.Models;
#if ANDROID
using AndroidApp = Android.App.Application;
using AndroidUri = Android.Net.Uri;
using pCloudPhotoOrganizer.Platforms.Android;
#endif

namespace pCloudPhotoOrganizer.Services;
public class PCloudFileService
{
    private readonly ILogger<PCloudFileService> _logger;
    private readonly HttpClient _webDavClient = new HttpClient { BaseAddress = new Uri("https://ewebdav.pcloud.com/") };
    private readonly HttpClient _apiClient = new HttpClient { BaseAddress = new Uri("https://eapi.pcloud.com/") };

    public PCloudFileService(ILogger<PCloudFileService> logger)
    {
        _logger = logger;
    }

    public async Task EnsureFolderExistsAsync(string username, string password, string remoteFolderPath, CancellationToken cancellationToken = default)
    {
        var segments = GetSafePathSegments(remoteFolderPath);
        var absolutePath = BuildAbsolutePath(segments);
        _logger.LogDebug("Ensuring pCloud absolute path: '{ResolvedPath}'", absolutePath);

        if (!segments.Any())
            return;

        var authToken = await GetAuthTokenAsync(username, password, cancellationToken);
        await EnsureFolderHierarchyAsync(authToken, segments, cancellationToken);
    }

    public async Task UploadAsync(string username, string password, string remoteFolderPath, MediaItem item, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var fileName = !string.IsNullOrWhiteSpace(item.FileName)
            ? item.FileName
            : Path.GetFileName(item.FilePath);

        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("Nom de fichier introuvable pour l'élément sélectionné.");

        var filePath = BuildFilePath(remoteFolderPath, fileName);

        using var request = new HttpRequestMessage(HttpMethod.Put, BuildUri(filePath));
        request.Headers.Authorization = BuildAuthHeader(username, password);

        var (stream, length) = await OpenReadStreamAsync(item, cancellationToken);
        await using var uploadStream = stream;

        var content = new ProgressableStreamContent(uploadStream, 8192, progress, cancellationToken, length)
        {
            Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") }
        };

        request.Content = content;

        var response = await _webDavClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            throw new PCloudAuthenticationException("Identifiants pCloud invalides pour le WebDAV.", response.StatusCode, body);

        if (!response.IsSuccessStatusCode)
            throw new PCloudUploadException($"Upload WebDAV échoué (HTTP {(int)response.StatusCode}).", response.StatusCode, body);
    }

    private async Task<(Stream Stream, long? Length)> OpenReadStreamAsync(MediaItem item, CancellationToken ct)
    {
#if ANDROID
        await MediaPermissionHelper.EnsureMediaPermissionAsync();

        if (item.ContentUri is null)
            throw new FileNotFoundException("URI de contenu manquante pour l'element Android.");

        var context = AndroidApp.Context;
        var androidUri = AndroidUri.Parse(item.ContentUri.ToString());

        _logger.LogDebug("Opening Android media stream. ContentUri={ContentUri}, LengthHint={Length}, FilePath={FilePath}", item.ContentUri, item.Length, item.FilePath);

        try
        {
            var stream = context.ContentResolver?.OpenInputStream(androidUri)
                ?? throw new FileNotFoundException($"Impossible d'ouvrir le media : {item.ContentUri}");

            long? length = item.Length;
            try
            {
                using var descriptor = context.ContentResolver?.OpenAssetFileDescriptor(androidUri, "r");
                if (descriptor is not null && descriptor.Length >= 0)
                    length = descriptor.Length;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de recuperer la taille du media {ContentUri}", item.ContentUri);
                // Ignore length resolution errors; upload will proceed with chunked transfer.
            }

            return (stream, length);
        }
        catch (Exception ex)
        {
            var permissionStatus = await MediaPermissionHelper.GetMediaPermissionStatusAsync();
            _logger.LogError(ex, "Ouverture du flux de media impossible pour {ContentUri} (persistable={Persistable}, runtimePermission={PermissionStatus}): {ErrorMessage}",
                item.ContentUri,
                item.HasPersistablePermission,
                permissionStatus,
                ex.Message);
            throw;
        }
#else
        if (string.IsNullOrWhiteSpace(item.FilePath))
            throw new FileNotFoundException("Chemin du fichier manquant pour l'upload.");

        var stream = File.OpenRead(item.FilePath);
        long? length = item.Length;

        if (stream.CanSeek)
            length ??= stream.Length;

        return (stream, length);
#endif
    }

    // pCloud constraint: folder creation cannot be done via WebDAV. It must use the public API:
    // - Paths must be absolute (start with "/")
    // - Nested creation needs folderid resolution: listfolder -> createfolder(folderid, name)
    private async Task EnsureFolderHierarchyAsync(string authToken, IReadOnlyList<string> segments, CancellationToken ct)
    {
        var currentPath = "/";
        var currentFolderId = await GetFolderIdByPathAsync(authToken, currentPath, ct)
            ?? throw new InvalidOperationException("Impossible de résoudre le dossier racine pCloud.");

        foreach (var segment in segments)
        {
            var nextPath = currentPath.EndsWith("/", StringComparison.Ordinal)
                ? currentPath + segment
                : $"{currentPath}/{segment}";

            var existingId = await GetFolderIdByPathAsync(authToken, nextPath, ct);
            if (existingId is not null)
            {
                currentFolderId = existingId.Value;
                currentPath = nextPath;
                continue;
            }

            currentFolderId = await CreateFolderAsync(authToken, currentFolderId, segment, nextPath, ct);
            currentPath = nextPath;
        }
    }

    private async Task<long?> GetFolderIdByPathAsync(string authToken, string absolutePath, CancellationToken ct)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(absolutePath) ? "/" : absolutePath;
        if (!normalizedPath.StartsWith("/", StringComparison.Ordinal))
            normalizedPath = "/" + normalizedPath.TrimStart('/');

        var url = $"listfolder?path={Uri.EscapeDataString(normalizedPath)}&auth={Uri.EscapeDataString(authToken)}&nofiles=1";
        _logger.LogDebug("pCloud listfolder URL: {Url}", new Uri(_apiClient.BaseAddress!, url));

        using var response = await _apiClient.GetAsync(url, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("pCloud listfolder response: {Response}", body);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Échec de la vérification du dossier ({(int)response.StatusCode}): {body}");

        try
        {
            using var doc = JsonDocument.Parse(body);
            var (result, error) = ParseResult(doc);

            if (result == 0)
            {
                if (doc.RootElement.TryGetProperty("metadata", out var metadata) &&
                    metadata.TryGetProperty("folderid", out var folderIdProp))
                {
                    return folderIdProp.GetInt64();
                }

                throw new InvalidOperationException("Réponse listfolder pCloud invalide : folderid manquant.");
            }

            if (result == 2004 || result == 2005)
                return null;

            if (result == 1000)
                throw new PCloudAuthenticationException($"Identifiants pCloud invalides pour l'API (code {result} : {error ?? "Erreur inconnue"}).");

            throw new InvalidOperationException($"Impossible de vérifier l'existence du dossier distant (code {result} : {error ?? "Erreur inconnue"}).");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Réponse JSON pCloud invalide lors de la vérification du dossier : {body}", ex);
        }
    }

    private async Task<long> CreateFolderAsync(string authToken, long parentFolderId, string folderName, string targetPath, CancellationToken ct)
    {
        var url = $"createfolder?folderid={parentFolderId}&name={Uri.EscapeDataString(folderName)}&auth={Uri.EscapeDataString(authToken)}";
        _logger.LogDebug("pCloud createfolder URL: {Url} (parentId {ParentId}, name '{FolderName}', targetPath '{TargetPath}')",
            new Uri(_apiClient.BaseAddress!, url), parentFolderId, folderName, targetPath);

        using var response = await _apiClient.GetAsync(url, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("pCloud createfolder response: {Response}", body);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Impossible de créer le dossier distant ({(int)response.StatusCode}): {body}");

        try
        {
            using var doc = JsonDocument.Parse(body);
            var (result, error) = ParseResult(doc);

            if (result == 0)
            {
                if (doc.RootElement.TryGetProperty("metadata", out var metadata) &&
                    metadata.TryGetProperty("folderid", out var folderIdProp))
                {
                    return folderIdProp.GetInt64();
                }

                throw new InvalidOperationException("Réponse createfolder pCloud invalide : folderid manquant.");
            }

            if (result == 2004 || result == 2005)
            {
                var fallbackId = await GetFolderIdByPathAsync(authToken, targetPath, ct);
                if (fallbackId is not null)
                    return fallbackId.Value;
            }

            if (result == 1000)
                throw new PCloudAuthenticationException($"Identifiants pCloud invalides pour l'API (code {result} : {error ?? "Erreur inconnue"}).", response.StatusCode, body);

            throw new InvalidOperationException($"Impossible de créer le dossier distant (code {result} : {error ?? "Erreur inconnue"}).");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Réponse JSON pCloud invalide lors de la création du dossier : {body}", ex);
        }
    }

    private async Task<string> GetAuthTokenAsync(string username, string password, CancellationToken ct)
    {
        var url = $"login?getauth=1&logout=1&username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";
        using var response = await _apiClient.GetAsync(url, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Authentification pCloud échouée ({(int)response.StatusCode}): {body}");

        try
        {
            using var doc = JsonDocument.Parse(body);
            var (result, error) = ParseResult(doc);
            if (result == 0)
            {
                var auth = doc.RootElement.GetProperty("auth").GetString();
                if (string.IsNullOrWhiteSpace(auth))
                    throw new InvalidOperationException("Réponse d'authentification pCloud invalide.");

                return auth;
            }

            throw new PCloudAuthenticationException($"Identifiants pCloud invalides pour l'API (code {result} : {error ?? "Erreur inconnue"}).", response.StatusCode, body);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Réponse JSON pCloud invalide lors de l'authentification : {body}", ex);
        }
    }

    private Uri BuildUri(string path)
    {
        var trimmed = path.TrimStart('/');
        return new Uri(_webDavClient.BaseAddress!, trimmed);
    }

    private static string BuildFilePath(string folderPath, string fileName)
    {
        var encodedSegments = GetSafePathSegments(folderPath)
            .Select(Uri.EscapeDataString)
            .ToList();
        var encodedFile = Uri.EscapeDataString(fileName);

        var combined = "/" + string.Join("/", encodedSegments);
        if (!combined.EndsWith("/", StringComparison.Ordinal))
            combined += "/";

        return combined + encodedFile;
    }

    private static string BuildAbsolutePath(IReadOnlyList<string> segments)
        => segments.Count == 0 ? "/" : "/" + string.Join("/", segments);

    private static AuthenticationHeaderValue BuildAuthHeader(string username, string password)
        => new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));

    private static readonly HashSet<char> InvalidSegmentChars = new(Path.GetInvalidFileNameChars());

    private static IReadOnlyList<string> GetSafePathSegments(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return Array.Empty<string>();

        var normalized = folderPath.Replace('\\', '/').Trim();
        normalized = normalized.Trim('/');

        var segments = normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SanitizeSegment)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        return segments;
    }

    private static string SanitizeSegment(string segment)
    {
        var cleaned = new string(segment.Where(c => !InvalidSegmentChars.Contains(c)).ToArray());
        return cleaned.Trim();
    }

    private static (int result, string? error) ParseResult(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("result", out var resultProperty))
            throw new InvalidOperationException("Réponse pCloud invalide : champ 'result' manquant.");

        var result = resultProperty.GetInt32();
        var error = doc.RootElement.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : null;
        return (result, error);
    }

    private class ProgressableStreamContent : HttpContent
    {
        private readonly Stream _source;
        private readonly int _bufferSize;
        private readonly IProgress<double>? _progress;
        private readonly CancellationToken _ct;
        private readonly long? _lengthHint;

        public ProgressableStreamContent(Stream source, int bufferSize, IProgress<double>? progress, CancellationToken ct, long? lengthHint)
        {
            _source = source;
            _bufferSize = bufferSize;
            _progress = progress;
            _ct = ct;
            _lengthHint = lengthHint;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => await SerializeToStreamAsync(stream, context, _ct);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            var buffer = new byte[_bufferSize];
            long totalRead = 0;
            var length = _lengthHint ?? TryGetLength(_source);

            int read;
            while ((read = await _source.ReadAsync(buffer.AsMemory(0, _bufferSize), cancellationToken)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalRead += read;
                _progress?.Report(length > 0 ? (double)totalRead / length : 0);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            if (_lengthHint.HasValue)
            {
                length = _lengthHint.Value;
                return true;
            }

            if (_source.CanSeek)
            {
                length = _source.Length;
                return true;
            }

            length = 0;
            return false;
        }

        private static long TryGetLength(Stream source)
        {
            if (!source.CanSeek)
                return 0;

            try
            {
                return source.Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}

public class PCloudAuthenticationException : Exception
{
    public HttpStatusCode? StatusCode { get; }
    public string? ResponseBody { get; }

    public PCloudAuthenticationException(string message, HttpStatusCode? statusCode = null, string? responseBody = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

public class PCloudUploadException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ResponseBody { get; }

    public PCloudUploadException(string message, HttpStatusCode statusCode, string? responseBody = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}


