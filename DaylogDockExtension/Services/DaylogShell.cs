using System.Diagnostics;

namespace DaylogDockExtension;

internal static class DaylogShell
{
    internal static void Open(string target)
    {
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }
}
