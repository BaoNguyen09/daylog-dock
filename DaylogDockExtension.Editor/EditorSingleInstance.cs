using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DaylogDockExtension;

internal static class EditorSingleInstance
{
    internal const string ShowEventName = @"Local\DaylogDock.Editor.Show";
    private const string MutexName = @"Local\DaylogDock.Editor";
    private const int AllowAnyProcess = -1;

    public static bool TryClaim(out Mutex mutex)
    {
        mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        return createdNew;
    }

    public static bool TryWakeRunningEditor()
    {
        if (!IsAnotherInstanceRunning())
        {
            return false;
        }

        return SignalExistingEditor();
    }

    public static bool TrySignalRunningInstance() => TryWakeRunningEditor();

    private static bool IsAnotherInstanceRunning()
    {
        try
        {
            using var mutex = Mutex.OpenExisting(MutexName);
            return true;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
    }

    public static bool SignalExistingEditor()
    {
        if (!IsAnotherInstanceRunning())
        {
            return false;
        }

        AllowSetForegroundWindow(AllowAnyProcess);

        try
        {
            using var showEvent = EventWaitHandle.OpenExisting(ShowEventName);
            showEvent.Set();
            return true;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);
}
