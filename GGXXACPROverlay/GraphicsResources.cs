using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using GameOverlay.Drawing;
using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    public enum GeneralPalette
    {
        DEFAULT = 0,
        BLACK,
        WHITE,
        GREEN,
        RED,
        BLUE,
        YELLOW,
        LABEL_BG,
        COLLISION,
        GRAB,
    }

    public enum FrameMeterElement
    {
        NONE, TYPE, PROPERTY1, PROPERTY2
    }
    internal readonly struct LegendEntry(FrameMeter.Frame exampleFrame, string label, FrameMeterElement elementType)
    {
        public readonly FrameMeter.Frame ExampleFrame = exampleFrame;
        public readonly string Label = label;
        public readonly FrameMeterElement ElementType = elementType;
    }

    internal class GraphicsResources : IDisposable
    {
        private const int _boxAlpha = 50;

        private static readonly Color _defaultHitboxClr = new Color(255, 0, 0);
        private static readonly Color _defaultHurtboxClr = new Color(0, 255, 0);
        private static readonly Color _defaultCollisionboxClr = new Color(0, 255, 255);
        private static readonly Color _defaultGrabboxClr = new Color(255, 0, 255);

        private static readonly LegendEntry[] _frameMeterLegend = [
                new LegendEntry(new FrameMeter.Frame(FrameMeter.FrameType.Neutral), "Neutral", FrameMeterElement.TYPE),
                new LegendEntry(new FrameMeter.Frame(FrameMeter.FrameType.Movement), "Movement", FrameMeterElement.TYPE),
                new LegendEntry(new FrameMeter.Frame(FrameMeter.FrameType.Startup), "Startup/CH", FrameMeterElement.TYPE),
                new LegendEntry(new FrameMeter.Frame(FrameMeter.FrameType.Active), "Active", FrameMeterElement.TYPE),
                new LegendEntry(new FrameMeter.Frame(FrameMeter.FrameType.Recovery), "Recovery", FrameMeterElement.TYPE),
                new LegendEntry(new FrameMeter.Frame(FrameMeter.FrameType.BlockStun), "Block/Hit Stun", FrameMeterElement.TYPE),
                new LegendEntry(new FrameMeter.Frame(0, FrameMeter.PrimaryFrameProperty.InvulnFull), "Full Invuln", FrameMeterElement.TYPE),
                new LegendEntry(new FrameMeter.Frame(0, FrameMeter.PrimaryFrameProperty.InvulnStrike), "Strike Invuln", FrameMeterElement.TYPE),
                new LegendEntry(new FrameMeter.Frame(0, FrameMeter.PrimaryFrameProperty.InvulnThrow), "Throw Invuln", FrameMeterElement.TYPE),
                new LegendEntry(new FrameMeter.Frame(0, FrameMeter.PrimaryFrameProperty.Armor), "Armor/Parry/Guard point", FrameMeterElement.TYPE),
                new LegendEntry(new FrameMeter.Frame(0, FrameMeter.PrimaryFrameProperty.SlashBack), "Slashback", FrameMeterElement.TYPE),
                new LegendEntry(new FrameMeter.Frame(0, 0, 0, FrameMeter.SecondaryFrameProperty.FRC), "FRC", FrameMeterElement.TYPE)
            ];

        private readonly Dictionary<GeneralPalette, SolidBrush> _generalPalette;
        private readonly Dictionary<GeneralPalette, SolidBrush> _generalTransparencyPalette;
        private readonly Dictionary<BoxId, SolidBrush> _hitboxOutlinePalette;
        private readonly Dictionary<BoxId, SolidBrush> _hitboxFillPalette;
        private readonly Dictionary<FrameMeter.FrameType, SolidBrush> _frameTypePalette;
        private readonly Dictionary<FrameMeter.PrimaryFrameProperty, SolidBrush> _framePropertyPalette;
        private readonly Dictionary<FrameMeter.SecondaryFrameProperty, SolidBrush> _frameProperty2Palette;

        [AllowNull]
        public Font Font { get; private set; }
        [AllowNull]
        public Font LegendFont { get; private set; }

        public GraphicsResources()
        {
            _generalPalette = [];
            _generalTransparencyPalette = [];
            _hitboxOutlinePalette = [];
            _hitboxFillPalette = [];
            _frameTypePalette = [];
            _framePropertyPalette = [];
            _frameProperty2Palette = [];
        }

        public void Initilize(Graphics g)
        {
            Font = g.CreateFont("Arial Bold", 24);
            LegendFont = g.CreateFont("Arial Bold", 16);

            _generalPalette.Add(GeneralPalette.DEFAULT, g.CreateSolidBrush(0, 0, 0));
            _generalPalette.Add(GeneralPalette.BLACK, g.CreateSolidBrush(0, 0, 0));
            _generalPalette.Add(GeneralPalette.WHITE, g.CreateSolidBrush(255, 255, 255));
            _generalPalette.Add(GeneralPalette.GREEN, g.CreateSolidBrush(150, 255, 150));
            _generalPalette.Add(GeneralPalette.RED, g.CreateSolidBrush(255, 150, 150));
            _generalPalette.Add(GeneralPalette.BLUE, g.CreateSolidBrush(0, 0, 255));
            _generalPalette.Add(GeneralPalette.YELLOW, g.CreateSolidBrush(255, 255, 0));
            _generalPalette.Add(GeneralPalette.LABEL_BG, g.CreateSolidBrush(0, 0, 0, 150));
            _generalPalette.Add(GeneralPalette.COLLISION, g.CreateSolidBrush(_defaultCollisionboxClr));
            _generalPalette.Add(GeneralPalette.GRAB, g.CreateSolidBrush(_defaultGrabboxClr));

            _generalTransparencyPalette.Add(GeneralPalette.DEFAULT, g.CreateSolidBrush(0, 0, 0, _boxAlpha));
            _generalTransparencyPalette.Add(GeneralPalette.COLLISION, g.CreateSolidBrush(new Color(_defaultCollisionboxClr, _boxAlpha)));
            _generalTransparencyPalette.Add(GeneralPalette.GRAB, g.CreateSolidBrush(new Color(_defaultGrabboxClr, _boxAlpha)));

            _hitboxOutlinePalette.Add(BoxId.DUMMY, g.CreateSolidBrush(10, 10, 10));
            _hitboxOutlinePalette.Add(BoxId.HIT, g.CreateSolidBrush(_defaultHitboxClr));
            _hitboxOutlinePalette.Add(BoxId.HURT, g.CreateSolidBrush(_defaultHurtboxClr));
            _hitboxOutlinePalette.Add(BoxId.UNKNOWN_3, g.CreateSolidBrush(10, 10, 10));
            _hitboxOutlinePalette.Add(BoxId.PUSH, g.CreateSolidBrush(_defaultCollisionboxClr));
            _hitboxOutlinePalette.Add(BoxId.UNKNOWN_5, g.CreateSolidBrush(10, 10, 10));
            _hitboxOutlinePalette.Add(BoxId.UNKNOWN_6, g.CreateSolidBrush(10, 10, 10));

            _hitboxFillPalette.Add(BoxId.DUMMY, g.CreateSolidBrush(10, 10, 10, _boxAlpha));
            _hitboxFillPalette.Add(BoxId.HIT, g.CreateSolidBrush(new Color(_defaultHitboxClr, _boxAlpha)));
            _hitboxFillPalette.Add(BoxId.HURT, g.CreateSolidBrush(new Color(_defaultHurtboxClr, _boxAlpha)));
            _hitboxFillPalette.Add(BoxId.UNKNOWN_3, g.CreateSolidBrush(10, 10, 10, _boxAlpha));
            _hitboxFillPalette.Add(BoxId.PUSH, g.CreateSolidBrush(new Color(_defaultCollisionboxClr, _boxAlpha)));
            _hitboxFillPalette.Add(BoxId.UNKNOWN_5, g.CreateSolidBrush(10, 10, 10, _boxAlpha));
            _hitboxFillPalette.Add(BoxId.UNKNOWN_6, g.CreateSolidBrush(10, 10, 10, _boxAlpha));

            _frameTypePalette.Add(FrameMeter.FrameType.None, g.CreateSolidBrush(15, 15, 15));
            _frameTypePalette.Add(FrameMeter.FrameType.Neutral, g.CreateSolidBrush(27, 27, 27));
            _frameTypePalette.Add(FrameMeter.FrameType.Movement, g.CreateSolidBrush(65, 248, 252));
            _frameTypePalette.Add(FrameMeter.FrameType.CounterHitState, g.CreateSolidBrush(1, 181, 151));
            _frameTypePalette.Add(FrameMeter.FrameType.Startup, g.CreateSolidBrush(1, 181, 151));
            _frameTypePalette.Add(FrameMeter.FrameType.Active, g.CreateSolidBrush(203, 43, 103));
            _frameTypePalette.Add(FrameMeter.FrameType.ActiveThrow, g.CreateSolidBrush(203, 43, 103));
            _frameTypePalette.Add(FrameMeter.FrameType.Recovery, g.CreateSolidBrush(0, 111, 188));
            _frameTypePalette.Add(FrameMeter.FrameType.BlockStun, g.CreateSolidBrush(200, 200, 0));
            _frameTypePalette.Add(FrameMeter.FrameType.HitStun, g.CreateSolidBrush(200, 200, 0));

            _framePropertyPalette.Add(FrameMeter.PrimaryFrameProperty.Default, g.CreateSolidBrush(0, 0, 0));
            _framePropertyPalette.Add(FrameMeter.PrimaryFrameProperty.SlashBack, g.CreateSolidBrush(255, 0, 0));
            _framePropertyPalette.Add(FrameMeter.PrimaryFrameProperty.InvulnFull, g.CreateSolidBrush(255, 255, 255));
            _framePropertyPalette.Add(FrameMeter.PrimaryFrameProperty.InvulnThrow, g.CreateSolidBrush(255, 125, 0));
            _framePropertyPalette.Add(FrameMeter.PrimaryFrameProperty.InvulnStrike, g.CreateSolidBrush(0, 125, 255));
            _framePropertyPalette.Add(FrameMeter.PrimaryFrameProperty.Armor, g.CreateSolidBrush(120, 80, 0));
            _framePropertyPalette.Add(FrameMeter.PrimaryFrameProperty.Parry, g.CreateSolidBrush(120, 80, 0));
            _framePropertyPalette.Add(FrameMeter.PrimaryFrameProperty.GuardPointFull, g.CreateSolidBrush(120, 80, 0));
            _framePropertyPalette.Add(FrameMeter.PrimaryFrameProperty.GuardPointHigh, g.CreateSolidBrush(120, 80, 0));
            _framePropertyPalette.Add(FrameMeter.PrimaryFrameProperty.GuardPointLow, g.CreateSolidBrush(120, 80, 0));
            _framePropertyPalette.Add(FrameMeter.PrimaryFrameProperty.TEST, g.CreateSolidBrush(255, 255, 0));

            _frameProperty2Palette.Add(FrameMeter.SecondaryFrameProperty.Default, g.CreateSolidBrush(0, 0, 0));
            _frameProperty2Palette.Add(FrameMeter.SecondaryFrameProperty.FRC, g.CreateSolidBrush(255, 255, 0));
        }

        public static LegendEntry[] GetLegend()
        {
            return _frameMeterLegend;
        }

        public SolidBrush GetBrush(GeneralPalette type)
        {
            return _generalPalette[type];
        }
        public SolidBrush GetFillBrush(GeneralPalette type)
        {
            return _generalTransparencyPalette[type];
        }
        public SolidBrush GetOutlineBrush(BoxId type)
        {
            return _hitboxOutlinePalette[type];
        }
        public SolidBrush GetFillBrush(BoxId type)
        {
            return _hitboxFillPalette[type];
        }
        public SolidBrush GetBrush(FrameMeter.FrameType type)
        {
            return _frameTypePalette[type];
        }
        public SolidBrush GetBrush(FrameMeter.PrimaryFrameProperty property)
        {
            return _framePropertyPalette[property];
        }
        public SolidBrush GetBrush(FrameMeter.SecondaryFrameProperty property)
        {
            return _frameProperty2Palette[property];
        }

        ~GraphicsResources()
        {
            Dispose(false);
        }

        // IDisposable stuff
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Font.Dispose();

                DisposeBrushDictionary(_generalPalette);
                DisposeBrushDictionary(_generalTransparencyPalette);
                DisposeBrushDictionary(_hitboxOutlinePalette);
                DisposeBrushDictionary(_hitboxFillPalette);
                DisposeBrushDictionary(_frameTypePalette);
                DisposeBrushDictionary(_framePropertyPalette);
                DisposeBrushDictionary(_frameProperty2Palette);

                disposedValue = true;
            }
        }

        private static void DisposeBrushDictionary<T>(Dictionary<T, SolidBrush> dict)
            where T : notnull
        {
            foreach (SolidBrush brush in dict.Values) { brush.Dispose(); }
            dict.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            Debug.WriteLine("Graphics Resources Disposed");
        }
    }
}
