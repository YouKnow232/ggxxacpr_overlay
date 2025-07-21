using GGXXACPROverlay.GGXXACPR;
using Vortice.Mathematics;

// TODO: reevaluate preallocated buffers
namespace GGXXACPROverlay
{
    /// <summary>
    /// Helper functions that convert game data from the GGXXACPR class into a ready to draw
    /// format using display information from the Settings class.
    /// </summary>
    internal static class Drawing
    {
        private const float LINE_THICKNESS = 1f;
        private const int LINE_THICKNESS_PX = 2;
        // Values in game pixels
        private const int PIVOT_CROSS_SIZE = 6;
        private const int FRAME_METER_Y = 400;
        private const int FRAME_METER_VERTICAL_SPACING = 1;

        public readonly struct Dimensions(int width, int height)
        {
            public readonly int Width = width;
            public readonly int Height = height;
        }
        public readonly struct PointInt(int x, int y)
        {
            public readonly int X = x;
            public readonly int Y = y;
        }


        private static readonly ColorRectangle[] _colorRectangleBuffer = new ColorRectangle[100];
        public static Span<ColorRectangle> GetHitboxPrimitives(Span<Hitbox> boxes)
        {
            for (int i = 0; i < boxes.Length; i++) Convert(boxes[i], out _colorRectangleBuffer[i]);

            return _colorRectangleBuffer.AsSpan(0, boxes.Length);
        }

        private static void Convert(Hitbox h, out ColorRectangle output)
        {
            var colorValue = Settings.Default;
            if (h.BoxTypeId == (ushort)BoxId.HIT) colorValue = Settings.Hitbox;
            else if (h.BoxTypeId == (ushort)BoxId.HURT) colorValue = Settings.Hurtbox;
            output = new ColorRectangle(h.XOffset, h.YOffset, h.Width, h.Height, colorValue);
        }

        public static ColorRectangle GetCLHitBox(Player p)
            => new ColorRectangle(GGXXACPR.GGXXACPR.GetCLRect(p), Settings.CLHitbox);

        /// <summary>
        /// Expresses the character origin point (pivot) as two rectangles forming a cross.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="p"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public static Span<ColorRectangle> GetPivot(Player p, float ratio)
        {
            var halfSize = Settings.PivotCrossSize * ratio / 2.0f;
            var halfThickness = Settings.PivotCrossThickness * ratio / 2.0f;

            _colorRectangleBuffer[0] = new ColorRectangle(
                p.XPos - halfSize,
                p.YPos - halfThickness,
                halfSize * 2,
                halfThickness * 2,
                Settings.PivotCrossColor
            );
            _colorRectangleBuffer[1] = new ColorRectangle(
                p.XPos - halfThickness,
                p.YPos - halfSize,
                halfThickness * 2,
                halfSize * 2,
                Settings.PivotCrossColor
            );

            return _colorRectangleBuffer.AsSpan(0, 2);
        }

        public static ColorRectangle GetPushboxPrimitives(Player p)
        {
            Rect push = GGXXACPR.GGXXACPR.GetPushBox(p);
            return new ColorRectangle(push, Settings.Push);
        }

        public static ColorRectangle GetGrabboxPrimitives(Player p)
        {
            return new ColorRectangle(GGXXACPR.GGXXACPR.GetGrabBox(p), Settings.Grab);
        }
        public static ColorRectangle GetCommnadGrabboxPrimitives(Player p)
        {
            GGXXACPR.GGXXACPR.GetCommandGrabBox(p, GGXXACPR.GGXXACPR.GetPushBox(p), out Rect cmdGrabBox);
            return new ColorRectangle(cmdGrabBox, Settings.Grab);
        }

