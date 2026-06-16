using System.Globalization;
using Android.App;
using Android.Content;
using Android.Media;
using AndroidX.Core.App;

namespace ScheduleTiger.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class ReminderNotificationReceiver : BroadcastReceiver
{
    public const string ExtraReminderId = "scheduleTiger.reminderId";
    public const string ExtraTitle = "scheduleTiger.title";
    public const string ExtraBody = "scheduleTiger.body";

    // A channel's importance cannot be raised after it is created, so a brand new id
    // is used to guarantee a high-importance alerting channel (heads-up, sound, vibration).
    public const string ChannelId = "schedule_tiger_alerts_v2";
    const string ChannelName = "ScheduleTiger reminders";
    const string ChannelDescription = "Reminders for ScheduleTiger";

    static readonly long[] VibrationPattern = { 0, 400, 200, 400 };

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null)
        {
            return;
        }

        var reminderId = intent.GetStringExtra(ExtraReminderId) ?? string.Empty;
        var title = intent.GetStringExtra(ExtraTitle) ?? "ScheduleTiger reminder";
        var body = intent.GetStringExtra(ExtraBody) ?? string.Empty;

        Show(context, GetNotificationId(reminderId), title, body);
    }

    // Builds and posts a high-importance, alerting notification on the shared channel.
    // Shared by scheduled alarms and the immediate "test now" path.
    public static void Show(Context context, int notificationId, string title, string body)
    {
        ArgumentNullException.ThrowIfNull(context);

        EnsureChannel(context);

        var text = string.IsNullOrWhiteSpace(body) ? title : body;

        var notification = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetContentTitle(title)
            .SetContentText(text)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(text))
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetCategory(NotificationCompat.CategoryReminder)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetDefaults((int)NotificationDefaults.All)
            .SetVibrate(VibrationPattern)
            .SetAutoCancel(true)
            .Build();

        NotificationManagerCompat.From(context).Notify(notificationId, notification);
    }

    // Creates the shared high-importance channel (default sound + vibration) once.
    public static void EnsureChannel(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null || manager.GetNotificationChannel(ChannelId) is not null)
        {
            return;
        }

        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High)
        {
            Description = ChannelDescription,
            LockscreenVisibility = NotificationVisibility.Public,
        };

        channel.EnableLights(true);
        channel.EnableVibration(true);

        var soundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);
        if (soundUri is not null)
        {
            var audioAttributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Notification)!
                .SetContentType(AudioContentType.Sonification)!
                .Build();
            channel.SetSound(soundUri, audioAttributes);
        }

        manager.CreateNotificationChannel(channel);
    }

    static int GetNotificationId(string reminderId)
    {
        if (string.IsNullOrWhiteSpace(reminderId))
        {
            return 0;
        }

        if (reminderId.Length >= 8
            && uint.TryParse(reminderId.AsSpan(0, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
        {
            return unchecked((int)parsed);
        }

        return reminderId.GetHashCode(StringComparison.Ordinal);
    }
}