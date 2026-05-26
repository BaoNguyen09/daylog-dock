using System.Globalization;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DaylogDockExtension;

internal sealed partial class CapturePage : ContentPage
{
    private readonly DaylogState _state;
    private readonly MarkdownContent _content = new();

    public CapturePage(DaylogState state)
    {
        _state = state;
        Icon = DaylogIcons.CommandPalette;
        Id = "daylog.dock.capture";
        Title = "Daylog";
        Name = "Editor";
        Commands =
        [
            new CommandContextItem(new OpenEditorCommand(state)) { Title = "Open Editor" },
            new CommandContextItem(new RevealFolderCommand(state)) { Title = "Reveal Folder" },
            new CommandContextItem(new HistoryPage(state)) { Title = "History" },
            new CommandContextItem(new ChangeFolderPage(state)) { Title = "Settings" },
        ];
    }

    public override IContent[] GetContent()
    {
        if (!_state.Settings.Current.IsConfigured)
        {
            _content.Body = "Choose a folder first.";
            return [_content];
        }

        var today = _state.Today.ToDateTime(default).ToString("dddd, MMMM d", CultureInfo.CurrentCulture);
        _content.Body = $"""
            **Daylog** - {today}

            Open the native editor for live autosave.
            """;
        return [_content];
    }
}
