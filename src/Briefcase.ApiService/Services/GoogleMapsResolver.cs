using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Briefcase.ApiService.Services;

public enum MapResolutionOutcome
{
    NotApplicable,
    Success,
    Failed
}

public record MapResolutionResult(MapResolutionOutcome Outcome, double? Latitude = null, double? Longitude = null, string? Error = null)
{
    public static MapResolutionResult NotApplicable() => new(MapResolutionOutcome.NotApplicable);
    public static MapResolutionResult Success(double latitude, double longitude) => new(MapResolutionOutcome.Success, latitude, longitude);
    public static MapResolutionResult Failed(string error) => new(MapResolutionOutcome.Failed, Error: error);
}

public interface IGoogleMapsResolver
{
    bool IsSupportedUrl(string? value);
    Task<MapResolutionResult> ResolveAsync(string value, CancellationToken cancellationToken = default);
}

public partial class GoogleMapsResolver(HttpClient httpClient) : IGoogleMapsResolver
{
    private const int MaxRedirects = 5;
    private const int MaxResponseBytes = 512 * 1024;

    [GeneratedRegex(@"!3d(?<lat>-?\d{1,2}(?:\.\d+)?).*?!4d(?<lon>-?\d{1,3}(?:\.\d+)?)", RegexOptions.CultureInvariant)]
    private static partial Regex DataCoordinatesRegex();

    [GeneratedRegex("""(?:"latitude"\s*:\s*|\"latitude\"\s*:\s*)(?<lat>-?\d{1,2}(?:\.\d+)?).*?(?:"longitude"\s*:\s*|\"longitude\"\s*:\s*)(?<lon>-?\d{1,3}(?:\.\d+)?)""", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex MetadataCoordinatesRegex();

    [GeneratedRegex(@"\[null,null,(?<lat>-?\d{1,2}(?:\.\d+)?),(?<lon>-?\d{1,3}(?:\.\d+)?)\],""0x[0-9a-f]+:0x[0-9a-f]+""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlaceCoordinatesRegex();

    [GeneratedRegex(@"href=""(?<path>/maps/preview/place\?[^""]+)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlacePreviewLinkRegex();

    [GeneratedRegex(@"/maps/(?:search|place)/(?<lat>-?\d{1,2}(?:\.\d+)?)[,+\s]+(?<lon>-?\d{1,3}(?:\.\d+)?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PathCoordinatesRegex();

    public bool IsSupportedUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https"
        && IsAllowedHost(uri);

    public async Task<MapResolutionResult> ResolveAsync(string value, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var current) || !IsAllowedHost(current))
            return MapResolutionResult.NotApplicable();

        for (var redirect = 0; redirect <= MaxRedirects; redirect++)
        {
            if (TryExtractCoordinates(current.OriginalString, out var latitude, out var longitude))
                return MapResolutionResult.Success(latitude, longitude);

            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (IsRedirect(response.StatusCode))
            {
                if (redirect == MaxRedirects || response.Headers.Location is null)
                    return MapResolutionResult.Failed("Google Maps redirect limit exceeded.");

                var next = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(current, response.Headers.Location);
                if (!IsAllowedHost(next))
                    return MapResolutionResult.Failed("Google Maps redirected to an unsupported host.");

                current = next;
                continue;
            }

            if (!response.IsSuccessStatusCode)
                return MapResolutionResult.Failed($"Google Maps returned HTTP {(int)response.StatusCode}.");

            var body = await ReadBoundedAsync(response.Content, cancellationToken);
            if (body is null)
                return MapResolutionResult.Failed("Google Maps response was too large.");

            if (TryExtractCoordinates(body, out latitude, out longitude))
                return MapResolutionResult.Success(latitude, longitude);

            if (TryGetPlacePreviewUri(current, body, out var previewUri))
            {
                using var previewRequest = new HttpRequestMessage(HttpMethod.Get, previewUri);
                using var previewResponse = await httpClient.SendAsync(
                    previewRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                if (!previewResponse.IsSuccessStatusCode)
                    return MapResolutionResult.Failed($"Google Maps place preview returned HTTP {(int)previewResponse.StatusCode}.");

                var previewBody = await ReadBoundedAsync(previewResponse.Content, cancellationToken);
                if (previewBody is null)
                    return MapResolutionResult.Failed("Google Maps place preview response was too large.");

                if (TryExtractCoordinates(previewBody, out latitude, out longitude))
                    return MapResolutionResult.Success(latitude, longitude);
            }

            return MapResolutionResult.Failed("No coordinates were found in the Google Maps response.");
        }

        return MapResolutionResult.Failed("Google Maps resolution failed.");
    }

    private static bool IsAllowedHost(Uri uri)
    {
        var host = uri.IdnHost.ToLowerInvariant();
        return host == "maps.app.goo.gl"
            || host == "maps.google.com"
            || host == "www.google.com" && uri.AbsolutePath.StartsWith("/maps", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryExtractCoordinates(string value, out double latitude, out double longitude)
    {
        foreach (var regex in new[]
        {
            DataCoordinatesRegex(),
            MetadataCoordinatesRegex(),
            PlaceCoordinatesRegex()
        })
        {
            var match = regex.Match(WebUtility.HtmlDecode(value));
            if (match.Success
                && double.TryParse(match.Groups["lat"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude)
                && double.TryParse(match.Groups["lon"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude)
                && latitude is >= -90 and <= 90
                && longitude is >= -180 and <= 180)
            {
                return true;
            }
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            var pathMatch = PathCoordinatesRegex().Match(Uri.UnescapeDataString(uri.AbsolutePath));
            if (TryReadCoordinates(pathMatch, out latitude, out longitude))
                return true;

            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            foreach (var key in new[] { "q", "query", "destination", "center", "ll" })
            {
                var parts = query[key]?.Split(',', 2, StringSplitOptions.TrimEntries);
                if (parts?.Length == 2
                    && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude)
                    && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude)
                    && latitude is >= -90 and <= 90
                    && longitude is >= -180 and <= 180)
                {
                    return true;
                }
            }
        }

        latitude = default;
        longitude = default;
        return false;
    }

    private static bool TryReadCoordinates(Match match, out double latitude, out double longitude)
    {
        if (match.Success
            && double.TryParse(match.Groups["lat"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude)
            && double.TryParse(match.Groups["lon"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude)
            && latitude is >= -90 and <= 90
            && longitude is >= -180 and <= 180)
        {
            return true;
        }

        latitude = default;
        longitude = default;
        return false;
    }

    private static bool TryGetPlacePreviewUri(Uri current, string body, out Uri previewUri)
    {
        var match = PlacePreviewLinkRegex().Match(body);
        if (match.Success)
        {
            var path = WebUtility.HtmlDecode(match.Groups["path"].Value);
            var candidate = new Uri(current, path);
            if (IsAllowedHost(candidate)
                && candidate.AbsolutePath.Equals("/maps/preview/place", StringComparison.OrdinalIgnoreCase))
            {
                previewUri = candidate;
                return true;
            }
        }

        previewUri = null!;
        return false;
    }

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod
        or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    private static async Task<string?> ReadBoundedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > MaxResponseBytes)
            return null;

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > MaxResponseBytes) return null;
            buffer.Write(chunk, 0, read);
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}