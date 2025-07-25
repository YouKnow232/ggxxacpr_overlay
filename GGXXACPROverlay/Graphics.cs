using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using SharpGen.Runtime;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D9;

namespace GGXXACPROverlay
{
    internal enum GraphicsContext
    {
        None,
        Hitbox,
        Pivot,
        ComboTime,
        Meter,
        HUD
    }

    internal unsafe class Graphics : IDisposable
    {
        private const string HLSL_COMPILE_TARGET_SUFFIX = "s_3_0";
        private const uint RECTANGLE_LIMIT = 100;
        private const uint VERTEX_PER_RECTANGLE = 6;    // 6 for 2 triangles

        private IDirect3DDevice9 _device;

        private IDirect3DVertexBuffer9? vertex3PositionColorVB;
        private IDirect3DVertexDeclaration9? vertex3PositionColorVD;

        private IDirect3DVertexShader9? hitboxVS;
        private IDirect3DPixelShader9? hitboxPS;
        private IDirect3DVertexShader9? meterVS;
        private IDirect3DPixelShader9? meterPS;
        private IDirect3DPixelShader9? comboTimePS;

        // TODO: handle lost device state
        public Graphics(nint rawDevicePointer)
        {
            _device = new IDirect3DDevice9(rawDevicePointer);

            LogDeviceState();

            Debug.Log("Initializing Graphics resources");
            InitBuffers();
            Debug.Log("Vertex buffers initialized");

            CompileCustomShaders();
            Debug.Log("Shaders compiled");

            Debug.Log("tempInit finished");
        }

        public void LogDeviceState()
        {
            Debug.Log();
            Debug.Log("== Device State ==");

            CreationParameters deviceParams = new CreationParameters();
            _device.GetCreationParameters(ref deviceParams);
            CreateFlags cFlags = (CreateFlags)deviceParams.BehaviorFlags;

            Debug.Log($"Device is multithreaded: {cFlags.HasFlag(CreateFlags.Multithreaded)}");
            Debug.Log($"Viewport w:{_device.Viewport.Width} h:{_device.Viewport.Height} x:{_device.Viewport.X} y:{_device.Viewport.Y}");
            Debug.Log($"Viewport maxZ:{_device.Viewport.MaxZ} minZ:{_device.Viewport.MinZ}");
            Debug.Log($"RenderState.Lighting: {_device.GetRenderState<bool>(RenderState.Lighting)}");
            Debug.Log($"RenderState.Ambient: 0x{_device.GetRenderState<uint>(RenderState.Ambient):X8}");
            Debug.Log($"RenderState.ColorVertex: {_device.GetRenderState<bool>(RenderState.ColorVertex)}");
            Debug.Log($"RenderState.CullMode: {_device.GetRenderState<Cull>(RenderState.CullMode)}");
            Debug.Log($"RenderState.FillMode: {_device.GetRenderState<FillMode>(RenderState.FillMode)}");
            Debug.Log($"RenderState.ShadeMode: {_device.GetRenderState<ShadeMode>(RenderState.ShadeMode)}");
            Debug.Log($"RenderState.AlphaBlendEnable: {_device.GetRenderState<bool>(RenderState.AlphaBlendEnable)}");
            Debug.Log($"RenderState.BlendOperation: {_device.GetRenderState<BlendOperation>(RenderState.BlendOperation)}");
            Debug.Log($"RenderState.BlendFactor: {_device.GetRenderState<int>(RenderState.BlendFactor)}");
            Debug.Log($"RenderState.SourceBlend: {_device.GetRenderState<Blend>(RenderState.SourceBlend)}");
            Debug.Log($"RenderState.DestinationBlend: {_device.GetRenderState<Blend>(RenderState.DestinationBlend)}");
            Debug.Log();

            Capabilities caps = new();
            _device.GetDeviceCaps(ref caps);

            Debug.Log("== Device Capabilities ==");
            Debug.Log($"caps.ShadeCaps: {caps.ShadeCaps}");
            Debug.Log($"caps.Caps: {caps.Caps}");
            Debug.Log($"caps.Caps2: {caps.Caps2}");
            Debug.Log($"caps.Caps3: {caps.Caps3}");
            Debug.Log($"caps.DeviceCaps: {caps.DeviceCaps}");
            Debug.Log($"caps.FVFCaps: 0x{(int)caps.FVFCaps:X8}");
            Debug.Log($"caps.VS20Caps.Caps: {caps.VS20Caps.Caps}");
            Debug.Log($"caps.PS20Caps.Caps: {caps.PS20Caps.Caps}");
            Debug.Log();
        }

        public void UpdateDevice(nint nativePointer)
        {
            if (_device.NativePointer != nativePointer)
            {
                Debug.Log("[WARNING] Updating device pointer!");
                _device = new IDirect3DDevice9(nativePointer);
            }
        }

