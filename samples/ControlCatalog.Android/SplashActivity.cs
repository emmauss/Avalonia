using Android.App;
using Android.Content;
using Android.OS;
<<<<<<< HEAD
=======
using Application = Android.App.Application;

using Avalonia;
using Avalonia.Android;
>>>>>>> 0f6e65aa4 (android support)

namespace ControlCatalog.Android
{
    [Activity(Theme = "@style/MyTheme.Splash", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        protected override void OnResume()
        {
            base.OnResume();
            
            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }
    }
}
