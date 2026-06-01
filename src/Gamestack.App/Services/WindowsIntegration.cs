using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Win32;
using Gamestack.App.Views;
using Gamestack.Platform;

namespace Gamestack.App.Services;

/// <summary>
/// Owns the desktop "ambience" features: a system-tray icon, close-to-tray (so the app keeps running
/// in the background), an end-of-day reminder for unpushed changes, and a best-effort shutdown hold.
/// All Windows-specific calls are guarded; on other platforms the integration is largely inert.
/// </summary>
public sealed class WindowsIntegration
{
    private const uint WM_QUERYENDSESSION = 0x0011;

    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly Window _window;
    private readonly WorkspaceSession _session;

    private TrayIcon? _tray;
    private DispatcherTimer? _timer;
    private int _tick;
    private DateOnly? _remindedOn;
    private bool _blocking;
    private bool _quitting;

    public WindowsIntegration(IClassicDesktopStyleApplicationLifetime desktop, Window window, WorkspaceSession session)
    {
        _desktop = desktop;
        _window = window;
        _session = session;
    }

    private nint Hwnd => _window.TryGetPlatformHandle()?.Handle ?? 0;

    /// <summary>Set up the tray, close-to-tray, shutdown-veto hook, and the background timer.</summary>
    public void Start()
    {
        SetupTray();

        _window.Closing += OnWindowClosing;

        if (OperatingSystem.IsWindows())
            Win32Properties.AddWndProcHookCallback(_window, WndProcHook);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _timer.Tick += async (_, _) => await OnTickAsync();
        _timer.Start();
    }

    private void SetupTray()
    {
        _tray = new TrayIcon { ToolTipText = "Gamestack" };
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://Gamestack.App/Assets/avalonia-logo.ico"));
            _tray.Icon = new WindowIcon(stream);
        }
        catch { /* icon is optional */ }

        var open = new NativeMenuItem("Open Gamestack");
        open.Click += (_, _) => ShowMainWindow();
        var quit = new NativeMenuItem("Quit");
        quit.Click += (_, _) => Quit();

        var menu = new NativeMenu();
        menu.Items.Add(open);
        menu.Items.Add(quit);
        _tray.Menu = menu;
        _tray.Clicked += (_, _) => ShowMainWindow();

        TrayIcon.SetIcons(Application.Current!, new TrayIcons { _tray });
    }

    private void ShowMainWindow()
    {
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_quitting) return;
        // Hide to tray instead of exiting, so reminders / shutdown-hold keep working.
        e.Cancel = true;
        _window.Hide();
    }

    private void Quit()
    {
        _quitting = true;
        UpdateShutdownGuard(0);
        _desktop.Shutdown();
    }

    private nint WndProcHook(nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WM_QUERYENDSESSION && _blocking)
        {
            handled = true;
            return 0; // FALSE → veto the shutdown so the user can deal with unpushed changes
        }
        return 0;
    }

    private async Task OnTickAsync()
    {
        var settings = _session.Settings;
        if (!_session.IsConfigured)
        {
            UpdateShutdownGuard(0);
            return;
        }

        _tick++;

        // Refresh the shutdown guard roughly every 5 minutes (the scan hashes files, so don't do it every tick).
        if (settings.HoldShutdownWithUnpushedChanges)
        {
            if (_tick % 5 == 1)
                UpdateShutdownGuard(await CountUnpushedAsync());
        }
        else
        {
            UpdateShutdownGuard(0);
        }

        // End-of-day reminder: once per day at the configured time, if anything is unpushed.
        if (settings.EndOfDayReminderEnabled && TimeMatchesNow(settings.EndOfDayReminderTime))
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (_remindedOn != today)
            {
                _remindedOn = today;
                var count = await CountUnpushedAsync();
                if (count > 0)
                {
                    ToastWindow.Show(
                        $"{count} unpushed change(s)",
                        "You have asset changes that haven't been pushed yet. Push them before you leave?",
                        ShowMainWindow);
                }
            }
        }
    }

    private async Task<int> CountUnpushedAsync()
    {
        try
        {
            var changes = await _session.ChangeDetector.DetectAsync(_session.Settings.WorkspaceRoot!, _session.Settings.Validation);
            return changes.Count;
        }
        catch
        {
            return 0;
        }
    }

    private void UpdateShutdownGuard(int unpushedCount)
    {
        if (_tray is not null)
            _tray.ToolTipText = unpushedCount > 0 ? $"Gamestack — {unpushedCount} unpushed change(s)" : "Gamestack";

        var hwnd = Hwnd;
        if (hwnd == 0) return;

        if (unpushedCount > 0)
        {
            ShutdownBlockReason.Create(hwnd, $"Gamestack has {unpushedCount} unpushed change(s).");
            _blocking = true;
        }
        else
        {
            ShutdownBlockReason.Destroy(hwnd);
            _blocking = false;
        }
    }

    private static bool TimeMatchesNow(string hhmm)
    {
        if (!TimeOnly.TryParse(hhmm, out var target))
            return false;
        var now = TimeOnly.FromDateTime(DateTime.Now);
        return now.Hour == target.Hour && now.Minute == target.Minute;
    }
}
