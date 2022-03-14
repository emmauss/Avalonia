using System;
using System.Threading;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan.Surfaces
{
    public class VulkanSurfaceRenderTarget : IDisposable
    {
        private readonly VulkanPlatformInterface _platformInterface;

        private bool _shouldRecreate = true;
        private readonly Format _format;
        private VulkanImage _image;
        private readonly bool _isRgba;
        private readonly VulkanDisplay _display;
        private readonly VulkanSurface _surface;
        private readonly ImageUsageFlags _imageUsageFlags;
        private PixelSize _size;
        private bool _isDisposed;
        private long _surfaceId;

        public VulkanImage Image
        {
            get => _image;
            private set => _image = value;
        }

        public uint MipLevels => Image.MipLevels;

        public VulkanSurfaceRenderTarget(VulkanPlatformInterface platformInterface, VulkanSurface surface)
        {
            _platformInterface = platformInterface;

            _display = VulkanDisplay.CreateDisplay(platformInterface.Instance, platformInterface.Device,
                platformInterface.PhysicalDevice, surface);
            _surface = surface;

            // Skia seems to only create surfaces from images with unorm format

            _isRgba = Display.SurfaceFormat.Format >= Format.R8G8B8A8Unorm &&
                     Display.SurfaceFormat.Format <= Format.R8G8B8A8Srgb;
            
            _format = IsRgba ? Format.R8G8B8A8Unorm : Format.B8G8R8A8Unorm;

            SurfaceId = 0;
        }

        public bool IsRgba => _isRgba;

        public uint ImageFormat => (uint) _format;

        public ulong MemorySize => Image.MemorySize;

        public VulkanDisplay Display => _display;

        public VulkanSurface Surface => _surface;

        public uint UsageFlags => (uint) _imageUsageFlags;

        public PixelSize Size
        {
            get => _size;
            private set => _size = value;
        }

        public bool IsDisposed
        {
            get => _isDisposed;
            private set => _isDisposed = value;
        }

        internal long SurfaceId
        {
            get => _surfaceId;
            private set => _surfaceId = value;
        }

        public void Dispose()
        {
            _platformInterface.Device.WaitIdle();   
            DestroyImage();
            Display?.Dispose();
            Surface?.Dispose();

            IsDisposed = true;
        }

        public VulkanSurfaceRenderingSession BeginDraw(float scaling)
        {
            var session = new VulkanSurfaceRenderingSession(Display, _platformInterface.Device, this, scaling);

            if (_shouldRecreate)
            {
                _shouldRecreate = false;
                DestroyImage();
                CreateImage();
            }
            else
            {
                Image.TransitionLayout(ImageLayout.ColorAttachmentOptimal, AccessFlags.AccessNoneKhr);
            }

            session.UpdateSurface();

            return session;
        }

        public void Invalidate()
        {
            _shouldRecreate = true;
        }

        private void CreateImage()
        {
            Size = Surface.SurfaceSize;

            Image = new VulkanImage(_platformInterface.Device, _platformInterface.PhysicalDevice, _platformInterface.Device.CommandBufferPool, ImageFormat, Size);

            SurfaceId++;
        }

        private void DestroyImage()
        {
            Image?.Dispose();
        }
    }
}
