using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.OpenGL;
using Avalonia.Skia.Gpu.Vulkan;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Imaging;
using Avalonia.Vulkan.Surfaces;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace Avalonia.Skia
{
    public class VulkanSkiaGpu : ISkiaGpu
    {
        private readonly VulkanPlatformInterface _vulkan;
        private readonly long? _maxResourceBytes;
        private GRContext _grContext;
        private GRVkBackendContext _grVkBackend;
        private bool _initialized;

        public VulkanSkiaGpu(VulkanPlatformInterface vulkan, long? maxResourceBytes)
        {
            _vulkan = vulkan;
            _maxResourceBytes = maxResourceBytes;
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            GRVkGetProcedureAddressDelegate getProc = (string name, IntPtr instanceHandle, IntPtr deviceHandle) =>
            {
                IntPtr addr = IntPtr.Zero;

                if (deviceHandle != IntPtr.Zero)
                {
                    addr = _vulkan.Device.Api.GetDeviceProcAddr(new Device(deviceHandle), name);
                    if (addr != IntPtr.Zero)
                        return addr;

                    addr = _vulkan.Device.Api.GetDeviceProcAddr(new Device(_vulkan.Device.Handle), name);

                    if (addr != IntPtr.Zero)
                        return addr;
                }

                addr = _vulkan.Device.Api.GetInstanceProcAddr(new Instance(_vulkan.Instance.Handle), name);


                if (addr == IntPtr.Zero)
                    addr = _vulkan.Device.Api.GetInstanceProcAddr(new Instance(instanceHandle), name);

                return addr;
            };
            
            _grVkBackend = new GRVkBackendContext()
            {
                VkInstance = _vulkan.Device.Handle,
                VkPhysicalDevice = _vulkan.PhysicalDevice.Handle,
                VkDevice = _vulkan.Device.Handle,
                VkQueue = _vulkan.Device.Queue.Handle,
                GraphicsQueueIndex = _vulkan.PhysicalDevice.QueueFamilyIndex,
                GetProcedureAddress = getProc
            };
            _grContext = GRContext.CreateVulkan(_grVkBackend);
            if (_maxResourceBytes.HasValue)
            {
                _grContext.SetResourceCacheLimit(_maxResourceBytes.Value);
            }
        }

        public ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var surface in surfaces)
            {
                if (surface is IVulkanPlatformSurface vulkanPlatformSurface)
                {
                    var vulkanRenderTarget = new VulkanRenderTarget(_vulkan, vulkanPlatformSurface);
                    
                    Initialize();

                    vulkanRenderTarget.GrContext = _grContext;

                    return vulkanRenderTarget;
                }
            }

            return null;
        }

        public ISkiaSurface TryCreateSurface(PixelSize size, ISkiaGpuRenderSession session)
        {
            return null;
        }

        internal IVulkanBitmapImpl CreateVulkamBitmap(VulkanPlatformInterface platformInterface, PixelSize pixelSize, Vector dpi, uint format)
        {
            return new VulkanBitmapImpl(platformInterface, pixelSize, dpi, format);
        }

    }
}
