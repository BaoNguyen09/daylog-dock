using DaylogDockExtension;

var root = Path.Combine(Path.GetTempPath(), "daylog-dock-smoke-root");
var settingsDirectory = Path.Combine(Path.GetTempPath(), "daylog-dock-smoke-settings");

Clean(root);
Clean(settingsDirectory);

try
{
    var settings = new DaylogSettingsStore(settingsDirectory);
    settings.SaveRootFolder(root);

    var reloadedSettings = new DaylogSettingsStore(settingsDirectory);
    Assert(reloadedSettings.Current.RootFolder == root, "settings reload mismatch");

    var store = new DaylogMarkdownStore(reloadedSettings);

    var firstPath = store.AppendEntry("First captured thought.", new DateTime(2026, 5, 15, 21, 42, 0));
    var secondPath = store.AppendEntry("Next day thought.", new DateTime(2026, 5, 16, 8, 5, 0));
    store.AppendEntry(string.Empty, new DateTime(2026, 5, 16, 8, 6, 0), allowEmpty: true);
    var selectedPath = store.AppendEntry(new DateOnly(2026, 5, 20), "Planned future thought.", new DateTime(2026, 5, 16, 8, 7, 0));

    Assert(
        firstPath == Path.Combine(root, "Daylog", "2026-05", "2026-05-15.md"),
        "first day path mismatch");
    Assert(
        secondPath == Path.Combine(root, "Daylog", "2026-05", "2026-05-16.md"),
        "second day path mismatch");
    Assert(
        selectedPath == Path.Combine(root, "Daylog", "2026-05", "2026-05-20.md"),
        "selected day path mismatch");

    var firstText = File.ReadAllText(firstPath);
    var secondText = File.ReadAllText(secondPath);

    Assert(firstText.Contains("## 21:42", StringComparison.Ordinal), "first timestamp missing");
    Assert(firstText.Contains("First captured thought.", StringComparison.Ordinal), "first body missing");
    Assert(secondText.Contains("## 08:05", StringComparison.Ordinal), "second timestamp missing");
    Assert(secondText.Contains("Next day thought.", StringComparison.Ordinal), "second body missing");
    Assert(secondText.Contains("## 08:06", StringComparison.Ordinal), "empty new-entry timestamp missing");
    Assert(store.ReadDailyText(new DateOnly(2026, 5, 20)).Contains("Planned future thought.", StringComparison.Ordinal), "selected date body missing");

    var recent = store.RecentFiles(14);
    Assert(recent.Count == 3, "history count mismatch");
    Assert(recent[0].Date == new DateOnly(2026, 5, 20), "history order mismatch");
    Assert(recent[1].EntryCount == 2, "history entry count mismatch");
    Assert(recent[1].Preview == "Next day thought.", "history preview mismatch");
    var dailyDates = store.DailyFileDates();
    Assert(dailyDates.SetEquals([new DateOnly(2026, 5, 15), new DateOnly(2026, 5, 16), new DateOnly(2026, 5, 20)]), "daily file dates mismatch");
    store.SaveDailyText(new DateOnly(2026, 5, 16), "Single daily note body.");
    Assert(store.ReadDailyText(new DateOnly(2026, 5, 16)).Trim() == "Single daily note body.", "single daily note save mismatch");
    await store.SaveDailyTextAsync(new DateOnly(2026, 5, 16), "Async daily note body.");
    Assert(store.ReadDailyText(new DateOnly(2026, 5, 16)).Trim() == "Async daily note body.", "async daily note save mismatch");

    var state = new DaylogState(reloadedSettings, () => new DateOnly(2026, 5, 16));
    Assert(state.SelectedDate == new DateOnly(2026, 5, 16), "selected date should start today");
    Assert(state.TryMoveSelectedDate(30, out _), "30-day future bound should be allowed");
    Assert(state.SelectedDate == new DateOnly(2026, 6, 15), "future selected date mismatch");
    Assert(!state.TryMoveSelectedDate(1, out var futureMessage), "31-day future bound should fail");
    Assert(futureMessage.Contains("30 days", StringComparison.Ordinal), "future bound message mismatch");
    Assert(state.TrySelectDate(new DateOnly(2026, 5, 15), out _), "first entry date should be allowed");
    Assert(!state.TryMoveSelectedDate(-1, out var pastMessage), "before-first-entry bound should fail");
    Assert(pastMessage.Contains("First Daylog entry", StringComparison.Ordinal), "past bound message mismatch");
    Assert(state.IsCalendarSelectable(new DateOnly(2026, 5, 15)), "past saved day should be calendar selectable");
    Assert(!state.IsCalendarSelectable(new DateOnly(2026, 5, 14)), "past empty day should not be calendar selectable");
    Assert(state.IsCalendarSelectable(new DateOnly(2026, 6, 15)), "future day inside 30-day window should be calendar selectable");
    Assert(!state.IsCalendarSelectable(new DateOnly(2026, 6, 16)), "future day outside 30-day window should not be calendar selectable");

    var futureOnlyRoot = Path.Combine(Path.GetTempPath(), "daylog-dock-smoke-future-root");
    var futureOnlySettingsDirectory = Path.Combine(Path.GetTempPath(), "daylog-dock-smoke-future-settings");
    Clean(futureOnlyRoot);
    Clean(futureOnlySettingsDirectory);
    var futureOnlySettings = new DaylogSettingsStore(futureOnlySettingsDirectory);
    futureOnlySettings.SaveRootFolder(futureOnlyRoot);
    var futureOnlyStore = new DaylogMarkdownStore(futureOnlySettings);
    futureOnlyStore.AppendEntry(new DateOnly(2026, 5, 20), "Future first.", new DateTime(2026, 5, 16, 8, 8, 0));
    var futureOnlyState = new DaylogState(futureOnlySettings, () => new DateOnly(2026, 5, 16));
    Assert(futureOnlyState.TrySelectDate(new DateOnly(2026, 5, 16), out _), "today should remain reachable when first saved log is future");
    Assert(!futureOnlyState.TrySelectDate(new DateOnly(2026, 5, 15), out _), "before-today should fail when first saved log is future");
    Clean(futureOnlyRoot);
    Clean(futureOnlySettingsDirectory);

    Console.WriteLine("Daylog markdown smoke tests passed.");
}
finally
{
    Clean(root);
    Clean(settingsDirectory);
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void Clean(string path)
{
    if (Directory.Exists(path))
    {
        Directory.Delete(path, recursive: true);
    }
}