        private static byte[] ReadShaderSource(string shaderURL)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            byte[] buffer = new byte[16 * 1024];
            using Stream? shaderStream = asm.GetManifestResourceStream(shaderURL);
            using MemoryStream memStream = new();

            if (shaderStream == null) { throw new FileNotFoundException($"Could not find shader resource: {shaderURL}"); }

            int read;
            while ((read = shaderStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                memStream.Write(buffer, 0, read);
            }
            return memStream.ToArray();
        }

        private static Blob CompileShader(byte[] source, string entryPoint, string target)
        {
            var result = Compiler.Compile(
                source,
                entryPoint,
                "CompileCustomShaders",
                target,
                out Blob output,
                out Blob err
            );
            if (result.Failure)
            {
                throw new InvalidOperationException($"Shader compilation error\nError Code: 0x{result.Code:X8}: {result.Description}\n" +
                    $"{Encoding.Default.GetString(err.AsSpan())}");
            }

            Debug.Log($"{entryPoint} shader compiled");
            return output;
        }

        private static IDirect3DVertexShader9 AddVertexShaderFromSource(IDirect3DDevice9 device, byte[] source, string entryPoint)
            => device.CreateVertexShader(CompileShader(source, entryPoint, "v" + HLSL_COMPILE_TARGET_SUFFIX).AsSpan());
        private static IDirect3DPixelShader9 AddPixelShaderFromSource(IDirect3DDevice9 device, byte[] source, string entryPoint)
            => device.CreatePixelShader(CompileShader(source, entryPoint, "p" + HLSL_COMPILE_TARGET_SUFFIX).AsSpan());

        private void CompileCustomShaders()
        {
            byte[] hitboxShaderSource = ReadShaderSource("GGXXACPROverlay.Shaders.HitboxShader.hlsl");
            byte[] comboTimeShaderSource = ReadShaderSource("GGXXACPROverlay.Shaders.ComboTimeMeter.hlsl");

            hitboxVS = AddVertexShaderFromSource(_device, hitboxShaderSource, "HitboxVS");
            hitboxPS = AddPixelShaderFromSource(_device, hitboxShaderSource, "HitboxPS");
            meterVS = AddVertexShaderFromSource(_device, comboTimeShaderSource, "MeterVS");
            meterPS = AddPixelShaderFromSource(_device, comboTimeShaderSource, "MeterPS");
            comboTimePS = AddPixelShaderFromSource(_device, comboTimeShaderSource, "ComboTimePS");

        }

        private void InitBuffers()
        {
            // Vert3 Buffer
            vertex3PositionColorVB = _device.CreateVertexBuffer(
                RECTANGLE_LIMIT * Vertex3PositionColor.SizeInBytes * VERTEX_PER_RECTANGLE,
                Usage.None, 0, Pool.Managed
            );

            // Vertex3PositionColor Vertex Declaration
            vertex3PositionColorVD = _device.CreateVertexDeclaration([
                new VertexElement(0, Vertex3PositionColor.PositionOffset, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position),
                new VertexElement(0, Vertex3PositionColor.ColorOffset, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color),
                new VertexElement(0, Vertex3PositionColor.UVOffset, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate),
                VertexElement.VertexDeclarationEnd
            ]);
        }

        public void BeginScene() => _device?.BeginScene();
        public void EndScene() => _device?.EndScene();

        public void SetScissorRect(Rect clip) => _device.ScissorRect = clip;

        private static readonly float[] _screenSizeUniform = new float[4];
        private static readonly float[] _borderThicknessUniform = new float[4];
        private static readonly float[] _miscShaderUniformBuffer = new float[4];
        public void SetDeviceContext(GraphicsContext context, params float[] args)
        {
            try
            {
                _device.TestCooperativeLevel();
            }
            catch (SharpGenException)
            {
                Debug.Log($"[WARNING] Device was not in a cooperative state for setting device context.");
                return;
            }

            if (_device == null || _device.NativePointer == nint.Zero)
            {
                Debug.Log("D3D9 Device is in an invalid state");
                return;
            }
            if (args.Length > 4) throw new ArgumentException("args cannot be more than 4");

            _screenSizeUniform[0] = _device.Viewport.Width;
            _screenSizeUniform[1] = _device.Viewport.Height;
            _borderThicknessUniform[0] = Settings.HitboxBorderThickness;
            args.CopyTo(_miscShaderUniformBuffer, 0);

            _device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);

