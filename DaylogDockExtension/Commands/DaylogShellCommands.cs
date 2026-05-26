using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DaylogDockExtension;

internal sealed partial class OpenEditorCommand : InvokableCommand
{
    private readonly DaylogState _state;

    public OpenEditorCommand(DaylogState state)
    {
        _state = state;
        Id = "daylog.dock.openEditor";
        Name = "Open Daylog";
        Icon = DaylogIcons.CommandPalette;
    }

    public override ICommandResult Invoke()
    {
        try
        {
            _state.Settings.Reload();
            if (!_state.Settings.IsConfigured)
            {
                var pickResult = new PickFolderCommand(_state).Invoke();
                _state.Settings.Reload();
                if (!_state.Settings.IsConfigured)
                {
                    return pickResult;
                }
            }

            _state.Markdown.EnsureTodayFile();
            OpenTodayCommand.LaunchEditor();
            return OpenTodayCommand.Toast("Opened Daylog editor.");
        }
        catch (Exception ex)
        {
            return OpenTodayCommand.Toast(ex.Message);
        }
    }
}

internal sealed partial class OpenTodayCommand : InvokableCommand
{
    private readonly DaylogState _state;

    public OpenTodayCommand(DaylogState state)
    {
        _state = state;
        Id = "daylog.dock.openToday";
        Name = "Open Today";
        Icon = new IconInfo("\uE8E5");
    }

    public override ICommandResult Invoke()
    {
        try
        {
            _state.SelectToday();
            var filePath = _state.Markdown.EnsureTodayFile();
            ShellOpen(filePath);
            return Toast("Opened today's Daylog.");
        }
        catch (Exception ex)
        {
            return Toast(ex.Message);
        }
    }

    internal static void ShellOpen(string target) => DaylogShell.Open(target);

    internal static void LaunchEditor()
    {
        if (EditorSingleInstance.TryWakeRunningEditor())
        {
            return;
        }

        var editorPath = ResolveEditorPath();
        Process.Start(new ProcessStartInfo(editorPath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(editorPath)!,
        });
    }

    internal static string ResolveEditorPath()
    {
        foreach (var candidate in GetEditorPathCandidates())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Daylog editor not found. Install the standalone app: .\\scripts\\install-daylog-standalone.ps1");
    }

    internal static IEnumerable<string> GetEditorPathCandidates()
    {
        var standaloneDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "Daylog");
        yield return Path.Combine(standaloneDir, "DaylogDockExtension.Editor.exe");

        var hostDir = Path.GetDirectoryName(Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName);
        if (!string.IsNullOrWhiteSpace(hostDir))
        {
            yield return Path.Combine(hostDir, "DaylogDockExtension.Editor.exe");
        }
    }

    internal static CommandResult Toast(string message)
    {
        return CommandResult.ShowToast(new ToastArgs { Message = message, Result = CommandResult.KeepOpen() });
    }
}

internal sealed partial class RevealFolderCommand : InvokableCommand
{
    private readonly DaylogState _state;

    public RevealFolderCommand(DaylogState state)
    {
        _state = state;
        Id = "daylog.dock.revealFolder";
        Name = "Reveal Folder";
        Icon = new IconInfo("\uE838");
    }

    public override ICommandResult Invoke()
    {
        try
        {
            var filePath = _state.Markdown.EnsureDailyFile(_state.SelectedDate);
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true });
            return OpenTodayCommand.Toast("Revealed Daylog file.");
        }
        catch (Exception ex)
        {
            return OpenTodayCommand.Toast(ex.Message);
        }
    }
}

internal sealed partial class OpenSelectedLogCommand : InvokableCommand
{
    private readonly DaylogState _state;

    public OpenSelectedLogCommand(DaylogState state)
    {
        _state = state;
        Id = "daylog.dock.openSelectedLog";
        Name = "Open Log";
        Icon = new IconInfo("\uE8E5");
    }

    public override ICommandResult Invoke()
    {
        try
        {
            var filePath = _state.Markdown.EnsureDailyFile(_state.SelectedDate);
            OpenTodayCommand.ShellOpen(filePath);
            return OpenTodayCommand.Toast($"Opened {_state.SelectedDate:yyyy-MM-dd}.");
        }
        catch (Exception ex)
        {
            return OpenTodayCommand.Toast(ex.Message);
        }
    }
}

internal sealed partial class MoveDayCommand : InvokableCommand
{
    private readonly DaylogState _state;
    private readonly int _days;
    private readonly Action _refresh;

