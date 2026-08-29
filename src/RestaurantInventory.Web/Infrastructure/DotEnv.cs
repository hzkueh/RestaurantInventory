namespace RestaurantInventory.Web.Infrastructure;

/// <summary>
/// A tiny, dependency-free <c>.env</c> loader. .NET has no built-in <c>.env</c> support, so this
/// reads the git-ignored <c>.env</c> (see <c>.env.example</c>) and layers its values into
/// configuration, translating the <c>Section__Key</c> convention to <c>Section:Key</c> exactly as
/// the environment-variable provider does. Values already present in configuration (real
/// environment variables, appsettings) win, so <c>.env</c> only fills gaps.
/// </summary>
public static class DotEnv
{
    /// <summary>
    /// Finds the nearest <c>.env</c> at or above <paramref name="startDirectory"/> and merges its
    /// values into <paramref name="config"/>. A missing file is a no-op — the app runs without it,
    /// and the AI feature simply reports itself unavailable.
    /// </summary>
    public static void Load(ConfigurationManager config, string startDirectory)
    {
        var path = FindEnvFile(startDirectory);
        if (path is null)
            return;

        var toAdd = new Dictionary<string, string?>();
        foreach (var (key, value) in Parse(File.ReadAllLines(path)))
        {
            // Don't override a value already supplied by a higher-precedence source.
            if (string.IsNullOrEmpty(config[key]))
                toAdd[key] = value;
        }

        if (toAdd.Count > 0)
            config.AddInMemoryCollection(toAdd);
    }

    /// <summary>
    /// Parses <c>KEY=VALUE</c> lines into configuration keys. Blank lines and <c>#</c> comment lines
    /// are skipped; an optional <c>export </c> prefix is dropped; <c>__</c> becomes <c>:</c>.
    /// A value may be wrapped in single or double quotes (stripped, and kept literal within); an
    /// unquoted value may carry a trailing <c>#</c> comment (introduced by whitespace) which is
    /// removed. Exposed for unit testing the parsing rules without a file on disk.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, string?>> Parse(IEnumerable<string> lines)
    {
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            if (line.StartsWith("export ", StringComparison.Ordinal))
                line = line["export ".Length..].TrimStart();

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            var name = line[..eq].Trim().Replace("__", ":");
            var value = line[(eq + 1)..].Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                // Quoted: the quotes delimit the value, so any '#' inside is literal.
                value = value[1..^1];
            }
            else
            {
                // Unquoted: a '#' introduced by whitespace starts a trailing comment. A '#' with no
                // leading space (e.g. inside a URL fragment) is kept as part of the value.
                var comment = value.IndexOf(" #", StringComparison.Ordinal);
                if (comment >= 0)
                    value = value[..comment].TrimEnd();
            }

            yield return new KeyValuePair<string, string?>(name, value);
        }
    }

    private static string? FindEnvFile(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return null;
    }
}
