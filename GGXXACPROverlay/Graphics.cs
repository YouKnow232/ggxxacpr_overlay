using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Direct3D;
using Vortice.Direct3D9;

namespace GGXXACPROverlay
{
    internal static class Graphics
    {
        private const string HLSL_COMPILE_TARGET_SUFFIX = "s_3_0";
        private static IDirect3DDevice9? device;
        private static VertexPositionColor[] testTri = [
            new VertexPositionColor(new Vector4(100f, 100f, 1f, 1f), new D3DCOLOR_ARGB(0xFFFF0000)),
            new VertexPositionColor(new Vector4(500f, 100f, 1f, 1f), new D3DCOLOR_ARGB(0xFF00FF00)),
            new VertexPositionColor(new Vector4(100f, 500f, 1f, 1f), new D3DCOLOR_ARGB(0xFF00FF00)),
            new VertexPositionColor(new Vector4(100f, 500f, 1f, 1f), new D3DCOLOR_ARGB(0xFF00FF00)),
            new VertexPositionColor(new Vector4(500f, 100f, 1f, 1f), new D3DCOLOR_ARGB(0xFF00FF00)),
            new VertexPositionColor(new Vector4(500f, 500f, 1f, 1f), new D3DCOLOR_ARGB(0xFFFF0000)),
        ];
        private static IDirect3DVertexBuffer9? vertexBuffer;
        private const uint MAX_RECTANGLE = 100;
        private static IDirect3DVertexBuffer9? rectangleListVertexBuffer;
        private static IDirect3DVertexShader9? customVertexBufferPtr;
        private static IDirect3DPixelShader9? customPixelBufferPtr;

        [StructLayout(LayoutKind.Sequential, Size = 32)]
        public readonly struct VertexPositionColor(Vector4 position, D3DCOLOR_ARGB color)
        {
            public static unsafe readonly uint SizeInBytes = (uint)sizeof(VertexPositionColor);
            public const VertexFormat VFV = VertexFormat.PositionRhw | VertexFormat.Diffuse;

            public readonly Vector4 Position = position;
            public readonly D3DCOLOR_ARGB Color = color;
        }

        public static void Initialize()
        {
            if (device != null) return;

            // find device pointer in memory
            // device = new IDirect3DDevice9();
            // use device to construct geometry buffers
        }

        public static void Dispose()
        {
            vertexBuffer?.Release();
            customVertexBufferPtr?.Release();
            customPixelBufferPtr?.Release();
        }

        internal static void MarshalDevice(nint unmanagedPointer)
        {
            if (device == null)
            {
                device = new IDirect3DDevice9(unmanagedPointer);
            }
        }

        private static bool CompileCustomShaders(IDirect3DDevice9 d)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            if (asm == null) { Console.WriteLine("asm was null"); return false; }
            byte[] buffer = new byte[16 * 1024];
            byte[] shaderSource;
            using (Stream? shaderStream = asm.GetManifestResourceStream("GGXXACPROverlay.Shaders.SolidColorShader.hlsl"))
            using (MemoryStream memStream = new())
            {
                if (shaderStream == null) { Console.WriteLine("shaderStream was null"); return false; }

                int read;
                while ((read = shaderStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memStream.Write(buffer, 0, read);
                }
                shaderSource = memStream.ToArray();
            }
            Console.WriteLine($"shaderSource length: {shaderSource.Length}");
            var result = Vortice.D3DCompiler.Compiler.Compile(
                shaderSource,
                "ColorVertexShader",
                "CompileCustomShaders",
                "v" + HLSL_COMPILE_TARGET_SUFFIX,
                out Blob vertShaderBlob,
                out Blob vertShaderErrBlob
            );
            if (result.Failure)
            {
                throw new Exception($"Vertex shader compilation error\nError Code: 0x{result.Code:X8}: {result.Description}\n" +
                    $"{Encoding.Default.GetString(vertShaderErrBlob.AsSpan())}");
            }
            Console.WriteLine("vertex shader successfully compiled");
            result = Vortice.D3DCompiler.Compiler.Compile(
                shaderSource,
                "ColorPixelShader",
                "CompileCustomShaders",
                "p" + HLSL_COMPILE_TARGET_SUFFIX,
                out Blob pixShaderBlob,
                out Blob pixShaderErrBlob
            );
            if (result.Failure)
            {
                throw new Exception($"Pixel shader compilation error\nError Code: 0x{result.Code:X8}: {result.Description}\n" +
                    $"{Encoding.Default.GetString(pixShaderErrBlob.AsSpan())}");
            }
            Console.WriteLine("pixel shader successfully compiled");

            customVertexBufferPtr = d.CreateVertexShader(vertShaderBlob.AsSpan());
            Console.WriteLine("Device Vertex Shader Added");
            customPixelBufferPtr = d.CreatePixelShader(pixShaderBlob.AsSpan());
            Console.WriteLine("Device Pixel Shader Added");

            return true;
        }

