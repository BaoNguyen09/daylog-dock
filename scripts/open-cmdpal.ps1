# Opens Command Palette via default Win+Alt+Space shortcut.
$ErrorActionPreference = 'Stop'
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class DaylogKeySend {
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    private const uint KeyUp = 0x0002;
    public static void WinAltSpace() {
        const byte Win = 0x5B, Alt = 0x12, Space = 0x20;
        keybd_event(Win, 0, 0, UIntPtr.Zero);
        keybd_event(Alt, 0, 0, UIntPtr.Zero);
        keybd_event(Space, 0, 0, UIntPtr.Zero);
        keybd_event(Space, 0, KeyUp, UIntPtr.Zero);
        keybd_event(Alt, 0, KeyUp, UIntPtr.Zero);
        keybd_event(Win, 0, KeyUp, UIntPtr.Zero);
    }
}
'@
[DaylogKeySend]::WinAltSpace()
