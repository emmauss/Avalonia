using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Vulkan.Imaging;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    public interface IVulkanAwarePlatformRenderInterface
    {
        IVulkanBitmapImpl CreateVulkamBitmap(VulkanPlatformInterface platformInterface, PixelSize pixelSize, Vector dpi, Format format);
    }
}
