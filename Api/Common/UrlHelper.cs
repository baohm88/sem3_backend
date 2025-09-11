using System;

namespace Api.Common;

/// <summary>
/// URL helper utilities for normalization and validation.
/// </summary>
public static class UrlHelper
{
    /// <summary>
    /// Normalize a raw URL string:
    /// - Trim spaces
    /// - Optionally add "https://" if scheme is missing
    /// - Enforce max length
    /// - Ensure it is a valid HTTP/HTTPS absolute URL
    /// </summary>
    /// <param name="raw">The raw input URL.</param>
    /// <param name="addHttpsIfMissing">Whether to automatically add https:// if missing.</param>
    /// <param name="maxLen">Maximum allowed length for the URL.</param>
    /// <returns>Normalized URL string or null if invalid.</returns>
    public static string? TryNormalizeUrl(string? raw, bool addHttpsIfMissing = true, int maxLen = 2048)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim();
        if (s.Length > maxLen) return null;

        // If scheme is missing, prepend https:// (depending on policy)
        if (addHttpsIfMissing && !s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                                !s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            s = "https://" + s;
        }

        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;

        // Optional: add domain whitelist or block internal IPs if needed
        return uri.ToString();
    }
}