            switch (context)
            {
                case GraphicsContext.Hitbox:
                    SetDeviceContext_Alpha();
                    _device.SetRenderState(RenderState.ScissorTestEnable, Settings.WidescreenClipping);
                    SetDeviceContext_HitboxShaders();
                    break;
                case GraphicsContext.Pivot:
                    _device.SetRenderState(RenderState.ScissorTestEnable, Settings.WidescreenClipping);
                    SetDeviceContext_HitboxShaders();
                    break;
                case GraphicsContext.ComboTime:
                    SetDeviceContext_Alpha();
                    _device.SetRenderState(RenderState.ScissorTestEnable, false);
                    SetDeviceContext_MeterVertexShader();
                    _device.PixelShader = comboTimePS;
                    break;
                case GraphicsContext.Meter:
                    SetDeviceContext_Alpha();
                    _device.SetRenderState(RenderState.ScissorTestEnable, false);
                    SetDeviceContext_MeterVertexShader();
                    _device.PixelShader = meterPS;
                    break;
            }
        }
        private void SetDeviceContext_Alpha()
        {
            _device.SetRenderState(RenderState.AlphaBlendEnable, true);
            _device.SetRenderState(RenderState.BlendOperation, BlendOperation.Add);
        }
        private void SetDeviceContext_HitboxShaders()
        {
            _device.SetStreamSource(0, vertex3PositionColorVB, 0, Vertex3PositionColor.SizeInBytes);
            _device.VertexDeclaration = vertex3PositionColorVD;
            _device.VertexShader = hitboxVS;
            _device.SetVertexShaderConstant(4, _screenSizeUniform);
            _device.PixelShader = hitboxPS;
            _device.SetPixelShaderConstant(5, _borderThicknessUniform);
        }
        private void SetDeviceContext_MeterVertexShader()
        {
            _device.SetStreamSource(0, vertex3PositionColorVB, 0, Vertex3PositionColor.SizeInBytes);
            _device.VertexDeclaration = vertex3PositionColorVD;
            _device.SetVertexShaderConstant(4, _screenSizeUniform);
            _device.VertexShader = meterVS;
            _device.SetPixelShaderConstant(5, _miscShaderUniformBuffer);
        }

        // preallocated buffers
        private static readonly ColorRectangle[] _singleRectBuffer = new ColorRectangle[1];
        private static readonly Vertex3PositionColor[] _v3pcBuffer = new Vertex3PositionColor[RECTANGLE_LIMIT * VERTEX_PER_RECTANGLE];
        public void DrawRectangles(Span<ColorRectangle> rectangleList, Matrix4x4 transform)
        {
            uint numRectangles = (uint)rectangleList.Length;

            if (_device == null || vertex3PositionColorVB == null || numRectangles > RECTANGLE_LIMIT || numRectangles == 0) return;

            Span<Vertex3PositionColor> vertSpan = _v3pcBuffer.AsSpan(0, (int)(numRectangles * VERTEX_PER_RECTANGLE));
            LoadVerticesToBuffer(rectangleList, vertSpan);
            var bufferLock = vertex3PositionColorVB.Lock<byte>(
                0,
                Vertex3PositionColor.SizeInBytes * numRectangles * VERTEX_PER_RECTANGLE,
                LockFlags.Discard
            );
            MemoryMarshal.AsBytes(vertSpan).CopyTo(bufferLock);
            vertex3PositionColorVB.Unlock();

            _device.SetVertexShaderConstant(0, Matrix4x4.Transpose(transform));
            _device.DrawPrimitive(PrimitiveType.TriangleList, 0, numRectangles * 2);
        }
        public void DrawRectangles(ColorRectangle rectangle, Matrix4x4 transform)
        {
            _singleRectBuffer[0] = rectangle;
            DrawRectangles(_singleRectBuffer, transform);
        }
        public void DrawRectangles(Span<ColorRectangle> rectangleList)
        {
            DrawRectangles(rectangleList, Matrix4x4.Identity);
        }
        public void DrawRectangles(ColorRectangle rectangle)
        {
            _singleRectBuffer[0] = rectangle;
            DrawRectangles(_singleRectBuffer);
        }

        private static void LoadVerticesToBuffer(Span<ColorRectangle> cRectangles, Span<Vertex3PositionColor> outBuffer)
        {
            if (outBuffer.Length < cRectangles.Length * 6)
                throw new ArgumentException("Output buffer too small when converting ColorRectangles to Vertex3PositionColor");

            int index = 0;
            for (int i = 0; i < cRectangles.Length; i++)
            {
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Left, cRectangles[i].rectangle.Top, 1f), cRectangles[i].color, new Vector2(0,0));
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Right, cRectangles[i].rectangle.Top, 1f), cRectangles[i].color, new Vector2(1,0));
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Left, cRectangles[i].rectangle.Bottom, 1f), cRectangles[i].color, new Vector2(0,1));
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Left, cRectangles[i].rectangle.Bottom, 1f), cRectangles[i].color, new Vector2(0,1));
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Right, cRectangles[i].rectangle.Top, 1f), cRectangles[i].color, new Vector2(1,0));
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Right, cRectangles[i].rectangle.Bottom, 1f), cRectangles[i].color, new Vector2(1, 1));
            }
        }

        private bool _disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed) return;

            if (disposing)
            {
                vertex3PositionColorVB?.Release();
                vertex3PositionColorVD?.Release();
                hitboxVS?.Release();
                hitboxPS?.Release();
                meterVS?.Release();
                comboTimePS?.Release();
            }

            _disposed = true;
        }
    }
}
