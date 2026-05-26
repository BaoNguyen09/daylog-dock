using System;

namespace DaylogDockExtension;

internal sealed class DaylogState
{
    private readonly Func<DateOnly> _todayProvider;

    public DaylogState()
        : this(new DaylogSettingsStore(), () => DateOnly.FromDateTime(DateTime.Now))
    {
    }

    internal DaylogState(DaylogSettingsStore settings, Func<DateOnly> todayProvider)
    {
        Settings = settings;
        Markdown = new DaylogMarkdownStore(Settings);
        _todayProvider = todayProvider;
        SelectedDate = Today;
    }

    public DaylogSettingsStore Settings { get; }

    public DaylogMarkdownStore Markdown { get; }

    public DateOnly SelectedDate { get; private set; }

    public DateOnly Today => _todayProvider();

    public DateOnly MaxFutureDate => Today.AddDays(30);

    public void SelectToday()
    {
        SelectedDate = Today;
    }

    public bool TryMoveSelectedDate(int days, out string message)
    {
        return TrySelectDate(SelectedDate.AddDays(days), out message);
    }

    public bool TrySelectDate(DateOnly date, out string message)
    {
        if (date > MaxFutureDate)
        {
            message = "Daylog can only prepare logs 30 days ahead.";
            return false;
        }

        var first = Markdown.FirstDailyFileDate();
        var lowerBound = first is null || first.Value > Today ? Today : first.Value;
        if (date < lowerBound)
        {
            message = $"First Daylog entry is {lowerBound:yyyy-MM-dd}.";
            return false;
        }

        SelectedDate = date;
        message = string.Empty;
        return true;
    }

    public bool IsCalendarSelectable(DateOnly date)
    {
        if (date > MaxFutureDate)
        {
            return false;
        }

        if (date >= Today)
        {
            return true;
        }

        return Markdown.DailyFileDates().Contains(date);
    }
}
