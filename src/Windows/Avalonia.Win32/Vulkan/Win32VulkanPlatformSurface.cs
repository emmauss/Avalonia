using System;
using System.Collections.Generic;
using Avalonia.OpenGL.Egl;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Surfaces;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Avalonia.Win32.Vulkan
{
    public class Win32VulkanPlatformSurface : IVulkanPlatformSurface
    {
        private readonly WindowImpl _window;

        public Win32VulkanPlatformSurface(WindowImpl window)
        {
            _window = window;
        }
        
        public unsafe SurfaceKHR CreateSurface(VulkanInstance instance)
        {
            if (instance.Api.TryGetInstanceExtension(new Instance(instance.Handle), out KhrWin32Surface surfaceExtension))
            {
                var createInfo = new Win32SurfaceCreateInfoKHR() { Hinstance = 0, Hwnd = _window.Handle.Handle, SType = StructureType.Win32SurfaceCreateInfoKhr };

                surfaceExtension.CreateWin32Surface(new Instance(instance.Handle), createInfo, null, out var surface).ThrowOnError();

                return surface;
            }

            throw new Exception("VK_KHR_win32_surface is not available on this platform.");
        }

        public PixelSize SurfaceSize => new PixelSize((int)(_window.ClientSize.Width * Scaling), (int)(_window.ClientSize.Height * Scaling));

        public float Scaling => Math.Max(0, (float)_window.RenderScaling);
    }
}
