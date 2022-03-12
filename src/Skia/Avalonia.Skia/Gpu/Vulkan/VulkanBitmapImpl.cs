using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Utilities;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Imaging;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace Avalonia.Skia.Gpu.Vulkan
{
    class VulkanBitmapImpl : IVulkanBitmapImpl, IDrawableBitmapImpl
    {
        private readonly VulkanPlatformInterface _platformInterface;
        private VulkanBitmapAttachment _surface;
        private object _lock = new object();

        public Vector Dpi { get; }

        public PixelSize PixelSize { get; }
        public Format Format { get; }

        public int Version {get; private set;}

        public VulkanImage Image => _surface.Image;

        public VulkanBitmapImpl(VulkanPlatformInterface platformInterface, PixelSize pixelSize, Vector dpi, Format format)
        {
            _platformInterface = platformInterface;
            PixelSize = pixelSize;
            Dpi = dpi;
            Format = Format.B8G8R8A8Unorm;
        }

        public IVulkanBitmapAttachment CreateFramebufferAttachment(VulkanPlatformInterface platformInterface, Action presentCallback)
        {
            return new VulkanBitmapAttachment(this, platformInterface, presentCallback);
        }

        public void Dispose()
        {
        }

        public void Save(string fileName)
        {
            throw new NotImplementedException();
        }

        public void Save(Stream stream)
        {
            throw new NotImplementedException();
        }

        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            lock (_lock)
            {
                if (_surface == null)
                    return;
                using (_surface.Lock())
                {
                    _platformInterface.Device.QueueWaitIdle();

                    var imageInfo = new GRVkImageInfo()
                    {
                        CurrentQueueFamily = _platformInterface.PhysicalDevice.QueueFamilyIndex,
                        Format = (uint)Format,
                        Image = _surface.Image.ApiHandle.Value.Handle,
                        ImageLayout = (uint)_surface.Image.CurrentLayout,
                        ImageTiling = (uint)ImageTiling.Optimal,
                        ImageUsageFlags = (uint)_surface.Image.ImageUsageFlags,
                        LevelCount = _surface.Image.MipLevels,
                        SampleCount = 1,
                        Protected = false,
                        Alloc = new GRVkAlloc()
                        {
                            Memory = _surface.Image.ImageMemory.Handle,
                            Flags = 0,
                            Offset = 0,
                            Size = _surface.Image.MemorySize
                        }
                    };

                    using (var backendTexture = new GRBackendRenderTarget(PixelSize.Width, PixelSize.Height, 1,
                            imageInfo))
                    using (var surface = SKSurface.Create(context.GrContext, backendTexture,
                        GRSurfaceOrigin.TopLeft,
                        SKColorType.Bgra8888, SKColorSpace.CreateSrgb()))
                    {
                        // Again, silently ignore, if something went wrong it's not our fault
                        if (surface == null)
                            return;

                        using (var snapshot = surface.Snapshot())
                            context.Canvas.DrawImage(snapshot, sourceRect, destRect, paint);
                    }
                }
            }
        }

        public void Present(VulkanBitmapAttachment surface)
        {
            lock (_lock)
            {
                _surface = surface;
            }
        }
    }

    class VulkanBitmapAttachment : IVulkanBitmapAttachment
    {
        private readonly VulkanBitmapImpl _bitmap;
        private readonly VulkanPlatformInterface _platformInterface;
        private readonly Action _presentCallback;
        private readonly DisposableLock _lock = new DisposableLock();
        private bool _disposed;

        public VulkanImage Image { get; set; }

        public VulkanBitmapAttachment(VulkanBitmapImpl bitmap, VulkanPlatformInterface platformInterface, Action presentCallback)
        {
            _bitmap = bitmap;
            _platformInterface = platformInterface;
            _presentCallback = presentCallback;

            Image = new VulkanImage(platformInterface.Device, platformInterface.PhysicalDevice, platformInterface.Device.CommandBufferPool, bitmap.Format, bitmap.PixelSize,
                ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferDstBit |
                              ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageSampledBit, 1);
        }

        public void Dispose()
        {
            Image.Dispose();
            _disposed = true;
        }

        public void Present()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VulkanBitmapAttachment));

            Image.TransitionLayout(ImageLayout.TransferSrcOptimal, AccessFlags.AccessNoneKhr);
            _bitmap.Present(this);
            _presentCallback();
        }

        public object GetBitmapImage()
        {
            return Image;
        }

        public IDisposable Lock() => _lock.Lock();
    }

}
