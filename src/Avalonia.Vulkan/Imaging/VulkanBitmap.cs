﻿using System;
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
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan.Imaging
{
    public class VulkanBitmap : Bitmap, IAffectsRender
    {
        private IVulkanBitmapImpl _impl;

        public VulkanBitmap(PixelSize size, Vector dpi)
            : base(CreateOrThrow(size, dpi))
        {
            _impl = (IVulkanBitmapImpl)PlatformImpl.Item;
        }

        static IVulkanBitmapImpl CreateOrThrow(PixelSize size, Vector dpi)
        {
            var platformInterface = AvaloniaLocator.Current.GetService<VulkanPlatformInterface>();
            if (!(AvaloniaLocator.Current.GetService<IPlatformRenderInterface>() is IVulkanAwarePlatformRenderInterface
                platformRenderInterface))
                throw new PlatformNotSupportedException("Rendering platform does not support Vulkan integration");
            return platformRenderInterface.CreateVulkanBitmap(platformInterface, size, dpi, (uint) Format.B8G8R8A8Unorm);
        }

        public IVulkanBitmapAttachment CreateFramebufferAttachment(VulkanPlatformInterface platformInterface) =>
            _impl.CreateFramebufferAttachment(platformInterface, SetIsDirty);

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
