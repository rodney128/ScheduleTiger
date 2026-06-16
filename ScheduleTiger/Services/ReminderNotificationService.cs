using ScheduleTiger.Models;

namespace ScheduleTiger.Services;

public sealed class ReminderNotificationService
{
    public async Task<bool> RequestNotificationPermissionAsync()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var permission = await Permissions.RequestAsync<Permissions.PostNotifications>();
            EnsureChannel();
            return permission == PermissionStatus.Granted;
        }

        EnsureChannel();
        return true;
#else
        return true;
#endif
    }

    public Task<bool> IsNotificationPermissionGrantedAsync()
    {
#if ANDROID
        return IsNotificationPermissionGrantedCoreAsync();
#else
        return Task.FromResult(true);
#endif
    }

    public Task<bool> ScheduleAsync(ReminderItem reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);

#if ANDROID
        return ScheduleReminderAlarmAsync(reminder);
#else
        LastNotificationStatus = "Notification scheduled.";
        return Task.FromResult(true);
#endif
    }

    public Task<bool> ScheduleTestNotificationAsync(TimeSpan delay)
    {
#if ANDROID
        return ScheduleTestAlarmAsync(delay);
#else
        LastNotificationStatus = "Test notification scheduled.";
        return Task.FromResult(true);
#endif
    }

    public async Task<bool> ShowTestNotificationNowAsync()
    {
#if ANDROID
        var notificationsAllowed = await IsNotificationPermissionGrantedCoreAsync();
        if (!notificationsAllowed)
        {
            LastNotificationStatus = "Notifications blocked.";
            return false;
        }

        var context = global::Android.App.Application.Context ?? throw new InvalidOperationException("Android application context is unavailable.");
        Platforms.Android.ReminderNotificationReceiver.Show(
            context,
            GetRequestCode(TestRequestCode),
            "ScheduleTiger",
            "Test reminder notification");

        LastNotificationStatus = "Test notification sent.";
        return true;
#else
        await Task.CompletedTask;
        LastNotificationStatus = "Test notification sent.";
        return true;
#endif
    }

    public Task OpenNotificationSettingsAsync()
    {
#if ANDROID
        var context = global::Android.App.Application.Context;
        if (context is not null)
        {
            var intent = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionAppNotificationSettings);
            intent.PutExtra(global::Android.Provider.Settings.ExtraAppPackage, context.PackageName);
            intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
#endif
        return Task.CompletedTask;
    }

    public Task OpenChannelSettingsAsync()
    {
#if ANDROID
        var context = global::Android.App.Application.Context;
        if (context is not null)
        {
            EnsureChannel();

            global::Android.Content.Intent intent;
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                intent = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionChannelNotificationSettings);
                intent.PutExtra(global::Android.Provider.Settings.ExtraAppPackage, context.PackageName);
                intent.PutExtra(global::Android.Provider.Settings.ExtraChannelId, Platforms.Android.ReminderNotificationReceiver.ChannelId);
            }
            else
            {
                intent = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionAppNotificationSettings);
                intent.PutExtra(global::Android.Provider.Settings.ExtraAppPackage, context.PackageName);
            }

            intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
#endif
        return Task.CompletedTask;
    }

    public Task OpenAlarmPermissionSettingsAsync()
    {
#if ANDROID
        var context = global::Android.App.Application.Context;
        if (context is not null)
        {
            var intent = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionRequestScheduleExactAlarm);
            intent.SetData(global::Android.Net.Uri.Parse($"package:{context.PackageName}"));
            intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
#endif
        return Task.CompletedTask;
    }

    public bool CanScheduleExactAlarms()
    {
#if ANDROID
        var context = global::Android.App.Application.Context;
        var alarmManager = context is null
            ? null
            : (global::Android.App.AlarmManager?)context.GetSystemService(global::Android.Content.Context.AlarmService);
        return alarmManager?.CanScheduleExactAlarms() ?? false;
#else
        return true;
#endif
    }

    public string LastNotificationStatus { get; private set; } = string.Empty;

    public Task CancelAsync(ReminderItem reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);

#if ANDROID
        var context = global::Android.App.Application.Context;
        if (context is not null)
        {
            var alarmManager = (global::Android.App.AlarmManager?)context.GetSystemService(global::Android.Content.Context.AlarmService);
            if (alarmManager is not null)
            {
                var intent = new global::Android.Content.Intent(context, typeof(Platforms.Android.ReminderNotificationReceiver));
                var pendingIntent = global::Android.App.PendingIntent.GetBroadcast(
                    context,
                    GetRequestCode(reminder.Id),
                    intent,
                    global::Android.App.PendingIntentFlags.NoCreate | PendingIntentFlagsForApiLevel());

                if (pendingIntent is not null)
                {
                    alarmManager.Cancel(pendingIntent);
                    pendingIntent.Cancel();
                }
            }
        }
#endif
        return Task.CompletedTask;
    }

    public async Task RescheduleAsync(IEnumerable<ReminderItem> reminders)
    {
        ArgumentNullException.ThrowIfNull(reminders);

        foreach (var reminder in reminders)
        {
            await ScheduleAsync(reminder);
        }
    }