        private static void InitVertexBuffers(IDirect3DDevice9 d)
        {
            // Test Buffer
            ReadOnlySpan<VertexPositionColor> vertexSpan = testTri;
            uint spanSizeInBytes = (uint)(VertexPositionColor.SizeInBytes * testTri.Length);

            vertexBuffer = d.CreateVertexBuffer(
                spanSizeInBytes,
                Usage.None,
                VertexPositionColor.VFV,
                Pool.Managed
            );
            var bufferLock = vertexBuffer.Lock<byte>(0, spanSizeInBytes, LockFlags.Discard);
            MemoryMarshal.AsBytes(vertexSpan).CopyTo(bufferLock);
            vertexBuffer.Unlock();

            // ColorRectangle Buffer
            rectangleListVertexBuffer = d.CreateVertexBuffer(
                (uint)(MAX_RECTANGLE * VertexPositionColor.SizeInBytes * 4),
                Usage.None,
                VertexPositionColor.VFV,
                Pool.Managed
            );
        }

        private static bool init = false;
        internal static unsafe void TempInit(IDirect3DDevice9 d)
        {
            Console.WriteLine("tempInit");

            Console.WriteLine("\n== Device State ==");
            Console.WriteLine($"Viewport w:{d.Viewport.Width} h:{d.Viewport.Height} x:{d.Viewport.X} y:{d.Viewport.Y}");
            Console.WriteLine($"Viewport maxZ:{d.Viewport.MaxZ} minZ:{d.Viewport.MinZ}");
            Console.WriteLine($"RenderState.Lighting: {d.GetRenderState<bool>(RenderState.Lighting)}");
            Console.WriteLine($"RenderState.Ambient: 0x{d.GetRenderState<uint>(RenderState.Ambient):X8}");
            Console.WriteLine($"RenderState.ColorVertex: {d.GetRenderState<bool>(RenderState.ColorVertex)}");
            Console.WriteLine($"RenderState.CullMode: {d.GetRenderState<Cull>(RenderState.CullMode)}");
            Console.WriteLine($"RenderState.FillMode: {d.GetRenderState<FillMode>(RenderState.FillMode)}");
            Console.WriteLine($"RenderState.ShadeMode: {d.GetRenderState<ShadeMode>(RenderState.ShadeMode)}");
            Console.WriteLine($"RenderState.BlendOperation: {d.GetRenderState<BlendOperation>(RenderState.BlendOperation)}");
            Console.WriteLine($"RenderState.BlendFactor: {d.GetRenderState<int>(RenderState.BlendFactor)}");
            Console.WriteLine($"RenderState.SourceBlend: {d.GetRenderState<Blend>(RenderState.SourceBlend)}");
            Console.WriteLine($"RenderState.DestinationBlend: {d.GetRenderState<Blend>(RenderState.DestinationBlend)}");
            Console.WriteLine($"VertexPositionColor.SizeInBytes: {VertexPositionColor.SizeInBytes}");
            Console.WriteLine($"PixelShader.NativePointer: 0x{d.PixelShader.NativePointer:X8}");

            Capabilities caps = new();
            d.GetDeviceCaps(ref caps);

            Console.WriteLine("\n== Device Capabilities ==");
            Console.WriteLine($"caps.ShadeCaps: {caps.ShadeCaps}");
            Console.WriteLine($"caps.Caps: {caps.Caps}");
            Console.WriteLine($"caps.Caps2: {caps.Caps2}");
            Console.WriteLine($"caps.Caps3: {caps.Caps3}");
            Console.WriteLine($"caps.DeviceCaps: {caps.DeviceCaps}");
            Console.WriteLine($"caps.FVFCaps: 0x{(int)caps.FVFCaps:X8}");
            Console.WriteLine($"caps.VS20Caps.Caps: {caps.VS20Caps.Caps}");
            Console.WriteLine($"caps.PS20Caps.Caps: {caps.PS20Caps.Caps}");


            InitVertexBuffers(d);
            Console.WriteLine("Vertex buffers initialized");

            if (!CompileCustomShaders(d)) { Console.WriteLine("Custom shaders failed to compile!"); }
            else { Console.WriteLine("Custom shaders compiled"); }

            init = true;
            Console.WriteLine("tempInit finished");
        }

        internal static void RenderOverlayFrame()
        {
            if (device == null) return;
            if (!init) { TempInit(device); }

            device.BeginScene();

            device.SetStreamSource(0, vertexBuffer, 0, VertexPositionColor.SizeInBytes);
            device.VertexFormat = VertexPositionColor.VFV;
            device.VertexShader = customVertexBufferPtr;
            device.PixelShader = customPixelBufferPtr;

            device.DrawPrimitive(PrimitiveType.TriangleList, 0, 2);

            device.EndScene();
        }


        public static void DrawRectangles(ColorRectangle[] rectangleList)
        {
            foreach (ColorRectangle cRect in rectangleList)
            {
                GetVerticies(cRect);
            }
        }
        private static VertexPositionColor[] GetVerticies(ColorRectangle cRect)
        {
            return [
                new VertexPositionColor(new Vector4(cRect.rectangle.Left,  cRect.rectangle.Top, 1f, 1f), cRect.color),
                new VertexPositionColor(new Vector4(cRect.rectangle.Right, cRect.rectangle.Top, 1f, 1f), cRect.color),
                new VertexPositionColor(new Vector4(cRect.rectangle.Right, cRect.rectangle.Bottom, 1f, 1f), cRect.color),
                new VertexPositionColor(new Vector4(cRect.rectangle.Left,  cRect.rectangle.Bottom, 1f, 1f), cRect.color),
            ];
        }
    }
}
