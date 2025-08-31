using System;

namespace Api.Common;

public static class UrlHelper
{
    // Chuẩn hoá URL: trim, tự thêm https:// nếu thiếu scheme (tuỳ ý)
    public static string? TryNormalizeUrl(string? raw, bool addHttpsIfMissing = true, int maxLen = 2048)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim();
        if (s.Length > maxLen) return null;

        // nếu thiếu scheme, thêm https:// (tùy chính sách)
        if (addHttpsIfMissing && !s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                                !s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            s = "https://" + s;
        }

        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;

        // có thể bổ sung whitelist domain / chặn IP nội bộ tại đây nếu cần
        return uri.ToString();
    }


}
