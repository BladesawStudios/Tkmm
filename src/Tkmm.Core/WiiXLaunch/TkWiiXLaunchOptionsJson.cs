using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tkmm.Core.WiiXLaunch;

public sealed class TkWiiXLaunchOptionsJson {
    [JsonPropertyName("Keys")]
    public required Dictionary<string, Option> Options { get; init; }

    public sealed class Option {
        public required string Name { get; init; }
        
        public required string Class { get; init; }
        
        public string? Section { get; init; }
        
        [JsonPropertyName("Name_Values")]
        public List<string>? NameValues { get; init; }
        
        public List<JsonElement>? Values { get; init; }
        
        public JsonElement Default { get; init; }
        
        public JsonElement Increments { get; init; }
        
        public string? Description { get; init; }
        
        [JsonPropertyName("Config_Key")]
        public string? ConfigKey { get; init; }
    }
}

[JsonSerializable(typeof(TkWiiXLaunchOptionsJson))]
public sealed partial class TkWiiXLaunchOptionsJsonContext : JsonSerializerContext;