        private static readonly ColorRectangle[] _pushGrabBuffer = new ColorRectangle[2];
        public static Span<ColorRectangle> GetPushAndGrabPrimitives(Player p)
        {
            Rect push = GGXXACPR.GGXXACPR.GetPushBox(p);
            _pushGrabBuffer[0] = new ColorRectangle(push, Settings.Push);
            ThrowDetection throwFlags = GGXXACPR.GGXXACPR.ThrowFlags;

            if (GGXXACPR.GGXXACPR.GetCommandGrabBox(p, push, out Rect cmdGrab))
            {
                _pushGrabBuffer[1] = new ColorRectangle(cmdGrab, Settings.Grab);
                return _pushGrabBuffer.AsSpan();
            }
            else if (p.PlayerIndex == 0 && throwFlags.HasFlag(ThrowDetection.Player1ThrowSuccess) ||    // TODO: throw recognition bug going on here
                     p.PlayerIndex == 1 && throwFlags.HasFlag(ThrowDetection.Player2ThrowSuccess))
            {
                Rect grab = GGXXACPR.GGXXACPR.GetGrabBox(p, push);
                _pushGrabBuffer[1] = new ColorRectangle(grab, Settings.Grab);
                return _pushGrabBuffer.AsSpan();
            }

            return _pushGrabBuffer.AsSpan(0, 1);
        }

    //    private const int FRAME_METER_Y_ALT = 90;
    //    private const int FRAME_METER_BASE_LINE_X = 5;
    //    internal static void DrawFrameMeter(Graphics g, GraphicsResources r, FrameMeter frameMeter, Dimensions windowDimensions)
    //    {
    //        FrameMeter.Frame[] frameArr;
    //        Rectangle rect, frameRect;
    //        Line line;
    //        int frameMeterLength = frameMeter.PlayerMeters[0].FrameArr.Length;

    //        int screenWidth = windowDimensions.Height * 4 / 3;

    //        int pipSpacing = (screenWidth - FRAME_METER_BASE_LINE_X * 2) / frameMeterLength;
    //        int pipWidth = pipSpacing - 1;
    //        int totalWidth = (pipSpacing * frameMeterLength) - 1;
    //        int pipHeight = pipWidth * 5 / 3;
    //        int entityPipHeight = pipWidth;
    //        int coreYPos = windowDimensions.Height * FRAME_METER_Y / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;
    //        int yPos = coreYPos - pipHeight - 1;
    //        int xPos = (windowDimensions.Width - totalWidth) / 2;
    //        int propertyHighlightHeight = pipHeight * 2 / 7;
    //        propertyHighlightHeight += propertyHighlightHeight == 0 ? 1 : 0;
    //        int propertyHighlightTop = pipHeight - propertyHighlightHeight;
    //        int borderThickness = 2 * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;

    //        // Border
    //        g.FillRectangle(r.GetBrush(GeneralPalette.BLACK), new Rectangle(
    //            xPos - borderThickness,
    //            yPos - borderThickness,
    //            xPos + totalWidth + borderThickness,
    //            yPos + 2 * pipHeight + FRAME_METER_VERTICAL_SPACING + borderThickness
    //        ));

    //        // Players
    //        for (int j = 0; j < frameMeter.PlayerMeters.Length; j++)
    //        {
    //            frameArr = frameMeter.PlayerMeters[j].FrameArr;
    //            for (int i = 0; i < frameArr.Length; i++)
    //            {
    //                frameRect = new Rectangle(
    //                    xPos + (i * pipSpacing),
    //                    yPos + (j * (FRAME_METER_VERTICAL_SPACING + pipHeight)),
    //                    xPos + pipWidth + (i * pipSpacing),
    //                    yPos + pipHeight + (j * (FRAME_METER_VERTICAL_SPACING + pipHeight))
    //                );
    //                g.FillRectangle(r.GetBrush(frameArr[i].Type), frameRect);
    //                if (frameArr[i].SecondaryProperty != FrameMeter.SecondaryFrameProperty.Default)
    //                {
    //                    rect = new Rectangle(
    //                        frameRect.Left,
    //                        frameRect.Top,
    //                        frameRect.Right,
    //                        frameRect.Top + propertyHighlightHeight
    //                    );
    //                    g.FillRectangle(r.GetBrush(frameArr[i].SecondaryProperty), rect);
    //                    line = new Line(
    //                        frameRect.Left,
    //                        frameRect.Top + propertyHighlightHeight,
    //                        frameRect.Right,
    //                        frameRect.Top + propertyHighlightHeight
    //                    );
    //                    g.DrawLine(r.GetBrush(GeneralPalette.BLACK), line, LINE_THICKNESS);
    //                }
    //                if (frameArr[i].PrimaryProperty1 != FrameMeter.PrimaryFrameProperty.Default)
    //                {
    //                    rect = new Rectangle(
    //                        frameRect.Left,
    //                        frameRect.Top + propertyHighlightTop,
    //                        frameRect.Right,
    //                        frameRect.Bottom
    //                    );
    //                    g.FillRectangle(r.GetBrush(frameArr[i].PrimaryProperty1), rect);
    //                    line = new Line(
    //                        frameRect.Left,
    //                        frameRect.Top + propertyHighlightTop,
    //                        frameRect.Right,
    //                        frameRect.Top + propertyHighlightTop
    //                    );
    //                    g.DrawLine(r.GetBrush(GeneralPalette.BLACK), line, LINE_THICKNESS);
    //                    if (frameArr[i].PrimaryProperty2 != FrameMeter.PrimaryFrameProperty.Default)
    //                    {
    //                        rect = new Rectangle(
    //                            frameRect.Left + pipWidth / 2,
    //                            frameRect.Top + propertyHighlightTop,
    //                            frameRect.Right,
    //                            frameRect.Bottom
    //                        );
    //                        g.FillRectangle(r.GetBrush(frameArr[i].PrimaryProperty2), rect);
    //                    }
    //                }
    //            }
    //        }

