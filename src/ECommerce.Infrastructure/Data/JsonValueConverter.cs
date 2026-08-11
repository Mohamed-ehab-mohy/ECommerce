using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ECommerce.Infrastructure.Data;

public static class JsonStorageOptions
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

/// <summary>Stores a value object as a <c>jsonb</c> column via System.Text.Json.</summary>
public sealed class JsonValueConverter<T> : ValueConverter<T, string>
{
    public JsonValueConverter()
        : base(
            value => JsonSerializer.Serialize(value, JsonStorageOptions.Options),
            json => JsonSerializer.Deserialize<T>(json, JsonStorageOptions.Options)!)
    {
    }
}
