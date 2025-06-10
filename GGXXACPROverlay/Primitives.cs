using System.Drawing;

namespace GGXXACPROverlay
{
    public class ColorRectangle(Rectangle rectangle, D3DCOLOR_ARGB color)
    {
        public Rectangle rectangle = rectangle;
        public D3DCOLOR_ARGB color = color;

        public ColorRectangle(int x, int y, int width, int height, D3DCOLOR_ARGB color)
            : this(new Rectangle(x, y, width, height), color) { }
        public ColorRectangle(int x, int y, int width, int height, uint packedValue)
            : this(new Rectangle(x, y, width, height), new D3DCOLOR_ARGB(packedValue)) { }
    }
}