    //        // Entity
    //        if (!frameMeter.EntityMeters[0].Hide)
    //        {
    //            // Border
    //            g.FillRectangle(r.GetBrush(GeneralPalette.BLACK), new Rectangle(
    //                xPos - borderThickness,
    //                yPos - borderThickness - FRAME_METER_VERTICAL_SPACING - entityPipHeight,
    //                xPos + totalWidth + borderThickness,
    //                yPos - FRAME_METER_VERTICAL_SPACING
    //            ));
    //            for (int i=0; i < frameMeter.EntityMeters[0].FrameArr.Length; i++)
    //            {
    //                rect = new Rectangle(
    //                    xPos + (i * pipSpacing),
    //                    yPos - FRAME_METER_VERTICAL_SPACING - entityPipHeight,
    //                    xPos + pipWidth + (i * pipSpacing),
    //                    yPos - FRAME_METER_VERTICAL_SPACING
    //                );
    //                g.FillRectangle(r.GetBrush(frameMeter.EntityMeters[0].FrameArr[i].Type), rect);
    //            }
    //        }
    //        if (!frameMeter.EntityMeters[1].Hide)
    //        {
    //            // Border
    //            g.FillRectangle(r.GetBrush(GeneralPalette.BLACK), new Rectangle(
    //                xPos - borderThickness,
    //                yPos + 2 * (pipHeight + FRAME_METER_VERTICAL_SPACING),
    //                xPos + totalWidth + borderThickness,
    //                yPos + borderThickness + entityPipHeight + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight)
    //            ));
    //            for (int i = 0; i < frameMeter.EntityMeters[1].FrameArr.Length; i++)
    //                {
    //                rect = new Rectangle(
    //                    xPos + (i * pipSpacing),
    //                    yPos + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight),
    //                    xPos + pipWidth + (i * pipSpacing),
    //                    yPos + entityPipHeight + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight)
    //                );
    //                g.FillRectangle(r.GetBrush(frameMeter.EntityMeters[1].FrameArr[i].Type), rect);
    //            }
    //        }

    //        // Labels
    //        var p1LabelPosition = new Point(xPos + pipWidth, yPos - FRAME_METER_VERTICAL_SPACING -
    //            entityPipHeight - borderThickness + 4 - (r.Font.FontSize * 3 / 2));
    //        var p2LabelPosition = new Point(xPos + pipWidth, yPos + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight) +
    //            entityPipHeight + borderThickness + -4);

