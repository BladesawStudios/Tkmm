using System.Text;

namespace Tkmm.Core.WiiXLaunch;

public static class TkWiiXLaunchIni {
    public static Dictionary<string, string> Parse(TextReader reader) {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);

        while (reader.ReadLine() is { } line) {
            if (!TrySplit(line, out var key, out var value)) {
                continue;
            }

            result.TryAdd(key, value);
        }

        return result;
    }

    public static Dictionary<string, string> Parse(string filePath) {
        using var reader = new StreamReader(filePath);
        return Parse(reader);
    }

    public static string Patch(string existingContent, IReadOnlyDictionary<string, string> updates) {
        var pending = new Dictionary<string, string>(updates, StringComparer.OrdinalIgnoreCase);
        var newline = existingContent.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = existingContent.Replace("\r\n", "\n").Split('\n').ToList();

        HashSet<string> replace = new(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < lines.Count; i++) {
            if (!TrySplit(lines[i], out var key, out _)) {
                continue;
            }

            if (!replace.Add(key) || !pending.Remove(key, out var value)) {
                continue;
            }

            var separatorIndex = lines[i].IndexOf('=');
            lines[i] = string.Concat(lines[i].AsSpan(0, separatorIndex + 1), " ", value);
        }

        if (pending.Count > 0) {
            if (lines.Count > 0 && lines[^1].Trim().Length > 0) {
                lines.Add(string.Empty);
            }

            foreach (var (key, value) in pending) {
                lines.Add($"{key} = {value}");
            }
        }
        
        return string.Join(newline, lines);
    }

    public static string Format(IReadOnlyDictionary<string, string> values) {
        StringBuilder builder = new();

        foreach (var (key, value) in values) {
            builder.Append(key).Append(" = ").AppendLine(value);
        }
        
        return builder.ToString();
    }

    private static bool TrySplit(string line, out string key, out string value) {
        key = string.Empty;
        value = string.Empty;
        
        var trimmed = line.AsSpan().Trim();

        if (trimmed.IsEmpty || trimmed[0] is '#' or ';' ||
            (trimmed.Length > 1 && trimmed[0] is '/' && trimmed[1] is '/')) {
            return false;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0) {
            return false;
        }

        key = line[..separatorIndex].Trim();
        value = line[(separatorIndex + 1)..].Trim();
        return key.Length > 0;
    }
}