#if ANDROID
    const string TestRequestCode = "scheduletiger.test.notification";

    async Task<bool> IsNotificationPermissionGrantedCoreAsync()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            return permission == PermissionStatus.Granted;
        }

        return true;
    }

    async Task<bool> ScheduleReminderAlarmAsync(ReminderItem reminder)
    {
        EnsureChannel();

        var context = global::Android.App.Application.Context ?? throw new InvalidOperationException("Android application context is unavailable.");
        var alarmManager = (global::Android.App.AlarmManager?)context.GetSystemService(global::Android.Content.Context.AlarmService)
            ?? throw new InvalidOperationException("AlarmManager is unavailable.");

        var notificationsAllowed = await IsNotificationPermissionGrantedCoreAsync();
        if (reminder.IsDone || reminder.DueDateTime <= DateTime.Now)
        {
            LastNotificationStatus = notificationsAllowed ? "Reminder saved. Overdue." : "Reminder saved. Notifications blocked.";
            return false;
        }

        var intent = CreateIntent(context, reminder.Id, reminder.Title, reminder.Notes);
        var flags = global::Android.App.PendingIntentFlags.UpdateCurrent | PendingIntentFlagsForApiLevel();
        var pendingIntent = global::Android.App.PendingIntent.GetBroadcast(context, GetRequestCode(reminder.Id), intent, flags);

        var triggerAtMillis = new DateTimeOffset(reminder.DueDateTime).ToUnixTimeMilliseconds();
        ScheduleAllowWhileIdle(alarmManager, triggerAtMillis, pendingIntent);

        LastNotificationStatus = notificationsAllowed ? "Notification scheduled." : "Notifications blocked.";
        return notificationsAllowed;
    }

    async Task<bool> ScheduleTestAlarmAsync(TimeSpan delay)
    {
        EnsureChannel();

        var notificationsAllowed = await IsNotificationPermissionGrantedCoreAsync();
        if (!notificationsAllowed)
        {
            LastNotificationStatus = "Notifications blocked.";
            return false;
        }

        var context = global::Android.App.Application.Context ?? throw new InvalidOperationException("Android application context is unavailable.");
        var alarmManager = (global::Android.App.AlarmManager?)context.GetSystemService(global::Android.Content.Context.AlarmService)
            ?? throw new InvalidOperationException("AlarmManager is unavailable.");

        var scheduledAt = DateTime.Now.Add(delay);
        var intent = CreateIntent(context, TestRequestCode, "ScheduleTiger", "Test reminder notification");
        var flags = global::Android.App.PendingIntentFlags.UpdateCurrent | PendingIntentFlagsForApiLevel();
        var pendingIntent = global::Android.App.PendingIntent.GetBroadcast(context, GetRequestCode(TestRequestCode), intent, flags);

        var triggerAtMillis = new DateTimeOffset(scheduledAt).ToUnixTimeMilliseconds();
        ScheduleAllowWhileIdle(alarmManager, triggerAtMillis, pendingIntent);
        LastNotificationStatus = $"Test notification scheduled for {scheduledAt:t}";
        return true;
    }

    static global::Android.Content.Intent CreateIntent(global::Android.Content.Context context, string reminderId, string title, string body)
    {
        var intent = new global::Android.Content.Intent(context, typeof(Platforms.Android.ReminderNotificationReceiver));
        intent.PutExtra(Platforms.Android.ReminderNotificationReceiver.ExtraReminderId, reminderId);
        intent.PutExtra(Platforms.Android.ReminderNotificationReceiver.ExtraTitle, title);
        intent.PutExtra(Platforms.Android.ReminderNotificationReceiver.ExtraBody, body);
        return intent;
    }

    // Schedules an alarm that still fires while the device is in Doze.
    // Uses exact delivery only when the exact-alarm permission is already granted
    // (API 31+), so near-future reminders alert on time without requiring
    // SCHEDULE_EXACT_ALARM. Falls back to inexact allow-while-idle otherwise.
    static void ScheduleAllowWhileIdle(global::Android.App.AlarmManager alarmManager, long triggerAtMillis, global::Android.App.PendingIntent pendingIntent)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            var exactAllowed = !OperatingSystem.IsAndroidVersionAtLeast(31) || alarmManager.CanScheduleExactAlarms();
            if (exactAllowed)
            {
                alarmManager.SetExactAndAllowWhileIdle(global::Android.App.AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            }
            else
            {
                alarmManager.SetAndAllowWhileIdle(global::Android.App.AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            }

            return;
        }

        alarmManager.Set(global::Android.App.AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
    }

    static global::Android.App.PendingIntentFlags PendingIntentFlagsForApiLevel()
    {
        return OperatingSystem.IsAndroidVersionAtLeast(23)
            ? global::Android.App.PendingIntentFlags.Immutable
            : 0;
    }

    static void EnsureChannel()
    {
        var context = global::Android.App.Application.Context;
        if (context is not null)
        {
            Platforms.Android.ReminderNotificationReceiver.EnsureChannel(context);
        }
    }

    private static int GetRequestCode(string value)
    {
        if (int.TryParse(value, out var requestCode))
        {
            return requestCode;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        unchecked
        {
            const int offsetBasis = unchecked((int)2166136261);
            const int prime = 16777619;
            int hash = offsetBasis;

            foreach (char c in value)
            {
                hash ^= c;
                hash *= prime;
            }

            return hash;
        }
    }
#endif
}