    //        // Semi-transparent label backgrounds
    //        var startupDimensions = g.MeasureString(r.Font, "S: 99  A: -99");
    //        g.FillRectangle(r.GetBrush(GeneralPalette.LABEL_BG), new Rectangle(p1LabelPosition.X - r.Font.FontSize / 2, p1LabelPosition.Y,
    //            p1LabelPosition.X + startupDimensions.X + r.Font.FontSize * 3 / 4, p1LabelPosition.Y + startupDimensions.Y + r.Font.FontSize / 8));
    //        g.FillRectangle(r.GetBrush(GeneralPalette.LABEL_BG), new Rectangle(p2LabelPosition.X - r.Font.FontSize / 2, p2LabelPosition.Y,
    //            p2LabelPosition.X + startupDimensions.X + r.Font.FontSize * 3 / 4, p2LabelPosition.Y + startupDimensions.Y + r.Font.FontSize / 8));

    //        // Startup
    //        DrawOutlinedText(g, r.Font, r.GetBrush(GeneralPalette.WHITE), r.GetBrush(GeneralPalette.BLACK), p1LabelPosition,
    //            $"S: {(frameMeter.PlayerMeters[0].Startup >= 0 ? frameMeter.PlayerMeters[0].Startup : "-")}");

    //        DrawOutlinedText(g, r.Font, r.GetBrush(GeneralPalette.WHITE), r.GetBrush(GeneralPalette.BLACK), p2LabelPosition,
    //            $"S: {(frameMeter.PlayerMeters[1].Startup >= 0 ? frameMeter.PlayerMeters[1].Startup : "-")}");


    //        // Advantage
    //        var display = frameMeter.PlayerMeters[0].DisplayAdvantage;
    //        IBrush p1AdvantageFontColor = r.GetBrush(GeneralPalette.WHITE);
    //        IBrush p2AdvantageFontColor = r.GetBrush(GeneralPalette.WHITE);
    //        if (display && frameMeter.PlayerMeters[0].Advantage > 0)
    //        {
    //            p1AdvantageFontColor = r.GetBrush(GeneralPalette.GREEN);
    //            p2AdvantageFontColor = r.GetBrush(GeneralPalette.RED);
    //        }
    //        else if (display && frameMeter.PlayerMeters[0].Advantage < 0)
    //        {
    //            p1AdvantageFontColor = r.GetBrush(GeneralPalette.RED);
    //            p2AdvantageFontColor = r.GetBrush(GeneralPalette.GREEN);
    //        }

    //        Point p1AdvLabelPosition = new Point(p1LabelPosition.X + (r.Font.FontSize * 3), p1LabelPosition.Y);
    //        Point p2AdvLabelPosition = new Point(p2LabelPosition.X + (r.Font.FontSize * 3), p2LabelPosition.Y);

    //        DrawOutlinedText(g, r.Font, p1AdvantageFontColor, r.GetBrush(GeneralPalette.BLACK), p1AdvLabelPosition,
    //            $"A: {(display ? frameMeter.PlayerMeters[0].Advantage : "-")}");
    //        DrawOutlinedText(g, r.Font, p2AdvantageFontColor, r.GetBrush(GeneralPalette.BLACK), p2AdvLabelPosition,
    //            $"A: {(display ? frameMeter.PlayerMeters[1].Advantage : "-")}");
    //    }

    //    private const int LEGEND_Y_POS_PX = 120;
    //    private const int LEGEND_ASSUMED_FRAME_METER_SIZE = 80;
    //    private const int LEGEND_MAX_COLUMN_SIZE = 6;
    //    internal static void DrawFrameMeterLegend(Graphics g, GraphicsResources r, Dimensions windowDimensions)
    //    {
    //        int screenWidth = windowDimensions.Height * 4 / 3;

    //        int pipSpacing = (screenWidth - FRAME_METER_BASE_LINE_X * 2) / LEGEND_ASSUMED_FRAME_METER_SIZE;
    //        int pipWidth = pipSpacing - 1;
    //        int totalWidth = (pipSpacing * LEGEND_ASSUMED_FRAME_METER_SIZE) - 1;
    //        int pipHeight = pipWidth * 5 / 3;
    //        int yPos = LEGEND_Y_POS_PX * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;
    //        int xPos = (windowDimensions.Width - totalWidth) / 2;
    //        int propertyHighlightHeight = pipHeight * 2 / 7;
    //        propertyHighlightHeight += propertyHighlightHeight == 0 ? 1 : 0;
    //        int propertyHighlightTop = pipHeight - propertyHighlightHeight;
    //        int borderThickness = 2 * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;