    public MoveDayCommand(DaylogState state, int days, Action refresh)
    {
        _state = state;
        _days = days;
        _refresh = refresh;
        Id = days < 0 ? "daylog.dock.previousDay" : "daylog.dock.nextDay";
        Name = days < 0 ? "Previous Day" : "Next Day";
        Icon = new IconInfo(days < 0 ? "\uE72B" : "\uE72A");
    }

    public override ICommandResult Invoke()
    {
        if (!_state.TryMoveSelectedDate(_days, out var message))
        {
            return OpenTodayCommand.Toast(message);
        }

        _refresh();
        return CommandResult.KeepOpen();
    }
}

internal sealed partial class SelectTodayCommand : InvokableCommand
{
    private readonly DaylogState _state;
    private readonly Action _refresh;

    public SelectTodayCommand(DaylogState state, Action refresh)
    {
        _state = state;
        _refresh = refresh;
        Id = "daylog.dock.selectToday";
        Name = "Today";
        Icon = new IconInfo("\uE787");
    }

    public override ICommandResult Invoke()
    {
        _state.SelectToday();
        _state.Markdown.EnsureTodayFile();
        _refresh();
        return CommandResult.KeepOpen();
    }
}

internal sealed partial class UseSuggestedFolderCommand : InvokableCommand
{
    private readonly DaylogState _state;

    public UseSuggestedFolderCommand(DaylogState state)
    {
        _state = state;
        Id = "daylog.dock.useSuggestedFolder";
        Name = "Use Suggested Folder";
        Icon = new IconInfo("\uE8FB");
    }

    public override ICommandResult Invoke()
    {
        try
        {
            var folder = DaylogSettingsStore.SuggestedRootFolder();
            Directory.CreateDirectory(folder);
            _state.Settings.SaveRootFolder(folder);
            _state.Markdown.EnsureTodayFile();
            Process.Start(new ProcessStartInfo(OpenTodayCommand.ResolveEditorPath()) { UseShellExecute = true });
            return OpenTodayCommand.Toast($"Daylog folder set. Opened editor at {folder}.");
        }
        catch (Exception ex)
        {
            return OpenTodayCommand.Toast(ex.Message);
        }
    }
}

internal sealed partial class PickFolderCommand : InvokableCommand
{
    private readonly DaylogState _state;

    public PickFolderCommand(DaylogState state)
    {
        _state = state;
        Id = "daylog.dock.pickFolder";
        Name = "Choose Folder";
        Icon = DaylogIcons.CommandPalette;
    }

    public override ICommandResult Invoke()
    {
        try
        {
            return InvokeOnStaThread();
        }
        catch (Exception ex)
        {
            return OpenTodayCommand.Toast(ex.Message);
        }
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    private ICommandResult InvokeOnStaThread()
    {
        ICommandResult? result = null;
        Exception? failure = null;

        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                result = PickAndSaveFolder();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }

        return result ?? CommandResult.KeepOpen();
    }

    private CommandResult PickAndSaveFolder()
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        picker.FileTypeFilter.Add("*");

        var window = GetForegroundWindow();
        if (window != IntPtr.Zero)
        {
            InitializeWithWindow.Initialize(picker, window);
        }

        var folder = picker.PickSingleFolderAsync().AsTask().GetAwaiter().GetResult();
        if (folder is null)
        {
            return CommandResult.KeepOpen();
        }

        _state.Settings.SaveRootFolder(folder.Path);
        _state.Markdown.EnsureTodayFile();
        Process.Start(new ProcessStartInfo(OpenTodayCommand.ResolveEditorPath()) { UseShellExecute = true });
        return OpenTodayCommand.Toast($"Daylog folder saved. Opened editor for {folder.Path}.");
    }
}

internal sealed partial class OpenFileCommand : InvokableCommand
{
    private readonly string _path;

    public OpenFileCommand(string path)
    {
        _path = path;
        Id = "daylog.dock.openFile." + System.IO.Path.GetFileNameWithoutExtension(path);
        Name = "Open";
        Icon = new IconInfo("\uE8E5");
    }

    public override ICommandResult Invoke()
    {
        try
        {
            OpenTodayCommand.ShellOpen(_path);
            return CommandResult.KeepOpen();
        }
        catch (Exception ex)
        {
            return OpenTodayCommand.Toast(ex.Message);
        }
    }
}
