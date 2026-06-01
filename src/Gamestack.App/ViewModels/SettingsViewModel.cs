using Gamestack.App.Services;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Settings;
using Gamestack.Core.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gamestack.App.ViewModels;

/// <summary>Edits and persists <see cref="AppSettings"/>, including the validation rules.</summary>
public partial class SettingsViewModel : ViewModelBase, IAsyncLoad
{
    private readonly ISettingsStore _settingsStore;
    private readonly INavigator _navigator;

    // Paths
    [ObservableProperty] private string? _syncedFolderRoot;
    [ObservableProperty] private string? _workspaceRoot;

    // Validation
    [ObservableProperty] private bool _validationEnabled = true;
    [ObservableProperty] private bool _requireDivisibleByFour;
    [ObservableProperty] private bool _requireSquare;
    [ObservableProperty] private bool _requirePowerOfTwo;
    [ObservableProperty] private bool _blockOnImageFailure;
    [ObservableProperty] private bool _checkSpineVersion;
    [ObservableProperty] private string? _requiredSpineVersion;
    [ObservableProperty] private bool _spineExactMatch;
    [ObservableProperty] private bool _blockOnSpineFailure;

    // Notifications
    [ObservableProperty] private bool _slackEnabled;
    [ObservableProperty] private string? _slackWebhookUrl;
    [ObservableProperty] private bool _emailEnabled;
    [ObservableProperty] private string? _smtpHost;
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private string? _smtpUser;
    [ObservableProperty] private string? _smtpPassword;
    [ObservableProperty] private string? _fromAddress;
    [ObservableProperty] private bool _smtpUseStartTls = true;

    // Behaviour
    [ObservableProperty] private bool _runOnStartup;
    [ObservableProperty] private bool _holdShutdownWithUnpushedChanges;
    [ObservableProperty] private bool _endOfDayReminderEnabled;
    [ObservableProperty] private string _endOfDayReminderTime = "17:30";

    [ObservableProperty] private string _status = "";

    public SettingsViewModel(ISettingsStore settingsStore, INavigator navigator)
    {
        _settingsStore = settingsStore;
        _navigator = navigator;
    }

    public async Task LoadAsync()
    {
        var s = await _settingsStore.LoadAsync();
        SyncedFolderRoot = s.SyncedFolderRoot;
        WorkspaceRoot = s.WorkspaceRoot;
        ValidationEnabled = s.Validation.Enabled;
        RequireDivisibleByFour = s.Validation.RequireDivisibleByFour;
        RequireSquare = s.Validation.RequireSquare;
        RequirePowerOfTwo = s.Validation.RequirePowerOfTwo;
        BlockOnImageFailure = s.Validation.BlockOnImageFailure;
        CheckSpineVersion = s.Validation.CheckSpineVersion;
        RequiredSpineVersion = s.Validation.RequiredSpineVersion;
        SpineExactMatch = s.Validation.SpineMatch == SpineVersionMatch.Exact;
        BlockOnSpineFailure = s.Validation.BlockOnSpineFailure;
        SlackEnabled = s.Notifications.SlackEnabled;
        SlackWebhookUrl = s.Notifications.SlackWebhookUrl;
        EmailEnabled = s.Notifications.EmailEnabled;
        SmtpHost = s.Notifications.SmtpHost;
        SmtpPort = s.Notifications.SmtpPort;
        SmtpUser = s.Notifications.SmtpUser;
        SmtpPassword = s.Notifications.SmtpPassword;
        FromAddress = s.Notifications.FromAddress;
        SmtpUseStartTls = s.Notifications.SmtpUseStartTls;
        RunOnStartup = s.RunOnStartup;
        HoldShutdownWithUnpushedChanges = s.HoldShutdownWithUnpushedChanges;
        EndOfDayReminderEnabled = s.EndOfDayReminderEnabled;
        EndOfDayReminderTime = s.EndOfDayReminderTime;
    }

    [RelayCommand]
    private async Task Save()
    {
        var s = await _settingsStore.LoadAsync();
        s.SyncedFolderRoot = SyncedFolderRoot;
        s.WorkspaceRoot = WorkspaceRoot;
        s.Validation = new ValidationSettings
        {
            Enabled = ValidationEnabled,
            RequireDivisibleByFour = RequireDivisibleByFour,
            RequireSquare = RequireSquare,
            RequirePowerOfTwo = RequirePowerOfTwo,
            BlockOnImageFailure = BlockOnImageFailure,
            CheckSpineVersion = CheckSpineVersion,
            RequiredSpineVersion = RequiredSpineVersion,
            SpineMatch = SpineExactMatch ? SpineVersionMatch.Exact : SpineVersionMatch.MajorMinor,
            BlockOnSpineFailure = BlockOnSpineFailure,
        };
        s.Notifications = new NotificationSettings
        {
            SlackEnabled = SlackEnabled,
            SlackWebhookUrl = SlackWebhookUrl,
            EmailEnabled = EmailEnabled,
            SmtpHost = SmtpHost,
            SmtpPort = SmtpPort,
            SmtpUser = SmtpUser,
            SmtpPassword = SmtpPassword,
            FromAddress = FromAddress,
            SmtpUseStartTls = SmtpUseStartTls,
        };
        s.RunOnStartup = RunOnStartup;
        s.HoldShutdownWithUnpushedChanges = HoldShutdownWithUnpushedChanges;
        s.EndOfDayReminderEnabled = EndOfDayReminderEnabled;
        s.EndOfDayReminderTime = EndOfDayReminderTime;

        await _settingsStore.SaveAsync(s);
        Status = "Saved.";
        await _navigator.ApplyConfigurationAsync();
    }
}
