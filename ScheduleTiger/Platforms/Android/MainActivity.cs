using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;

namespace ScheduleTiger
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Window is not null)
            {
                var brandBlack = Android.Graphics.Color.ParseColor("#0B0B0B");
                Window.SetStatusBarColor(brandBlack);
                Window.SetNavigationBarColor(brandBlack);
            }
        }
    }
}
