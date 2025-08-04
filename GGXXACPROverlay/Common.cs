using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Mathematics;

namespace GGXXACPROverlay
{
    /// <summary>
    /// Each draw operation draws all of one specific type of box.
    /// </summary>
    public enum DrawOperation
    {
        None,
        Push,
        MiscRange,
        Grab,
        Hurt,
        Hit,
        CleanHit,
        Pivot,
    }

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
        public readonly Rect rectangle = rectangle;
        public readonly D3DCOLOR_ARGB color = color;

        public ColorRectangle(float x, float y, float width, float height, D3DCOLOR_ARGB color)
            : this(new Rect(x, y, width, height), color) { }
        public ColorRectangle(float x, float y, float width, float height, uint packedValue = 0xFFFFFFFF)
            : this(new Rect(x, y, width, height), new D3DCOLOR_ARGB(packedValue)) { }

        public override string ToString() => $"X:{rectangle.Left},Y:{rectangle.Top},W:{rectangle.Width},H:{rectangle.Height},0x{color.ARGB:X8}";
    }


    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public readonly struct Vertex4PositionColor(Vector4 position, D3DCOLOR_ARGB color, Vector2 uv)
    {
        public static unsafe readonly uint SizeInBytes = (uint)sizeof(Vertex4PositionColor);
        public const Vortice.Direct3D9.VertexFormat VFV = Vortice.Direct3D9.VertexFormat.PositionRhw | Vortice.Direct3D9.VertexFormat.Diffuse;
        public const short PositionOffset = 0x00;
        public const short ColorOffset    = 0x10;
        public const short UVOffset       = 0x18;

        [FieldOffset(0x00)] public readonly Vector4 Position = position;
        [FieldOffset(0x10)] public readonly D3DCOLOR_ARGB Color = color;
        [FieldOffset(0x18)] public readonly Vector2 UV = uv;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public readonly struct Vertex3PositionColor(Vector3 position, D3DCOLOR_ARGB color, Vector2 uv)
    {
        public static unsafe readonly uint SizeInBytes = (uint)sizeof(Vertex3PositionColor);
        public const Vortice.Direct3D9.VertexFormat VFV = Vortice.Direct3D9.VertexFormat.Position | Vortice.Direct3D9.VertexFormat.Diffuse;
        public const short PositionOffset = 0x00;
        public const short ColorOffset    = 0x0C;
        public const short UVOffset       = 0x10;
        //public const short UnusedOffset   = 0x18;

        // 0x00 - 0x0B
        [FieldOffset(0x00)] public readonly Vector3 Position = position;
        // 0x0C - 0x0F
        [FieldOffset(0x0C)] public readonly D3DCOLOR_ARGB Color = color;
        // 0x10 - 0x17
        [FieldOffset(0x10)] public readonly Vector2 UV = uv;
        //// 0x18 - 0x20
        //[FieldOffset(0x18)] public readonly Vector2 Unused = placeholder;

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

    /// <summary>
    /// Wrapper struct for arrays rented from ArrayPool.Shared. Compatible with `using` keyword to return the array to the array pool when finished.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public ref struct RentedArraySlice<T>
    {
        private readonly T[] _pooledArray;
        private readonly Span<T> _slice;
        public readonly Span<T> Span => _slice;

        private bool _isReturned = false;

        public readonly int Length => _slice.Length;

        public RentedArraySlice()
            : this([], 0, 0) { }
        public RentedArraySlice(int size)
            : this(ArrayPool<T>.Shared.Rent(size), 0, size) { }
        public RentedArraySlice(T[] pooledArray)
            : this(pooledArray, pooledArray.AsSpan()) { }
        public RentedArraySlice(T[] pooledArray, int start, int length)
            : this(pooledArray, pooledArray.AsSpan(start, length)) { }
        public RentedArraySlice(T[] pooledArray, Span<T> slice)
        {
            _pooledArray = pooledArray;
            _slice = slice;
        }
        public ref T this[int key]
        {
            get => ref _slice[key];
        }

        public static implicit operator Span<T>(RentedArraySlice<T> rentedArray) => rentedArray.Span;

        public void Dispose()
        {
            if (!_isReturned && _pooledArray is not null)
            {
                ArrayPool<T>.Shared.Return(_pooledArray);
                _isReturned = true;
            }
        }
    }

    /// <summary>
    /// Taken from https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
    /// </summary>
    public enum VirtualKeyCodes
    {
        None = 0,
        VK_LBUTTON = 0x01,  //Left mouse button
        VK_RBUTTON = 0x02,  //Right mouse button
        VK_CANCEL = 0x03,   //Control-break processing
        VK_MBUTTON = 0x04,  //Middle mouse button
        VK_XBUTTON1 = 0x05, //X1 mouse button
        VK_XBUTTON2 = 0x06, //X2 mouse button
        VK_BACK = 0x08,     //Backspace key
        VK_TAB = 0x09,      //Tab key
        VK_CLEAR = 0x0C,    //Clear key
        VK_RETURN = 0x0D,   //Enter key
        VK_SHIFT = 0x10,    //Shift key
        VK_CONTROL = 0x11,  //Ctrl key
        VK_MENU = 0x12,     //Alt key
        VK_PAUSE = 0x13,    //Pause key
        VK_CAPITAL = 0x14,  //Caps lock key
        VK_KANA = 0x15,     //IME Kana mode
        VK_IME_ON = 0x16,   //IME On
        VK_JUNJA = 0x17,    //IME Junja mode
        VK_FINAL = 0x18,    //IME final mode
        VK_KANJI = 0x19,    //IME Kanji mode
        VK_IME_OFF = 0x1A,  //IME Off
        VK_ESCAPE = 0x1B,   //Esc key
        VK_CONVERT = 0x1C,  //IME convert
        VK_NONCONVERT = 0x1D,   //IME nonconvert
        VK_ACCEPT = 0x1E,       //IME accept
        VK_MODECHANGE = 0x1F,   //IME mode change request
        VK_SPACE = 0x20,    //Spacebar key
        VK_PRIOR = 0x21,    //Page up key
        VK_NEXT = 0x22,     //Page down key
        VK_END = 0x23,      //End key
        VK_HOME = 0x24,     //Home key
        VK_LEFT = 0x25,     //Left arrow key
        VK_UP = 0x26,       //Up arrow key
        VK_RIGHT = 0x27,    //Right arrow key
        VK_DOWN = 0x28,     //Down arrow key
        VK_SELECT = 0x29,   //Select key
        VK_PRINT = 0x2A,    //Print key
        VK_EXECUTE = 0x2B,  //Execute key
        VK_SNAPSHOT = 0x2C, //Print screen key
        VK_INSERT = 0x2D,   //Insert key
        VK_DELETE = 0x2E,   //Delete key
        VK_HELP = 0x2F,     //Help key
        VK_0 = 0x30,
        VK_1 = 0x31,
        VK_2 = 0x32,
        VK_3 = 0x33,
        VK_4 = 0x34,
        VK_5 = 0x35,
        VK_6 = 0x36,
        VK_7 = 0x37,
        VK_8 = 0x38,
        VK_9 = 0x39,
        VK_A = 0x41,
        VK_B = 0x42,
        VK_C = 0x43,
        VK_D = 0x44,
        VK_E = 0x45,
        VK_F = 0x46,
        VK_G = 0x47,
        VK_H = 0x48,
        VK_I = 0x49,
        VK_J = 0x4A,
        VK_K = 0x4B,
        VK_L = 0x4C,
        VK_M = 0x4D,
        VK_N = 0x4E,
        VK_O = 0x4F,
        VK_P = 0x50,
        VK_Q = 0x51,
        VK_R = 0x52,
        VK_S = 0x53,
        VK_T = 0x54,
        VK_U = 0x55,
        VK_V = 0x56,
        VK_W = 0x57,
        VK_X = 0x58,
        VK_Y = 0x59,
        VK_Z = 0x5A,
        VK_LWIN = 0x5B,         //Left Windows logo key
        VK_RWIN = 0x5C,         //Right Windows logo key
        VK_APPS = 0x5D,         //Application key
        VK_SLEEP = 0x5F,        //Computer Sleep key
        VK_NUMPAD0 = 0x60,      //Numeric keypad 0 key
        VK_NUMPAD1 = 0x61,      //Numeric keypad 1 key
        VK_NUMPAD2 = 0x62,      //Numeric keypad 2 key
        VK_NUMPAD3 = 0x63,      //Numeric keypad 3 key
        VK_NUMPAD4 = 0x64,      //Numeric keypad 4 key
        VK_NUMPAD5 = 0x65,      //Numeric keypad 5 key
        VK_NUMPAD6 = 0x66,      //Numeric keypad 6 key
        VK_NUMPAD7 = 0x67,      //Numeric keypad 7 key
        VK_NUMPAD8 = 0x68,      //Numeric keypad 8 key
        VK_NUMPAD9 = 0x69,      //Numeric keypad 9 key
        VK_MULTIPLY = 0x6A,     //Multiply key
        VK_ADD = 0x6B,          //Add key
        VK_SEPARATOR = 0x6C,    //Separator key
        VK_SUBTRACT = 0x6D,     //Subtract key
        VK_DECIMAL = 0x6E,      //Decimal key
        VK_DIVIDE = 0x6F,       //Divide key
        VK_F1 = 0x70,           //F1 key
        VK_F2 = 0x71,           //F2 key
        VK_F3 = 0x72,           //F3 key
        VK_F4 = 0x73,           //F4 key
        VK_F5 = 0x74,           //F5 key
        VK_F6 = 0x75,           //F6 key
        VK_F7 = 0x76,           //F7 key
        VK_F8 = 0x77,           //F8 key
        VK_F9 = 0x78,           //F9 key
        VK_F10 = 0x79,          //F10 key
        VK_F11 = 0x7A,          //F11 key
        VK_F12 = 0x7B,          //F12 key
        VK_F13 = 0x7C,          //F13 key
        VK_F14 = 0x7D,          //F14 key
        VK_F15 = 0x7E,          //F15 key
        VK_F16 = 0x7F,          //F16 key
        VK_F17 = 0x80,          //F17 key
        VK_F18 = 0x81,          //F18 key
        VK_F19 = 0x82,          //F19 key
        VK_F20 = 0x83,          //F20 key
        VK_F21 = 0x84,          //F21 key
        VK_F22 = 0x85,          //F22 key
        VK_F23 = 0x86,          //F23 key
        VK_F24 = 0x87,          //F24 key
        VK_NUMLOCK = 0x90,      //Num lock key
        VK_SCROLL = 0x91,       //Scroll lock key
        VK_LSHIFT = 0xA0,       //Left Shift key
        VK_RSHIFT = 0xA1,       //Right Shift key
        VK_LCONTROL = 0xA2,     //Left Ctrl key
        VK_RCONTROL = 0xA3,     //Right Ctrl key
        VK_LMENU = 0xA4,        //Left Alt key
        VK_RMENU = 0xA5,        //Right Alt key
        VK_BROWSER_BACK = 0xA6, //Browser Back key
        VK_BROWSER_FORWARD = 0xA7,  //Browser Forward key
        VK_BROWSER_REFRESH = 0xA8,  //Browser Refresh key
        VK_BROWSER_STOP = 0xA9, //Browser Stop key
        VK_BROWSER_SEARCH = 0xAA,   //Browser Search key
        VK_BROWSER_FAVORITES = 0xAB,    //Browser Favorites key
        VK_BROWSER_HOME = 0xAC, //Browser Start and Home key
        VK_VOLUME_MUTE = 0xAD,  //Volume Mute key
        VK_VOLUME_DOWN = 0xAE,  //Volume Down key
        VK_VOLUME_UP = 0xAF,    //Volume Up key
        VK_MEDIA_NEXT_TRACK = 0xB0, //Next Track key
        VK_MEDIA_PREV_TRACK = 0xB1, //Previous Track key
        VK_MEDIA_STOP = 0xB2,   //Stop Media key
        VK_MEDIA_PLAY_PAUSE = 0xB3, //Play/Pause Media key
        VK_LAUNCH_MAIL = 0xB4,  //Start Mail key
        VK_LAUNCH_MEDIA_SELECT = 0xB5,  //Select Media key
        VK_LAUNCH_APP1 = 0xB6,  //Start Application 1 key
        VK_LAUNCH_APP2 = 0xB7,  //Start Application 2 key
        VK_OEM_1 = 0xBA,        //It can vary by keyboard. For the US ANSI keyboard , the Semiсolon and Colon key
        VK_OEM_PLUS = 0xBB,     //For any country/region, the Equals and Plus key
        VK_OEM_COMMA = 0xBC,    //For any country/region, the Comma and Less Than key
        VK_OEM_MINUS = 0xBD,    //For any country/region, the Dash and Underscore key
        VK_OEM_PERIOD = 0xBE,   //For any country/region, the Period and Greater Than key
        VK_OEM_2 = 0xBF,        //It can vary by keyboard. For the US ANSI keyboard, the Forward Slash and Question Mark key
        VK_OEM_3 = 0xC0,        //It can vary by keyboard. For the US ANSI keyboard, the Grave Accent and Tilde key
        VK_OEM_4 = 0xDB,        //It can vary by keyboard. For the US ANSI keyboard, the Left Brace key
        VK_OEM_5 = 0xDC,        //It can vary by keyboard. For the US ANSI keyboard, the Backslash and Pipe key
        VK_OEM_6 = 0xDD,        //It can vary by keyboard. For the US ANSI keyboard, the Right Brace key
        VK_OEM_7 = 0xDE,        //It can vary by keyboard. For the US ANSI keyboard, the Apostrophe and Double Quotation Mark key
        VK_OEM_8 = 0xDF,        //It can vary by keyboard. For the Canadian CSA keyboard, the Right Ctrl key
        VK_OEM_102 = 0xE2,      //It can vary by keyboard. For the European ISO keyboard, the Backslash and Pipe key
        VK_PROCESSKEY = 0xE5,   //IME PROCESS key
        VK_PACKET = 0xE7,       //Used to pass Unicode characters as if they were keystrokes. The VK_PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods. For more information, see Remark in KEYBDINPUT, SendInput, WM_KEYDOWN, and WM_KEYUP
        VK_ATTN = 0xF6,         //Attn key
        VK_CRSEL = 0xF7,        //CrSel key
        VK_EXSEL = 0xF8,        //ExSel key
        VK_EREOF = 0xF9,        //Erase EOF key
        VK_PLAY = 0xFA,         //Play key
        VK_ZOOM = 0xFB,         //Zoom key
        VK_NONAME = 0xFC,       //Reserved
        VK_PA1 = 0xFD,          //PA1 key
        VK_OEM_CLEAR = 0xFE,    //Clear key
    }
}
