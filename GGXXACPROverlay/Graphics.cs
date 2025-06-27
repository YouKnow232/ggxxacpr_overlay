using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
        HUD
    }
    internal unsafe class Graphics
    {
        private const string HLSL_COMPILE_TARGET_SUFFIX = "s_3_0";
        private const uint RECTANGLE_LIMIT = 100;
        private const uint VERTEX_PER_RECTANGLE = 6;    // 6 for 2 triangles

        private readonly IDirect3DDevice9 _device;

        // TODO: Move D3D9 Resources to its own class
        private IDirect3DVertexBuffer9? vertex3PositionColorVB;
        private IDirect3DVertexBuffer9? vertex4PositionColorVB;
        private IDirect3DVertexShader9? customVertexShader;
        private IDirect3DPixelShader9? customPixelShader;
        private IDirect3DPixelShader9? solidColorPixelShader;

        private IDirect3DVertexDeclaration9? vertex3PositionColorVD;
        private IDirect3DVertexDeclaration9? vertex4PositionColorVD;
        private IDirect3DVertexShader9? hitboxVS;
        private IDirect3DPixelShader9? hitboxPS;

        public Graphics(nint rawDevicePointer)
        {
            _device = new IDirect3DDevice9(rawDevicePointer);

            Debug.Log("Initializing Graphics resources");

            Debug.Log("\n== Device State ==");
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
            Debug.Log($"VertexPositionColor.SizeInBytes: {Vertex4PositionColor.SizeInBytes}");
            Debug.Log($"PixelShader.NativePointer: 0x{_device.PixelShader.NativePointer:X8}");

            Capabilities caps = new();
            _device.GetDeviceCaps(ref caps);

            Debug.Log("\n== Device Capabilities ==");
            Debug.Log($"caps.ShadeCaps: {caps.ShadeCaps}");
            Debug.Log($"caps.Caps: {caps.Caps}");
            Debug.Log($"caps.Caps2: {caps.Caps2}");
            Debug.Log($"caps.Caps3: {caps.Caps3}");
            Debug.Log($"caps.DeviceCaps: {caps.DeviceCaps}");
            Debug.Log($"caps.FVFCaps: 0x{(int)caps.FVFCaps:X8}");
            Debug.Log($"caps.VS20Caps.Caps: {caps.VS20Caps.Caps}");
            Debug.Log($"caps.PS20Caps.Caps: {caps.PS20Caps.Caps}");

            InitBuffers();
            Debug.Log("Vertex buffers initialized");

            CompileCustomShaders();
            Debug.Log("Shaders compiled");

            Debug.Log("tempInit finished");
        }

        public void Dispose()
        {
            vertex3PositionColorVB?.Release();
            vertex4PositionColorVB?.Release();
            customVertexShader?.Release();
            customPixelShader?.Release();
            solidColorPixelShader?.Release();
            vertex3PositionColorVD?.Release();
            vertex4PositionColorVD?.Release();
            hitboxVS?.Release();
            hitboxPS?.Release();
        }

        private static byte[] ReadShaderSource(string shaderURL)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            byte[] buffer = new byte[16 * 1024];
            using Stream? shaderStream = asm.GetManifestResourceStream(shaderURL);
            using MemoryStream memStream = new();

            if (shaderStream == null) { throw new NullReferenceException($"Could not find shader resource: {shaderURL}"); }

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
                throw new Exception($"Shader compilation error\nError Code: 0x{result.Code:X8}: {result.Description}\n" +
                    $"{Encoding.Default.GetString(err.AsSpan())}");
            }

            return output;
        }

        private void CompileCustomShaders()
        {
            // UI Shaders
            byte[] shaderSource = ReadShaderSource("GGXXACPROverlay.Shaders.SolidColorShader.hlsl");
            Debug.Log($"shaderSource length: {shaderSource.Length}");

            var vertShaderBlob = CompileShader(shaderSource, "ColorVertexShader", "v" + HLSL_COMPILE_TARGET_SUFFIX);
            Debug.Log("vertex shader successfully compiled");
            var pixShaderBlob  = CompileShader(shaderSource, "ColorPixelShader", "p" + HLSL_COMPILE_TARGET_SUFFIX);
            Debug.Log("pixel shader successfully compiled");
            var colorPixShaderBlob = CompileShader(shaderSource, "SolidColorPixelShader", "p" + HLSL_COMPILE_TARGET_SUFFIX);
            Debug.Log("solid color pixel shader successfully compiled");

            // Hitbox shaders
            byte[] hitboxShaderSource = ReadShaderSource("GGXXACPROverlay.Shaders.HitboxShader.hlsl");
            Debug.Log($"shaderSource length: {shaderSource.Length}");

            var hitboxVSBlob = CompileShader(hitboxShaderSource, "HitboxVS", "v" + HLSL_COMPILE_TARGET_SUFFIX);
            Debug.Log("hitbox vertex shader successfully compiled");
            var hitboxPSBlob = CompileShader(hitboxShaderSource, "HitboxPS", "p" + HLSL_COMPILE_TARGET_SUFFIX);
            Debug.Log("hitbox pixel shader successfully compiled");

            // Create device shaders
            customVertexShader = _device.CreateVertexShader(vertShaderBlob.AsSpan());
            customPixelShader = _device.CreatePixelShader(pixShaderBlob.AsSpan());
            solidColorPixelShader = _device.CreatePixelShader(colorPixShaderBlob.AsSpan());
            hitboxVS = _device.CreateVertexShader(hitboxVSBlob.AsSpan());
            hitboxPS = _device.CreatePixelShader(hitboxPSBlob.AsSpan());
        }

        private void InitBuffers()
        {
            // Vert3 Buffer
            vertex3PositionColorVB = _device.CreateVertexBuffer(
                RECTANGLE_LIMIT * Vertex3PositionColor.SizeInBytes * VERTEX_PER_RECTANGLE,
                Usage.None, 0, Pool.Managed
            );

            // Vert4 Buffer
            vertex4PositionColorVB = _device.CreateVertexBuffer(
                RECTANGLE_LIMIT * Vertex4PositionColor.SizeInBytes * VERTEX_PER_RECTANGLE,
                Usage.None, 0, Pool.Managed
            );

            // Vertex3PositionColor Vertex Declaration
            vertex3PositionColorVD = _device.CreateVertexDeclaration([
                new VertexElement(0, Vertex3PositionColor.PositionOffset, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position),
                new VertexElement(0, Vertex3PositionColor.ColorOffset, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color),
                new VertexElement(0, Vertex3PositionColor.BoxDimOffset, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                new VertexElement(0, Vertex3PositionColor.UVOffset, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 1),  // TODO: change to DeclarationMethod.UV
                VertexElement.VertexDeclarationEnd
            ]);

            // Vertex4PositionColor Vertex Declaration
            vertex4PositionColorVD = _device.CreateVertexDeclaration([
                new VertexElement(0, Vertex4PositionColor.PositionOffset, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.Position),
                new VertexElement(0, Vertex4PositionColor.ColorOffset, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color),
                VertexElement.VertexDeclarationEnd
            ]);
        }

        public void BeginScene() => _device?.BeginScene();
        public void EndScene() => _device?.EndScene();

        public void SetDeviceContext(GraphicsContext context)
        {
            switch (context)
            {
                case GraphicsContext.Hitbox:
                    _device.SetRenderState(RenderState.AlphaBlendEnable, true);
                    goto case GraphicsContext.Pivot;
                case GraphicsContext.Pivot:
                    _device.SetStreamSource(0, vertex3PositionColorVB, 0, Vertex3PositionColor.SizeInBytes);
                    _device.VertexDeclaration = vertex3PositionColorVD;
                    _device.VertexShader = hitboxVS;
                    _device.SetVertexShaderConstant(4, [(float)_device.Viewport.Width, (float)_device.Viewport.Height, 0f, 0f]);
                    _device.PixelShader = hitboxPS;
                    _device.SetPixelShaderConstant(5, [Settings.HitboxBorderThickness, 0f, 0f, 0f]);
                    break;
            }

            //_shaderConstants[0] = -1.0f;
        }

        //private static Matrix4x4 _modelTransform;
        //private static Matrix4x4 _viewTransform;
        //private static Matrix4x4 _projectionTransform;
        //internal static unsafe void RenderOverlayFrame()
        //{
        //    if (_device is null || !GGXXACPR.GGXXACPR.ShouldRender())
        //    {
        //        return;
        //    }

        //    var p1 = GGXXACPR.GGXXACPR.Player1;
        //    var p2 = GGXXACPR.GGXXACPR.Player2;
        //    var cam = GGXXACPR.GGXXACPR.Camera;

        //    _device.BeginScene();

        //    // Device setup
        //    _device.SetRenderState(RenderState.AlphaBlendEnable, true);
        //    _shaderConstants[0] = Settings.Get("Hitboxes", "BorderThickness", 2.0f);    // TODO: Cache this in init function
        //    //_shaderConstants[1] = cam.Zoom;
        //    _device.SetPixelShaderConstant(5, _shaderConstants);
        //    _modelTransform = GetHitboxModelTransform(p2);
        //    _viewTransform = Matrix4x4.Identity;
        //    _projectionTransform = GetProjectionTransform(cam);

        //    DrawRectangles(Drawing.GetHitboxPrimitives(p2), _modelTransform * _viewTransform * _projectionTransform);
        //    DrawRectangles([Drawing.GetCLHitBox(p2)], _modelTransform * _viewTransform * _projectionTransform);

        //    _modelTransform = GetHitboxModelTransform(p1);
        //    DrawRectangles(Drawing.GetHitboxPrimitives(p1), _modelTransform * _viewTransform * _projectionTransform);
        //    DrawRectangles([Drawing.GetCLHitBox(p1)], _modelTransform * _viewTransform * _projectionTransform);

        //    //_shaderConstants[0] = -1.0f;
        //    //_device.SetPixelShaderConstant(5, _shaderConstants);
        //    DrawRectangles(Drawing.GetPivot(p1, WorldCoorPerGamePixel(cam)), _projectionTransform);
        //    DrawRectangles(Drawing.GetPivot(p2, WorldCoorPerGamePixel(cam)), _projectionTransform);

        //    // Why isn't this rendering?
        //    //GGXXACPR.GGXXACPR.RenderText("TEST!", 212, 368, 0xFF);

        //    _device.EndScene();
        //}

        // preallocated buffer
        private static readonly Vertex3PositionColor[] _v3pcBuffer = new Vertex3PositionColor[RECTANGLE_LIMIT * VERTEX_PER_RECTANGLE];
        //private static readonly Func<ColorRectangle, IEnumerable<Vertex3PositionColor>> _verticesCallback = GetVerticies;
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

            _device.SetStreamSource(0, vertex3PositionColorVB, 0, Vertex3PositionColor.SizeInBytes);
            _device.VertexDeclaration = vertex3PositionColorVD;
            _device.VertexShader = hitboxVS;
            _device.SetVertexShaderConstant(0, Matrix4x4.Transpose(transform));
            _device.SetVertexShaderConstant(4, [(float)_device.Viewport.Width, (float)_device.Viewport.Height, 0f, 0f]);
            _device.PixelShader = hitboxPS;

            _device.DrawPrimitive(PrimitiveType.TriangleList, 0, numRectangles * 2);
        }

        private static void LoadVerticesToBuffer(Span<ColorRectangle> cRectangles, Span<Vertex3PositionColor> outBuffer)
        {
            if (outBuffer.Length < cRectangles.Length * 6) throw new ArgumentException("Output buffer too small when converting ColorRectangles to Vertex3PositionColor");

            int index = 0;
            for (int i = 0; i < cRectangles.Length; i++)
            {
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Left, cRectangles[i].rectangle.Top, 1f), cRectangles[i].color,
                    new Vector2(cRectangles[i].rectangle.Width, cRectangles[i].rectangle.Height), new Vector2(0,0));
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Right, cRectangles[i].rectangle.Top, 1f), cRectangles[i].color,
                    new Vector2(cRectangles[i].rectangle.Width, cRectangles[i].rectangle.Height), new Vector2(1,0));
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Left, cRectangles[i].rectangle.Bottom, 1f), cRectangles[i].color,
                    new Vector2(cRectangles[i].rectangle.Width, cRectangles[i].rectangle.Height), new Vector2(0,1));
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Left, cRectangles[i].rectangle.Bottom, 1f), cRectangles[i].color,
                    new Vector2(cRectangles[i].rectangle.Width, cRectangles[i].rectangle.Height), new Vector2(0,1));
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Right, cRectangles[i].rectangle.Top, 1f), cRectangles[i].color,
                    new Vector2(cRectangles[i].rectangle.Width, cRectangles[i].rectangle.Height), new Vector2(1,0));
                outBuffer[index++] = new Vertex3PositionColor(
                    new Vector3(cRectangles[i].rectangle.Right, cRectangles[i].rectangle.Bottom, 1f), cRectangles[i].color,
                    new Vector2(cRectangles[i].rectangle.Width, cRectangles[i].rectangle.Height), new Vector2(1, 1));
            }
        }
    }
}
