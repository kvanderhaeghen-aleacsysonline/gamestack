using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Gamestack.Platform;

/// <summary>
/// Thin wrapper over the Win32 <c>ShutdownBlockReasonCreate/Destroy</c> APIs. Registering a reason
/// makes Windows list the app on the "these apps are preventing shutdown" screen; combined with a
/// window that returns FALSE from <c>WM_QUERYENDSESSION</c>, it lets the app hold shutdown so the
/// user can deal with unpushed changes. All calls are no-ops off Windows.
/// </summary>
public static class ShutdownBlockReason
{
    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShutdownBlockReasonCreate(nint hWnd, string reason);

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShutdownBlockReasonDestroy(nint hWnd);

    /// <summary>Register a shutdown-block reason for the given window handle.</summary>
    public static void Create(nint hWnd, string reason)
    {
        if (!OperatingSystem.IsWindows() || hWnd == 0) return;
        try { ShutdownBlockReasonCreate(hWnd, reason); } catch (DllNotFoundException) { /* non-Windows */ }
    }

    /// <summary>Clear any shutdown-block reason for the given window handle.</summary>
    public static void Destroy(nint hWnd)
    {
        if (!OperatingSystem.IsWindows() || hWnd == 0) return;
        try { ShutdownBlockReasonDestroy(hWnd); } catch (DllNotFoundException) { /* non-Windows */ }
    }
}
