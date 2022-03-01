using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    public class VulkanImage : IDisposable
    {
        private readonly VulkanDevice _device;
        private readonly VulkanPhysicalDevice _physicalDevice;
        private readonly VulkanCommandBufferPool _commandBufferPool;
        private ImageLayout _currentLayout;
        private AccessFlags _currentAccessFlags;

        public Image? ApiHandle { get; private set; }
        public ImageView? ImageView { get; private set; }
        public uint MipLevels { get; private set; }
        public PixelSize Size { get; }
        public ImageUsageFlags ImageUsageFlags { get; }
        public Format Format { get; }
        public DeviceMemory ImageMemory { get; private set; }
        public ulong MemorySize { get; private set; }
        public ImageAspectFlags AspectFlags { get; private set; }
        public ImageLayout CurrentLayout => _currentLayout;

        public VulkanImage(VulkanDevice device, VulkanPhysicalDevice physicalDevice, VulkanCommandBufferPool commandBufferPool, Format format, PixelSize size, ImageUsageFlags imageUsageFlags)
        {
            _device = device;
            _physicalDevice = physicalDevice;
            _commandBufferPool = commandBufferPool;
            Format = format;
            Size = size;
            ImageUsageFlags = imageUsageFlags;

            Initialize();
        }

        public unsafe void Initialize()
        {
            if (!ApiHandle.HasValue)
            {
                MipLevels = (uint)Math.Floor(Math.Log(Math.Max(Size.Width, Size.Height), 2));

                var imageCreateInfo = new ImageCreateInfo
                {
                    SType = StructureType.ImageCreateInfo,
                    ImageType = ImageType.ImageType2D,
                    Format = Format,
                    Extent =
                        new Extent3D((uint?)Size.Width,
                            (uint?)Size.Height, 1),
                    MipLevels = MipLevels,
                    ArrayLayers = 1,
                    Samples = SampleCountFlags.SampleCount1Bit,
                    Tiling = ImageTiling.Optimal,
                    Usage = ImageUsageFlags,
                    SharingMode = SharingMode.Exclusive,
                    InitialLayout = ImageLayout.Undefined,
                    Flags = ImageCreateFlags.ImageCreateMutableFormatBit
                };

                _device.Api
                    .CreateImage(_device.ApiHandle, imageCreateInfo, null, out var image).ThrowOnError();
                ApiHandle = image;

                _device.Api.GetImageMemoryRequirements(_device.ApiHandle, ApiHandle.Value,
                    out var memoryRequirements);

                var memoryAllocateInfo = new MemoryAllocateInfo
                {
                    SType = StructureType.MemoryAllocateInfo,
                    AllocationSize = memoryRequirements.Size,
                    MemoryTypeIndex = (uint)VulkanMemoryHelper.FindSuitableMemoryTypeIndex(
                        _physicalDevice,
                        memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit)
                };

                _device.Api.AllocateMemory(_device.ApiHandle, memoryAllocateInfo, null,
                    out var imageMemory);

                ImageMemory = imageMemory;

                _device.Api.BindImageMemory(_device.ApiHandle, ApiHandle.Value, ImageMemory, 0);

                MemorySize = memoryRequirements.Size;

                var componentMapping = new ComponentMapping(
                    ComponentSwizzle.R,
                    ComponentSwizzle.G,
                    ComponentSwizzle.B,
                    ComponentSwizzle.A);

                AspectFlags = ImageAspectFlags.ImageAspectColorBit;

                var subresourceRange = new ImageSubresourceRange(AspectFlags, 0, MipLevels, 0, 1);

                var imageViewCreateInfo = new ImageViewCreateInfo
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = ApiHandle.Value,
                    ViewType = ImageViewType.ImageViewType2D,
                    Format = Format,
                    Components = componentMapping,
                    SubresourceRange = subresourceRange
                };

                _device.Api
                    .CreateImageView(_device.ApiHandle, imageViewCreateInfo, null, out var imageView)
                    .ThrowOnError();

                ImageView = imageView;

                _currentLayout = ImageLayout.Undefined;

                TransitionLayout(ImageLayout.ColorAttachmentOptimal, AccessFlags.AccessNoneKhr);
            }
        }


        public void TransitionLayout(ImageLayout destinationLayout, AccessFlags destinationAccessFlags)
        {
            var commandBuffer = _commandBufferPool.RentCommandBuffer();
            commandBuffer.BeginRecording();

            VulkanMemoryHelper.TransitionLayout(_device, commandBuffer.ApiHandle, ApiHandle.Value,
                _currentLayout,
                _currentAccessFlags,
                destinationLayout, destinationAccessFlags,
                MipLevels);

            commandBuffer.EndRecording();

            commandBuffer.Submit();

            _currentLayout = destinationLayout;
            _currentAccessFlags = destinationAccessFlags;
        }

        public unsafe void Dispose()
        {
            _device.Api.DestroyImageView(_device.ApiHandle, ImageView.Value, null);
            _device.Api.DestroyImage(_device.ApiHandle, ApiHandle.Value, null);
            _device.Api.FreeMemory(_device.ApiHandle, ImageMemory, null);
        }
    }
}
