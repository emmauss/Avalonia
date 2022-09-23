using System;
using System.Collections.Generic;

using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.OpenGL;
using Avalonia.Android.Platform.Specific;
using Avalonia.Android.Platform.Specific.Helpers;
using Avalonia.Android.Platform.Storage;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;

namespace Avalonia.Android.Platform.SkiaPlatform
{
    class TopLevelImpl : IAndroidView, ITopLevelImpl, EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo,
        ITopLevelImplWithTextInputMethod, ITopLevelImplWithNativeControlHost, ITopLevelImplWithStorageProvider
    {
        private IGlPlatformSurface _gl;
        private readonly IFramebufferPlatformSurface _framebuffer;
        private readonly AndroidNavigation _navigation;
        private readonly AndroidKeyboardEventsHelper<TopLevelImpl> _keyboardHelper;
        private readonly AndroidMotionEventsHelper _pointerHelper;
        private readonly ITextInputMethodImpl _textInputMethod;
        private ViewImpl _view;

        public TopLevelImpl(AvaloniaView avaloniaView, bool placeOnTop = true)
        {
            _view = new ViewImpl(avaloniaView.Context, this, placeOnTop);
            _textInputMethod = new AndroidInputMethod<ViewImpl>(_view);
            _keyboardHelper = new AndroidKeyboardEventsHelper<TopLevelImpl>(this);
            _pointerHelper = new AndroidMotionEventsHelper(this);
            _framebuffer = new FramebufferManager(this);
            _navigation = new AndroidNavigation(avaloniaView);

            RenderScaling = _view.Scaling;

            MaxClientSize = new PixelSize(_view.Resources.DisplayMetrics.WidthPixels,
                _view.Resources.DisplayMetrics.HeightPixels).ToSize(RenderScaling);

            NativeControlHost = new AndroidNativeControlHostImpl(avaloniaView);
            StorageProvider = new AndroidStorageProvider((AvaloniaActivity)avaloniaView.Context);

            _view.VisibilityChanged += (s, e) => avaloniaView.OnVisibilityChanged(e);
        }

        public virtual Point GetAvaloniaPointFromEvent(MotionEvent e, int pointerIndex) =>
            new Point(e.GetX(pointerIndex), e.GetY(pointerIndex)) / RenderScaling;

        public IInputRoot InputRoot { get; private set; }

        public virtual Size ClientSize => Size.ToSize(RenderScaling);

        public Size? FrameSize => null;

        public IMouseDevice MouseDevice { get; } = new MouseDevice();

        public Action Closed { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Size MaxClientSize { get; protected set; }

        public Action<Rect> Paint { get; set; }

        public Action<Size, PlatformResizeReason> Resized { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public View View => _view;

        internal InvalidationAwareSurfaceView InternalView => _view;

        public IPlatformHandle Handle => _view;

        public IEnumerable<object> Surfaces => new object[] { _gl, _framebuffer, Handle };

        public IRenderer CreateRenderer(IRenderRoot root) =>
            AndroidPlatform.Options.UseCompositor
                ? new CompositingRenderer(root, AndroidPlatform.Compositor)
                : AndroidPlatform.Options.UseDeferredRendering
                    ? new DeferredRenderer(root, AvaloniaLocator.Current.GetRequiredService<IRenderLoop>()) { RenderOnlyOnRenderThread = true }
                    : new ImmediateRenderer(root);

        public void CreateGlPlatformSurface()
        {
            if(_gl != null)
            {
                _gl = GlPlatformSurface.TryCreate(this);
            }
        }

        public virtual void Hide()
        {
            _view.Visibility = ViewStates.Invisible;
        }

        public void Invalidate(Rect rect)
        {
            if (_view.Holder?.Surface?.IsValid == true) _view.Invalidate();
        }

        public Point PointToClient(PixelPoint point)
        {
            return point.ToPoint(RenderScaling);
        }

        public PixelPoint PointToScreen(Point point)
        {
            return PixelPoint.FromPoint(point, RenderScaling);
        }

        public void SetCursor(ICursorImpl cursor)
        {
            //still not implemented
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }
        
        public virtual void Show()
        {
            _view.Visibility = ViewStates.Visible;
        }

        public double RenderScaling { get; }

        void Draw()
        {
            Paint?.Invoke(new Rect(new Point(0, 0), ClientSize));
        }
        
        public virtual void Dispose()
        {
            _view.Dispose();
            _view = null;
        }

        protected virtual void OnResized(Size size)
        {
            Resized?.Invoke(size, PlatformResizeReason.Unspecified);
        }

        class ViewImpl : InvalidationAwareSurfaceView, ISurfaceHolderCallback, IInitEditorInfo
        {
            public event EventHandler<bool> VisibilityChanged;

            private readonly TopLevelImpl _tl;
            private Size _oldSize;
            public ViewImpl(Context context,  TopLevelImpl tl, bool placeOnTop) : base(context)
            {
                _tl = tl;
                if (placeOnTop)
                    SetZOrderOnTop(true);
            }

            protected override void Draw()
            {
                _tl.Draw();
            }

            protected override bool DispatchGenericPointerEvent(MotionEvent e)
            {
                bool callBase;
                bool? result = _tl._pointerHelper.DispatchMotionEvent(e, out callBase);
                bool baseResult = callBase ? base.DispatchGenericPointerEvent(e) : false;

                return result != null ? result.Value : baseResult;
            }

            public override bool DispatchTouchEvent(MotionEvent e)
            {
                bool callBase;
                bool? result = _tl._pointerHelper.DispatchMotionEvent(e, out callBase);
                bool baseResult = callBase ? base.DispatchTouchEvent(e) : false;

                return result != null ? result.Value : baseResult;
            }

            public override bool DispatchKeyEvent(KeyEvent e)
            {
                bool callBase;
                bool? res = _tl._keyboardHelper.DispatchKeyEvent(e, out callBase);
                bool baseResult = callBase ? base.DispatchKeyEvent(e) : false;

                return res != null ? res.Value : baseResult;
            }

            void ISurfaceHolderCallback.SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
            {
                _tl.CreateGlPlatformSurface();

                var newSize = new PixelSize(width, height).ToSize(_tl.RenderScaling);

                if (newSize != _oldSize)
                {
                    _oldSize = newSize;
                    _tl.OnResized(newSize);
                }

                base.SurfaceChanged(holder, format, width, height);
            }

            public sealed override bool OnCheckIsTextEditor()
            {
                return true;
            }

            private Action<EditorInfo> _initEditorInfo;

            public void InitEditorInfo(Action<EditorInfo> init)
            {
                _initEditorInfo = init;
            }

            public sealed override IInputConnection OnCreateInputConnection(EditorInfo outAttrs)
            {
                if (_initEditorInfo != null)
                    _initEditorInfo(outAttrs);

                return base.OnCreateInputConnection(outAttrs);
            }

            public override void OnVisibilityAggregated(bool isVisible)
            {
                base.OnVisibilityAggregated(isVisible);
                OnVisibilityChanged(isVisible);
            }

            private void OnVisibilityChanged(bool isVisible)
            {
                VisibilityChanged?.Invoke(this, isVisible);
            }

            protected override void OnVisibilityChanged(View changedView, [GeneratedEnum] ViewStates visibility)
            {
                base.OnVisibilityChanged(changedView, visibility);
                OnVisibilityChanged(visibility == ViewStates.Visible);
            }

        }

        public IPopupImpl CreatePopup() => null;
        
        public Action LostFocus { get; set; }
        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => new AcrylicPlatformCompensationLevels(1, 1, 1);

        IntPtr EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo.Handle => ((IPlatformHandle)_view).Handle;

        public PixelSize Size => _view.Size;

        public double Scaling => RenderScaling;

        public ITextInputMethodImpl TextInputMethod => _textInputMethod;

        public INativeControlHostImpl NativeControlHost { get; }
        
        public IStorageProvider StorageProvider { get; }

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
        {
            throw new NotImplementedException();
        }

        public void Navigate(UserControl content)
        {
            _navigation.Navigate(content);
        }
    }
}
