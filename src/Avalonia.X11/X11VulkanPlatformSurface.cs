using System;
using System.Collections.Generic;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Surfaces;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Avalonia.X11
{
    public class X11VulkanPlatformSurface : IVulkanPlatformSurface
    {
        private readonly IntPtr _display;
        private readonly IWindowImpl _window;

        internal X11VulkanPlatformSurface(IntPtr display, IWindowImpl windowImpl)
        {
            _display = display;
            _window = windowImpl;
        }
        
        public unsafe SurfaceKHR CreateSurface(VulkanInstance instance)
        {
            if (instance.Api.TryGetInstanceExtension(instance.ApiHandle, out KhrXlibSurface surfaceExtension))
            {
                var createInfo = new XlibSurfaceCreateInfoKHR()
                {
                    SType = StructureType.XlibSurfaceCreateInfoKhr,
                    Dpy = (nint*) _display.ToPointer(),
                    Window = _window.Handle.Handle
                };

                surfaceExtension.CreateXlibSurface(instance.ApiHandle, createInfo, null, out var surface).ThrowOnError();

                return surface;
            }

            throw new Exception("VK_KHR_xlib_surface is not available on this platform.");
        }

        public PixelSize SurfaceSize => new PixelSize((int)(_window.ClientSize.Width * Scaling), (int)(_window.ClientSize.Height * Scaling));

        public float Scaling => Math.Max(0, (float)_window.RenderScaling);
    }
}