    //        int legendEntryVerticalSpacing = (int)Math.Max((r.LegendFont.FontSize * 2), pipHeight + borderThickness * 2 + 2);
    //        int legendColumnSpacing = (int)(r.LegendFont.FontSize * 12);

    //        Rectangle rect;
    //        Rectangle frameRect;
    //        Line line;
    //        LegendEntry[] entries = GraphicsResources.GetLegend();

    //        // Background
    //        rect = new Rectangle(xPos - r.LegendFont.FontSize, yPos - r.LegendFont.FontSize,
    //            xPos + pipWidth * 2 + legendColumnSpacing + r.LegendFont.FontSize * 12,
    //            yPos + legendEntryVerticalSpacing * 5 + pipHeight + r.LegendFont.FontSize);
    //        g.FillRectangle(r.GetBrush(GeneralPalette.LABEL_BG), rect);

    //        for (int i = 0; i < entries.Length; i++)
    //        {
    //            frameRect = new Rectangle(
    //                xPos + ((i / LEGEND_MAX_COLUMN_SIZE) * legendColumnSpacing),
    //                yPos + ((i % LEGEND_MAX_COLUMN_SIZE) * legendEntryVerticalSpacing),
    //                xPos + pipWidth + ((i / LEGEND_MAX_COLUMN_SIZE) * legendColumnSpacing),
    //                yPos + pipHeight + ((i % LEGEND_MAX_COLUMN_SIZE) * legendEntryVerticalSpacing)
    //            );
    //            // Border
    //            rect = new Rectangle(frameRect.Left - borderThickness, frameRect.Top - borderThickness,
    //                frameRect.Right + borderThickness, frameRect.Bottom + borderThickness);
    //            g.FillRectangle(r.GetBrush(GeneralPalette.BLACK), rect);

    //            g.FillRectangle(r.GetBrush(entries[i].ExampleFrame.Type), frameRect);
    //            if (entries[i].ExampleFrame.SecondaryProperty != FrameMeter.SecondaryFrameProperty.Default)
    //            {
    //                rect = new Rectangle(
    //                    frameRect.Left,
    //                    frameRect.Top,
    //                    frameRect.Right,
    //                    frameRect.Top + propertyHighlightHeight
    //                );
    //                g.FillRectangle(r.GetBrush(entries[i].ExampleFrame.SecondaryProperty), rect);
    //                line = new Line(
    //                    frameRect.Left,
    //                    frameRect.Top + propertyHighlightHeight,
    //                    frameRect.Right,
    //                    frameRect.Top + propertyHighlightHeight
    //                );
    //                g.DrawLine(r.GetBrush(GeneralPalette.BLACK), line, LINE_THICKNESS);
    //            }
    //            if (entries[i].ExampleFrame.PrimaryProperty1 != FrameMeter.PrimaryFrameProperty.Default)
    //            {
    //                rect = new Rectangle(
    //                    frameRect.Left,
    //                    frameRect.Top + propertyHighlightTop,
    //                    frameRect.Right,
    //                    frameRect.Bottom
    //                );
    //                g.FillRectangle(r.GetBrush(entries[i].ExampleFrame.PrimaryProperty1), rect);
    //                line = new Line(
    //                    frameRect.Left,
    //                    frameRect.Top + propertyHighlightTop,
    //                    frameRect.Right,
    //                    frameRect.Top + propertyHighlightTop
    //                );
    //                g.DrawLine(r.GetBrush(GeneralPalette.BLACK), line, LINE_THICKNESS);
    //            }

    //            // Draw label
    //            var pos = new Point(frameRect.Right + r.LegendFont.FontSize, frameRect.Top);
    //            g.DrawOutlinedText(r.LegendFont, r.GetBrush(GeneralPalette.WHITE), r.GetBrush(GeneralPalette.BLACK),
    //                pos, entries[i].Label);
                    
    //        }
    //    }

    //    public static void DrawSettingsLabels(Graphics g, GraphicsResources r, OverlaySettings settings)
    //    {
    //        ArrayList labels = [];
    //        if (settings.RecordDuringHitstop) labels.Add("Hitstop");
    //        if (settings.RecordDuringSuperFlash) labels.Add("Superflash");

