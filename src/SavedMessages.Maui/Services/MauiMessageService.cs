using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using SavedMessages.Components.Services;
using SavedMessages.Domain.Entities;

namespace SavedMessages.Maui.Services;

public class MauiMessageService : IMessageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenStorageService _tokenStorage;
    private readonly IConnectivity _connectivity;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly string _stateFilePath;
    private readonly string _downloadsDirectory;
    private readonly string _previewsDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public MauiMessageService(IHttpClientFactory httpClientFactory, ITokenStorageService tokenStorage, IConnectivity connectivity)
    {
        _httpClientFactory = httpClientFactory;
        _tokenStorage = tokenStorage;
        _connectivity = connectivity;

        var offlineRoot = Path.Combine(FileSystem.Current.AppDataDirectory, "offline");
        _stateFilePath = Path.Combine(offlineRoot, "messages-state.json");
        _downloadsDirectory = Path.Combine(offlineRoot, "downloads");
        _previewsDirectory = Path.Combine(offlineRoot, "previews");

        Directory.CreateDirectory(offlineRoot);
        Directory.CreateDirectory(_downloadsDirectory);
        Directory.CreateDirectory(_previewsDirectory);

        _connectivity.ConnectivityChanged += HandleConnectivityChanged;
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("ApiClient");
    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    private void HandleConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
            _ = Task.Run(SyncPendingInBackgroundAsync);
    }

    private record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

    private record MessageResponse(
        Guid Id, MessageKind Kind, string? Content, Guid? FileId, string? FileName, string? FilePreviewUrl,
        bool IsPinned, DateTime? PinnedAt, bool IsEncrypted, DateTime CreatedAt, DateTime UpdatedAt);

    private record FileUploadResponse(Guid Id, string OriginalName, string ContentType, long SizeBytes, DateTime CreatedAt);

    private enum PendingOperationType
    {
        CreateMessage,
        UploadFile,
        EditMessage,
        DeleteMessage,
        TogglePin
    }

    private sealed class OfflineState
    {
        public List<CachedMessage> Messages { get; set; } = [];
        public List<PendingOperation> PendingOperations { get; set; } = [];
        public List<DownloadedFileEntry> DownloadedFiles { get; set; } = [];
        public List<CachedPreviewEntry> CachedPreviews { get; set; } = [];
    }

    private sealed class CachedMessage
    {
        public Guid Id { get; set; }
        public MessageKind Kind { get; set; }
        public string? Content { get; set; }
        public Guid? FileId { get; set; }
        public string? FileName { get; set; }
        public string? FilePreviewUrl { get; set; }
        public bool Downloaded { get; set; }
        public bool IsPinned { get; set; }
        public DateTime? PinnedAt { get; set; }
        public bool IsEncrypted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class PendingOperation
    {
        public PendingOperationType Type { get; set; }
        public Guid MessageId { get; set; }
        public MessageKind? Kind { get; set; }
        public string? Content { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public byte[]? FileData { get; set; }
        public string? Comment { get; set; }
        public Guid? LocalFileId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class DownloadedFileEntry
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public string LocalPath { get; set; } = string.Empty;
    }

    private sealed class CachedPreviewEntry
    {
        public Guid FileId { get; set; }
        public string ContentType { get; set; } = "image/jpeg";
        public string LocalPath { get; set; } = string.Empty;
    }

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(int page = 1, int pageSize = 20)
    {
        await _stateLock.WaitAsync();
        try
        {
            var state = await LoadStateAsync();

            var result = state.Messages
                .OrderByDescending(m => m.IsPinned)
                .ThenByDescending(m => m.PinnedAt)
                .ThenByDescending(m => m.CreatedAt)
                .Select(ToDomain)
                .ToList();

            ApplyCachedPreviewUrls(state, result);

            // Offline-first UX: return cached data immediately, then refresh in background.
            if (IsOnline)
                _ = Task.Run(() => RefreshMessagesInBackgroundAsync(page, pageSize));

            return result.AsReadOnly();
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private static string? AppendAccessToken(string? previewUrl, string? accessToken, Uri? apiBaseAddress)
    {
        if (string.IsNullOrWhiteSpace(previewUrl))
            return previewUrl;

        var resolvedPreviewUrl = previewUrl;
        if (apiBaseAddress is not null && Uri.TryCreate(previewUrl, UriKind.Relative, out _))
            resolvedPreviewUrl = new Uri(apiBaseAddress, previewUrl).ToString();

        if (string.IsNullOrWhiteSpace(accessToken))
            return resolvedPreviewUrl;

        var separator = resolvedPreviewUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{resolvedPreviewUrl}{separator}access_token={Uri.EscapeDataString(accessToken)}";
    }

    public async Task<Message> CreateMessageAsync(MessageKind kind, string content)
    {
        await _stateLock.WaitAsync();
        try
        {
            var state = await LoadStateAsync();

            var offlineMessage = new Message
            {
                Id = Guid.NewGuid(),
                Kind = kind,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsPinned = false
            };

            UpsertMessage(state, offlineMessage);
            state.PendingOperations.Add(new PendingOperation
            {
                Type = PendingOperationType.CreateMessage,
                MessageId = offlineMessage.Id,
                Kind = kind,
                Content = content,
                CreatedAt = DateTime.UtcNow
            });

            await SaveStateAsync(state);

            if (IsOnline)
                _ = Task.Run(SyncPendingInBackgroundAsync);

            return offlineMessage;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task<(byte[] Data, string ContentType, string FileName)> DownloadFileAsync(Guid fileId)
    {
        await _stateLock.WaitAsync();
        try
        {
            var state = await LoadStateAsync();

            if (IsOnline)
            {
                try
                {
                    var client = CreateClient();
                    var response = await client.GetAsync($"api/files/{fileId}");
                    response.EnsureSuccessStatusCode();
                    var data = await response.Content.ReadAsByteArrayAsync();
                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                    var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                        ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                        ?? state.Messages.FirstOrDefault(m => m.FileId == fileId)?.FileName
                        ?? "download";

                    await SaveDownloadedFileAsync(state, fileId, fileName, contentType, data);
                    if (IsImageContentType(contentType))
                        await SavePreviewImageAsync(state, fileId, contentType, data);

                    await SaveStateAsync(state);
                    return (data, contentType, fileName);
                }
                catch
                {
                    // Fall back to local downloaded file.
                }
            }

            if (TryReadDownloadedFile(state, fileId, out var cached))
                return cached;

            throw new InvalidOperationException("File is not downloaded yet. Connect to the internet and download it once.");
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        await _stateLock.WaitAsync();
        try
        {
            var state = await LoadStateAsync();

            state.PendingOperations.Add(new PendingOperation
            {
                Type = PendingOperationType.DeleteMessage,
                MessageId = messageId,
                CreatedAt = DateTime.UtcNow
            });

            state.Messages.RemoveAll(m => m.Id == messageId);
            await SaveStateAsync(state);

            if (IsOnline)
                _ = Task.Run(SyncPendingInBackgroundAsync);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task EditMessageAsync(Guid messageId, string? content)
    {
        await _stateLock.WaitAsync();
        try
        {
            var state = await LoadStateAsync();
            var cached = state.Messages.FirstOrDefault(m => m.Id == messageId);
            if (cached is not null)
            {
                cached.Content = content;
                cached.UpdatedAt = DateTime.UtcNow;
            }

            state.PendingOperations.Add(new PendingOperation
            {
                Type = PendingOperationType.EditMessage,
                MessageId = messageId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            });

            await SaveStateAsync(state);

            if (IsOnline)
                _ = Task.Run(SyncPendingInBackgroundAsync);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task<Message> UploadFileAsync(string fileName, string contentType, Stream fileStream, string? comment = null)
    {
        await _stateLock.WaitAsync();
        try
        {
            var state = await LoadStateAsync();
            var contentValue = string.IsNullOrWhiteSpace(comment) ? fileName : comment;

            using var buffered = new MemoryStream();
            await fileStream.CopyToAsync(buffered);
            var fileData = buffered.ToArray();

            if (IsOnline)
            {
                try
                {
                    await TrySyncPendingOperationsAsync(state);
                    await using var onlineStream = new MemoryStream(fileData, writable: false);
                    var created = await UploadFileOnlineAsync(fileName, contentType, onlineStream, comment);
                    created.FileName = fileName;
                    created.Downloaded = true;

                    UpsertMessage(state, created);

                    if (created.FileId.HasValue)
                    {
                        await SaveDownloadedFileAsync(state, created.FileId.Value, fileName, contentType, fileData);
                        if (IsImageContentType(contentType))
                            await SavePreviewImageAsync(state, created.FileId.Value, contentType, fileData);
                    }

                    await SaveStateAsync(state);
                    return created;
                }
                catch
                {
                    // Fall back to offline queue below.
                }
            }

            var localFileId = Guid.NewGuid();
            var localMessage = new Message
            {
                Id = Guid.NewGuid(),
                Kind = MessageKind.File,
                Content = contentValue,
                FileId = localFileId,
                FileName = fileName,
                Downloaded = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            UpsertMessage(state, localMessage);
            await SaveDownloadedFileAsync(state, localFileId, fileName, contentType, fileData);
            if (IsImageContentType(contentType))
                await SavePreviewImageAsync(state, localFileId, contentType, fileData);

            state.PendingOperations.Add(new PendingOperation
            {
                Type = PendingOperationType.UploadFile,
                MessageId = localMessage.Id,
                Content = contentValue,
                FileName = fileName,
                ContentType = contentType,
                FileData = fileData,
                Comment = comment,
                LocalFileId = localFileId,
                CreatedAt = DateTime.UtcNow
            });

            await SaveStateAsync(state);
            return localMessage;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task TogglePinAsync(Guid messageId)
    {
        await _stateLock.WaitAsync();
        try
        {
            var state = await LoadStateAsync();
            var cached = state.Messages.FirstOrDefault(m => m.Id == messageId);
            if (cached is not null)
            {
                cached.IsPinned = !cached.IsPinned;
                cached.PinnedAt = cached.IsPinned ? DateTime.UtcNow : null;
                cached.UpdatedAt = DateTime.UtcNow;
            }

            state.PendingOperations.Add(new PendingOperation
            {
                Type = PendingOperationType.TogglePin,
                MessageId = messageId,
                CreatedAt = DateTime.UtcNow
            });

            await SaveStateAsync(state);

            if (IsOnline)
                _ = Task.Run(SyncPendingInBackgroundAsync);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task<IReadOnlyList<Message>> GetRemoteMessagesAsync(int page, int pageSize)
    {
        var client = CreateClient();
        var paged = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>($"api/messages?page={page}&pageSize={pageSize}");
        if (paged is null)
            return [];

        var accessToken = await _tokenStorage.GetAccessTokenAsync();
        var apiBaseAddress = client.BaseAddress;

        return paged.Items.Select(r => new Message
        {
            Id = r.Id,
            Kind = r.Kind,
            Content = r.Content,
            FileId = r.FileId,
            FileName = r.FileName,
            FilePreviewUrl = AppendAccessToken(r.FilePreviewUrl, accessToken, apiBaseAddress),
            IsPinned = r.IsPinned,
            PinnedAt = r.PinnedAt,
            IsEncrypted = r.IsEncrypted,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList().AsReadOnly();
    }

    private async Task<Message> UploadFileOnlineAsync(string fileName, string contentType, Stream fileStream, string? comment)
    {
        var client = CreateClient();

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var uploadResponse = await client.PostAsync("api/files", content);
        uploadResponse.EnsureSuccessStatusCode();
        var fileResult = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponse>();

        var messageContent = string.IsNullOrWhiteSpace(comment) ? fileName : comment;
        var msgResponse = await client.PostAsJsonAsync("api/messages", new { kind = MessageKind.File, content = messageContent, fileId = fileResult!.Id });
        msgResponse.EnsureSuccessStatusCode();
        var created = (await msgResponse.Content.ReadFromJsonAsync<Message>())!;
        created.FileId = fileResult.Id;
        return created;
    }

    private async Task TrySyncPendingOperationsAsync(OfflineState state)
    {
        if (!IsOnline || state.PendingOperations.Count == 0)
            return;

        var client = CreateClient();
        var pending = state.PendingOperations.OrderBy(op => op.CreatedAt).ToList();
        var remaining = new List<PendingOperation>();
        var idMap = new Dictionary<Guid, Guid>();

        for (var i = 0; i < pending.Count; i++)
        {
            var operation = pending[i];
            try
            {
                switch (operation.Type)
                {
                    case PendingOperationType.CreateMessage:
                    {
                        var response = await client.PostAsJsonAsync("api/messages", new
                        {
                            kind = operation.Kind ?? MessageKind.Text,
                            content = operation.Content
                        });
                        response.EnsureSuccessStatusCode();
                        var created = (await response.Content.ReadFromJsonAsync<Message>())!;
                        idMap[operation.MessageId] = created.Id;
                        ReplaceLocalMessage(state, operation.MessageId, created);
                        break;
                    }
                    case PendingOperationType.UploadFile:
                    {
                        var fileData = operation.FileData ?? [];
                        using var uploadContent = new MultipartFormDataContent();
                        using var bytesContent = new ByteArrayContent(fileData);
                        bytesContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(operation.ContentType ?? "application/octet-stream");
                        uploadContent.Add(bytesContent, "file", operation.FileName ?? "upload.bin");

                        var uploadResponse = await client.PostAsync("api/files", uploadContent);
                        uploadResponse.EnsureSuccessStatusCode();
                        var fileResult = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponse>();

                        var msgResponse = await client.PostAsJsonAsync("api/messages", new
                        {
                            kind = MessageKind.File,
                            content = operation.Content,
                            fileId = fileResult!.Id
                        });
                        msgResponse.EnsureSuccessStatusCode();
                        var created = (await msgResponse.Content.ReadFromJsonAsync<Message>())!;
                        created.FileId = fileResult.Id;
                        created.FileName = operation.FileName;
                        created.Downloaded = true;

                        idMap[operation.MessageId] = created.Id;
                        ReplaceLocalMessage(state, operation.MessageId, created);

                        if (operation.LocalFileId.HasValue)
                        {
                            RemapDownloadedFile(state, operation.LocalFileId.Value, fileResult.Id);
                            RemapCachedPreview(state, operation.LocalFileId.Value, fileResult.Id);
                        }

                        if (IsImageContentType(operation.ContentType) && fileData.Length > 0)
                            await SavePreviewImageAsync(state, fileResult.Id, operation.ContentType!, fileData);

                        break;
                    }
                    case PendingOperationType.EditMessage:
                    {
                        var serverMessageId = ResolveMessageId(operation.MessageId, idMap);
                        var response = await client.PutAsJsonAsync($"api/messages/{serverMessageId}", new { content = operation.Content });
                        response.EnsureSuccessStatusCode();

                        var message = state.Messages.FirstOrDefault(m => m.Id == operation.MessageId || m.Id == serverMessageId);
                        if (message is not null)
                        {
                            message.Content = operation.Content;
                            message.UpdatedAt = DateTime.UtcNow;
                        }
                        break;
                    }
                    case PendingOperationType.DeleteMessage:
                    {
                        var serverMessageId = ResolveMessageId(operation.MessageId, idMap);
                        var response = await client.DeleteAsync($"api/messages/{serverMessageId}");
                        response.EnsureSuccessStatusCode();
                        state.Messages.RemoveAll(m => m.Id == operation.MessageId || m.Id == serverMessageId);
                        break;
                    }
                    case PendingOperationType.TogglePin:
                    {
                        var serverMessageId = ResolveMessageId(operation.MessageId, idMap);
                        var response = await client.PatchAsync($"api/messages/{serverMessageId}/pin", null);
                        response.EnsureSuccessStatusCode();

                        var message = state.Messages.FirstOrDefault(m => m.Id == operation.MessageId || m.Id == serverMessageId);
                        if (message is not null)
                        {
                            message.IsPinned = !message.IsPinned;
                            message.PinnedAt = message.IsPinned ? DateTime.UtcNow : null;
                            message.UpdatedAt = DateTime.UtcNow;
                        }
                        break;
                    }
                }
            }
            catch
            {
                remaining.AddRange(pending.Skip(i));
                break;
            }
        }

        state.PendingOperations = remaining;
        await SaveStateAsync(state);
    }

    private async Task SyncPendingInBackgroundAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            var state = await LoadStateAsync();
            await TrySyncPendingOperationsAsync(state);
        }
        catch
        {
            // Keep failures silent; next operation/load retries sync.
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task RefreshMessagesInBackgroundAsync(int page, int pageSize)
    {
        await _stateLock.WaitAsync();
        try
        {
            if (!IsOnline)
                return;

            var state = await LoadStateAsync();
            await TrySyncPendingOperationsAsync(state);
            if (state.PendingOperations.Count > 0)
                return;

            try
            {
                var downloadedFileIds = state.Messages
                    .Where(m => m.Downloaded && m.FileId.HasValue)
                    .Select(m => m.FileId!.Value)
                    .ToHashSet();

                var remoteMessages = (await GetRemoteMessagesAsync(page, pageSize)).ToList();
                foreach (var message in remoteMessages)
                    await CachePreviewForMessageAsync(state, message);

                state.Messages = remoteMessages
                    .Select(message =>
                    {
                        message.Downloaded = message.FileId.HasValue && downloadedFileIds.Contains(message.FileId.Value);
                        return ToCached(message);
                    })
                    .ToList();

                await SaveStateAsync(state);
            }
            catch
            {
                // Keep existing cache when refresh fails.
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private static Guid ResolveMessageId(Guid messageId, IReadOnlyDictionary<Guid, Guid> idMap) =>
        idMap.TryGetValue(messageId, out var mapped) ? mapped : messageId;

    private static void ReplaceLocalMessage(OfflineState state, Guid localMessageId, Message serverMessage)
    {
        var existing = state.Messages.FirstOrDefault(m => m.Id == localMessageId);
        var downloaded = existing?.Downloaded ?? false;

        state.Messages.RemoveAll(m => m.Id == localMessageId || m.Id == serverMessage.Id);
        var cached = ToCached(serverMessage);
        cached.Downloaded = downloaded || cached.Downloaded;
        state.Messages.Add(cached);
    }

    private static void RemapDownloadedFile(OfflineState state, Guid previousFileId, Guid newFileId)
    {
        var entry = state.DownloadedFiles.FirstOrDefault(f => f.FileId == previousFileId);
        if (entry is null)
            return;

        entry.FileId = newFileId;
        foreach (var message in state.Messages.Where(m => m.FileId == previousFileId))
            message.FileId = newFileId;
    }

    private static void RemapCachedPreview(OfflineState state, Guid previousFileId, Guid newFileId)
    {
        var entry = state.CachedPreviews.FirstOrDefault(f => f.FileId == previousFileId);
        if (entry is null)
            return;

        entry.FileId = newFileId;
    }

    private async Task CachePreviewForMessageAsync(OfflineState state, Message message)
    {
        if (!message.FileId.HasValue || string.IsNullOrWhiteSpace(message.FilePreviewUrl))
            return;

        if (TryGetCachedPreview(state, message.FileId.Value, out _))
            return;

        try
        {
            var client = CreateClient();
            var response = await client.GetAsync(message.FilePreviewUrl);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            await SavePreviewImageAsync(state, message.FileId.Value, contentType, data);
        }
        catch
        {
            // Preview caching is best-effort.
        }
    }

    private static void ApplyCachedPreviewUrls(OfflineState state, List<Message> messages)
    {
        foreach (var message in messages)
        {
            if (!message.FileId.HasValue)
                continue;

            if (!TryGetCachedPreview(state, message.FileId.Value, out var previewEntry))
                continue;

            var previewDataUrl = TryBuildPreviewDataUrl(previewEntry);
            if (!string.IsNullOrWhiteSpace(previewDataUrl))
                message.FilePreviewUrl = previewDataUrl;
        }
    }

    private static bool TryGetCachedPreview(OfflineState state, Guid fileId, out CachedPreviewEntry entry)
    {
        var cached = state.CachedPreviews.FirstOrDefault(p => p.FileId == fileId);
        if (cached is null || string.IsNullOrWhiteSpace(cached.LocalPath) || !File.Exists(cached.LocalPath))
        {
            if (cached is not null)
                state.CachedPreviews.Remove(cached);

            entry = default!;
            return false;
        }

        entry = cached;
        return true;
    }

    private static string? TryBuildPreviewDataUrl(CachedPreviewEntry preview)
    {
        try
        {
            var data = File.ReadAllBytes(preview.LocalPath);
            if (data.Length == 0)
                return null;

            var contentType = string.IsNullOrWhiteSpace(preview.ContentType) ? "image/jpeg" : preview.ContentType;
            return $"data:{contentType};base64,{Convert.ToBase64String(data)}";
        }
        catch
        {
            return null;
        }
    }

    private async Task SavePreviewImageAsync(OfflineState state, Guid fileId, string contentType, byte[] data)
    {
        if (data.Length == 0)
            return;

        var extension = contentType switch
        {
            "image/png" => "png",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => "jpg"
        };

        var localPath = Path.Combine(_previewsDirectory, $"{fileId:N}.{extension}");
        await File.WriteAllBytesAsync(localPath, data);

        state.CachedPreviews.RemoveAll(p => p.FileId == fileId);
        state.CachedPreviews.Add(new CachedPreviewEntry
        {
            FileId = fileId,
            ContentType = contentType,
            LocalPath = localPath
        });
    }

    private static bool IsImageContentType(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType)
        && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private static CachedMessage ToCached(Message message) => new()
    {
        Id = message.Id,
        Kind = message.Kind,
        Content = message.Content,
        FileId = message.FileId,
        FileName = message.FileName,
        FilePreviewUrl = message.FilePreviewUrl,
        Downloaded = message.Downloaded,
        IsPinned = message.IsPinned,
        PinnedAt = message.PinnedAt,
        IsEncrypted = message.IsEncrypted,
        CreatedAt = message.CreatedAt,
        UpdatedAt = message.UpdatedAt
    };

    private static Message ToDomain(CachedMessage message) => new()
    {
        Id = message.Id,
        Kind = message.Kind,
        Content = message.Content,
        FileId = message.FileId,
        FileName = message.FileName,
        FilePreviewUrl = message.FilePreviewUrl,
        Downloaded = message.Downloaded,
        IsPinned = message.IsPinned,
        PinnedAt = message.PinnedAt,
        IsEncrypted = message.IsEncrypted,
        CreatedAt = message.CreatedAt,
        UpdatedAt = message.UpdatedAt
    };

    private static void UpsertMessage(OfflineState state, Message message)
    {
        state.Messages.RemoveAll(m => m.Id == message.Id);
        state.Messages.Add(ToCached(message));
    }

    private async Task<OfflineState> LoadStateAsync()
    {
        if (!File.Exists(_stateFilePath))
            return new OfflineState();

        try
        {
            await using var stream = File.OpenRead(_stateFilePath);
            var state = await JsonSerializer.DeserializeAsync<OfflineState>(stream, JsonOptions);
            return state ?? new OfflineState();
        }
        catch
        {
            return new OfflineState();
        }
    }

    private async Task SaveStateAsync(OfflineState state)
    {
        await using var stream = File.Create(_stateFilePath);
        await JsonSerializer.SerializeAsync(stream, state, JsonOptions);
    }

    private async Task SaveDownloadedFileAsync(OfflineState state, Guid fileId, string fileName, string contentType, byte[] data)
    {
        var localPath = Path.Combine(_downloadsDirectory, $"{fileId:N}.bin");
        await File.WriteAllBytesAsync(localPath, data);

        state.DownloadedFiles.RemoveAll(f => f.FileId == fileId);
        state.DownloadedFiles.Add(new DownloadedFileEntry
        {
            FileId = fileId,
            FileName = fileName,
            ContentType = contentType,
            LocalPath = localPath
        });

        foreach (var message in state.Messages.Where(m => m.FileId == fileId))
            message.Downloaded = true;
    }

    private static bool TryReadDownloadedFile(OfflineState state, Guid fileId, out (byte[] Data, string ContentType, string FileName) file)
    {
        var entry = state.DownloadedFiles.FirstOrDefault(f => f.FileId == fileId);
        if (entry is null || string.IsNullOrWhiteSpace(entry.LocalPath) || !File.Exists(entry.LocalPath))
        {
            file = default;
            return false;
        }

        var data = File.ReadAllBytes(entry.LocalPath);
        file = (data, entry.ContentType, entry.FileName);
        return true;
    }
}
