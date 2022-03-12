using System;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Surfaces;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class VulkanRenderTarget : ISkiaGpuRenderTarget
    {
        internal GRContext GrContext { get; set; }
        
        private readonly VulkanSurfaceRenderTarget _surface;
        private readonly IVulkanPlatformSurface _vulkanPlatformSurface;

        public VulkanRenderTarget(VulkanPlatformInterface vulkanPlatformInterface,
            IVulkanPlatformSurface vulkanPlatformSurface)
        {
            _surface = vulkanPlatformInterface.CreateRenderTarget(vulkanPlatformSurface);
            _vulkanPlatformSurface = vulkanPlatformSurface;
        }

        public void Dispose()
        {
            _surface.Dispose();
        }

        public ISkiaGpuRenderSession BeginRenderingSession()
        {
            var session = _surface.BeginDraw(_vulkanPlatformSurface.Scaling);
            bool success = false;
            try
            {
                var disp = session.Display;
                var api = session.Api;

                var size = session.Size;
                var scaling = session.Scaling;
                if (size.Width <= 0 || size.Height <= 0 || scaling < 0)
                {
                    session.Dispose();
                    throw new InvalidOperationException(
                        $"Can't create drawing context for surface with {size} size and {scaling} scaling");
                }

                lock (GrContext)
                {
                    GrContext.ResetContext();

                    var imageInfo = new GRVkImageInfo()
                    {
                        CurrentQueueFamily = disp.QueueFamilyIndex,
                        Format = (uint)_surface.Format,
                        Image = _surface.Image.ApiHandle.Value.Handle,
                        ImageLayout = (uint)_surface.Image.CurrentLayout,
                        ImageTiling = (uint)ImageTiling.Optimal,
                        ImageUsageFlags = (uint)_surface.ImageUsageFlags,
                        LevelCount = _surface.MipLevels,
                        SampleCount = 1,
                        Protected = false,
                        Alloc = new GRVkAlloc()
                        {
                            Memory = _surface.Image.ImageMemory.Handle,
                            Flags = 0,
                            Offset = 0,
                            Size = _surface.MemorySize
                        }
                    };

                    var renderTarget =
                        new GRBackendRenderTarget((int)size.Width, (int)size.Height, 1,
                            imageInfo);
                    var surface = SKSurface.Create(GrContext, renderTarget,
                        session.IsYFlipped ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft,
                        IsRgba(_surface.Format) ? SKColorType.Rgba8888 : SKColorType.Bgra8888, SKColorSpace.CreateSrgb());

                    if (surface == null)
                        throw new InvalidOperationException(
                            $"Surface can't be created with the provided render target");

                    success = true;

                    return new VulkanGpuSession(GrContext, renderTarget, surface, session);
                }
            }
            finally
            {
                if (!success)
                    session.Dispose();
            }
        }

        private bool IsRgba(Format format)
        {
            return format >= Format.R8G8B8A8Unorm && format <= Format.R8G8B8A8Srgb;
        }

        public bool IsCorrupted { get; }

        internal class VulkanGpuSession : ISkiaGpuRenderSession
        {
            private readonly GRBackendRenderTarget _backendRenderTarget;
            private readonly VulkanSurfaceRenderingSession _vulkanSession;

            public VulkanGpuSession(GRContext grContext,
                GRBackendRenderTarget backendRenderTarget,
                SKSurface surface,
                VulkanSurfaceRenderingSession vulkanSession)
            {
                GrContext = grContext;
                _backendRenderTarget = backendRenderTarget;
                SkSurface = surface;
                _vulkanSession = vulkanSession;

                SurfaceOrigin = vulkanSession.IsYFlipped ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft;
            }

            public void Dispose()
            {
                lock (_vulkanSession.Display.Device.Lock)
                {
                    SkSurface.Canvas.Flush();
                    SkSurface.Dispose();
                    _backendRenderTarget.Dispose();
                    GrContext.Flush();
                }
                _vulkanSession.Dispose();
            }

            public GRContext GrContext { get; }
            public SKSurface SkSurface { get; }
            public double ScaleFactor => _vulkanSession.Scaling;
            public GRSurfaceOrigin SurfaceOrigin { get; }
        }
    }
}
