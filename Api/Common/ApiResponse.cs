namespace Api.Common;

/// <summary>
/// Standard error payload for API responses.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Machine-readable error code (e.g., "VALIDATION_ERROR", "NOT_FOUND").
    /// </summary>
    public string code { get; set; } = "";

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string message { get; set; } = "";

    /// <summary>
    /// Optional, any extra data to help clients debug (fields, ids, etc.).
    /// </summary>
    public object? details { get; set; }
}

/// <summary>
/// Generic API response envelope.
/// Exactly one of <see cref="data"/> or <see cref="error"/> should be populated.
/// </summary>
/// <typeparam name="T">Type of the successful response payload.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was handled successfully.
    /// </summary>
    public bool success { get; set; }

    /// <summary>
    /// Successful result payload when <see cref="success"/> is true.
    /// </summary>
    public T? data { get; set; }

    /// <summary>
    /// Error payload when <see cref="success"/> is false.
    /// </summary>
    public ApiError? error { get; set; }

    /// <summary>
    /// Factory: build a successful response with data.
    /// </summary>
    public static ApiResponse<T> Ok(T data) => new() { success = true, data = data };

    /// <summary>
    /// Factory: build a failed response with an error.
    /// </summary>
    public static ApiResponse<T> Fail(string code, string message, object? details = null) =>
        new() { success = false, error = new ApiError { code = code, message = message, details = details } };
}

/// <summary>
/// Standard pagination envelope for list endpoints.
/// </summary>
/// <typeparam name="T">Type of each item in the page.</typeparam>
public class PageResult<T>
{
    /// <summary>
    /// 1-based current page index.
    /// </summary>
    public int page { get; set; }

    /// <summary>
    /// Page size (items per page).
    /// </summary>
    public int size { get; set; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int totalItems { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int totalPages { get; set; }

    /// <summary>
    /// True if a next page exists.
    /// </summary>
    public bool hasNext { get; set; }

    /// <summary>
    /// True if a previous page exists.
    /// </summary>
    public bool hasPrev { get; set; }

    /// <summary>
    /// Items contained in the current page.
    /// </summary>
    public IEnumerable<T> items { get; set; } = Enumerable.Empty<T>();
}
