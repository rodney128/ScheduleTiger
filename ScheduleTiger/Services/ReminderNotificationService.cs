using ScheduleTiger.Models;

namespace ScheduleTiger.Services;

public sealed class ReminderNotificationService
{
    public async Task<bool> EnsurePermissionAsync()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var permission = await Permissions.RequestAsync<Permissions.PostNotifications>();
            if (permission != PermissionStatus.Granted)
            {
                return false;
            }
        }

        EnsureChannel();
        return true;
#else
        return true;
#endif
    }

    public Task ScheduleAsync(ReminderItem reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);

#if ANDROID
        if (reminder.IsDone || reminder.DueDateTime <= DateTime.Now)
        {
            return Task.CompletedTask;
        }

        EnsureChannel();

        var context = global::Android.App.Application.Context ?? throw new InvalidOperationException("Android application context is unavailable.");
        var alarmManager = (global::Android.App.AlarmManager?)context.GetSystemService(global::Android.Content.Context.AlarmService)
            ?? throw new InvalidOperationException("AlarmManager is unavailable.");

        var intent = new global::Android.Content.Intent(context, typeof(Platforms.Android.ReminderNotificationReceiver));
        intent.PutExtra(Platforms.Android.ReminderNotificationReceiver.ExtraReminderId, reminder.Id);
        intent.PutExtra(Platforms.Android.ReminderNotificationReceiver.ExtraTitle, reminder.Title);
        intent.PutExtra(Platforms.Android.ReminderNotificationReceiver.ExtraBody, reminder.Title);

        var pendingIntent = global::Android.App.PendingIntent.GetBroadcast(
            context,
            GetRequestCode(reminder),
            intent,
            global::Android.App.PendingIntentFlags.UpdateCurrent | global::Android.App.PendingIntentFlags.Immutable);

        var triggerAtMillis = new DateTimeOffset(reminder.DueDateTime).ToUnixTimeMilliseconds();
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            alarmManager.SetExactAndAllowWhileIdle(global::Android.App.AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
        else
        {
            alarmManager.SetExact(global::Android.App.AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
#endif
        return Task.CompletedTask;
    }

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
                    GetRequestCode(reminder),
                    intent,
                    global::Android.App.PendingIntentFlags.NoCreate | global::Android.App.PendingIntentFlags.Immutable);

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
    const string ChannelId = "scheduletiger.reminders";

    static void EnsureChannel()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var context = global::Android.App.Application.Context;
            if (context is null)
            {
                return;
            }

            var manager = (global::Android.App.NotificationManager?)context.GetSystemService(global::Android.Content.Context.NotificationService);
            if (manager is null)
            {
                return;
            }

            var channel = manager.GetNotificationChannel(ChannelId);
            if (channel is null)
            {
                channel = new global::Android.App.NotificationChannel(ChannelId, "ScheduleTiger reminders", global::Android.App.NotificationImportance.Default)
                {
                    Description = "Reminders for ScheduleTiger",
                };
                manager.CreateNotificationChannel(channel);
            }
        }
    }

    static int GetRequestCode(ReminderItem reminder)
    {
        return unchecked((int)uint.Parse(reminder.Id[..8], System.Globalization.NumberStyles.HexNumber));
    }
#endif
}