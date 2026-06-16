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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (initialized)
        {
            return;
        }

        initialized = true;
        await reminderService.InitializeAsync();
        UpdateReminderCount();
        ResetDueTime();
    }

    async void OnAddReminderClicked(object sender, EventArgs e)
    {
        try
        {
            var dueDateTime = SelectedDate.Date.Add(SelectedTime);
            await reminderService.AddAsync(NewTitle, NewNotes, SelectedTag, dueDateTime);
            ClearForm();
            UpdateReminderCount();
        }
        catch (ArgumentException ex)
        {
            await DisplayAlert("Missing title", ex.Message, "OK");
        }
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