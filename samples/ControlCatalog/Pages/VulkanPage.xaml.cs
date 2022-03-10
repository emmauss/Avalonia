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

        private int _shaderProgram;
        private int _vertexBufferObject;
        private int _indexBufferObject;
        private int _vertexArrayObject;

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

        protected override void OnVulkanRender(VulkanPlatformInterface platformInterface, VulkanImageInfo info)
        {
            throw new NotImplementedException();
        }

        protected unsafe override void OnVulkanDeinit(VulkanPlatformInterface platformInterface, VulkanImageInfo info)
        {
            base.OnVulkanDeinit(platformInterface, info);

            var api = platformInterface.Instance.Api;
            var device = platformInterface.Device.ApiHandle;

            api.DestroyShaderModule(device, _vertShader, null);
            api.DestroyShaderModule(device, _fragShader, null);
        }

        private unsafe void CreateTemporalObjects()
        {
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

            var bindingDescription = Vertex.VertexInputBindingDescription;
            var attributeDescription = Vertex.VertexInputAttributeDescription;


            fixed (VertexInputAttributeDescription* attrPtr = attributeDescription)
            {
                var vertextInputInfo = new PipelineVertexInputStateCreateInfo()
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexAttributeDescriptionCount = 1,
                    VertexBindingDescriptionCount = (uint)attributeDescription.Length,
                    PVertexAttributeDescriptions = attrPtr,
                    PVertexBindingDescriptions = &bindingDescription
                };

            }


            Marshal.FreeHGlobal(pname);
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

            CreateTemporalObjects();
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
                            Offset = (uint)sizeof(Vector3)
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
