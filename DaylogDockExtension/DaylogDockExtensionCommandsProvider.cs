// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DaylogDockExtension;

public partial class DaylogDockExtensionCommandsProvider : CommandProvider
{
    private readonly DaylogState _state = new();
    private readonly IconInfo _icon = DaylogIcons.CommandPalette;

    public DaylogDockExtensionCommandsProvider()
    {
        Id = "daylog.dock";
        DisplayName = "Daylog";
        Icon = _icon;
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return
        [
            new CommandItem(new DaylogDockExtensionPage(_state))
            {
                Title = DisplayName,
                Subtitle = "Daily markdown journal",
                Icon = _icon,
            },
        ];
    }

    public override ICommandItem[]? GetDockBands()
    {
        return [CreateBandItem()];
    }

    public override ICommandItem? GetCommandItem(string id)
    {
        return id == "daylog.dock.openEditor" ? CreateBandItem() : null;
    }

    private CommandItem CreateBandItem()
    {
        IContextItem[] moreCommands = _state.Settings.IsConfigured
            ?
            [
                new CommandContextItem(new OpenTodayCommand(_state)) { Title = "Open Today" },
                new CommandContextItem(new RevealFolderCommand(_state)) { Title = "Reveal Folder" },
                new CommandContextItem(new HistoryPage(_state)) { Title = "History" },
                new CommandContextItem(new ChangeFolderPage(_state)) { Title = "Settings" },
                new CommandContextItem(new PickFolderCommand(_state)) { Title = "Change Folder" },
            ]
            :
            [
                new CommandContextItem(new ChangeFolderPage(_state)) { Title = "Settings" },
                new CommandContextItem(new UseSuggestedFolderCommand(_state)) { Title = "Use Suggested Folder" },
                new CommandContextItem(new PickFolderCommand(_state)) { Title = "Choose Folder" },
            ];

        return new CommandItem(new OpenEditorCommand(_state))
            {
                Title = DisplayName,
                Subtitle = _state.Settings.IsConfigured ? "Today" : "Choose folder",
                Icon = _icon,
                MoreCommands = moreCommands,
            };
    }

}
