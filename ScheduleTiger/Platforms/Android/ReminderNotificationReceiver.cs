using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace ScheduleTiger.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class ReminderNotificationReceiver : BroadcastReceiver
{
    public const string ExtraReminderId = "scheduleTiger.reminderId";
    public const string ExtraTitle = "scheduleTiger.title";
    public const string ExtraBody = "scheduleTiger.body";
    const string ChannelId = "scheduletiger.reminders";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null)
        {
            return;
        }

        var reminderId = intent.GetStringExtra(ExtraReminderId) ?? string.Empty;
        var title = intent.GetStringExtra(ExtraTitle) ?? "ScheduleTiger reminder";
        var body = intent.GetStringExtra(ExtraBody) ?? string.Empty;

        EnsureChannel(context);

        var notification = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetContentTitle($"ScheduleTiger reminder")
            .SetContentText(string.IsNullOrWhiteSpace(body) ? title : body)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(string.IsNullOrWhiteSpace(body) ? title : body))
            .SetAutoCancel(true)
            .SetPriority((int)NotificationPriority.High)
            .Build();

        var notificationManager = NotificationManagerCompat.From(context);
        notificationManager.Notify(GetNotificationId(reminderId), notification);
    }

    static void EnsureChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null || manager.GetNotificationChannel(ChannelId) is not null)
        {
            return;
        }

        var channel = new NotificationChannel(ChannelId, "ScheduleTiger reminders", NotificationImportance.Default)
        {
            Description = "Reminders for ScheduleTiger",
        };

        manager.CreateNotificationChannel(channel);
    }

    static int GetNotificationId(string reminderId)
    {
        if (string.IsNullOrWhiteSpace(reminderId) || reminderId.Length < 8)
        {
            return reminderId.GetHashCode(StringComparison.Ordinal);
        }

        return unchecked((int)uint.Parse(reminderId[..8], System.Globalization.NumberStyles.HexNumber));
    }
}