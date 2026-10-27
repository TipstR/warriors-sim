using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WarriorsSim.Simulation;

/// <summary>Marks a field in <see cref="Tuning" /> as a value that can be changed, and puts it in a group.</summary>
[AttributeUsage(AttributeTargets.Field)]
public class TuningGroupAttribute : Attribute {
    public TuningGroupAttribute(string group) {
        Group = group;
    }

    public string Group { get; }
}

/// <summary>One value from <see cref="Tuning" /> that can be read and changed, e.g. from a settings screen.</summary>
public class TuningSetting {
    private readonly FieldInfo _field;

    internal TuningSetting(FieldInfo field, string group) {
        _field = field;
        Group = group;
        DefaultValue = field.GetValue(null);
    }

    /// <summary>The name in the code, e.g. "TicksUntilExhausted".</summary>
    public string Name => _field.Name;

    /// <summary>The group from the <see cref="TuningGroupAttribute" />, e.g. "Energy".</summary>
    public string Group { get; }

    /// <summary>int, float or an enum like <see cref="Season" />.</summary>
    public Type Type => _field.FieldType;

    /// <summary>The value the game starts with.</summary>
    public object DefaultValue { get; }

    /// <summary>
    ///     The current value. Setting it converts automatically, so a double from a SpinBox or a string
    ///     from a text field works too.
    /// </summary>
    public object Value {
        get => _field.GetValue(null);
        set => _field.SetValue(null, Convert(value));
    }

    private object Convert(object value) {
        if (!Type.IsEnum)
            return System.Convert.ChangeType(value, Type);

        return value is string text
            ? Enum.Parse(Type, text)
            : Enum.ToObject(Type, System.Convert.ToInt32(value));
    }
}

/// <summary>Lists all tunable values in <see cref="Tuning" /> and can reset them to the values the game starts with.</summary>
public static class TuningSettings {
    /// <summary>All tunable values, in the order they appear in <see cref="Tuning" />.</summary>
    public static IReadOnlyList<TuningSetting> All { get; } = typeof(Tuning)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (Field: f, Attribute: f.GetCustomAttribute<TuningGroupAttribute>()))
        .Where(f => f.Attribute != null)
        .Select(f => new TuningSetting(f.Field, f.Attribute.Group))
        .ToList();

    /// <summary>The values grouped, e.g. "Energy" → TicksUntilExhausted, SleepEnergy, ...</summary>
    public static IEnumerable<IGrouping<string, TuningSetting>> ByGroup() {
        return All.GroupBy(s => s.Group);
    }

    /// <summary>Finds a value by its name, e.g. Get("TicksUntilExhausted"). Returns null if there is none.</summary>
    public static TuningSetting Get(string name) {
        return All.FirstOrDefault(s => s.Name == name);
    }

    public static void ResetToDefaults() {
        foreach (var setting in All)
            setting.Value = setting.DefaultValue;
        Tuning.DefaultFeedingPolicy = FeedingPolicy.Balanced;
    }
}
