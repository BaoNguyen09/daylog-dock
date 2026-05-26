// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DaylogDockExtension;

internal sealed partial class DaylogDockExtensionPage : ListPage
{
    private readonly DaylogState _state;

    public DaylogDockExtensionPage(DaylogState state)
    {
        _state = state;
        Icon = DaylogIcons.CommandPalette;
        Id = "daylog.dock.home";
        Title = "Daylog";
        Name = "Open";
    }

    public override IListItem[] GetItems()
    {
        if (!_state.Settings.Current.IsConfigured)
        {
            return
            [
                new ListItem(new PickFolderCommand(_state))
                {
                    Title = "Choose folder",
                    Subtitle = "Open the native Windows folder picker.",
                },
                new ListItem(new ChangeFolderPage(_state))
                {
                    Title = "Enter folder path",
                    Subtitle = "First run setup. Creates Daylog\\yyyy-MM\\yyyy-MM-dd.md.",
                },
                new ListItem(new UseSuggestedFolderCommand(_state))
                {
                    Title = "Use suggested folder",
                    Subtitle = DaylogSettingsStore.SuggestedRootFolder(),
                },
            ];
        }

        _state.Markdown.EnsureTodayFile();

        return [
            new ListItem(new OpenEditorCommand(_state))
            {
                Title = "Editor",
                Subtitle = "Native notepad with live autosave.",
            },
            new ListItem(new OpenTodayCommand(_state))
            {
                Title = "Open Today",
                Subtitle = _state.Markdown.TodayFilePath(),
            },
            new ListItem(new RevealFolderCommand(_state))
            {
                Title = "Reveal Folder",
                Subtitle = _state.Markdown.DaylogRoot,
            },
            new ListItem(new HistoryPage(_state))
            {
                Title = "History",
                Subtitle = "Recent daily files.",
            },
            new ListItem(new ChangeFolderPage(_state))
            {
                Title = "Settings",
                Subtitle = "Change folder.",
            },
        ];
    }
}
