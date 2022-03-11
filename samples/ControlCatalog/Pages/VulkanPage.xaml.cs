using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Vulkan;
using Avalonia.Vulkan.Controls;
using Avalonia.Platform.Interop;
using Avalonia.Threading;
using Silk.NET.Vulkan;
using Buffer = System.Buffer;
using Image = Silk.NET.Vulkan.Image;

// ReSharper disable StringLiteralTypo

namespace ControlCatalog.Pages
{
    public class VulkanPage : UserControl
    {

    }

    public class VulkanPageControl : VulkanControlBase
    {
        private float _yaw;

        public static readonly DirectProperty<VulkanPageControl, float> YawProperty =
            AvaloniaProperty.RegisterDirect<VulkanPageControl, float>("Yaw", o => o.Yaw, (o, v) => o.Yaw = v);

        public float Yaw
        {
            get => _yaw;
            set => SetAndRaise(YawProperty, ref _yaw, value);
        }

        private float _pitch;

        public static readonly DirectProperty<VulkanPageControl, float> PitchProperty =
            AvaloniaProperty.RegisterDirect<VulkanPageControl, float>("Pitch", o => o.Pitch, (o, v) => o.Pitch = v);

        public float Pitch
        {
            get => _pitch;
            set => SetAndRaise(PitchProperty, ref _pitch, value);
        }


        private float _roll;

        public static readonly DirectProperty<VulkanPageControl, float> RollProperty =
            AvaloniaProperty.RegisterDirect<VulkanPageControl, float>("Roll", o => o.Roll, (o, v) => o.Roll = v);

        public float Roll
        {
            get => _roll;
            set => SetAndRaise(RollProperty, ref _roll, value);
        }


        private float _disco;

        public static readonly DirectProperty<VulkanPageControl, float> DiscoProperty =
            AvaloniaProperty.RegisterDirect<VulkanPageControl, float>("Disco", o => o.Disco, (o, v) => o.Disco = v);

        public float Disco
        {
            get => _disco;
            set => SetAndRaise(DiscoProperty, ref _disco, value);
        }

        private string _info;

        public static readonly DirectProperty<VulkanPageControl, string> InfoProperty =
            AvaloniaProperty.RegisterDirect<VulkanPageControl, string>("Info", o => o.Info, (o, v) => o.Info = v);

        public string Info
        {
            get => _info;
            private set => SetAndRaise(InfoProperty, ref _info, value);
        }

        static VulkanPageControl()
        {
            AffectsRender<VulkanPageControl>(YawProperty, PitchProperty, RollProperty, DiscoProperty);
        }

        private ShaderModule _vertShader;
        private ShaderModule _fragShader;
        private PipelineLayout _pipelineLayout;
        private RenderPass _renderPass;
        private Pipeline _pipeline;
        private Silk.NET.Vulkan.Buffer _vertexBuffer;
        private DeviceMemory _vertexBufferMemory;
        private Silk.NET.Vulkan.Buffer _indexBuffer;
        private DeviceMemory _indexBufferMemory;
        private DescriptorSetLayout _vertextDescriptorSet;
        private DescriptorSetLayout _fragmentDescriptorSet;
        private DescriptorPool _descriptorPool;
        private Framebuffer _framebuffer;

        private DescriptorSet[] _descriptorSets;
        
        private Silk.NET.Vulkan.Buffer _vertexUniformBuffer;
        private DeviceMemory _vertexUniformBufferMemory;
        
        private Silk.NET.Vulkan.Buffer _fragmentUniformBuffer;
        private DeviceMemory _fragmentUniformBufferMemory;

        private Image _depthImage;
        private DeviceMemory _depthImageMemory;
        private ImageView _depthImageView;
        private Fence _fence;

        private byte[] GetShader(bool fragment)
        {
            var name = typeof(VulkanPage).Assembly.GetManifestResourceNames().First(x => x.Contains((fragment ? "frag" : "vert" ) + ".spirv"));
            using (var sr = typeof(VulkanPage).Assembly.GetManifestResourceStream(name))
            {
                using(var mem = new MemoryStream())
                {
                    sr.CopyTo(mem);
                    return mem.ToArray();
                }
            }
        }

        private PixelSize _previousImageSize = PixelSize.Empty;

        protected unsafe override void OnVulkanRender(VulkanPlatformInterface platformInterface, VulkanImageInfo info)
        {
            var api = platformInterface.Instance.Api;
            var device = platformInterface.Device.ApiHandle;
            var physicalDevice = platformInterface.PhysicalDevice.ApiHandle;

            if (info.PixelSixe != _previousImageSize)
                CreateTemporalObjects(api, device, info, physicalDevice);

            _previousImageSize = info.PixelSixe;

            api.WaitForFences(device, 1, _fence, true, ulong.MaxValue);
            
            UpdateUniforms(api, device);

            var commandBuffer = platformInterface.Device.CommandBufferPool.RentCommandBuffer();
            commandBuffer.BeginRecording();

            api.CmdSetViewport(commandBuffer.ApiHandle, 0, 1,
                new Viewport()
                {
                    Width = (float)Bounds.Width,
                    Height = (float)Bounds.Height,
                    MaxDepth = 1,
                    MinDepth = 0,
                    X = 0,
                    Y = 0
                });

            var clearColor = new ClearValue(new ClearColorValue(0, 0, 0, 0));

            var beginInfo = new RenderPassBeginInfo()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = _renderPass,
                Framebuffer = _framebuffer,
                RenderArea = new Rect2D(new Offset2D(0, 0), new Extent2D((uint?)Bounds.Width, (uint?)Bounds.Height)),
                ClearValueCount = 1,
                PClearValues = &clearColor
            };

