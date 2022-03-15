using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.Vulkan.Imaging;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace Avalonia.Vulkan.Skia
{
    public class VulkanBitmapImpl : IBitmapImpl
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

        public VulkanBitmapAttachment CreateFramebufferAttachment(VulkanPlatformInterface platformInterface, Action presentCallback)
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

        public void Present(VulkanBitmapAttachment surface)
        {
            lock (_lock)
            {
                _surface?.Dispose();

                _surface = surface;
            }
        }
    }

    public class VulkanBitmapAttachment
    {
        private readonly VulkanPlatformInterface _platformInterface;
        private readonly Action _presentCallback;
        private readonly DisposableLock _lock = new DisposableLock();
        private bool _disposed;

        public VulkanImage Image { get; set; }

        private int _referenceCount;

        public VulkanBitmapAttachment(VulkanBitmapImpl bitmap, VulkanPlatformInterface platformInterface, Action presentCallback)
        {
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
            Image.TransitionLayout(ImageLayout.TransferSrcOptimal, 0);
            _referenceCount++;
            _presentCallback();
        }

        public IDisposable Lock() => _lock.Lock();
    }

}
