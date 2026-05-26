using System.Globalization;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DaylogDockExtension;

internal sealed partial class HistoryPage : ListPage
{
    private readonly DaylogState _state;

    public HistoryPage(DaylogState state)
    {
        _state = state;
        Icon = new IconInfo("\uE81C");
        Id = "daylog.dock.history";
        Title = "Recent";
        Name = "History";
    }

    public override IListItem[] GetItems()
    {
        if (!_state.Settings.Current.IsConfigured)
        {
            return
            [
                new ListItem(new ChangeFolderPage(_state))
                {
                    Title = "Choose folder",
                    Subtitle = "Set where Daylog writes markdown files.",
                },
            ];
        }

        var files = _state.Markdown.RecentFiles(14);
        if (files.Count == 0)
        {
            return
            [
                new ListItem(new OpenTodayCommand(_state))
                {
                    Title = "No entries yet",
                    Subtitle = "Open today to create the first daily file.",
                },
            ];
        }

        return files
            .Select(file => new ListItem(new OpenFileCommand(file.Path))
            {
                Title = file.Date.ToDateTime(default).ToString("ddd, MMM d", CultureInfo.CurrentCulture),
                Subtitle = Describe(file),
                MoreCommands =
                [
                    new CommandContextItem("Reveal", action: () => OpenTodayCommand.ShellOpen(System.IO.Path.GetDirectoryName(file.Path)!)),
                ],
            })
            .Cast<IListItem>()
            .ToArray();
    }

    private static string Describe(DaylogDailyFile file)
    {
        var entries = file.EntryCount == 1 ? "1 entry" : $"{file.EntryCount} entries";
        return string.IsNullOrWhiteSpace(file.Preview) ? entries : $"{entries} - {file.Preview}";
    }
}
