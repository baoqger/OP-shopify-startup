using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuntieDot.Infrastructure;

public static class DefaultJsonSerializer
{
    public static JsonSerializerOptions Settings = new JsonSerializerOptions
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(object obj) => JsonSerializer.Serialize(obj, DefaultJsonSerializer.Settings);

    public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, DefaultJsonSerializer.Settings);
}
