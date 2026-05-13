using System.Net;
using System.Net.Http.Headers;

namespace SavedMessages.Components.Services;

public class AuthDelegatingHandler(ITokenStorageService tokenStorage) : DelegatingHandler
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await AttachTokenAsync(request);

        var response = await base.SendAsync(request, cancellationToken);

        // Don't attempt refresh for auth endpoints themselves to avoid loops
        if (response.StatusCode == HttpStatusCode.Unauthorized
            && request.RequestUri?.AbsolutePath.Contains("/api/auth/") != true)
        {
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                // Retry with a fresh token (another thread may have already refreshed)
                var freshToken = await tokenStorage.GetAccessTokenAsync();
                if (freshToken is not null && request.Headers.Authorization?.Parameter != freshToken)
                {
                    // Token was refreshed by another thread, just retry
                    var retryRequest = await CloneRequestAsync(request);
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);
                    response.Dispose();
                    return await base.SendAsync(retryRequest, cancellationToken);
                }

                // Attempt refresh — we need a separate HttpClient to avoid recursion.
                // The refresh is handled by AuthService which uses the base HttpClient.
                // For now, return the 401 and let the UI layer trigger refresh/logout.
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        return response;
    }

    private async Task AttachTokenAsync(HttpRequestMessage request)
    {
        var token = await tokenStorage.GetAccessTokenAsync();
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content is not null)
        {
            var content = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return clone;
    }
}
