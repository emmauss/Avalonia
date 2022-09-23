using System;
using System.ComponentModel;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using AndroidX.Lifecycle;
using AndroidView = Android.Views;

namespace Avalonia.Android
{
    public abstract class AvaloniaActivity : AppCompatActivity
    {
        internal Action<int, Result, Intent> ActivityResult;
        internal AvaloniaView View;
        internal AvaloniaViewModel _viewModel;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _viewModel = new ViewModelProvider(this).Get(Java.Lang.Class.FromType(typeof(AvaloniaViewModel))) as AvaloniaViewModel;

            SetContentView(Resource.Layout.main);

            if (savedInstanceState == null)
            {
                View = new AvaloniaView( null);

                if (_viewModel.Content != null)
                {
                    View.Content = _viewModel.Content;
                }

                if (Avalonia.Application.Current.ApplicationLifetime is SingleViewLifetime lifetime)
                {
                    lifetime.View = View;
                }

                Content = View.Content;
                SupportFragmentManager
                    .BeginTransaction()
                    .SetReorderingAllowed(true)
                    .Replace(Resource.Id.container, View, "MAINVIEW")
                    .CommitNow();
            }
        }

        public object Content
        {
            get
            {
                return _viewModel.Content;
            }
            set
            {
                _viewModel.Content = value;
                if (View != null)
                    View.Content = value;
            }
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
        }

        protected override void OnDestroy()
        {
            if (View != null)
            {
                View.Content = null;
            }

            base.OnDestroy();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            ActivityResult?.Invoke(requestCode, resultCode, data);
        }
    }
}
