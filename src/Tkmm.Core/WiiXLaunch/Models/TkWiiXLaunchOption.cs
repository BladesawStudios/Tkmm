using System.Text.Json;

namespace Tkmm.Core.WiiXLaunch.Models;

public sealed class TkWiiXLaunchOption(string name, string? description, TkWiiXLaunchValue value) {
    public string Name { get; } = name;
    
    public string Description { get; } = description ?? string.Empty;
    
    public bool HasDescription => Description.Length > 0;
    
    public TkWiiXLaunchValue Value { get; } = value;

    public static TkWiiXLaunchOption? FromJson(string modId, string key, TkWiiXLaunchOptionsJson.Option option) {
        var configKey = option.ConfigKey ?? key;

        TkWiiXLaunchValue? value = option.Class switch {
            "bool" => new TkWiiXLaunchBoolValue {
                ModId = modId,
                ConfigKey = configKey,
                Value = option.Default.ValueKind is JsonValueKind.True
            },
            "scale" when option.Values is { Count: 2 } => new TkWiiXLaunchRangeValue {
                ModId = modId,
                ConfigKey = configKey,
                MinValue = option.Values[0].GetInt32(),
                MaxValue = option.Values[1].GetInt32(),
                IncrementSize = option.Increments.ValueKind is JsonValueKind.Number ? option.Increments.GetInt32() : 1,
                Value = option.Default.ValueKind is JsonValueKind.Number ? option.Default.GetInt32() : 0
            },
            "dropdown" when option.NameValues is { Count: > 0 } && option.Values is { Count: > 0 } &&
                            option.NameValues.Count == option.Values.Count => new TkWiiXLaunchEnumValue {
                ModId = modId,
                ConfigKey = configKey,
                Values = [
                    .. option.NameValues.Select((name, i) =>
                        new TkWiiXLaunchEnumValue(name, option.Values[i].GetInt32()))
                ],
                Index = option.Default.ValueKind is JsonValueKind.Number ? option.Default.GetInt32() : 0
            },
            _ => null
        };
        
        return value is null ? null : new TkWiiXLaunchOption(option.Name, option.Description, value);
    }
}

public sealed class TkWiiXLaunchOptionGroup(string modId, string name) {
    public string ModId { get; } = modId;
    
    public string Name { get; } = name;

    public List<TkWiiXLaunchOption> Options { get; } = [];
}