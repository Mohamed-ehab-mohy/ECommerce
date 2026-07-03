namespace ECommerce.API.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public T? Data { get; set; }
    public ApiMetadata Meta { get; set; } = new ApiMetadata();

    public static ApiResponse<T> Ok(T data, string traceId, string? message = null, PaginationMetadata? pagination = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Meta = new ApiMetadata
            {
                TraceId = traceId,
                Pagination = pagination
            }
        };
    }
}
