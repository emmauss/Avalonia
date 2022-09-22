using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Rendering;
using static Android.Views.View;

namespace Avalonia.Android
{
    public class AvaloniaView : Fragment
    {
        private EmbeddableControlRoot _root;
        private ViewImpl _view;

        private IDisposable _timerSubscription;
        private object _content;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            _view = new ViewImpl(this);
            var frame = new FrameLayout(Context);
            frame.AddView(_view.View);

            Prepare();
            return frame;
        }

        internal void Prepare ()
        {
            _root = new EmbeddableControlRoot(_view);
            _root.Content = _content;
            _root.Prepare();
        }

        internal TopLevelImpl TopLevelImpl => _view;

        public object Content
        {
            get { return _root?.Content ?? _content; }
            set
            {
                _content = value;
                if (_root != null)
                {
                    _root.Content = value;
                }
            }
        }

        internal void OnVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                if (AvaloniaLocator.Current.GetService<IRenderTimer>() is ChoreographerTimer timer)
                {
                    _timerSubscription = timer.SubscribeView(this);
                }

                _root.Renderer.Start();
            }
            else
            {
                _root.Renderer.Stop();
                _timerSubscription?.Dispose();
            }
        }

        class ViewImpl : TopLevelImpl
        {
            public ViewImpl(AvaloniaView avaloniaView) : base(avaloniaView)
            {
                View.Focusable = true;
                View.FocusChange += ViewImpl_FocusChange;
            }

            private void ViewImpl_FocusChange(object sender, FocusChangeEventArgs e)
            {
                if(!e.HasFocus)
                    LostFocus?.Invoke();
            }

            protected override void OnResized(Size size)
            {
                MaxClientSize = size;
                base.OnResized(size);
            }

            public WindowState WindowState { get; set; }
            public IDisposable ShowDialog() => null;
        }
    }
}
