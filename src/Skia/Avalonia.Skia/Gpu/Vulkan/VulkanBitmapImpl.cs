using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Utilities;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Imaging;
using SkiaSharp;

namespace Avalonia.Skia.Gpu.Vulkan
{
    class VulkanBitmapImpl : IVulkanBitmapImpl, IDrawableBitmapImpl
    {
        private readonly VulkanPlatformInterface _platformInterface;
        private VulkanBitmapAttachment _surface;
        private object _lock = new object();
        private readonly Vector _dpi;
        private readonly PixelSize _pixelSize;
        private readonly int _version;

        public Vector Dpi => _dpi;

        public PixelSize PixelSize => _pixelSize;

        public uint Format { get; }

        public int Version => _version;

        public VulkanImage Image => _surface.Image;

        internal VulkanBitmapImpl(VulkanPlatformInterface platformInterface, PixelSize pixelSize, Vector dpi, uint format)
        {
            _platformInterface = platformInterface;
            _pixelSize = pixelSize;
            _dpi = dpi;
            Format = format;
        }

        public IVulkanBitmapAttachment CreateFramebufferAttachment(VulkanPlatformInterface platformInterface, Action presentCallback)
        {
            return new VulkanBitmapAttachment(this, platformInterface, presentCallback);
        }

        public void Dispose()
        {
            _surface?.Dispose();
            _surface = null;
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
                    if (_surface.Image == null)
                        return;

                    _platformInterface.Device.QueueWaitIdle();

                    var imageInfo = new GRVkImageInfo()
                    {
                        CurrentQueueFamily = _platformInterface.PhysicalDevice.QueueFamilyIndex,
                        Format = (uint)Format,
                        Image = _surface.Image.Handle,
                        ImageLayout = (uint)_surface.Image.CurrentLayout,
                        ImageTiling = (uint)_surface.Image.Tiling,
                        ImageUsageFlags = (uint)_surface.Image.UsageFlags,
                        LevelCount = _surface.Image.MipLevels,
                        SampleCount = 1,
                        Protected = false,
                        Alloc = new GRVkAlloc()
                        {
                            Memory = _surface.Image.MemoryHandle,
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
                _surface?.Dispose();

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

        private int _referenceCount;

        public VulkanBitmapAttachment(VulkanBitmapImpl bitmap, VulkanPlatformInterface platformInterface, Action presentCallback)
        {
            _bitmap = bitmap;
            _platformInterface = platformInterface;
            _presentCallback = presentCallback;

            Image = new VulkanImage(platformInterface.Device, platformInterface.PhysicalDevice, platformInterface.Device.CommandBufferPool, bitmap.Format, bitmap.PixelSize, 1);

            _referenceCount = 1;
        }

        public void Dispose()
        {
            _referenceCount--;

            if (_referenceCount == 0)
            {
                Image.Dispose();
                Image = null;
                _disposed = true;
            }
        }

        public void Present()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VulkanBitmapAttachment));
            // ImageLayout.TransferSrcOptimal = 6
            Image.TransitionLayout(6, 0);
            _referenceCount++;
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
