using System;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    public class VulkanSemaphorePair : IDisposable
    {
        private readonly VulkanDevice _device;

        public unsafe VulkanSemaphorePair(VulkanDevice device)
        {
            _device = device;

            var semaphoreCreateInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };

            _device.Api.CreateSemaphore(_device.ApiHandle, semaphoreCreateInfo, null, out var semaphore).ThrowOnError();
            ImageAvailableSemaphore = semaphore;

            _device.Api.CreateSemaphore(_device.ApiHandle, semaphoreCreateInfo, null, out semaphore).ThrowOnError();
            RenderFinishedSemaphore = semaphore;
        }

        public Semaphore ImageAvailableSemaphore { get; }
        public Semaphore RenderFinishedSemaphore { get; }

        public unsafe void Dispose()
        {
            _device.Api.DestroySemaphore(_device.ApiHandle, ImageAvailableSemaphore, null);
            _device.Api.DestroySemaphore(_device.ApiHandle, RenderFinishedSemaphore, null);
        }
    }
}
