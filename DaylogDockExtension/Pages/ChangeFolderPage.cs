using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DaylogDockExtension;

internal sealed partial class ChangeFolderPage : ContentPage
{
    private readonly MarkdownContent _header;
    private readonly ChangeFolderForm _form;

    public ChangeFolderPage(DaylogState state)
    {
        _header = new MarkdownContent();
        _form = new ChangeFolderForm(state);

        Icon = new IconInfo("\uE713");
        Id = "daylog.dock.settings";
        Title = "Daylog Settings";
        Name = "Settings";

        var current = state.Settings.Current.RootFolder;
        var suggested = DaylogSettingsStore.SuggestedRootFolder();
        _header.Body = $"""
            ## Daylog folder

            Daylog writes to `Daylog\yyyy-MM\yyyy-MM-dd.md` inside chosen folder.

            Current: `{(string.IsNullOrWhiteSpace(current) ? "not set" : current)}`

            Suggested: `{suggested}`
            """;

        Commands =
        [
            new CommandContextItem(new PickFolderCommand(state)) { Title = "Choose Folder" },
            new CommandContextItem(new UseSuggestedFolderCommand(state)) { Title = "Use Suggested Folder" },
        ];
    }

    public override IContent[] GetContent() => [_header, _form];
}

internal sealed partial class ChangeFolderForm : FormContent
{
    private const string FolderField = "rootFolder";

    private readonly DaylogState _state;

    public ChangeFolderForm(DaylogState state)
    {
        _state = state;
        var current = state.Settings.Current.RootFolder;
        TemplateJson = $$"""
            {
              "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
              "type": "AdaptiveCard",
              "version": "1.6",
              "body": [
                {
                  "type": "Input.Text",
                  "label": "Folder",
                  "id": "rootFolder",
                  "value": "{{EscapeJsonString(current)}}",
                  "placeholder": "C:\\Users\\you\\Documents\\Freewrite",
                  "isRequired": true,
                  "errorMessage": "Folder path is required."
                }
              ],
              "actions": [
                {
                  "type": "Action.Submit",
                  "title": "Save Folder"
                }
              ]
            }
            """;
    }

    public override ICommandResult SubmitForm(string payload)
    {
        try
        {
            var formInput = JsonNode.Parse(payload)?.AsObject();
            var folder = formInput?[FolderField]?.GetValue<string>() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(folder))
            {
                return OpenTodayCommand.Toast("Folder path is required.");
            }

            _state.Settings.SaveRootFolder(folder);
            Directory.CreateDirectory(_state.Settings.Current.RootFolder);
            _state.Markdown.EnsureTodayFile();
            Process.Start(new System.Diagnostics.ProcessStartInfo(OpenTodayCommand.ResolveEditorPath()) { UseShellExecute = true });

            return OpenTodayCommand.Toast($"Daylog folder saved. Opened editor.");
        }
        catch (Exception ex)
        {
            return OpenTodayCommand.Toast(ex.Message);
        }
    }

    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
