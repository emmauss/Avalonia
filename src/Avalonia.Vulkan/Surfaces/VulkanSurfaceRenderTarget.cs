using System;
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

            Format = Display.SurfaceFormat.Format;

            ImageUsageFlags = ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferDstBit |
                              ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageSampledBit;
        }

        public Format Format { get; }
        public ulong MemorySize => Image.MemorySize;

        public VulkanDisplay Display { get; }
        public VulkanSurface Surface { get; }

        public ImageUsageFlags ImageUsageFlags { get; }

        public PixelSize Size { get; private set; }

        public void Dispose()
        {
            _platformInterface.Device.WaitIdle();   
            DestroyImage();
            Display?.Dispose();
            Surface?.Dispose();
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
        }

        private void DestroyImage()
        {
            Image?.Dispose();
        }
    }
}
