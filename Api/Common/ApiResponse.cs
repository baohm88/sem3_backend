namespace Api.Common;

public class ApiError
{
    public string code { get; set; } = "";
    public string message { get; set; } = "";
    public object? details { get; set; }
}

public class ApiResponse<T>
{
    public bool success { get; set; }
    public T? data { get; set; }
    public ApiError? error { get; set; }

    public static ApiResponse<T> Ok(T data) => new() { success = true, data = data };
    public static ApiResponse<T> Fail(string code, string message, object? details = null) =>
        new() { success = false, error = new ApiError { code = code, message = message, details = details } };
}

public class PageResult<T>
{
    public int page { get; set; }
    public int size { get; set; }
    public int totalItems { get; set; }
    public int totalPages { get; set; }
    public bool hasNext { get; set; }
    public bool hasPrev { get; set; }
    public IEnumerable<T> items { get; set; } = Enumerable.Empty<T>();
}