using System.Text.Json.Serialization;

namespace ECommerce.API.Models;

public class ApiMetadata
{
    public string TraceId { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMetadata? Pagination { get; set; }
}