            api.CmdBeginRenderPass(commandBuffer.ApiHandle, beginInfo, SubpassContents.Inline);

            api.CmdBindPipeline(commandBuffer.ApiHandle, PipelineBindPoint.Graphics, _pipeline);

            api.CmdDrawIndexed(commandBuffer.ApiHandle, (uint) _indices.Length, 1, 0, 0, 0);

            api.ResetFences(device, 1, _fence);
            commandBuffer.Submit(null, null, null, _fence);
        }

        protected unsafe override void OnVulkanDeinit(VulkanPlatformInterface platformInterface, VulkanImageInfo info)
        {
            base.OnVulkanDeinit(platformInterface, info);

            if (_isInit)
            {
                var api = platformInterface.Instance.Api;
                var device = platformInterface.Device.ApiHandle;
                
                DestroyTemporalObjects(api, device);

                api.DestroyShaderModule(device, _vertShader, null);
                api.DestroyShaderModule(device, _fragShader, null);
                
                api.DestroyBuffer(device, _vertexBuffer, null);
                api.FreeMemory(device, _vertexBufferMemory, null);
                
                api.DestroyBuffer(device, _indexBuffer, null);
                api.FreeMemory(device, _indexBufferMemory, null);
                
                api.DestroyBuffer(device, _vertexUniformBuffer, null);
                api.FreeMemory(device, _vertexUniformBufferMemory, null);
                
                api.DestroyBuffer(device, _fragmentUniformBuffer, null);
                api.FreeMemory(device, _fragmentUniformBufferMemory, null);
                
                api.DestroyDescriptorSetLayout(device, _vertextDescriptorSet, null);
                api.DestroyDescriptorSetLayout(device, _fragmentDescriptorSet, null);
                
                api.DestroyDescriptorPool(device, _descriptorPool, null);
                api.DestroyFence(device, _fence, null);
            }
        }

        public unsafe void DestroyTemporalObjects(Vk api, Device device)
        {
            if (_isInit)
            {
                api.DestroyImageView(device, _depthImageView, null);
                api.DestroyImage(device, _depthImage, null);
                api.FreeMemory(device, _depthImageMemory, null);

                api.DestroyFramebuffer(device, _framebuffer, null);
                api.DestroyPipeline(device, _pipeline, null);
                api.DestroyPipelineLayout(device, _pipelineLayout, null);
                api.DestroyRenderPass(device, _renderPass, null);
            }
        }

        private unsafe void CreateDepthAttachment(Vk api, Device device, PhysicalDevice physicalDevice)
        {
            var imageCreateInfo = new ImageCreateInfo
            {
                SType = StructureType.ImageCreateInfo,
                ImageType = ImageType.ImageType2D,
                Format = Format.D32Sfloat,
                Extent =
                    new Extent3D((uint?)Bounds.Width,
                        (uint?)Bounds.Height, 1),
                MipLevels = 1,
                ArrayLayers = 1,
                Samples = SampleCountFlags.SampleCount1Bit,
                Tiling = ImageTiling.Optimal,
                Usage = ImageUsageFlags.ImageUsageDepthStencilAttachmentBit,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined,
                Flags = ImageCreateFlags.ImageCreateMutableFormatBit
            };

            api
                .CreateImage(device, imageCreateInfo, null, out _depthImage).ThrowOnError();

            api.GetImageMemoryRequirements(device, _depthImage,
                out var memoryRequirements);

            var memoryAllocateInfo = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = (uint) FindSuitableMemoryTypeIndex(api,
                    physicalDevice,
                    memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit)
            };

            api.AllocateMemory(device, memoryAllocateInfo, null,
                out _depthImageMemory).ThrowOnError();

            api.BindImageMemory(device, _depthImage, _depthImageMemory, 0);

            var componentMapping = new ComponentMapping(
                ComponentSwizzle.R,
                ComponentSwizzle.G,
                ComponentSwizzle.B,
                ComponentSwizzle.A);

            var subresourceRange = new ImageSubresourceRange(ImageAspectFlags.ImageAspectDepthBit,
                0, 1, 0, 1);

