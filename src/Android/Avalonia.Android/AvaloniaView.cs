using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using AndroidX.Lifecycle;
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
        private AvaloniaViewModel _viewModel;
        private ViewImpl _view;

        private IDisposable _timerSubscription;
        private object _content;

        public AvaloniaView() { }

        internal AvaloniaView(object content)
        {
            _content = content;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            _viewModel = new ViewModelProvider(this).Get(Java.Lang.Class.FromType(typeof(AvaloniaViewModel))) as AvaloniaViewModel;
            _view = new ViewImpl(this);
            var frame = new FrameLayout(Context);
            frame.AddView(_view.View);

            if(_content == null)
            {
                _content = _viewModel.Content;
            }

            _viewModel.Content = _content;
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

        public override void OnSaveInstanceState(Bundle outState)
        {
            (View as FrameLayout).RemoveAllViews();
            base.OnSaveInstanceState(outState);
        }

        public override void OnDestroyView()
        {
            Content = null;

            base.OnDestroyView();

            _root?.Dispose();
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
