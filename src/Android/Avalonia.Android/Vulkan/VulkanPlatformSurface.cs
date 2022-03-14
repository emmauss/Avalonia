using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Runtime;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Surfaces;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Avalonia.Android.Vulkan
{
    internal class VulkanPlatformSurface : IVulkanPlatformSurface
    {
        private readonly TopLevelImpl _topLevel;

        public VulkanPlatformSurface(TopLevelImpl topLevel)
        {
            _topLevel = topLevel;
        }

        public unsafe SurfaceKHR CreateSurface(VulkanInstance instance)
        {
            if (instance.Api.TryGetInstanceExtension(new Instance(instance.Handle), out KhrAndroidSurface surfaceExtension))
            {
                var window = AndroidFramebuffer.ANativeWindow_fromSurface(JNIEnv.Handle, _topLevel.InternalView.Holder.Surface.Handle);
                var createInfo = new AndroidSurfaceCreateInfoKHR() {
                    Window = (nint*)window, SType = StructureType.AndroidSurfaceCreateInfoKhr };

                surfaceExtension.CreateAndroidSurface(new Instance(instance.ApiHandle), createInfo, null, out var surface).ThrowOnError();

                return surface;
            }

            throw new Exception("VK_KHR_android_surface is not available on this platform.");
        }
        public static VulkanPlatformSurface TryCreate(TopLevelImpl topLevel)
        {
            if (AvaloniaLocator.Current.GetService<VulkanPlatformInterface>() != null)
            {
                return new VulkanPlatformSurface(topLevel);
            }

            return null;
        }

        public PixelSize SurfaceSize => _topLevel.Size;

        public float Scaling => Math.Max(0, (float)_topLevel.RenderScaling);
    }
}
