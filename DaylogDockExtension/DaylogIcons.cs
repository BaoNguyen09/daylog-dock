using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DaylogDockExtension;

internal static class DaylogIcons
{
    // IconHelpers resolves paths relative to the extension package root (exe folder).
    // Do not pass absolute paths to IconInfo — that breaks image loading in CmdPal.
    internal static IconInfo CommandPalette =>
        IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
}
