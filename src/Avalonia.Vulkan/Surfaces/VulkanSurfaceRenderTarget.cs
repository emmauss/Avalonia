using System;
using System.Threading;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan.Surfaces
{
    public class VulkanSurfaceRenderTarget : IDisposable
    {
        private readonly VulkanPlatformInterface _platformInterface;

        private bool _shouldRecreate = true;

        public VulkanImage Image { get; private set; }

        public uint MipLevels => Image.MipLevels;

        public VulkanSurfaceRenderTarget(VulkanPlatformInterface platformInterface, VulkanSurface surface)
        {
            _platformInterface = platformInterface;

            Display = VulkanDisplay.CreateDisplay(platformInterface.Instance, platformInterface.Device,
                platformInterface.PhysicalDevice, surface);
            Surface = surface;

            // Skia seems to only create surfaces from images with unorm format
            Format = Display.SurfaceFormat.Format >= Format.R8G8B8A8Unorm &&
                     Display.SurfaceFormat.Format <= Format.R8G8B8A8Srgb ?
                Format.R8G8B8A8Unorm :
                Format.B8G8R8A8Unorm;

            ImageUsageFlags = ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferDstBit |
                              ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageSampledBit;

            SurfaceId = 0;
        }

        public Format Format { get; }
        public ulong MemorySize => Image.MemorySize;

        public VulkanDisplay Display { get; }
        public VulkanSurface Surface { get; }

        public ImageUsageFlags ImageUsageFlags { get; }

        public PixelSize Size { get; private set; }
        public bool IsDisposed { get; private set; }

        internal long SurfaceId { get; private set; }

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

        private unsafe void CreateImage()
        {
            Size = Surface.SurfaceSize;

            Image = new VulkanImage(_platformInterface.Device, _platformInterface.PhysicalDevice, _platformInterface.Device.CommandBufferPool, Format, Size, ImageUsageFlags);

            SurfaceId++;
        }

        private void DestroyImage()
        {
            Image?.Dispose();
        }
    }
}
