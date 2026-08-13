using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Briefcase.Components.Services;
using Briefcase.Domain.Entities;

namespace Briefcase.Maui.Services;

/// <summary>
/// .NET MAUI implementation of <see cref="IMessageStreamService"/>. Maintains a single
/// authenticated SignalR connection to <c>/hubs/messages</c> and re-raises the server's
/// message events so open pages update live when another device changes a message.
/// </summary>
public class MauiMessageStreamService(IHttpClientFactory httpClientFactory, ITokenStorageService tokenStorage)
    : IMessageStreamService, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim _startLock = new(1, 1);
    private HubConnection? _connection;

    public event Func<Message, Task>? MessageCreated;
    public event Func<Message, Task>? MessageUpdated;
    public event Func<Guid, Task>? MessageRemoved;
    public event Func<Task>? SessionRevoked;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is not null)
        {
            if (_connection.State == HubConnectionState.Disconnected)
                await _connection.StartAsync(cancellationToken);
            return;
        }

        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null)
                return;

            var client = httpClientFactory.CreateClient("ApiClient");
            var apiBaseAddress = client.BaseAddress
                ?? throw new InvalidOperationException("ApiClient BaseAddress is not configured.");
            var hubUrl = new Uri(apiBaseAddress, "/hubs/messages").ToString();

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = async () => await tokenStorage.GetAccessTokenAsync();
                })
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers(connection, apiBaseAddress);

            await connection.StartAsync(cancellationToken);
            _connection = connection;
        }
        finally
        {
            _startLock.Release();
        }
    }

    private void RegisterHandlers(HubConnection connection, Uri apiBaseAddress)
    {
        connection.On<JsonElement>("MessageCreated", payload => RaiseUpsert(payload, apiBaseAddress, MessageCreated));
        connection.On<JsonElement>("MessageRestored", payload => RaiseUpsert(payload, apiBaseAddress, MessageCreated));
        connection.On<JsonElement>("MessageUpdated", payload => RaiseUpsert(payload, apiBaseAddress, MessageUpdated));
        connection.On<JsonElement>("MessageTrashed", RaiseRemoved);
        connection.On<JsonElement>("MessageDeleted", RaiseRemoved);
        connection.On<JsonElement>("SessionRevoked", _ => SessionRevoked?.Invoke() ?? Task.CompletedTask);
    }

    private async Task RaiseUpsert(JsonElement payload, Uri apiBaseAddress, Func<Message, Task>? handler)
    {
        if (handler is null)
            return;

        var dto = payload.Deserialize<MessageDto>(JsonOptions);
        if (dto is null)
            return;

        var accessToken = await tokenStorage.GetAccessTokenAsync();
        await handler.Invoke(dto.ToMessage(apiBaseAddress, accessToken));
    }

    private Task RaiseRemoved(JsonElement payload)
    {
        if (MessageRemoved is null)
            return Task.CompletedTask;

        if ((payload.TryGetProperty("id", out var idProp) || payload.TryGetProperty("Id", out idProp))
            && idProp.TryGetGuid(out var id))
        {
            return MessageRemoved.Invoke(id);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_connection is null)
            return;

        await _connection.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _startLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private record MessageDto(
        Guid Id, MessageKind Kind, string? Content, Guid? FileId, string? FileName, string? FilePreviewUrl,
        bool IsPinned, DateTime? PinnedAt, bool IsEncrypted, string? EncryptionIV, DateTime CreatedAt, DateTime UpdatedAt)
    {
        public Message ToMessage(Uri apiBaseAddress, string? accessToken) => new()
        {
            Id = Id,
            Kind = Kind,
            Content = Content,
            FileId = FileId,
            FileName = FileName,
            FilePreviewUrl = AppendAccessToken(FilePreviewUrl, accessToken, apiBaseAddress),
            Downloaded = false,
            IsPinned = IsPinned,
            PinnedAt = PinnedAt,
            IsEncrypted = IsEncrypted,
            EncryptionIV = EncryptionIV,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };

        private static string? AppendAccessToken(string? previewUrl, string? accessToken, Uri apiBaseAddress)
        {
            if (string.IsNullOrWhiteSpace(previewUrl))
                return previewUrl;

            var resolvedPreviewUrl = previewUrl;
            if (Uri.TryCreate(previewUrl, UriKind.Relative, out _))
                resolvedPreviewUrl = new Uri(apiBaseAddress, previewUrl).ToString();

            if (string.IsNullOrWhiteSpace(accessToken))
                return resolvedPreviewUrl;

            var separator = resolvedPreviewUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            return $"{resolvedPreviewUrl}{separator}access_token={Uri.EscapeDataString(accessToken)}";
        }
    }
}
