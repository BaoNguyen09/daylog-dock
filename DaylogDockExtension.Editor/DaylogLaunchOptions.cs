using System;

namespace DaylogDockExtension;

internal static class DaylogLaunchOptions
{
    internal static bool Startup { get; private set; }

    internal static void Apply(string[] args)
    {
        Startup = false;
        foreach (var arg in args)
        {
            if (string.Equals(arg, "--startup", StringComparison.OrdinalIgnoreCase))
            {
                Startup = true;
            }
        }
    }
}
