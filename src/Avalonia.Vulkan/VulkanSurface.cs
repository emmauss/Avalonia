using System;
using Avalonia.Vulkan.Surfaces;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Avalonia.Vulkan
{
    public class VulkanSurface : IDisposable
    {
        private readonly VulkanInstance _instance;
        private readonly IVulkanPlatformSurface _vulkanPlatformSurface;

        private VulkanSurface(IVulkanPlatformSurface vulkanPlatformSurface, VulkanInstance instance)
        {
            _vulkanPlatformSurface = vulkanPlatformSurface;
            _instance = instance;
            ApiHandle = vulkanPlatformSurface.CreateSurface(instance);
        }

        internal SurfaceKHR ApiHandle { get; }

        internal static KhrSurface SurfaceExtension { get; private set; }

        internal PixelSize SurfaceSize => _vulkanPlatformSurface.SurfaceSize;

        public unsafe void Dispose()
        {
            SurfaceExtension.DestroySurface(_instance.ApiHandle, ApiHandle, null);
        }

        internal static VulkanSurface CreateSurface(VulkanInstance instance, IVulkanPlatformSurface vulkanPlatformSurface)
        {
            if (SurfaceExtension == null)
            {
                instance.Api.TryGetInstanceExtension(instance.ApiHandle, out KhrSurface extension);

                SurfaceExtension = extension;
            }

            return new VulkanSurface(vulkanPlatformSurface, instance);
        }

        internal bool CanSurfacePresent(VulkanPhysicalDevice physicalDevice)
        {
            SurfaceExtension.GetPhysicalDeviceSurfaceSupport(physicalDevice.ApiHandle, physicalDevice.QueueFamilyIndex, ApiHandle, out var isSupported);

            return isSupported;
        }

        internal unsafe SurfaceFormatKHR GetSurfaceFormat(VulkanPhysicalDevice physicalDevice)
        {
            uint surfaceFormatsCount;

            SurfaceExtension.GetPhysicalDeviceSurfaceFormats(physicalDevice.ApiHandle, ApiHandle,
                &surfaceFormatsCount, null);

            var surfaceFormats = new SurfaceFormatKHR[surfaceFormatsCount];

            fixed (SurfaceFormatKHR* pSurfaceFormats = surfaceFormats)
            {
                SurfaceExtension.GetPhysicalDeviceSurfaceFormats(physicalDevice.ApiHandle, ApiHandle,
                    &surfaceFormatsCount, pSurfaceFormats);
            }

            if (surfaceFormats.Length == 1 && surfaceFormats[0].Format == Format.Undefined)
                return new SurfaceFormatKHR(Format.B8G8R8A8Srgb, ColorSpaceKHR.ColorspaceSrgbNonlinearKhr);
            foreach (var format in surfaceFormats)
                if (format.Format == Format.B8G8R8A8Srgb &&
                    format.ColorSpace == ColorSpaceKHR.ColorspaceSrgbNonlinearKhr)
                    return format;

            return surfaceFormats[0];
        }
    }
}
