using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Mathematics;

namespace GGXXACPROverlay
{
    public readonly struct D3DCOLOR_ARGB
    {
        private readonly uint _packedValue;

        public uint ARGB => _packedValue;
        public byte Alpha { get => (byte)(_packedValue >> 24 & 0xff); }
        public int Red { get => (byte)(_packedValue >> 16 & 0xff); }
        public int Green { get => (byte)(_packedValue >> 8 & 0xff); }
        public int Blue { get => (byte)(_packedValue & 0xff); }

        public D3DCOLOR_ARGB(uint packedValue)
        {
            _packedValue = packedValue;
        }
        public D3DCOLOR_ARGB(uint a, uint packedRGBValue)
        {
            _packedValue = (a << 24) | (packedRGBValue & 0xFFFFFF);
        }
        public D3DCOLOR_ARGB(byte a, byte r, byte g, byte b)
        {
            _packedValue = (uint)((a << 24) | (r << 16) | (g << 8) | b);
        }
    }

    public class ColorRectangle(Rect rectangle, D3DCOLOR_ARGB color)
    {
        public Rect rectangle = rectangle;
        public D3DCOLOR_ARGB color = color;

        public ColorRectangle(float x, float y, float width, float height, D3DCOLOR_ARGB color)
            : this(new Rect(x, y, width, height), color) { }
        public ColorRectangle(float x, float y, float width, float height, uint packedValue)
            : this(new Rect(x, y, width, height), new D3DCOLOR_ARGB(packedValue)) { }

        public override string ToString() => $"X:{rectangle.Left},Y:{rectangle.Top},W:{rectangle.Width},H:{rectangle.Height},0x{color.ARGB:X8}";
    }


    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public readonly struct Vertex4PositionColor(Vector4 position, D3DCOLOR_ARGB color)
    {
        public static unsafe readonly uint SizeInBytes = (uint)sizeof(Vertex4PositionColor);
        public const Vortice.Direct3D9.VertexFormat VFV = Vortice.Direct3D9.VertexFormat.PositionRhw | Vortice.Direct3D9.VertexFormat.Diffuse;
        public const short PositionOffset = 0x00;
        public const short ColorOffset    = 0x10;

        [FieldOffset(0x00)] public readonly Vector4 Position = position;
        [FieldOffset(0x10)] public readonly D3DCOLOR_ARGB Color = color;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public readonly struct Vertex3PositionColor(Vector3 position, D3DCOLOR_ARGB color, Vector2 boxDim, Vector2 uv)
    {
        public static unsafe readonly uint SizeInBytes = (uint)sizeof(Vertex3PositionColor);
        public const Vortice.Direct3D9.VertexFormat VFV = Vortice.Direct3D9.VertexFormat.Position | Vortice.Direct3D9.VertexFormat.Diffuse;
        public const short PositionOffset = 0x00;
        public const short ColorOffset    = 0x0C;
        public const short BoxDimOffset   = 0x10;
        public const short UVOffset       = 0x18;

        // 0x00 - 0x0B
        [FieldOffset(0x00)] public readonly Vector3 Position = position;
        // 0x0C - 0x0F
        [FieldOffset(0x0C)] public readonly D3DCOLOR_ARGB Color = color;
        // 0x10 - 0x17
        [FieldOffset(0x10)] public readonly Vector2 BoxDim = boxDim;
        // 0x18 - 0x20
        [FieldOffset(0x18)] public readonly Vector2 UV = uv;

        public override string ToString() => $"X:{Position.X},Y:{Position.Y},Z:{Position.Z},0x{Color.ARGB:X8},U:{UV.X},V:{UV.Y}";
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public readonly struct LineVertex(Vector3 position, D3DCOLOR_ARGB color)
    {
        public static unsafe readonly uint SizeInBytes = (uint)sizeof(LineVertex);
        public const short PositionOffset = 0x00;
        public const short ColorOffset = 0x0C;

        // 0x00 - 0x0B
        [FieldOffset(0x00)] public readonly Vector3 Position = position;
        // 0x0C - 0x0F
        [FieldOffset(0x0C)] public readonly D3DCOLOR_ARGB Color = color;

        public LineVertex(float x, float y, D3DCOLOR_ARGB color) : this(new Vector3(x, y, 0), color) { }
    }
}
