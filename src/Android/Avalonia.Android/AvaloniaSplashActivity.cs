using Android.App;
using Android.OS;
using static Avalonia.Android.AvaloniaActivity;

namespace Avalonia.Android
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public abstract class AvaloniaSplashActivity : Activity
    {
        protected abstract AppBuilder CreateAppBuilder();

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var builder = CreateAppBuilder();

            var lifetime = new SingleViewLifetime();

            builder.SetupWithLifetime(lifetime);
        }
    }

    public abstract class AvaloniaSplashActivity<TApp> : AvaloniaSplashActivity where TApp : Application, new()
    {
        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder.UseAndroid();

        protected override AppBuilder CreateAppBuilder()
        {
            var builder = AppBuilder.Configure<TApp>();

            return CustomizeAppBuilder(builder);
        }
    }
}