    //        if (labels.Count == 0) return;

    //        var label = String.Join(", ", labels.ToArray());

    //        g.DrawTextWithBackground(r.LegendFont, r.GetBrush(GeneralPalette.WHITE), r.GetBrush(GeneralPalette.LABEL_BG), new Point(), label);
    //    }

    //    // Kinda hacky text outline. Might make a proper text renderer later.
    //    private static void DrawOutlinedText(Graphics g, Font font, IBrush fillBrush, IBrush outlineBrush, Point location, string text)
    //    {
    //        Point[] outlineOffsets =
    //        [
    //            new Point(location.X, location.Y-1),
    //            new Point(location.X-1, location.Y),
    //            new Point(location.X+1, location.Y),
    //            new Point(location.X, location.Y+1),
    //        ];

    //        foreach(Point p in outlineOffsets)
    //        {
    //            g.DrawText(font, outlineBrush, p, text);
    //        }

    //        g.DrawText(font, fillBrush, location, text);
    //    }

    //    internal static int FlipVector(Player p) { return p.IsFacingRight ? -1 : 1; }
    //    internal static int FlipVector(Entity proj) { return proj.IsFacingRight ? -1 : 1; }

    //    // Converts the game coordinate space [0,-48000]:[64000:0] to pixel grid coordinate space [0,0]:[639,479]
    //    private static PointInt WorldToPixel(PointInt p, Camera cam)
    //    {
    //        float z = cam.Zoom;
    //        int x = (int)Math.Floor((p.X - cam.LeftEdge) * z / 100);
    //        int y = (int)Math.Floor((p.Y - cam.BottomEdge) * z / 100);
    //        y = y + GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS - GGXXACPR.GGXXACPR.SCREEN_GROUND_PIXEL_OFFSET;
    //        return new PointInt(x, y);
    //    }
    //    private static Rectangle WorldToPixel(Rectangle rect, Camera cam)
    //    {
    //        float z = cam.Zoom;
    //        int x1 = (int)Math.Floor((rect.Left - cam.LeftEdge) * z / 100);
    //        int y1 = (int)Math.Floor((rect.Top - cam.BottomEdge) * z / 100);
    //        int x2 = (int)Math.Floor((rect.Right - cam.LeftEdge) * z / 100);
    //        int y2 = (int)Math.Floor((rect.Bottom - cam.BottomEdge) * z / 100);
    //        y1 += GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS - GGXXACPR.GGXXACPR.SCREEN_GROUND_PIXEL_OFFSET;
    //        y2 += GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS - GGXXACPR.GGXXACPR.SCREEN_GROUND_PIXEL_OFFSET;
    //        return new Rectangle(x1, y1, x2, y2);
    //    }

    //    // Converts the game's pixel grid coordinate space [0,0]:[639,479] to the game window coordinate space [1,1]:[window width, window height]
    //    private static PointInt PixelToWindow(PointInt coor, Dimensions windowDimensions)
    //    {
    //        // Should math out to zero if not in widescreen
    //        int wideScreenOffset = (windowDimensions.Width - (windowDimensions.Height * 4 / 3)) / 2;

    //        // Adding 1 because top left pixel is [1,1] for graphics functions but it's [0,0] for game coordinates
    //        return new PointInt(
    //            (int)Math.Floor(1.0f * (coor.X + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS) + wideScreenOffset,
    //            (int)Math.Floor(1.0f * (coor.Y + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS)
    //        );
    //    }
    //    private static Rectangle PixelToWindow(Rectangle r, Dimensions windowDimensions)
    //    {
    //        // Should math out to zero if not in widescreen
    //        int wideScreenOffset = (windowDimensions.Width - (windowDimensions.Height * 4 / 3)) / 2;

    //        // Adding 1 because top left pixel is [1,1] for graphics functions but it's [0,0] for game coordinates
    //        return new Rectangle(
    //            ((r.Left + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS) + wideScreenOffset,
    //            (r.Top + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS,
    //            ((r.Right + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS) + wideScreenOffset,
    //            (r.Bottom + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS
    //        );
    //    }
    }
}
