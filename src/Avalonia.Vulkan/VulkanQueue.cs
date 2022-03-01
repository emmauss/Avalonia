using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    public class VulkanQueue
    {
        public VulkanQueue(VulkanDevice device, Queue apiHandle)
        {
            Device = device;
            ApiHandle = apiHandle;
        }

        public VulkanDevice Device { get; }
        public Queue ApiHandle { get; }
    }
}
