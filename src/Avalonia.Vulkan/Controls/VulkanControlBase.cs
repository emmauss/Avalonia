using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Vulkan.Imaging;
using Avalonia.Vulkan.Surfaces;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan.Controls
{
    public abstract class VulkanControlBase : Control
    {
        private VulkanPlatformInterface _platformInterface;
        private VulkanBitmap _bitmap;
        private IVulkanBitmapAttachment _attachment;
        private IVulkanBitmapAttachment _oldAttachment;
        private bool _initialized;

        public sealed override void Render(DrawingContext context)
        {
            if (!EnsureInitialized())
                return;

            lock (_platformInterface.Device.Lock)
            {
                _oldAttachment?.Dispose();
                _oldAttachment = null;
                _platformInterface.Device.QueueWaitIdle();

                EnsureTextureAttachment();

                OnVulkanRender(_platformInterface, new VulkanImageInfo(_attachment.GetBitmapImage() as VulkanImage));
                _attachment.Present();
            }

            context.DrawImage(_bitmap, new Rect(_bitmap.Size), Bounds);
            base.Render(context);
        }

        void EnsureTextureAttachment()
        {
            if (_platformInterface != null)
                if (_bitmap == null || _attachment == null || _bitmap.PixelSize != GetPixelSize())
                {
                    _oldAttachment?.Dispose();
                    _oldAttachment = _attachment;
                    _bitmap?.Dispose();
                    _bitmap = null;
                    _bitmap = new VulkanBitmap(GetPixelSize(), new Vector(96, 96));
                    _attachment = _bitmap.CreateFramebufferAttachment(_platformInterface);
                }

            (_attachment.GetBitmapImage() as VulkanImage).TransitionLayout(ImageLayout.ColorAttachmentOptimal, AccessFlags.AccessColorAttachmentReadBit);
        }

        void DoCleanup()
        {
            if (_platformInterface != null)
            {

                try
                {
                    if (_initialized)
                    {
                        _initialized = false;
                        OnVulkanDeinit(_platformInterface, new VulkanImageInfo(_attachment.GetBitmapImage() as VulkanImage));
                    }
                }
                finally
                {
                    _attachment?.Dispose();
                    _bitmap?.Dispose();
                    _oldAttachment?.Dispose();
                    _oldAttachment = null;

                    _attachment = null;
                    _bitmap = null;
                    _platformInterface = null;
                }
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            DoCleanup();
            base.OnDetachedFromVisualTree(e);
        }

        private bool EnsureInitializedCore()
        {
            _platformInterface = AvaloniaLocator.Current.GetService<VulkanPlatformInterface>();
            if (_platformInterface == null || _platformInterface.Device == null)
            {
                // Device is not ready. Try to initialize on the next render
                Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Render);
                return false;
            }

            lock (_platformInterface.Device.Lock)
            {
                try
                {
                    _bitmap = new VulkanBitmap(GetPixelSize(), new Vector(96, 96));
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("VulkanControlBase",
                        "Unable to initialize Vulkan: unable to create VulkanBitmap: {exception}", e);
                    return false;
                }

                try
                {
                    EnsureTextureAttachment();

                    return true;
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "Vulkan")?.Log("VulkanControlBase",
                        "Unable to initialize Vulkan Image: {exception}", e);
                    return false;
                }
            }
        }

        private bool EnsureInitialized()
        {
            if (_initialized)
                return true;
            _initialized = EnsureInitializedCore();

            if (!_initialized)
                return false;

            OnVulkanInit(_platformInterface, new VulkanImageInfo(_attachment.GetBitmapImage() as VulkanImage));

            return true;
        }
        
        private PixelSize GetPixelSize()
        {
            var scaling = VisualRoot.RenderScaling;
            return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),
                Math.Max(1, (int)(Bounds.Height * scaling)));
        }


        protected virtual void OnVulkanInit(VulkanPlatformInterface platformInterface, VulkanImageInfo info)
        {
            
        }

        protected virtual void OnVulkanDeinit(VulkanPlatformInterface platformInterface, VulkanImageInfo info)
        {
            
        }
        
        protected abstract void OnVulkanRender(VulkanPlatformInterface platformInterface, VulkanImageInfo info);
    }

    public struct VulkanImageInfo
    {
        internal VulkanImageInfo(VulkanImage image)
        {
            Image = image;
        }

        public Format Format => Image.Format;
        public PixelSize PixelSixe => Image.Size;
        public VulkanImage Image { get; }
    }
}