            var imageViewCreateInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = _depthImage,
                ViewType = ImageViewType.ImageViewType2D,
                Format = Format.D32Sfloat,
                Components = componentMapping,
                SubresourceRange = subresourceRange
            };

            api
                .CreateImageView(device, imageViewCreateInfo, null, out _depthImageView)
                .ThrowOnError();
        }

        private unsafe void CreateTemporalObjects(Vk api, Device device, VulkanImageInfo info, PhysicalDevice physicalDevice)
        {
            DestroyTemporalObjects(api, device);
            
            CreateDepthAttachment(api, device, physicalDevice);
            
            // create renderpasses
            var colorAttachment = new AttachmentDescription()
            {
                Format = info.Format,
                Samples = SampleCountFlags.SampleCount1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                InitialLayout = info.Image.CurrentLayout,
                FinalLayout = info.Image.CurrentLayout,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare
            };
            
            var depthAttachment = new AttachmentDescription()
            {
                Format = Format.D32Sfloat,
                Samples = SampleCountFlags.SampleCount1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare
            };

            var subpassDependency = new SubpassDependency()
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
                DstAccessMask = AccessFlags.AccessColorAttachmentWriteBit
            };

            var colorAttachmentReference = new AttachmentReference()
            {
                Attachment = 0, Layout = ImageLayout.ColorAttachmentOptimal
            };

            var depthAttachmentReference = new AttachmentReference()
            {
                Attachment = 1, Layout = ImageLayout.DepthStencilAttachmentOptimal
            };

            var subpassDescription = new SubpassDescription()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentReference,
                PDepthStencilAttachment = &depthAttachmentReference
            };

            var attachments = new[] { colorAttachment, depthAttachment };

            fixed (AttachmentDescription* atPtr = attachments)
            {
                var renderPassCreateInfo = new RenderPassCreateInfo()
                {
                    SType = StructureType.RenderPassCreateInfo,
                    AttachmentCount = (uint) attachments.Length,
                    PAttachments = atPtr,
                    SubpassCount = 1,
                    PSubpasses = &subpassDescription,
                    DependencyCount = 1,
                    PDependencies = &subpassDependency
                };

                api.CreateRenderPass(device, renderPassCreateInfo, null, out _renderPass).ThrowOnError();
                
            
                // create framebuffer
                var frameBufferAttachments = new[] { info.Image.ImageView.Value, _depthImageView };

                fixed (ImageView* frAtPtr = frameBufferAttachments)
                {
                    var framebufferCreateInfo = new FramebufferCreateInfo()
                    {
                        SType = StructureType.FramebufferCreateInfo,
                        RenderPass = _renderPass,
                        AttachmentCount = (uint)frameBufferAttachments.Length,
                        PAttachments = frAtPtr,
                        Width = (uint)info.PixelSixe.Width,
                        Height = (uint)info.PixelSixe.Height,
                        Layers = 1
                    };

                    api.CreateFramebuffer(device, framebufferCreateInfo, null, out _framebuffer).ThrowOnError();
                }
            }

            // Create pipeline
            var pname = Marshal.StringToHGlobalAnsi("main");
            var vertShaderStageInfo = new PipelineShaderStageCreateInfo()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.ShaderStageVertexBit,
                Module = _vertShader,
                PName = (byte*)pname,
            };
            var fragShaderStageInfo = new PipelineShaderStageCreateInfo()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.ShaderStageFragmentBit,
                Module = _fragShader,
                PName = (byte*)pname,
            };

            var stages = new[] { vertShaderStageInfo, fragShaderStageInfo };

            var bindingDescription = Vertex.VertexInputBindingDescription;
            var attributeDescription = Vertex.VertexInputAttributeDescription;

            fixed (VertexInputAttributeDescription* attrPtr = attributeDescription)
            {
                var vertextInputInfo = new PipelineVertexInputStateCreateInfo()
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexAttributeDescriptionCount = (uint) attributeDescription.Length,
                    VertexBindingDescriptionCount = 1,
                    PVertexAttributeDescriptions = attrPtr,
                    PVertexBindingDescriptions = &bindingDescription
                };

                var inputAssembly = new PipelineInputAssemblyStateCreateInfo()
                {
                    SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                    Topology = PrimitiveTopology.TriangleList,
                    PrimitiveRestartEnable = false
                };

                var viewport = new Viewport()
                {
                    X = 0,
                    Y = 0,
                    Width = (float)Bounds.Width,
                    Height = (float)Bounds.Height,
                    MinDepth = 0,
                    MaxDepth = 1
                };

                var scissor = new Rect2D()
                {
                    Offset = new Offset2D(0, 0), Extent = new Extent2D((uint)viewport.Width, (uint)viewport.Height)
                };

                var pipelineViewPortCreateInfo = new PipelineViewportStateCreateInfo()
                {
                    SType = StructureType.PipelineViewportStateCreateInfo,
                    ViewportCount = 1,
                    PViewports = &viewport,
                    ScissorCount = 1,
                    PScissors = &scissor
                };

                var rasterizerStateCreateInfo = new PipelineRasterizationStateCreateInfo()
                {
                    SType = StructureType.PipelineRasterizationStateCreateInfo,
                    DepthClampEnable = false,
                    RasterizerDiscardEnable = false,
                    PolygonMode = PolygonMode.Fill,
                    LineWidth = 1,
                    CullMode = CullModeFlags.CullModeBackBit,
                    DepthBiasEnable = false
                };

                var multisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo()
                {
                    SType = StructureType.PipelineMultisampleStateCreateInfo,
                    SampleShadingEnable = false,
                    RasterizationSamples = SampleCountFlags.SampleCount1Bit
                };

                var depthStencilCreateInfo = new PipelineDepthStencilStateCreateInfo()
                {
                    SType = StructureType.PipelineDepthStencilStateCreateInfo,
                    StencilTestEnable = false,
                    DepthCompareOp = CompareOp.Less,
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthBoundsTestEnable = false,
                };

                var colorBlendAttachmentState = new PipelineColorBlendAttachmentState()
                {
                    ColorWriteMask = ColorComponentFlags.ColorComponentABit |
                                     ColorComponentFlags.ColorComponentRBit |
                                     ColorComponentFlags.ColorComponentGBit |
                                     ColorComponentFlags.ColorComponentBBit,
                    BlendEnable = false
                };

                var colorBlendState = new PipelineColorBlendStateCreateInfo()
                {
                    SType = StructureType.PipelineColorBlendStateCreateInfo,
                    LogicOpEnable = false,
                    AttachmentCount = 1,
                    PAttachments = &colorBlendAttachmentState
                };

                var dynamicStates = new DynamicState[] { DynamicState.Viewport, DynamicState.Scissor };

                fixed (DynamicState* states = dynamicStates)
                {
                    var dynamicStateCreateInfo = new PipelineDynamicStateCreateInfo()
                    {
                        SType = StructureType.PipelineDynamicStateCreateInfo,
                        DynamicStateCount = (uint)dynamicStates.Length, PDynamicStates = states
                    };

                    var sets = new[] { _vertextDescriptorSet, _fragmentDescriptorSet };

                    fixed (DescriptorSetLayout* set = sets)
                    {
                        var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo()
                        {
                            SType = StructureType.PipelineLayoutCreateInfo, SetLayoutCount = 2,
                            PSetLayouts = set
                        };

                        api.CreatePipelineLayout(device, pipelineLayoutCreateInfo, null, out _pipelineLayout)
                            .ThrowOnError();
                    }

                    fixed (PipelineShaderStageCreateInfo* stPtr = stages)
                    {
                        var pipelineCreateInfo = new GraphicsPipelineCreateInfo()
                        {
                            SType = StructureType.GraphicsPipelineCreateInfo,
                            StageCount = 2,
                            PStages = stPtr,
                            PVertexInputState = &vertextInputInfo,
                            PInputAssemblyState = &inputAssembly,
                            PViewportState = &pipelineViewPortCreateInfo,
                            PRasterizationState = &rasterizerStateCreateInfo,
                            PMultisampleState = &multisampleStateCreateInfo,
                            PDepthStencilState = &depthStencilCreateInfo,
                            PColorBlendState = &colorBlendState,
                            PDynamicState = &dynamicStateCreateInfo,
                            Layout = _pipelineLayout,
                            RenderPass = _renderPass,
                            Subpass = 0,
                            BasePipelineHandle = _pipeline.Handle != 0 ? _pipeline : new Pipeline(),
                            BasePipelineIndex = _pipeline.Handle != 0 ? 0 : -1
                        };
                        
                        api.CreateGraphicsPipelines(device, new PipelineCache(), 1, &pipelineCreateInfo, null, out _pipeline).ThrowOnError();
                    }
                }
            }

            Marshal.FreeHGlobal(pname);
            _isInit = true;
        }

        private unsafe void CreateBuffers(Vk api, Device device, VulkanPlatformInterface platformInterface)
        {
            // vertex buffer
            var vertexSize = Marshal.SizeOf<Vertex>();

            var bufferInfo = new BufferCreateInfo()
            {
                SType = StructureType.BufferCreateInfo,
                Size = (ulong)(_points.Length * vertexSize),
                Usage = BufferUsageFlags.BufferUsageVertexBufferBit,
                SharingMode = SharingMode.Exclusive
            };

            api.CreateBuffer(device, bufferInfo, null, out _vertexBuffer).ThrowOnError();

            api.GetBufferMemoryRequirements(device, _vertexBuffer, out var memoryRequirements);

            var memoryAllocateInfo = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = (uint)FindSuitableMemoryTypeIndex(api,
                    platformInterface.PhysicalDevice.ApiHandle,
                    memoryRequirements.MemoryTypeBits,
                    MemoryPropertyFlags.MemoryPropertyHostCoherentBit |
                    MemoryPropertyFlags.MemoryPropertyHostVisibleBit)
            };

            api.AllocateMemory(device, memoryAllocateInfo, null, out _vertexBufferMemory).ThrowOnError();
            api.BindBufferMemory(device, _vertexBuffer, _vertexBufferMemory, 0);

            fixed (Vertex* points = _points)
            {
                void* pointer = null;
                api.MapMemory(device, _vertexBufferMemory, 0, bufferInfo.Size, 0, ref pointer);

                Buffer.MemoryCopy(points, pointer, bufferInfo.Size, bufferInfo.Size);
                api.UnmapMemory(device, _vertexBufferMemory);
            }

            // uniform buffer
            //vertex stage
            var modelLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageVertexBit
            };
            var projectionLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageVertexBit
            };
            var viewLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 2,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageVertexBit
            };
            var timeLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 3,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageVertexBit
            };
            var discoLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 4,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageVertexBit
            };

            var vertexUniformBindings = new[]
            {
                modelLayoutBinding, projectionLayoutBinding, viewLayoutBinding, timeLayoutBinding,
                discoLayoutBinding
            };

            fixed (DescriptorSetLayoutBinding* bindings = vertexUniformBindings)
            {
                var layoutInfo = new DescriptorSetLayoutCreateInfo()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    BindingCount = (uint)vertexUniformBindings.Length,
                    PBindings = bindings
                };

                api.CreateDescriptorSetLayout(device, layoutInfo, null, out _vertextDescriptorSet).ThrowOnError();
            }

            //fragment stage
            var maxYLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageFragmentBit
            };
            var minYLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageFragmentBit
            };
            timeLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 2,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageFragmentBit
            };
            discoLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 3,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ShaderStageFragmentBit
            };

            var fragmentUniformBindings = new[]
            {
                maxYLayoutBinding, minYLayoutBinding, timeLayoutBinding, discoLayoutBinding
            };

            fixed (DescriptorSetLayoutBinding* bindings = fragmentUniformBindings)
            {
                var layoutInfo = new DescriptorSetLayoutCreateInfo()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    BindingCount = (uint)fragmentUniformBindings.Length,
                    PBindings = bindings
                };

                api.CreateDescriptorSetLayout(device, layoutInfo, null, out _fragmentDescriptorSet).ThrowOnError();
            }

            var indexSize = Marshal.SizeOf<ushort>();

            bufferInfo = new BufferCreateInfo()
            {
                SType = StructureType.BufferCreateInfo,
                Size = (ulong)(_indices.Length * indexSize),
                Usage = BufferUsageFlags.BufferUsageIndexBufferBit,
                SharingMode = SharingMode.Exclusive
            };

            api.CreateBuffer(device, bufferInfo, null, out _indexBuffer).ThrowOnError();

            api.GetBufferMemoryRequirements(device, _indexBuffer, out memoryRequirements);

            memoryAllocateInfo = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = (uint)FindSuitableMemoryTypeIndex(api,
                    platformInterface.PhysicalDevice.ApiHandle,
                    memoryRequirements.MemoryTypeBits,
                    MemoryPropertyFlags.MemoryPropertyHostCoherentBit |
                    MemoryPropertyFlags.MemoryPropertyHostVisibleBit)
            };

            api.AllocateMemory(device, memoryAllocateInfo, null, out _indexBufferMemory).ThrowOnError();
            api.BindBufferMemory(device, _indexBuffer, _indexBufferMemory, 0);

            fixed (ushort* indice = _indices)
            {
                void* pointer = null;
                api.MapMemory(device, _indexBufferMemory, 0, bufferInfo.Size, 0, ref pointer);

                Buffer.MemoryCopy(indice, pointer, bufferInfo.Size, bufferInfo.Size);
                api.UnmapMemory(device, _indexBufferMemory);
            }

            var bufferSize = Marshal.SizeOf<VertexUniformBufferObject>();

            bufferInfo = new BufferCreateInfo()
            {
                SType = StructureType.BufferCreateInfo,
                Size = (ulong)bufferSize,
                Usage = BufferUsageFlags.BufferUsageUniformBufferBit,
                SharingMode = SharingMode.Exclusive
            };

            api.CreateBuffer(device, bufferInfo, null, out _vertexUniformBuffer).ThrowOnError();

            api.GetBufferMemoryRequirements(device, _vertexUniformBuffer, out memoryRequirements);

            memoryAllocateInfo = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = (uint)FindSuitableMemoryTypeIndex(api,
                    platformInterface.PhysicalDevice.ApiHandle,
                    memoryRequirements.MemoryTypeBits,
                    MemoryPropertyFlags.MemoryPropertyHostCoherentBit |
                    MemoryPropertyFlags.MemoryPropertyHostVisibleBit)
            };

            api.AllocateMemory(device, memoryAllocateInfo, null, out _vertexUniformBufferMemory).ThrowOnError();
            api.BindBufferMemory(device, _vertexUniformBuffer, _vertexUniformBufferMemory, 0);

            fixed (ushort* indice = _indices)
            {
                void* pointer = null;
                api.MapMemory(device, _vertexUniformBufferMemory, 0, bufferInfo.Size, 0, ref pointer);

                Buffer.MemoryCopy(indice, pointer, bufferInfo.Size, bufferInfo.Size);
                api.UnmapMemory(device, _vertexUniformBufferMemory);
            }

            bufferSize = Marshal.SizeOf<FragmentUniformBufferObject>();

            bufferInfo = new BufferCreateInfo()
            {
                SType = StructureType.BufferCreateInfo,
                Size = (ulong)bufferSize,
                Usage = BufferUsageFlags.BufferUsageUniformBufferBit,
                SharingMode = SharingMode.Exclusive
            };

            api.CreateBuffer(device, bufferInfo, null, out _fragmentUniformBuffer).ThrowOnError();

            api.GetBufferMemoryRequirements(device, _fragmentUniformBuffer, out memoryRequirements);

            memoryAllocateInfo = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = (uint)FindSuitableMemoryTypeIndex(api,
                    platformInterface.PhysicalDevice.ApiHandle,
                    memoryRequirements.MemoryTypeBits,
                    MemoryPropertyFlags.MemoryPropertyHostCoherentBit |
                    MemoryPropertyFlags.MemoryPropertyHostVisibleBit)
            };

            api.AllocateMemory(device, memoryAllocateInfo, null, out _fragmentUniformBufferMemory).ThrowOnError();
            api.BindBufferMemory(device, _fragmentUniformBuffer, _fragmentUniformBufferMemory, 0);

            fixed (ushort* indice = _indices)
            {
                void* pointer = null;
                api.MapMemory(device, _fragmentUniformBufferMemory, 0, bufferInfo.Size, 0, ref pointer);

                Buffer.MemoryCopy(indice, pointer, bufferInfo.Size, bufferInfo.Size);
                api.UnmapMemory(device, _fragmentUniformBufferMemory);
            }

            //Descriptor pool
            var poolSize = new DescriptorPoolSize() { DescriptorCount = 2 };
            var sets = new[] { _vertextDescriptorSet, _fragmentDescriptorSet };

            var descriptorPoolCreateInfo = new DescriptorPoolCreateInfo()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = 1,
                PPoolSizes = &poolSize,
                MaxSets = 2
            };

            api.CreateDescriptorPool(device, descriptorPoolCreateInfo, null, out _descriptorPool).ThrowOnError();
            _descriptorSets = new DescriptorSet[sets.Length];

            fixed (DescriptorSetLayout* set = sets)
            {
                var descriptorSetAllocationInfo = new DescriptorSetAllocateInfo()
                {
                    SType = StructureType.DescriptorSetAllocateInfo,
                    DescriptorPool = _descriptorPool,
                    DescriptorSetCount = (uint)sets.Length,
                    PSetLayouts = set
                };

                fixed (DescriptorSet* descSet = _descriptorSets)
                {
                    api.AllocateDescriptorSets(device, &descriptorSetAllocationInfo, descSet);
                }
            }

            var descriptorSet = _descriptorSets[0];

            var modelBufferInfo = new DescriptorBufferInfo()
            {
                Offset = 0, Buffer = _vertexUniformBuffer, Range = (ulong)Marshal.SizeOf<Matrix4x4>()
            };
            var modelWriteDescSet = new WriteDescriptorSet()
            {
                SType = StructureType.WriteDescriptorSet, DstSet = descriptorSet,
                DstBinding = 0,
                DescriptorCount = 1,
                DstArrayElement = 0,
                PBufferInfo = &modelBufferInfo,
                DescriptorType = DescriptorType.UniformBuffer
            };
            var projBufferInfo = new DescriptorBufferInfo()
            {
                Offset = (ulong) Marshal.OffsetOf<VertexUniformBufferObject>("Projection"),
                Buffer = _vertexUniformBuffer,
                Range = (ulong)Marshal.SizeOf<Matrix4x4>()
            };
            var projectWriteDescSet = new WriteDescriptorSet()
            {
                SType = StructureType.WriteDescriptorSet, DstSet = descriptorSet,
                DstBinding = 1,
                DescriptorCount = 1,
                DstArrayElement = 0,
                PBufferInfo = &projBufferInfo,
                DescriptorType = DescriptorType.UniformBuffer
            };
            var viewBufferInfo = new DescriptorBufferInfo()
            {
                Offset = (ulong) Marshal.OffsetOf<VertexUniformBufferObject>("View"),
                Buffer = _vertexUniformBuffer,
                Range = (ulong)Marshal.SizeOf<Matrix4x4>()
            };
            var viewWriteDescSet = new WriteDescriptorSet()
            {
                SType = StructureType.WriteDescriptorSet, DstSet = descriptorSet,
                DstBinding = 2,
                DescriptorCount = 1,
                DstArrayElement = 0,
                PBufferInfo = &viewBufferInfo,
                DescriptorType = DescriptorType.UniformBuffer
            };
            var timeBufferInfo = new DescriptorBufferInfo()
            {
                Offset = (ulong) Marshal.OffsetOf<VertexUniformBufferObject>("Time"),
                Buffer = _vertexUniformBuffer,
                Range = (ulong)Marshal.SizeOf<float>()
            };
            var timeWriteDescSet = new WriteDescriptorSet()
            {
                SType = StructureType.WriteDescriptorSet, DstSet = descriptorSet,
                DstBinding = 2,
                DescriptorCount = 1,
                DstArrayElement = 0,
                PBufferInfo = &timeBufferInfo,
                DescriptorType = DescriptorType.UniformBuffer
            };
            var discoBufferInfo = new DescriptorBufferInfo()
            {
                Offset = (ulong) Marshal.OffsetOf<VertexUniformBufferObject>("Disco"),
                Buffer = _vertexUniformBuffer,
                Range = (ulong)Marshal.SizeOf<float>()
            };
            var discoWriteDescSet = new WriteDescriptorSet()
            {
                SType = StructureType.WriteDescriptorSet, DstSet = descriptorSet,
                DstBinding = 2,
                DescriptorCount = 1,
                DstArrayElement = 0,
                PBufferInfo = &discoBufferInfo,
                DescriptorType = DescriptorType.UniformBuffer
            };
            var writes = new[]
            {
                modelWriteDescSet, projectWriteDescSet, viewWriteDescSet, timeWriteDescSet, discoWriteDescSet
            };

            fixed (WriteDescriptorSet* write = writes)
            {
                api.UpdateDescriptorSets(device, (uint)writes.Length, write, 0, null);
            }
            
            descriptorSet = _descriptorSets[1];

            var maxYBufferInfo = new DescriptorBufferInfo()
            {
                Offset = 0, Buffer = _fragmentUniformBuffer, Range = (ulong)Marshal.SizeOf<float>()
            };
            var maxYWriteDescSet = new WriteDescriptorSet()
            {
                SType = StructureType.WriteDescriptorSet, DstSet = descriptorSet,
                DstBinding = 0,
                DescriptorCount = 1,
                DstArrayElement = 0,
                PBufferInfo = &maxYBufferInfo,
                DescriptorType = DescriptorType.UniformBuffer
            };
            var minYBufferInfo = new DescriptorBufferInfo()
            {
                Offset = (ulong) Marshal.OffsetOf<FragmentUniformBufferObject>("MinY"),
                Buffer = _fragmentUniformBuffer,
                Range = (ulong)Marshal.SizeOf<float>()
            };
            var minYWriteDescSet = new WriteDescriptorSet()
            {
                SType = StructureType.WriteDescriptorSet, DstSet = descriptorSet,
                DstBinding = 1,
                DescriptorCount = 1,
                DstArrayElement = 0,
                PBufferInfo = &minYBufferInfo,
                DescriptorType = DescriptorType.UniformBuffer
            };
            timeBufferInfo = new DescriptorBufferInfo()
            {
                Offset = (ulong) Marshal.OffsetOf<FragmentUniformBufferObject>("Time"),
                Buffer = _fragmentUniformBuffer,
                Range = (ulong)Marshal.SizeOf<float>()
            };
            timeWriteDescSet = new WriteDescriptorSet()
            {
                SType = StructureType.WriteDescriptorSet, DstSet = descriptorSet,
                DstBinding = 2,
                DescriptorCount = 1,
                DstArrayElement = 0,
                PBufferInfo = &timeBufferInfo,
                DescriptorType = DescriptorType.UniformBuffer
            };
            discoBufferInfo = new DescriptorBufferInfo()
            {
                Offset = (ulong) Marshal.OffsetOf<FragmentUniformBufferObject>("Disco"),
                Buffer = _fragmentUniformBuffer,
                Range = (ulong)Marshal.SizeOf<float>()
            };
            discoWriteDescSet = new WriteDescriptorSet()
            {
                SType = StructureType.WriteDescriptorSet, DstSet = descriptorSet,
                DstBinding = 2,
                DescriptorCount = 1,
                DstArrayElement = 0,
                PBufferInfo = &discoBufferInfo,
                DescriptorType = DescriptorType.UniformBuffer
            };
            writes = new[]
            {
                modelWriteDescSet, projectWriteDescSet, viewWriteDescSet, timeWriteDescSet, discoWriteDescSet
            };

            // TODO use push constants
            fixed (WriteDescriptorSet* write = writes)
            {
                api.UpdateDescriptorSets(device, (uint)writes.Length, write, 0, null);
            }
        }

        private unsafe void UpdateUniforms(Vk api, Device device)
        {
            var view = Matrix4x4.CreateLookAt(new Vector3(25, 25, 25), new Vector3(), new Vector3(0, -1, 0));
            var model = Matrix4x4.CreateFromYawPitchRoll(_yaw, _pitch, _roll);
            var projection =
                Matrix4x4.CreatePerspectiveFieldOfView((float)(Math.PI / 4), (float)(Bounds.Width / Bounds.Height),
                    0.01f, 1000);
            
            // update vertex uniform
            var vubo = new VertexUniformBufferObject()
            {
                Disco = this.Disco,
                Time = (float) St.Elapsed.TotalSeconds,
                Model = model,
                Projection = projection,
                View = view
            };

            var bufferSize = (ulong)Marshal.SizeOf<VertexUniformBufferObject>();

            void* pointer = null;
            api.MapMemory(device, _vertexUniformBufferMemory, 0, bufferSize, 0,
                ref pointer);
            Buffer.MemoryCopy(&vubo, pointer, bufferSize, bufferSize);
            api.UnmapMemory(device, _vertexUniformBufferMemory);
            
            // update fragment uniform
            var fubo = new FragmentUniformBufferObject()
            {
                Disco = this.Disco,
                Time = (float) St.Elapsed.TotalSeconds,
                MaxY = _maxY,
                MinY = _minY
            };

            bufferSize = (ulong)Marshal.SizeOf<FragmentUniformBufferObject>();

            pointer = null;
            api.MapMemory(device, _fragmentUniformBufferMemory, 0, bufferSize, 0,
                ref pointer);
            Buffer.MemoryCopy(&fubo, pointer, bufferSize, bufferSize);
            api.UnmapMemory(device, _fragmentUniformBufferMemory);

        }

        private static int FindSuitableMemoryTypeIndex(Vk api, PhysicalDevice physicalDevice, uint memoryTypeBits,
            MemoryPropertyFlags flags)
        {
            api.GetPhysicalDeviceMemoryProperties(physicalDevice, out var properties);

            for (var i = 0; i < properties.MemoryTypeCount; i++)
            {
                var type = properties.MemoryTypes[i];

                if ((memoryTypeBits & (1 << i)) != 0 && type.PropertyFlags.HasFlag(flags)) return i;
            }

            return -1;
        }
        
        protected unsafe override void OnVulkanInit(VulkanPlatformInterface platformInterface, VulkanImageInfo info)
        {
            base.OnVulkanInit(platformInterface, info);

            var api = platformInterface.Instance.Api;
            var device = platformInterface.Device.ApiHandle;

            var vertShaderData = GetShader(false);
            var fragShaderData = GetShader(true);

            fixed (byte* ptr = vertShaderData)
            {
                var shaderCreateInfo = new ShaderModuleCreateInfo()
                {
                    SType = StructureType.ShaderModuleCreateInfo,
                    CodeSize = (nuint)vertShaderData.Length,
                    PCode = (uint*)ptr,
                };

                api.CreateShaderModule(device, shaderCreateInfo, null, out _vertShader);
            }

            fixed (byte* ptr = fragShaderData)
            {
                var shaderCreateInfo = new ShaderModuleCreateInfo()
                {
                    SType = StructureType.ShaderModuleCreateInfo,
                    CodeSize = (nuint)fragShaderData.Length,
                    PCode = (uint*)ptr,
                };

                api.CreateShaderModule(device, shaderCreateInfo, null, out _fragShader);
            }
            
            CreateBuffers(api, device, platformInterface);

            CreateTemporalObjects(api, device, info, platformInterface.PhysicalDevice.ApiHandle);

            var fenceCreateInfo = new FenceCreateInfo()
            {
                SType = StructureType.FenceCreateInfo, Flags = FenceCreateFlags.FenceCreateSignaledBit
            };

            api.CreateFence(device, fenceCreateInfo, null, out _fence);

            _previousImageSize = info.PixelSixe;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;

            public static unsafe VertexInputBindingDescription VertexInputBindingDescription
            {
                get
                {
                    return new VertexInputBindingDescription()
                    {
                        Binding = 0,
                        Stride = (uint)sizeof(Vertex),
                        InputRate = VertexInputRate.Vertex
                    };
                }
            }

            public static unsafe VertexInputAttributeDescription[] VertexInputAttributeDescription
            {
                get
                {
                    return new VertexInputAttributeDescription[]{
                        new VertexInputAttributeDescription {
                            Binding = 0,
                            Location = 0,
                            Format = Format.R32G32B32Sfloat,
                            Offset = 0,
                        },
                         new VertexInputAttributeDescription{
                            Binding = 0,
                            Location = 1,
                            Format = Format.R32G32B32Sfloat,
                            Offset = (uint)Marshal.OffsetOf<Vertex>("Normal")
                        }
                    };
                }
            }
        }

        private readonly Vertex[] _points;
        private readonly ushort[] _indices;
        private readonly float _minY;
        private readonly float _maxY;


        static Stopwatch St = Stopwatch.StartNew();
        private bool _isInit;
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct VertexUniformBufferObject
        {
            public Matrix4x4 Model;
            public Matrix4x4 Projection;
            public Matrix4x4 View;

            public float Time;
            public float Disco;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct FragmentUniformBufferObject
        {
            public float MaxY;
            public float MinY;

            public float Time;
            public float Disco;
        }

        public VulkanPageControl()
        {
            var name = typeof(VulkanPage).Assembly.GetManifestResourceNames().First(x => x.Contains("teapot.bin"));
            using (var sr = new BinaryReader(typeof(VulkanPage).Assembly.GetManifestResourceStream(name)))
            {
                var buf = new byte[sr.ReadInt32()];
                sr.Read(buf, 0, buf.Length);
                var points = new float[buf.Length / 4];
                Buffer.BlockCopy(buf, 0, points, 0, buf.Length);
                buf = new byte[sr.ReadInt32()];
                sr.Read(buf, 0, buf.Length);
                _indices = new ushort[buf.Length / 2];
                Buffer.BlockCopy(buf, 0, _indices, 0, buf.Length);
                _points = new Vertex[points.Length / 3];
                for (var primitive = 0; primitive < points.Length / 3; primitive++)
                {
                    var srci = primitive * 3;
                    _points[primitive] = new Vertex
                    {
                        Position = new Vector3(points[srci], points[srci + 1], points[srci + 2])
                    };
                }

                for (int i = 0; i < _indices.Length; i += 3)
                {
                    Vector3 a = _points[_indices[i]].Position;
                    Vector3 b = _points[_indices[i + 1]].Position;
                    Vector3 c = _points[_indices[i + 2]].Position;
                    var normal = Vector3.Normalize(Vector3.Cross(c - b, a - b));

                    _points[_indices[i]].Normal += normal;
                    _points[_indices[i + 1]].Normal += normal;
                    _points[_indices[i + 2]].Normal += normal;
                }

                for (int i = 0; i < _points.Length; i++)
                {
                    _points[i].Normal = Vector3.Normalize(_points[i].Normal);
                    _maxY = Math.Max(_maxY, _points[i].Position.Y);
                    _minY = Math.Min(_minY, _points[i].Position.Y);
                }
            }

        }
    }
}
