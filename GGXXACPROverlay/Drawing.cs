
using GameOverlay.Drawing;
using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    internal class Drawing
    {
        private static readonly float LINE_THICKNESS = 1f;
        // Values in game pixels
        private static readonly int PIVOT_CROSS_SIZE = 6;
        private static readonly int FRAME_METER_X = 19;
        private static readonly int FRAME_METER_Y = 390;
        private static readonly int FRAME_METER_PIP_WIDTH = 5;
        private static readonly int FRAME_METER_PIP_HEIGHT = 7;
        private static readonly int FRAME_METER_ENTITY_PIP_HEIGHT = 4;
        private static readonly int FRAME_METER_PIP_SPACING = 1 + FRAME_METER_PIP_WIDTH;
        private static readonly int FRAME_METER_PROPERTY_HIGHLIGHT_HEIGHT = 2;
        private static readonly int FRAME_METER_PROPERTY_HIGHLIGHT_TOP = FRAME_METER_PIP_HEIGHT - FRAME_METER_PROPERTY_HIGHLIGHT_HEIGHT;
        private static readonly int FRAME_METER_VERTICAL_SPACING = 1;
        private static readonly int FRAME_METER_FONT_SPACING = 5;

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
            PointInt coor = PixelToWindow(WorldToPixel(new PointInt(p.XPos, p.YPos), state.Camera), windowDimensions);
            // GameOverlay.Net Lines seem to not include the starting point pixel. The line defintions are extended to compensate.
            var line1 = new Line(coor.X - PIVOT_CROSS_SIZE - 1, coor.Y, coor.X + PIVOT_CROSS_SIZE, coor.Y);
            var line2 = new Line(coor.X, coor.Y - PIVOT_CROSS_SIZE - 1, coor.X, coor.Y + PIVOT_CROSS_SIZE);
            g.DrawLine(r.PivotBrush, line1, LINE_THICKNESS);
            g.DrawLine(r.PivotBrush, line2, LINE_THICKNESS);
        }
        internal static void DrawEntityPivot(Graphics g, GraphicsResources r, GameState state, Entity e, Dimensions windowDimensions)
        {
            PointInt coor = PixelToWindow(WorldToPixel(new PointInt(e.XPos, e.YPos), state.Camera), windowDimensions);
            // GameOverlay.Net Lines seem to not include the starting point pixel. The line defintions are extended to compensate.
            var line1 = new Line(coor.X - PIVOT_CROSS_SIZE - 1, coor.Y, coor.X + PIVOT_CROSS_SIZE, coor.Y);
            var line2 = new Line(coor.X, coor.Y - PIVOT_CROSS_SIZE - 1, coor.X, coor.Y + PIVOT_CROSS_SIZE);
            g.DrawLine(r.PivotBrush, line1, LINE_THICKNESS);
            g.DrawLine(r.PivotBrush, line2, LINE_THICKNESS);
        }
        internal static void DrawEntityCore(Graphics g, GraphicsResources r, GameState state, Entity e, Dimensions windowDimensions)
        {
            PointInt coor = PixelToWindow(WorldToPixel(new PointInt(e.XPos + (e.CoreX * 100), e.YPos + (e.CoreY * 100)), state.Camera), windowDimensions);
            var line1 = new Line(coor.X - PIVOT_CROSS_SIZE - 1, coor.Y, coor.X + PIVOT_CROSS_SIZE, coor.Y);
            var line2 = new Line(coor.X, coor.Y - PIVOT_CROSS_SIZE - 1, coor.X, coor.Y + PIVOT_CROSS_SIZE);
            g.DrawLine(r.EntityCoreBrush, line1, LINE_THICKNESS);
            g.DrawLine(r.EntityCoreBrush, line2, LINE_THICKNESS);
        }
        internal static void DrawPlayerPushBox(Graphics g, GraphicsResources r, GameState state, Player player, Dimensions windowDimensions)
        {
            if (player.Status.NoCollision) { return; }

            var pos = new PointInt(player.XPos + player.PushBox.XOffset * 100, player.YPos + player.PushBox.YOffset * 100);

            PointInt coor = PixelToWindow(WorldToPixel(pos, state.Camera), windowDimensions);
            Dimensions boxDim = ScaleBoxDimensions(player.PushBox.Width, player.PushBox.Height, state.Camera, windowDimensions);

            g.OutlineFillRectangle(
                r.CollisionOutlineBrush,
                r.CollisionFillBrush,
                Shrink(CreateRectangle(coor, boxDim), (int)LINE_THICKNESS),
                LINE_THICKNESS
            );
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
                        player.HitstopCounter == 0 
                        ) ||
                    !drawList.Contains(hitbox.BoxTypeId))
                    { continue; }

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
        }

        internal static void DrawProjectileBoxes(Graphics g, GraphicsResources r, BoxId[] drawList, GameState state, Dimensions windowDimensions)
        {
            foreach(Entity e in state.Entities)
            {
                DrawEntityPivot(g, r, state, e, windowDimensions);
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
            }
        }

        internal static void DrawFrameMeter(Graphics g, GraphicsResources r, FrameMeter frameMeter, Dimensions windowDimensions)
        {
            FrameMeter.Frame[] frameArr;
            Rectangle rect;

            // Border
            g.FillRectangle(r.FontBorderBrush, PixelToWindow(new Rectangle(
                FRAME_METER_X - 1,
                FRAME_METER_Y - 1,
                FRAME_METER_X + FRAME_METER_PIP_SPACING * frameMeter.PlayerMeters[0].FrameArr.Length,
                FRAME_METER_Y + 2 + FRAME_METER_PIP_HEIGHT * 2),
                windowDimensions
            ));

            // Player
            for (int j = 0; j < frameMeter.PlayerMeters.Length; j++)
            {
                frameArr = frameMeter.PlayerMeters[j].FrameArr;
                for (int i = 0; i < frameArr.Length; i++)
                {
                    rect = new Rectangle(
                        FRAME_METER_X + (i * FRAME_METER_PIP_SPACING),
                        FRAME_METER_Y + (j * (FRAME_METER_VERTICAL_SPACING + FRAME_METER_PIP_HEIGHT)),
                        FRAME_METER_X + FRAME_METER_PIP_WIDTH + (i * FRAME_METER_PIP_SPACING),
                        FRAME_METER_Y + FRAME_METER_PIP_HEIGHT + (j * (FRAME_METER_VERTICAL_SPACING + FRAME_METER_PIP_HEIGHT))
                    );
                    g.FillRectangle(r.GetBrush(frameArr[i].Type), PixelToWindow(rect, windowDimensions));
                    if (frameArr[i].Property2 != FrameMeter.FrameProperty2.Default)
                    {
                        rect = new Rectangle(
                            FRAME_METER_X + (i * FRAME_METER_PIP_SPACING),
                            FRAME_METER_Y + (j * (FRAME_METER_VERTICAL_SPACING + FRAME_METER_PIP_HEIGHT)),
                            FRAME_METER_X + FRAME_METER_PIP_WIDTH + (i * FRAME_METER_PIP_SPACING),
                            FRAME_METER_Y + (j * (FRAME_METER_VERTICAL_SPACING + FRAME_METER_PIP_HEIGHT)) + FRAME_METER_PROPERTY_HIGHLIGHT_HEIGHT
                        );
                        g.FillRectangle(r.GetBrush(frameArr[i].Property2), PixelToWindow(rect, windowDimensions));
                    }
                    if (frameArr[i].Property != FrameMeter.FrameProperty1.Default)
                    {
                        rect = new Rectangle(
                            FRAME_METER_X + (i * FRAME_METER_PIP_SPACING),
                            FRAME_METER_Y + FRAME_METER_PROPERTY_HIGHLIGHT_TOP + (j * (FRAME_METER_VERTICAL_SPACING + FRAME_METER_PIP_HEIGHT)),
                            FRAME_METER_X + FRAME_METER_PIP_WIDTH + (i * FRAME_METER_PIP_SPACING),
                            FRAME_METER_Y + FRAME_METER_PIP_HEIGHT + (j * (FRAME_METER_VERTICAL_SPACING + FRAME_METER_PIP_HEIGHT))
                        );
                        g.FillRectangle(r.GetBrush(frameArr[i].Property), PixelToWindow(rect, windowDimensions));
                    }
                }
            }

            

            // Entity
            if (!frameMeter.EntityMeters[0].Hide)
            {
                // Border
                g.FillRectangle(r.FontBorderBrush, PixelToWindow(new Rectangle(
                    FRAME_METER_X - 1,
                    FRAME_METER_Y - 1 - FRAME_METER_VERTICAL_SPACING - FRAME_METER_ENTITY_PIP_HEIGHT,
                    FRAME_METER_X + FRAME_METER_PIP_SPACING * frameMeter.PlayerMeters[0].FrameArr.Length,
                    FRAME_METER_Y - 1),
                    windowDimensions
                ));
                for (int i=0; i < frameMeter.EntityMeters[0].FrameArr.Length; i++)
                {
                    rect = new Rectangle(
                        FRAME_METER_X + (i * FRAME_METER_PIP_SPACING),
                        FRAME_METER_Y - FRAME_METER_VERTICAL_SPACING - FRAME_METER_ENTITY_PIP_HEIGHT,
                        FRAME_METER_X + FRAME_METER_PIP_WIDTH + (i * FRAME_METER_PIP_SPACING),
                        FRAME_METER_Y - FRAME_METER_VERTICAL_SPACING
                    );
                    g.FillRectangle(r.GetBrush(frameMeter.EntityMeters[0].FrameArr[i].Type), PixelToWindow(rect, windowDimensions));
                }
            }
            if (!frameMeter.EntityMeters[1].Hide)
            {
                // Border
                g.FillRectangle(r.FontBorderBrush, PixelToWindow(new Rectangle(
                    FRAME_METER_X - 1,
                    FRAME_METER_Y + 2 * (FRAME_METER_VERTICAL_SPACING + FRAME_METER_PIP_HEIGHT),
                    FRAME_METER_X + FRAME_METER_PIP_SPACING * frameMeter.PlayerMeters[1].FrameArr.Length,
                    FRAME_METER_Y + 1 + FRAME_METER_ENTITY_PIP_HEIGHT + 2 * (FRAME_METER_VERTICAL_SPACING + FRAME_METER_PIP_HEIGHT)),
                    windowDimensions
                ));
                for (int i = 0; i < frameMeter.EntityMeters[1].FrameArr.Length; i++)
                    {
                    rect = new Rectangle(
                        FRAME_METER_X + (i * FRAME_METER_PIP_SPACING),
                        FRAME_METER_Y + 2 * (FRAME_METER_VERTICAL_SPACING + FRAME_METER_PIP_HEIGHT),
                        FRAME_METER_X + FRAME_METER_PIP_WIDTH + (i * FRAME_METER_PIP_SPACING),
                        FRAME_METER_Y + FRAME_METER_ENTITY_PIP_HEIGHT + 2 * (FRAME_METER_VERTICAL_SPACING + FRAME_METER_PIP_HEIGHT)
                    );
                    g.FillRectangle(r.GetBrush(frameMeter.EntityMeters[1].FrameArr[i].Type), PixelToWindow(rect, windowDimensions));
                }
            }

            // Labels
            Point pos;
            Point p1LabelPosition = new Point(FRAME_METER_X, FRAME_METER_Y - FRAME_METER_VERTICAL_SPACING - FRAME_METER_ENTITY_PIP_HEIGHT - FRAME_METER_FONT_SPACING);
            Point p2LabelPosition = new Point(FRAME_METER_X, FRAME_METER_Y + FRAME_METER_FONT_SPACING + (2 * (FRAME_METER_VERTICAL_SPACING + FRAME_METER_PIP_HEIGHT)) + FRAME_METER_ENTITY_PIP_HEIGHT);
            if (frameMeter.PlayerMeters[0].Startup >= 0)
            {
                pos = PixelToWindow(p1LabelPosition, windowDimensions);
                pos = new Point(pos.X, pos.Y - r.Font.FontSize);
                g.DrawTextWithBackground(r.Font, r.FontBrush, r.FontBorderBrush, pos, $"S:{frameMeter.PlayerMeters[0].Startup}");
            }
            if (frameMeter.PlayerMeters[1].Startup >= 0)
            {
                g.DrawTextWithBackground(r.Font, r.FontBrush, r.FontBorderBrush, PixelToWindow(p2LabelPosition, windowDimensions), $"S:{frameMeter.PlayerMeters[1].Startup}");
            }
            if (frameMeter.PlayerMeters[0].DisplayAdvantage)
            {
                pos = PixelToWindow(p1LabelPosition, windowDimensions);
                pos = new Point(pos.X + (r.Font.FontSize * 4), pos.Y - r.Font.FontSize);
                g.DrawTextWithBackground(r.Font, r.FontBrush, r.FontBorderBrush, pos, $"Adv:{frameMeter.PlayerMeters[0].Advantage}");

                pos = PixelToWindow(p2LabelPosition, windowDimensions);
                pos = new Point(pos.X + (r.Font.FontSize * 4), pos.Y);
                g.DrawTextWithBackground(r.Font, r.FontBrush, r.FontBorderBrush, pos, $"Adv:{frameMeter.PlayerMeters[1].Advantage}");
            }
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
