using System.Collections.ObjectModel;
using ScheduleTiger.Models;

namespace ScheduleTiger.Services;

public sealed class ReminderService
{
    public static readonly IReadOnlyList<string> Tags = ["Health", "Boat", "RV", "Bills", "Software", "Other"];

    readonly ReminderStore store;
    readonly ReminderNotificationService notifications;
    readonly ObservableCollection<ReminderItem> reminders = [];
    bool initialized;

    public ReminderService(ReminderStore store, ReminderNotificationService notifications)
    {
        this.store = store;
        this.notifications = notifications;
    }

    public ObservableCollection<ReminderItem> Reminders => reminders;

    public async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        var loaded = await store.LoadAsync();
        UpdateCollection(loaded);
        await notifications.EnsurePermissionAsync();
        await notifications.RescheduleAsync(reminders);
        initialized = true;
    }

    public async Task<ReminderItem> AddAsync(string title, string? notes, string tag, DateTime dueDateTime)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A reminder title is required.", nameof(title));
        }

        if (!Tags.Contains(tag, StringComparer.Ordinal))
        {
            tag = "Other";
        }

        var reminder = new ReminderItem
        {
            Title = title.Trim(),
            Notes = notes?.Trim() ?? string.Empty,
            Tag = tag,
            DueDateTime = dueDateTime,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        reminders.Add(reminder);
        SortReminders();
        await SaveAndRescheduleAsync(reminder);
        return reminder;
    }

    public async Task ToggleDoneAsync(ReminderItem reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);
        reminder.IsDone = !reminder.IsDone;
        reminder.UpdatedAt = DateTime.Now;
        SortReminders();
        await SaveAsync();
        await UpdateNotificationAsync(reminder);
    }

    public async Task SetDoneAsync(ReminderItem reminder, bool isDone)
    {
        ArgumentNullException.ThrowIfNull(reminder);
        if (reminder.IsDone == isDone)
        {
            return;
        }

        reminder.IsDone = isDone;
        reminder.UpdatedAt = DateTime.Now;
        SortReminders();
        await SaveAsync();
        await UpdateNotificationAsync(reminder);
    }

    public async Task UpdateAsync(ReminderItem reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);
        reminder.UpdatedAt = DateTime.Now;
        SortReminders();
        await SaveAsync();
        await UpdateNotificationAsync(reminder);
    }

    public async Task DeleteAsync(ReminderItem reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);
        if (!reminders.Remove(reminder))
        {
            return;
        }

        await notifications.CancelAsync(reminder);
        await SaveAsync();
    }

    async Task SaveAndRescheduleAsync(ReminderItem reminder)
    {
        await SaveAsync();
        await notifications.ScheduleAsync(reminder);
    }

    async Task UpdateNotificationAsync(ReminderItem reminder)
    {
        await notifications.CancelAsync(reminder);
        if (!reminder.IsDone)
        {
            await notifications.ScheduleAsync(reminder);
        }
    }

    async Task SaveAsync()
    {
        await store.SaveAsync(reminders);
    }

    void UpdateCollection(IEnumerable<ReminderItem> items)
    {
        reminders.Clear();
        foreach (var reminder in items.OrderBy(r => r.DueDateTime).ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase))
        {
            reminders.Add(reminder);
        }
    }

    void SortReminders()
    {
        var ordered = reminders.OrderBy(r => r.DueDateTime).ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase).ToList();
        reminders.Clear();
        foreach (var reminder in ordered)
        {
            reminders.Add(reminder);
        }
    }
}