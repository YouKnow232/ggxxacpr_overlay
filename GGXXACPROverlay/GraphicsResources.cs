// TODO: Split this among FrameMeter and Drawing

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

    internal class GraphicsResources
    {
        public readonly ColorRectangle ComboTimeMeterP2 =
            new ColorRectangle(
                Settings.Get("Misc", "HSDMeterXPosition", -0.95f),
                Settings.Get("Misc", "HSDMeterYPosition", 0.1f),
                0.05f,
                0.4f);
        public readonly ColorRectangle UntechTimeMeterP2 =
            new ColorRectangle(
                Settings.Get("Misc", "HSDMeterXPosition", -0.95f) + 0.1f,
                Settings.Get("Misc", "HSDMeterYPosition", 0.1f),
                0.05f,
                0.4f,
                Settings.Get("Misc", "UntechMeterColor", 0xFF00FFFF));

        public readonly ColorRectangle ComboTimeMeterP1 =
            new ColorRectangle(
                Settings.Get("Misc", "HSDMeterXPosition", -0.95f) * -1 - 0.05f,
                Settings.Get("Misc", "HSDMeterYPosition", 0.1f),
                0.05f,
                0.4f);
        public readonly ColorRectangle UntechTimeMeterP1 =
            new ColorRectangle(
                (Settings.Get("Misc", "HSDMeterXPosition", -0.95f) + 0.1f) * -1 - 0.05f,
                Settings.Get("Misc", "HSDMeterYPosition", 0.1f),
                0.05f,
                0.4f,
                Settings.Get("Misc", "UntechMeterColor", 0xFF00FFFF));


        //private static readonly LegendEntry[] _frameMeterLegend = [
        //        new LegendEntry(new FrameMeter.Frame { Type = FrameMeter.FrameType.Neutral },  "Neutral", FrameMeterElement.TYPE),

        //        new LegendEntry(new FrameMeter.Frame { Type = FrameMeter.FrameType.Neutral }, "Neutral", FrameMeterElement.TYPE),
        //        new LegendEntry(new FrameMeter.Frame { Type = FrameMeter.FrameType.Movement }, "Movement", FrameMeterElement.TYPE),
        //        new LegendEntry(new FrameMeter.Frame { Type = FrameMeter.FrameType.Startup }, "Startup/CH", FrameMeterElement.TYPE),
        //        new LegendEntry(new FrameMeter.Frame { Type = FrameMeter.FrameType.Active }, "Active", FrameMeterElement.TYPE),
        //        new LegendEntry(new FrameMeter.Frame { Type = FrameMeter.FrameType.Recovery }, "Recovery", FrameMeterElement.TYPE),
        //        new LegendEntry(new FrameMeter.Frame { Type = FrameMeter.FrameType.BlockStun }, "Block/Hit Stun", FrameMeterElement.TYPE),
        //        new LegendEntry(new FrameMeter.Frame { PrimaryProperty1 = FrameMeter.PrimaryFrameProperty.InvulnFull }, "Full Invuln", FrameMeterElement.TYPE),
        //        new LegendEntry(new FrameMeter.Frame { PrimaryProperty1 = FrameMeter.PrimaryFrameProperty.InvulnStrike }, "Strike Invuln", FrameMeterElement.TYPE),
        //        new LegendEntry(new FrameMeter.Frame { PrimaryProperty1 = FrameMeter.PrimaryFrameProperty.InvulnThrow }, "Throw Invuln", FrameMeterElement.TYPE),
        //        new LegendEntry(new FrameMeter.Frame { PrimaryProperty1 = FrameMeter.PrimaryFrameProperty.Armor }, "Armor/Parry/Guard point", FrameMeterElement.TYPE),
        //        new LegendEntry(new FrameMeter.Frame { PrimaryProperty1 = FrameMeter.PrimaryFrameProperty.SlashBack }, "Slashback", FrameMeterElement.TYPE),
        //        new LegendEntry(new FrameMeter.Frame { SecondaryProperty = FrameMeter.SecondaryFrameProperty.FRC }, "FRC", FrameMeterElement.TYPE)
        //    ];

        //public static LegendEntry[] GetLegend()
        //{
        //    return _frameMeterLegend;
        //}
    }
}
