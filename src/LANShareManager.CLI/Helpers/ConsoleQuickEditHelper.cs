using System.Runtime.InteropServices;

namespace LANShareManager.CLI.Helpers;

/// <summary>
/// Disables QuickEdit mode in the Windows Console.
/// When QuickEdit is enabled, clicking with the mouse inside the terminal pauses
/// the execution of the process (freezes the thread) until Enter is pressed.
/// Disabling it prevents accidental freezes during interactive wizard and diagnostics.
/// (Inspired by MAS console robustness patterns)
/// </summary>
public static class ConsoleQuickEditHelper
{
    private const int STD_INPUT_HANDLE = -10;
    private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
    private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    public static void DisableQuickEdit()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        try
        {
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);
            if (consoleHandle == IntPtr.Zero || consoleHandle == new IntPtr(-1))
            {
                return;
            }

            if (GetConsoleMode(consoleHandle, out uint mode))
            {
                // Clear the ENABLE_QUICK_EDIT_MODE flag and set ENABLE_EXTENDED_FLAGS
                mode &= ~ENABLE_QUICK_EDIT_MODE;
                mode |= ENABLE_EXTENDED_FLAGS;
                SetConsoleMode(consoleHandle, mode);
            }
        }
        catch
        {
            // Silently continue if running in non-standard console or redirected stream
        }
    }
}
