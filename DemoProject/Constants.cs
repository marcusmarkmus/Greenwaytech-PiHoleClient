using System.Text.Json;

namespace DemoProject;
public static class Constants
{
   
    public static JsonSerializerOptions SerializerSettings = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
}
