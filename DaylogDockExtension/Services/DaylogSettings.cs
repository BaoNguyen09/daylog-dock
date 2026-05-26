using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DaylogDockExtension;

internal sealed class DaylogSettings
{
    public string RootFolder { get; set; } = string.Empty;

    public string ColorScheme { get; set; } = "light";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(RootFolder);

    public bool IsDarkMode => string.Equals(ColorScheme, "dark", StringComparison.OrdinalIgnoreCase);
}

internal sealed class DaylogSettingsStore
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public DaylogSettings Current { get; private set; }

    public string SettingsPath { get; }

    public string SettingsDirectory => Path.GetDirectoryName(SettingsPath)!;

    public DaylogSettingsStore()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DaylogDock"))
    {
    }

    internal DaylogSettingsStore(string settingsDirectory)
    {
        Directory.CreateDirectory(settingsDirectory);

        SettingsPath = Path.Combine(settingsDirectory, "settings.json");
        Current = Load();
    }

    public void SaveRootFolder(string rootFolder)
    {
        Current.RootFolder = NormalizeFolder(rootFolder);
        Save();
    }

    public void SaveColorScheme(string colorScheme)
    {
        Current.ColorScheme = string.Equals(colorScheme, "dark", StringComparison.OrdinalIgnoreCase)
            ? "dark"
            : "light";
        Save();
    }

    public void Reload()
    {
        Current = Load();
    }

    public bool IsConfigured => Current.IsConfigured;

    public static string SuggestedRootFolder()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var freewrite = Path.Combine(documents, "Freewrite");
        return Directory.Exists(freewrite) ? freewrite : documents;
    }

    private DaylogSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new DaylogSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath, Utf8NoBom);
            var node = JsonNode.Parse(json);
            var rootFolder = node?["RootFolder"]?.GetValue<string>() ?? string.Empty;
            rootFolder = string.IsNullOrWhiteSpace(rootFolder) ? string.Empty : NormalizeFolder(rootFolder);
            var colorScheme = node?["ColorScheme"]?.GetValue<string>() ?? "light";
            if (!string.Equals(colorScheme, "dark", StringComparison.OrdinalIgnoreCase))
            {
                colorScheme = "light";
            }

            return new DaylogSettings { RootFolder = rootFolder, ColorScheme = colorScheme };
        }
        catch
        {
            return new DaylogSettings();
        }
    }

    private void Save()
    {
        var node = new JsonObject
        {
            ["RootFolder"] = Current.RootFolder,
            ["ColorScheme"] = Current.ColorScheme,
        };
        File.WriteAllText(SettingsPath, node.ToJsonString(JsonOptions), Utf8NoBom);
    }

    private static string NormalizeFolder(string rootFolder)
    {
        if (string.IsNullOrWhiteSpace(rootFolder))
        {
            return string.Empty;
        }

        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(rootFolder.Trim().Trim('"')));
    }
}
