using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Tkmm.Core.WiiXLaunch.Models;

public abstract class TkWiiXLaunchValue : ObservableObject {
    public required string ModId { get; init; }
    
    public required string ConfigKey { get; init; }

    public abstract string ToConfigValue();

    public abstract void Restore(JsonElement stored);

    protected void Persist(JsonElement value) {
        TkWiiXLaunchStore.Current.Set(ModId, ConfigKey, value);
    }
}

public sealed partial class TkWiiXLaunchBoolValue : TkWiiXLaunchValue {
    [ObservableProperty] 
    private bool _value;

    partial void OnValueChanged(bool value) => Persist(JsonSerializer.SerializeToElement(value));
    
    public override string ToConfigValue() => Value ? "1" : "0";

    public override void Restore(JsonElement stored) {
        if (stored.ValueKind is JsonValueKind.True or JsonValueKind.False) {
            SetProperty(ref _value, stored.GetBoolean(), nameof(Value));
        }
    }
}

public sealed partial class TkWiiXLaunchRangeValue : TkWiiXLaunchValue {
    [ObservableProperty] 
    private double _value;
    
    [ObservableProperty] 
    private int _minValue;
    
    [ObservableProperty]
    private int _maxValue;

    [ObservableProperty] 
    private int _incrementSize = 1;
    
    partial void OnValueChanged(int value) => Persist(JsonSerializer.SerializeToElement(value));
    
    public override string ToConfigValue() => Value.ToString(CultureInfo.InvariantCulture);

    public override void Restore(JsonElement stored) {
        if (stored.ValueKind is JsonValueKind.Number && stored.TryGetInt32(out var restored)) {
            SetProperty(ref _value, Math.Clamp(restored, MinValue, MaxValue), nameof(Value));
        }
    }
}

public sealed record TkWiiXLaunchEnum(string Name, int Value);

public sealed partial class TkWiiXLaunchEnumValue : TkWiiXLaunchValue {
    [ObservableProperty] 
    private int _index;
    
    public required List<TkWiiXLaunchEnum>Values { get; init; }
    
    partial void OnIndexChanged(int value) => Persist(JsonSerializer.SerializeToElement(value));

    public override string ToConfigValue() {
        var clamped = Math.Clamp(Index, 0, Values.Count - 1);
        return Values[clamped].Value.ToString(CultureInfo.InvariantCulture);
    }
    
    public override void Restore(JsonElement stored) {
        if (stored.ValueKind is JsonValueKind.Number && stored.TryGetInt32(out var restored)) {
            SetProperty(ref _index, Math.Clamp(restored, 0, Values.Count - 1), nameof(Index));
        }
    }
}