using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaylogDockExtension;

internal sealed record DaylogDailyFile(DateOnly Date, string Path, int EntryCount, string Preview);

internal sealed class DaylogMarkdownStore
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private readonly DaylogSettingsStore _settingsStore;

    public DaylogMarkdownStore(DaylogSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public string DaylogRoot
    {
        get
        {
            var rootFolder = _settingsStore.Current.RootFolder;
            return string.IsNullOrWhiteSpace(rootFolder) ? string.Empty : Path.Combine(rootFolder, "Daylog");
        }
    }

    public string TodayFilePath() => FilePathFor(DateOnly.FromDateTime(DateTime.Now));

    public string EnsureTodayFile() => EnsureDailyFile(DateOnly.FromDateTime(DateTime.Now));

    public string FilePathForSelectedDate(DateOnly date) => FilePathFor(date);

    public string EnsureDailyFile(DateOnly date)
    {
        var filePath = FilePathFor(date);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, string.Empty, Utf8NoBom);
        }

        return filePath;
    }

    public string AppendEntry(string body, bool allowEmpty = false) =>
        AppendEntry(body, DateTime.Now, allowEmpty);

    internal string AppendEntry(string body, DateTime now, bool allowEmpty = false)
    {
        return AppendEntry(DateOnly.FromDateTime(now), body, now, allowEmpty);
    }

    public string AppendEntry(DateOnly date, string body, bool allowEmpty = false) =>
        AppendEntry(date, body, DateTime.Now, allowEmpty);

    public string SaveDailyText(DateOnly date, string body)
    {
        if (!_settingsStore.Current.IsConfigured)
        {
            throw new InvalidOperationException("Choose a Daylog folder first.");
        }

        var filePath = EnsureDailyFile(date);
        File.WriteAllText(filePath, NormalizeDailyBody(body), Utf8NoBom);
        return filePath;
    }

    public async Task<string> SaveDailyTextAsync(DateOnly date, string body)
    {
        if (!_settingsStore.Current.IsConfigured)
        {
            throw new InvalidOperationException("Choose a Daylog folder first.");
        }

        var filePath = EnsureDailyFile(date);
        await File.WriteAllTextAsync(filePath, NormalizeDailyBody(body), Utf8NoBom).ConfigureAwait(false);
        return filePath;
    }

    internal string AppendEntry(DateOnly date, string body, DateTime timestamp, bool allowEmpty = false)
    {
        if (!_settingsStore.Current.IsConfigured)
        {
            throw new InvalidOperationException("Choose a Daylog folder first.");
        }

        var cleaned = (body ?? string.Empty).Trim();
        if (!allowEmpty && string.IsNullOrWhiteSpace(cleaned))
        {
            throw new InvalidOperationException("Write something before appending.");
        }

        var filePath = EnsureDailyFile(date);
        var prefix = NeedsLeadingBreak(filePath) ? Environment.NewLine : string.Empty;
        var entry = $"{prefix}## {timestamp:HH:mm}{Environment.NewLine}{Environment.NewLine}{cleaned}{Environment.NewLine}";
        File.AppendAllText(filePath, entry, Utf8NoBom);
        return filePath;
    }

    public string ReadDailyText(DateOnly date)
    {
        var filePath = FilePathFor(date);
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        return File.ReadAllText(filePath);
    }

    public DateOnly? FirstDailyFileDate()
    {
        if (string.IsNullOrWhiteSpace(DaylogRoot) || !Directory.Exists(DaylogRoot))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(DaylogRoot, "*.md", SearchOption.AllDirectories)
            .Select(TryReadDailyFile)
            .Where(file => file is not null)
            .Select(file => file!.Date)
            .OrderBy(date => date)
            .Cast<DateOnly?>()
            .FirstOrDefault();
    }

    public IReadOnlySet<DateOnly> DailyFileDates()
    {
        if (string.IsNullOrWhiteSpace(DaylogRoot) || !Directory.Exists(DaylogRoot))
        {
            return new HashSet<DateOnly>();
        }

        return Directory
            .EnumerateFiles(DaylogRoot, "*.md", SearchOption.AllDirectories)
            .Select(TryReadDailyFile)
            .Where(file => file is not null)
            .Select(file => file!.Date)
            .ToHashSet();
    }

    public IReadOnlyList<DaylogDailyFile> RecentFiles(int take = 14)
    {
        if (string.IsNullOrWhiteSpace(DaylogRoot) || !Directory.Exists(DaylogRoot))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(DaylogRoot, "*.md", SearchOption.AllDirectories)
            .Select(TryReadDailyFile)
            .Where(file => file is not null)
            .Select(file => file!)
            .OrderByDescending(file => file.Date)
            .Take(take)
            .ToArray();
    }

    private string FilePathFor(DateOnly date)
    {
        if (!_settingsStore.Current.IsConfigured)
        {
            throw new InvalidOperationException("Choose a Daylog folder first.");
        }

        var month = date.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var fileName = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".md";
        return Path.Combine(DaylogRoot, month, fileName);
    }

    private static bool NeedsLeadingBreak(string filePath)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists || info.Length == 0)
        {
            return false;
        }

        var text = File.ReadAllText(filePath);
        return !text.EndsWith(Environment.NewLine, StringComparison.Ordinal);
    }

    private static string NormalizeDailyBody(string? body) => (body ?? string.Empty).TrimEnd() + Environment.NewLine;

    private static DaylogDailyFile? TryReadDailyFile(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (!DateOnly.TryParseExact(fileName, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return null;
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch
        {
            return null;
        }

        var entryCount = lines.Count(line => line.StartsWith("## ", StringComparison.Ordinal));
        var preview = lines
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0 && !line.StartsWith("## ", StringComparison.Ordinal))
            ?? string.Empty;

        return new DaylogDailyFile(date, filePath, entryCount, preview);
    }
}
