using System;
using Avalonia.Vulkan.Surfaces;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    public class VulkanSurfaceRenderingSession : IDisposable
    {
        private readonly VulkanDevice _device;
        private readonly VulkanSurfaceRenderTarget _renderTarget;
        private VulkanCommandBufferPool.VulkanCommandBuffer _commandBuffer;

        public VulkanSurfaceRenderingSession(VulkanDisplay display, VulkanDevice device,
            VulkanSurfaceRenderTarget renderTarget, float scaling)
        {
            Display = display;
            _device = device;
            _renderTarget = renderTarget;
            Scaling = scaling;
            Begin();
        }

        private long _surfaceId;

        public VulkanDisplay Display { get; }

        public PixelSize Size => _renderTarget.Size;
        public Vk Api => _device.Api;

        public float Scaling { get; }

        public bool IsYFlipped { get; } = true;

        public bool IsImageValid => _renderTarget.SurfaceId == _surfaceId;

        public void UpdateSurface()
        {
            _surfaceId = _renderTarget.SurfaceId;
        }

        public void Dispose()
        {
            _commandBuffer = Display.StartPresentation(_renderTarget);

            Display.BlitImageToCurrentImage(_renderTarget, _commandBuffer.InternalHandle);

            Display.EndPresentation(_commandBuffer);
        }

        private void Begin()
        {
            if (!Display.EnsureSwapchainAvailable())
            {
                _renderTarget.Invalidate();
            }
        }
    }
}
