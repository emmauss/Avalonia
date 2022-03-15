using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Imaging;
using Avalonia.Vulkan.Skia;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan.Imaging
{
    public class VulkanBitmap : Bitmap, IAffectsRender
    {
        private VulkanBitmapImpl _impl;

        public VulkanBitmap(PixelSize size, Vector dpi)
            : base(CreateOrThrow(size, dpi))
        {
            _impl = (VulkanBitmapImpl)PlatformImpl.Item;
        }

        static VulkanBitmapImpl CreateOrThrow(PixelSize size, Vector dpi)
        {
            var platformInterface = AvaloniaLocator.Current.GetService<VulkanPlatformInterface>();
            return new VulkanBitmapImpl(platformInterface, size, dpi, (uint) Format.B8G8R8A8Unorm);
        }

        public VulkanBitmapAttachment CreateFramebufferAttachment(VulkanPlatformInterface platformInterface) =>
            new VulkanBitmapAttachment(_impl, platformInterface, SetIsDirty);

        void SetIsDirty()
        {
            if (Dispatcher.UIThread.CheckAccess())
                CallInvalidated();
            else
                Dispatcher.UIThread.Post(CallInvalidated);
        }

        private void CallInvalidated() => Invalidated?.Invoke(this, EventArgs.Empty);

        public event EventHandler Invalidated;
    }
}
