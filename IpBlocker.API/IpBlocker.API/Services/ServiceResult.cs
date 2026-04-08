namespace IpBlocker.API.Services;


public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int StatusCode { get; private set; }
    private ServiceResult() { }

    /// <summary>Operation succeeded. Returns 200 by default.</summary>
    public static ServiceResult<T> Success(T data, int statusCode = 200)
        => new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    /// <summary>Resource not found — maps to HTTP 404.</summary>
    public static ServiceResult<T> NotFound(string message)
        => new() { IsSuccess = false, ErrorMessage = message, StatusCode = 404 };

    /// <summary>Conflict — duplicate resource, maps to HTTP 409.</summary>
    public static ServiceResult<T> Conflict(string message)
        => new() { IsSuccess = false, ErrorMessage = message, StatusCode = 409 };

    /// <summary>Bad input — maps to HTTP 400.</summary>
    public static ServiceResult<T> BadRequest(string message)
        => new() { IsSuccess = false, ErrorMessage = message, StatusCode = 400 };

    /// <summary>External dependency failed — maps to HTTP 502 Bad Gateway.</summary>
    public static ServiceResult<T> ExternalError(string message)
        => new() { IsSuccess = false, ErrorMessage = message, StatusCode = 502 };
}