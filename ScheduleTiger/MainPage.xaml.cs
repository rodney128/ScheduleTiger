using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ScheduleTiger.Models;
using ScheduleTiger.Services;

namespace ScheduleTiger;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    readonly ReminderService reminderService;
    string newTitle = string.Empty;
    string newNotes = string.Empty;
    string selectedTag = ReminderService.Tags.Last();
    DateTime selectedDate = DateTime.Today;
    TimeSpan selectedTime = DateTime.Now.AddHours(1).TimeOfDay;
    string reminderCountText = "0 reminders";
    string statusMessage = string.Empty;
    bool initialized;

    public MainPage(ReminderService reminderService)
    {
        InitializeComponent();
        this.reminderService = reminderService;
        BindingContext = this;
        UpdateReminderCount();
        ResetDueTime();
    }

    public ObservableCollection<ReminderItem> Reminders => reminderService.Reminders;

    public IReadOnlyList<string> TagOptions => ReminderService.Tags;

    public string NewTitle
    {
        get => newTitle;
        set => SetField(ref newTitle, value);
    }

    public string NewNotes
    {
        get => newNotes;
        set => SetField(ref newNotes, value);
    }

    public string SelectedTag
    {
        get => selectedTag;
        set => SetField(ref selectedTag, value);
    }

    public DateTime SelectedDate
    {
        get => selectedDate;
        set => SetField(ref selectedDate, value);
    }

    public TimeSpan SelectedTime
    {
        get => selectedTime;
        set => SetField(ref selectedTime, value);
    }

    public string ReminderCountText
    {
        get => reminderCountText;
        set => SetField(ref reminderCountText, value);
    }

    public string SavedRemindersHeader => $"Saved reminders ({Reminders.Count})";

    public bool HasReminders => Reminders.Count > 0;

    public bool HasNoReminders => Reminders.Count == 0;

    public string StatusMessage
    {
        get => statusMessage;
        set => SetField(ref statusMessage, value);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (initialized)
        {
            await RefreshPermissionStatusAsync();
            return;
        }

        initialized = true;
        await reminderService.InitializeAsync();
        UpdateReminderCount();
        ResetDueTime();
        await RefreshPermissionStatusAsync();
    }

    async void OnAddReminderClicked(object sender, EventArgs e)
    {
        try
        {
            var dueDateTime = SelectedDate.Date.Add(SelectedTime);
            var result = await reminderService.AddAsync(NewTitle, NewNotes, SelectedTag, dueDateTime);
            ClearForm();
            UpdateReminderCount();
            StatusMessage = string.IsNullOrWhiteSpace(reminderService.LastNotificationStatus)
                ? (result.NotificationScheduled ? "Notification scheduled" : "Alarm permission still needed")
                : reminderService.LastNotificationStatus;

            if (!result.NotificationScheduled && !string.IsNullOrWhiteSpace(StatusMessage))
            {
                await DisplayAlert("Reminder saved", StatusMessage, "OK");
            }
        }
        catch (ArgumentException ex)
        {
            await DisplayAlert("Missing title", ex.Message, "OK");
        }
    }

    async void OnEnableNotificationsClicked(object sender, EventArgs e)
    {
        var granted = await reminderService.RequestNotificationPermissionAsync();
        StatusMessage = granted ? "Notifications allowed" : "Notifications blocked";
        await RefreshPermissionStatusAsync();
    }

    async void OnSendTestNotificationNowClicked(object sender, EventArgs e)
    {
        var granted = await reminderService.RequestNotificationPermissionAsync();
        if (!granted)
        {
            StatusMessage = "Notifications blocked";
            await RefreshPermissionStatusAsync();
            return;
        }

        var shown = await reminderService.ShowTestNotificationNowAsync();
        StatusMessage = reminderService.LastNotificationStatus;
        if (!shown && !string.IsNullOrWhiteSpace(StatusMessage))
        {
            await DisplayAlert("Test notification", StatusMessage, "OK");
        }
    }

    async void OnTestNotificationClicked(object sender, EventArgs e)
    {
        var granted = await reminderService.RequestNotificationPermissionAsync();
        if (!granted)
        {
            StatusMessage = "Notifications blocked";
            await RefreshPermissionStatusAsync();
            return;
        }

        var scheduled = await reminderService.ScheduleTestNotificationAsync(TimeSpan.FromSeconds(30));
        StatusMessage = reminderService.LastNotificationStatus;
        if (!scheduled && !string.IsNullOrWhiteSpace(StatusMessage))
        {
            await DisplayAlert("Test notification", StatusMessage, "OK");
        }
    }

    async void OnOpenNotificationSettingsClicked(object sender, EventArgs e)
    {
        await reminderService.OpenNotificationSettingsAsync();
        StatusMessage = "Open notification settings, then return to ScheduleTiger.";
    }

    async void OnOpenChannelSettingsClicked(object sender, EventArgs e)
    {
        await reminderService.OpenChannelSettingsAsync();
        StatusMessage = "Check that the channel is allowed and set to Urgent/High, then return to ScheduleTiger.";
    }

    async void OnOpenAlarmPermissionClicked(object sender, EventArgs e)
    {
        await reminderService.OpenAlarmPermissionSettingsAsync();
        StatusMessage = "Turn on Allow permission, then return to ScheduleTiger.";
    }

    async void OnReminderCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.BindingContext is not ReminderItem reminder)
        {
            return;
        }

        await reminderService.SetDoneAsync(reminder, e.Value);
        UpdateReminderCount();
    }

    async void OnDeleteReminderClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not ReminderItem reminder)
        {
            return;
        }

        await reminderService.DeleteAsync(reminder);
        UpdateReminderCount();
    }

    void ClearForm()
    {
        NewTitle = string.Empty;
        NewNotes = string.Empty;
        ResetDueTime();
    }

    void ResetDueTime()
    {
        var due = DateTime.Now.AddHours(1);
        SelectedDate = due.Date;
        SelectedTime = new TimeSpan(due.Hour, due.Minute, 0);
    }

    void UpdateReminderCount()
    {
        var count = Reminders.Count;
        ReminderCountText = count == 1 ? "1 reminder" : $"{count} reminders";
        NotifyPropertyChanged(nameof(SavedRemindersHeader));
        NotifyPropertyChanged(nameof(HasReminders));
        NotifyPropertyChanged(nameof(HasNoReminders));
    }

    async Task RefreshPermissionStatusAsync()
    {
        var notificationsGranted = await reminderService.IsNotificationPermissionGrantedAsync();
#if ANDROID
        var exactAllowed = reminderService.CanScheduleExactAlarms();
#else
        var exactAllowed = true;
#endif

        if (notificationsGranted && exactAllowed)
        {
            StatusMessage = "Reminder notifications ready";
        }
        else if (!notificationsGranted)
        {
            StatusMessage = "Notifications blocked";
        }
        else
        {
            StatusMessage = "Alarm permission still needed";
        }
    }

    bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        NotifyPropertyChanged(propertyName);
        return true;
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}