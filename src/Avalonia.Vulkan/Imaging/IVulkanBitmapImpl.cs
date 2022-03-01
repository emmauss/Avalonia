using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Vulkan.Imaging
{
    public interface IVulkanBitmapImpl : IBitmapImpl
    {
        IVulkanBitmapAttachment CreateFramebufferAttachment(VulkanPlatformInterface platformInterface, Action presentCallback);
    }

    public interface IVulkanBitmapAttachment : IDisposable
    {
        void Present();
        object GetBitmapImage();
    }
}
