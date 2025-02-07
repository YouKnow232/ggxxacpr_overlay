
using System.Data.Common;
using GameOverlay.Drawing;
using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    internal class Drawing
    {
        private const float LINE_THICKNESS = 1f;
        // Values in game pixels
        private const int PIVOT_CROSS_SIZE = 6;
        private const int FRAME_METER_X = 19;
        private const int FRAME_METER_Y = 400;
        private const int FRAME_METER_PIP_WIDTH = 5;
        private const int FRAME_METER_PIP_HEIGHT = 7;
        private const int FRAME_METER_ENTITY_PIP_HEIGHT = 4;
        private const int FRAME_METER_PIP_SPACING = 1 + FRAME_METER_PIP_WIDTH;
        private const int FRAME_METER_PROPERTY_HIGHLIGHT_HEIGHT = 2;
        private const int FRAME_METER_PROPERTY_HIGHLIGHT_TOP = FRAME_METER_PIP_HEIGHT - FRAME_METER_PROPERTY_HIGHLIGHT_HEIGHT;
        private const int FRAME_METER_VERTICAL_SPACING = 1;
        private const int FRAME_METER_FONT_SPACING = 2;

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


        internal static void DrawPlayerPivot(Graphics g, GraphicsResources r, GameState state, Player p, Dimensions windowDimensions)
        {
            DrawPivot(g, r, state, p.XPos, p.YPos, windowDimensions);
        }
        internal static void DrawEntityPivot(Graphics g, GraphicsResources r, GameState state, Entity e, Dimensions windowDimensions)
        {
            DrawPivot(g, r, state, e.XPos, e.YPos, windowDimensions);
        }
        private static void DrawPivot(Graphics g, GraphicsResources r, GameState state, int x, int y, Dimensions windowDimensions)
        {
            g.ClipRegionStart(PixelToWindow(new Rectangle(0, 0, GGXXACPR.GGXXACPR.SCREEN_WIDTH_PIXELS,
                GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS), windowDimensions));
            PointInt coor = PixelToWindow(WorldToPixel(new PointInt(x, y), state.Camera), windowDimensions);
            // GameOverlay.Net Lines seem to not include the starting point pixel. The line defintions are extended to compensate.
            var outline1 = new Line(coor.X - PIVOT_CROSS_SIZE - 2, coor.Y, coor.X + PIVOT_CROSS_SIZE + 1, coor.Y);
            var outline2 = new Line(coor.X, coor.Y - PIVOT_CROSS_SIZE - 2, coor.X, coor.Y + PIVOT_CROSS_SIZE + 1);
            var line1 = new Line(coor.X - PIVOT_CROSS_SIZE - 1, coor.Y, coor.X + PIVOT_CROSS_SIZE, coor.Y);
            var line2 = new Line(coor.X, coor.Y - PIVOT_CROSS_SIZE - 1, coor.X, coor.Y + PIVOT_CROSS_SIZE);
            g.DrawLine(r.FontBorderBrush, outline1, LINE_THICKNESS * 3);
            g.DrawLine(r.FontBorderBrush, outline2, LINE_THICKNESS * 3);
            g.DrawLine(r.PivotBrush, line1, LINE_THICKNESS);
            g.DrawLine(r.PivotBrush, line2, LINE_THICKNESS);
            g.ClipRegionEnd();
        }
        internal static void DrawEntityCore(Graphics g, GraphicsResources r, GameState state, Entity e, Dimensions windowDimensions)
        {
            g.ClipRegionStart(PixelToWindow(new Rectangle(0, 0, GGXXACPR.GGXXACPR.SCREEN_WIDTH_PIXELS,
                GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS), windowDimensions));

            PointInt coor = PixelToWindow(WorldToPixel(new PointInt(e.XPos + (e.CoreX * 100), e.YPos + (e.CoreY * 100)), state.Camera), windowDimensions);
            var line1 = new Line(coor.X - PIVOT_CROSS_SIZE - 1, coor.Y, coor.X + PIVOT_CROSS_SIZE, coor.Y);
            var line2 = new Line(coor.X, coor.Y - PIVOT_CROSS_SIZE - 1, coor.X, coor.Y + PIVOT_CROSS_SIZE);
            g.DrawLine(r.EntityCoreBrush, line1, LINE_THICKNESS);
            g.DrawLine(r.EntityCoreBrush, line2, LINE_THICKNESS);
            g.ClipRegionEnd();
        }
        internal static void DrawPlayerPushBox(Graphics g, GraphicsResources r, GameState state, Player player, Dimensions windowDimensions)
        {
            if (player.Status.NoCollision) { return; }

            g.ClipRegionStart(PixelToWindow(new Rectangle(0, 0, GGXXACPR.GGXXACPR.SCREEN_WIDTH_PIXELS,
                GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS), windowDimensions));

            var pos = new PointInt(player.XPos + player.PushBox.XOffset * 100, player.YPos + player.PushBox.YOffset * 100);

            PointInt coor = PixelToWindow(WorldToPixel(pos, state.Camera), windowDimensions);
            Dimensions boxDim = ScaleBoxDimensions(player.PushBox.Width, player.PushBox.Height, state.Camera, windowDimensions);

            g.OutlineFillRectangle(
                r.CollisionOutlineBrush,
                r.CollisionFillBrush,
                Shrink(CreateRectangle(coor, boxDim), (int)LINE_THICKNESS),
                LINE_THICKNESS
            );
            g.ClipRegionEnd();
        }
        private static Rectangle CreateRectangle(PointInt p, Dimensions dim)
        {
            return new Rectangle(
                p.X,
                p.Y,
                p.X + dim.Width,
                p.Y + dim.Height
            );
        }
        /// <summary>
        /// Creates a new rectangle smaller on all sides by n pixels.
        /// We need this because OutlineFillBox extends the box proprotional to the outline thickness
        /// </summary>
        private static Rectangle Shrink(Rectangle r, int n)
        {
            return new Rectangle(
                r.Left + n,
                r.Top + n,
                r.Right - n,
                r.Bottom - n
            );
        }

        internal static void DrawPlayerBoxes(Graphics g, GraphicsResources r, BoxId[] drawList, GameState state, Player player, Dimensions windowDimensions)
        {
            if (player.HitboxSet == null) return;

            g.ClipRegionStart(PixelToWindow(new Rectangle(0, 0, GGXXACPR.GGXXACPR.SCREEN_WIDTH_PIXELS,
                GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS), windowDimensions));

            foreach (Hitbox hitbox in player.HitboxSet)
            {
                if (hitbox.BoxTypeId == BoxId.HURT && (
                        player.Extra.InvulnCounter > 0 ||
                        player.Status.DisableHurtboxes ||
                        player.Status.StrikeInvuln
                        ) ||
                    // hitboxes are technically disabled in recovery state, but we're going to draw them during hitstop anyway
                    (hitbox.BoxTypeId == BoxId.HIT) && (
                        player.Status.DisableHitboxes &&
                        !(player.HitstopCounter > 0 &&
                        player.AttackFlags.HasConnected)    // Hitstop counter is also used in super flash, so need to check attack flags as well
                        ) ||
                    !drawList.Contains(hitbox.BoxTypeId))
                {
                    continue;
                }

                Hitbox drawbox = ScaleHitbox(hitbox, player);

                var pos = new PointInt(player.XPos + drawbox.XOffset * 100 * FlipVector(player), player.YPos + drawbox.YOffset * 100);

                PointInt coor = PixelToWindow(WorldToPixel(pos, state.Camera), windowDimensions);
                Dimensions boxDim = ScaleBoxDimensions(drawbox.Width, drawbox.Height, state.Camera, windowDimensions);

                if (player.IsFacingRight)
                {
                    coor = new PointInt(coor.X - boxDim.Width, coor.Y);
                }

                g.OutlineFillRectangle(
                    r.GetOutlineBrush(drawbox.BoxTypeId),
                    r.GetFillBrush(drawbox.BoxTypeId),
                    Shrink(CreateRectangle(coor, boxDim), (int)LINE_THICKNESS),
                    LINE_THICKNESS
                );
            }
            g.ClipRegionEnd();
        }

        internal static void DrawProjectileBoxes(Graphics g, GraphicsResources r, BoxId[] drawList, GameState state, Dimensions windowDimensions)
        {
            g.ClipRegionStart(PixelToWindow(new Rectangle(0, 0, GGXXACPR.GGXXACPR.SCREEN_WIDTH_PIXELS,
                GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS), windowDimensions));

            foreach (Entity e in state.Entities)
            {
                foreach(Hitbox hitbox in e.HitboxSet)
                {
                    if (e.Status.DisableHitboxes && hitbox.BoxTypeId == BoxId.HIT ||
                        e.Status.DisableHurtboxes && hitbox.BoxTypeId == BoxId.HURT ||
                        !drawList.Contains(hitbox.BoxTypeId))
                    { continue; }

                    Hitbox drawbox = ScaleHitbox(hitbox, e);

                    var pos = new PointInt(e.XPos + drawbox.XOffset * 100 * FlipVector(e), e.YPos + drawbox.YOffset * 100);

                    PointInt coor = PixelToWindow(WorldToPixel(pos, state.Camera), windowDimensions);
                    Dimensions boxDim = ScaleBoxDimensions(drawbox.Width, drawbox.Height, state.Camera, windowDimensions);

                    if (e.IsFacingRight)
                    {
                        coor = new PointInt(coor.X - boxDim.Width, coor.Y);
                    }

                    g.OutlineFillRectangle(
                        r.GetOutlineBrush(drawbox.BoxTypeId),
                        r.GetFillBrush(drawbox.BoxTypeId),
                        Shrink(CreateRectangle(coor, boxDim), (int)LINE_THICKNESS),
                        LINE_THICKNESS
                    );
                }
                DrawEntityPivot(g, r, state, e, windowDimensions);
            }
            g.ClipRegionEnd();
        }

        private const int FRAME_METER_Y_ALT = 90;
        private const int FRAME_METER_BASE_LINE_X = 5;
        private const int FRAME_METER_BORDER_THICKNESS = 3;
        internal static void DrawFrameMeter(Graphics g, GraphicsResources r, FrameMeter frameMeter, Dimensions windowDimensions)
        {
            FrameMeter.Frame[] frameArr;
            Rectangle rect;
            int frameMeterLength = frameMeter.PlayerMeters[0].FrameArr.Length;

            int screenHeight = windowDimensions.Height;
            int screenWidth = windowDimensions.Height * 4 / 3;

            int pipSpacing = (screenWidth - FRAME_METER_BASE_LINE_X * 2) / frameMeterLength;
            int pipWidth = pipSpacing - 1;
            int totalWidth = (pipSpacing * frameMeterLength) - 1;
            int pipHeight = pipWidth * 5 / 3;
            int entityPipHeight = pipWidth;
            int coreYPos = windowDimensions.Height * FRAME_METER_Y / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;
            int yPos = coreYPos - pipHeight - 1;
            int xPos = (windowDimensions.Width - totalWidth) / 2;
            int propertyHighlightHeight = pipHeight * 2 / 7;
            propertyHighlightHeight += propertyHighlightHeight == 0 ? 1 : 0;
            int propertyHighlightTop = pipHeight - propertyHighlightHeight;
            int borderThickness = 2 * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;

            // Border
            g.FillRectangle(r.FontBorderBrush, new Rectangle(
                xPos - borderThickness,
                yPos - borderThickness,
                xPos + totalWidth + borderThickness,
                yPos + 2 * pipHeight + FRAME_METER_VERTICAL_SPACING + borderThickness
            ));

            // Players
            for (int j = 0; j < frameMeter.PlayerMeters.Length; j++)
            {
                frameArr = frameMeter.PlayerMeters[j].FrameArr;
                for (int i = 0; i < frameArr.Length; i++)
                {
                    rect = new Rectangle(
                        xPos + (i * pipSpacing),
                        yPos + (j * (FRAME_METER_VERTICAL_SPACING + pipHeight)),
                        xPos + pipWidth + (i * pipSpacing),
                        yPos + pipHeight + (j * (FRAME_METER_VERTICAL_SPACING + pipHeight))
                    );
                    g.FillRectangle(r.GetBrush(frameArr[i].Type), rect);
                    if (frameArr[i].Property2 != FrameMeter.FrameProperty2.Default)
                    {
                        rect = new Rectangle(
                            xPos + (i * pipSpacing),
                            yPos + (j * (FRAME_METER_VERTICAL_SPACING + pipHeight)),
                            xPos + pipWidth + (i * pipSpacing),
                            yPos + (j * (FRAME_METER_VERTICAL_SPACING + pipHeight)) + propertyHighlightHeight
                        );
                        g.FillRectangle(r.GetBrush(frameArr[i].Property2), rect);
                    }
                    if (frameArr[i].Property != FrameMeter.FrameProperty1.Default)
                    {
                        rect = new Rectangle(
                            xPos + (i * pipSpacing),
                            yPos + propertyHighlightTop + (j * (FRAME_METER_VERTICAL_SPACING + pipHeight)),
                            xPos + pipWidth + (i * pipSpacing),
                            yPos + pipHeight + (j * (FRAME_METER_VERTICAL_SPACING + pipHeight))
                        );
                        g.FillRectangle(r.GetBrush(frameArr[i].Property), rect);
                    }
                }
            }

            // Entity
            if (!frameMeter.EntityMeters[0].Hide)
            {
                // Border
                g.FillRectangle(r.FontBorderBrush, new Rectangle(
                    xPos - borderThickness,
                    yPos - borderThickness - FRAME_METER_VERTICAL_SPACING - entityPipHeight,
                    xPos + totalWidth + borderThickness,
                    yPos - FRAME_METER_VERTICAL_SPACING
                ));
                for (int i=0; i < frameMeter.EntityMeters[0].FrameArr.Length; i++)
                {
                    rect = new Rectangle(
                        xPos + (i * pipSpacing),
                        yPos - FRAME_METER_VERTICAL_SPACING - entityPipHeight,
                        xPos + pipWidth + (i * pipSpacing),
                        yPos - FRAME_METER_VERTICAL_SPACING
                    );
                    g.FillRectangle(r.GetBrush(frameMeter.EntityMeters[0].FrameArr[i].Type), rect);


                    if (frameMeter.EntityMeters[0].FrameArr[i].Property != FrameMeter.FrameProperty1.Default)
                    {
                        rect = new Rectangle(
                            xPos + (i * pipSpacing),
                            yPos - 1 - FRAME_METER_VERTICAL_SPACING,
                            xPos + pipWidth + (i * pipSpacing),
                            yPos - FRAME_METER_VERTICAL_SPACING
                        );
                        g.FillRectangle(r.GetBrush(frameMeter.EntityMeters[0].FrameArr[i].Property), rect);
                    }
                }
            }
            if (!frameMeter.EntityMeters[1].Hide)
            {
                // Border
                g.FillRectangle(r.FontBorderBrush, new Rectangle(
                    xPos - borderThickness,
                    yPos + 2 * (pipHeight + FRAME_METER_VERTICAL_SPACING),
                    xPos + totalWidth + borderThickness,
                    yPos + borderThickness + entityPipHeight + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight)
                ));
                for (int i = 0; i < frameMeter.EntityMeters[1].FrameArr.Length; i++)
                    {
                    rect = new Rectangle(
                        xPos + (i * pipSpacing),
                        yPos + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight),
                        xPos + pipWidth + (i * pipSpacing),
                        yPos + entityPipHeight + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight)
                    );
                    g.FillRectangle(r.GetBrush(frameMeter.EntityMeters[1].FrameArr[i].Type), rect);
                }
            }

            // Labels
            Point pos;
            Point p1LabelPosition = new Point(xPos + pipWidth, yPos - FRAME_METER_VERTICAL_SPACING -
                entityPipHeight - borderThickness + 4 - (r.Font.FontSize * 3 / 2));
            Point p2LabelPosition = new Point(xPos + pipWidth, yPos + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight) +
                entityPipHeight + borderThickness + -4);

            // Semi-transparent label backgrounds
            var startupDimensions = g.MeasureString(r.Font, "S: 99  A: -99");
            g.FillRectangle(r.LabelBgBrush, new Rectangle(p1LabelPosition.X - r.Font.FontSize / 2, p1LabelPosition.Y,
                p1LabelPosition.X + startupDimensions.X + r.Font.FontSize * 3 / 4, p1LabelPosition.Y + startupDimensions.Y + r.Font.FontSize / 8));
            g.FillRectangle(r.LabelBgBrush, new Rectangle(p2LabelPosition.X - r.Font.FontSize / 2, p2LabelPosition.Y,
                p2LabelPosition.X + startupDimensions.X + r.Font.FontSize * 3 / 4, p2LabelPosition.Y + startupDimensions.Y + r.Font.FontSize / 8));

            // Startup
            DrawOutlinedText(g, r.Font, r.FontBrush, r.FontBorderBrush, p1LabelPosition,
                $"S: {(frameMeter.PlayerMeters[0].Startup >= 0 ? frameMeter.PlayerMeters[0].Startup : "-")}");

            DrawOutlinedText(g, r.Font, r.FontBrush, r.FontBorderBrush, p2LabelPosition,
                $"S: {(frameMeter.PlayerMeters[1].Startup >=0 ? frameMeter.PlayerMeters[1].Startup : "-")}");


            var display = frameMeter.PlayerMeters[0].DisplayAdvantage;
            IBrush p1AdvantageFontColor = r.FontBrush;
            IBrush p2AdvantageFontColor = r.FontBrush;
            if (display && frameMeter.PlayerMeters[0].Advantage > 0)
            {
                p1AdvantageFontColor = r.FontBrushGreen;
                p2AdvantageFontColor = r.FontBrushRed;
            }
            else if (display && frameMeter.PlayerMeters[0].Advantage < 0)
            {
                p1AdvantageFontColor = r.FontBrushRed;
                p2AdvantageFontColor = r.FontBrushGreen;
            }

            // Advantage
            Point p1AdvLabelPosition = new Point(p1LabelPosition.X + (r.Font.FontSize * 3), p1LabelPosition.Y);
            Point p2AdvLabelPosition = new Point(p2LabelPosition.X + (r.Font.FontSize * 3), p2LabelPosition.Y);

            DrawOutlinedText(g, r.Font, p1AdvantageFontColor, r.FontBorderBrush, p1AdvLabelPosition,
                $"A: {(display ? frameMeter.PlayerMeters[0].Advantage : "-")}");
            DrawOutlinedText(g, r.Font, p2AdvantageFontColor, r.FontBorderBrush, p2AdvLabelPosition,
                $"A: {(display ? frameMeter.PlayerMeters[1].Advantage : "-")}");
        }

        private static void HatchRegion(Graphics g, SolidBrush b, Rectangle rect, Dimensions windowDimensions)
        {
            Line line;
            g.ClipRegionStart(PixelToWindow(rect, windowDimensions));
            float height = rect.Bottom - rect.Top;
            float width = rect.Right - rect.Left;
            for (int k = 0; k < height + width; k += (int)(LINE_THICKNESS * 2.5f))
            {
                line = new Line(
                    rect.Left,
                    rect.Top - rect.Right + rect.Left + k,
                    rect.Right,
                    rect.Top + k
                );
                g.DrawLine(b, PixelToWindow(line, windowDimensions), LINE_THICKNESS);
            }
            g.ClipRegionEnd();
        }

        // Kinda hacky text outline. Might make a proper text renderer later.
        private static void DrawOutlinedText(Graphics g, Font font, IBrush fillBrush, IBrush outlineBrush, Point location, string text)
        {
            Point[] outlineOffsets =
            [
                new Point(location.X, location.Y-1),
                new Point(location.X-1, location.Y),
                new Point(location.X+1, location.Y),
                new Point(location.X, location.Y+1),
            ];

            foreach(Point p in outlineOffsets)
            {
                g.DrawText(font, outlineBrush, p, text);
            }

            g.DrawText(font, fillBrush, location, text);
        }

        internal static int FlipVector(Player p) { return p.IsFacingRight ? -1 : 1; }
        internal static int FlipVector(Entity proj) { return proj.IsFacingRight ? -1 : 1; }

        // Converts the game coordinate space [0,-48000]:[64000:0] to pixel grid coordinate space [0,0]:[640,480]
        private static PointInt WorldToPixel(PointInt p, Camera cam)
        {
            float z = cam.Zoom;
            int x = (int)Math.Floor((p.X - cam.LeftEdge) * z / 100);
            int y = (int)Math.Floor((p.Y - cam.BottomEdge) * z / 100);
            y = y + GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS - GGXXACPR.GGXXACPR.SCREEN_GROUND_PIXEL_OFFSET;
            return new PointInt(x, y);
        }

        // Converts the game's pixel grid coordinate space [0,0]:[640,480] to the game window coordinate space [0,0]:[window width, window height]
        private static PointInt PixelToWindow(PointInt coor, Dimensions windowDimensions)
        {
            // Should math out to zero if not in widescreen
            int wideScreenOffset = (windowDimensions.Width - (windowDimensions.Height * 4 / 3)) / 2;

            return new PointInt(
                (int)Math.Floor(1.0f * coor.X * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS) + wideScreenOffset,
                (int)Math.Floor(1.0f * coor.Y * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS)
            );
        }
        private static Rectangle PixelToWindow(Rectangle r, Dimensions windowDimensions)
        {
            // Should math out to zero if not in widescreen
            int wideScreenOffset = (windowDimensions.Width - (windowDimensions.Height * 4 / 3)) / 2;

            return new Rectangle(
                (r.Left * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS) + wideScreenOffset,
                r.Top * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS,
                (r.Right * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS) + wideScreenOffset,
                r.Bottom * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS
            );
        }
        private static Point PixelToWindow(Point p, Dimensions windowDimensions)
        {
            // Should math out to zero if not in widescreen
            int wideScreenOffset = (windowDimensions.Width - (windowDimensions.Height * 4 / 3)) / 2;

            return new Point(
                (p.X * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS) + wideScreenOffset,
               p.Y * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS
            );
        }
        private static Line PixelToWindow(Line l, Dimensions windowDimensions)
        {
            return new Line(PixelToWindow(l.Start, windowDimensions), PixelToWindow(l.End, windowDimensions));
        }

        private static Dimensions ScaleBoxDimensions(int width, int height, Camera cam, Dimensions windowDimensions)
        {
            return new Dimensions(
                (int)Math.Floor(cam.Zoom * width * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS),
                (int)Math.Floor(cam.Zoom * height * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS)
            );
        }

        private static Hitbox ScaleHitbox(Hitbox hitbox, Player p)
        {
            if (p.ScaleX < 0 && p.ScaleY < 0) { return hitbox; }
            // If scale var is -1, apply the other var to both dimensions
            var scaleY = p.ScaleY < 0 ? p.ScaleX : p.ScaleY;
            var scaleX = p.ScaleX < 0 ? p.ScaleY : p.ScaleX;

            return new Hitbox()
            {
                XOffset = (short)Math.Floor(hitbox.XOffset * scaleX / 1000f),
                YOffset = (short)Math.Floor(hitbox.YOffset * scaleY / 1000f),
                Width = (short)Math.Floor(hitbox.Width * scaleX / 1000f),
                Height = (short)Math.Floor(hitbox.Height * scaleY / 1000f),
                BoxTypeId = hitbox.BoxTypeId,
                BoxFlags = hitbox.BoxFlags
            };
        }
        private static Hitbox ScaleHitbox(Hitbox hitbox, Entity e)
        {
            if (e.ScaleX < 0 && e.ScaleY < 0) { return hitbox; }
            // If scale var is -1, apply the other var to both dimensions
            var scaleY = e.ScaleY < 0 ? e.ScaleX : e.ScaleY;
            var scaleX = e.ScaleX < 0 ? e.ScaleY : e.ScaleX;

            return new Hitbox()
            {
                XOffset = (short)(hitbox.XOffset * scaleX / 1000),
                YOffset = (short)(hitbox.YOffset * scaleY / 1000),
                Width = (short)(hitbox.Width * scaleX / 1000),
                Height = (short)(hitbox.Height * scaleY / 1000),
                BoxTypeId = hitbox.BoxTypeId,
                BoxFlags = hitbox.BoxFlags
            };
        }
    }
}
