
using GameOverlay.Drawing;

namespace GGXXACPROverlay
{
    internal class Drawing
    {
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

        internal static void DrawPlayerPivot(Graphics g, SolidBrush brush, Player p, Camera cam, Dimensions windowDimensions)
        {
            PointInt coor = ScreenToWindow(WorldToScreen(new PointInt(p.XPos, p.YPos), cam), windowDimensions);
            // GameOverlay.Net Lines seem to not include the starting point pixel. The line defintions are extended to compensate.
            var line1 = new Line(coor.X - 3, coor.Y, coor.X + 2, coor.Y);
            var line2 = new Line(coor.X, coor.Y - 3, coor.X, coor.Y + 2);
            g.DrawLine(brush, line1, 1);
            g.DrawLine(brush, line2, 1);
        }
        internal static void DrawPlayerPushBox(Graphics g, SolidBrush outline, SolidBrush fill, Player p, Camera cam, Dimensions windowDimensions)
        {
            var pos = new PointInt(p.XPos + p.PushBox.XOffset * 100, p.YPos + p.PushBox.YOffset * 100);

            PointInt coor = ScreenToWindow(WorldToScreen(pos, cam), windowDimensions);
            Dimensions boxDim = ScaleBoxDimensions(p.PushBox.Width, p.PushBox.Height, cam, windowDimensions);

            g.OutlineFillRectangle(
                outline,
                fill,
                CreateRectangle(coor, boxDim),
                1
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

        internal static void DrawPlayerBoxesById(Graphics g, SolidBrush outlineBrush, SolidBrush fillBrush, int boxId, Player p, Camera cam, Dimensions windowDimensions)
        {
            if (p.HitboxSet == null) return;

            foreach (Hitbox hitbox in p.HitboxSet)
            {
                if (GGXXACPR.IsHurtBox(hitbox) && (
                        p.extra.InvulnCounter > 0 ||
                        p.Status.IsStrikeInvuln
                        ) ||
                    // hitboxes are technically disabled in recovery state, but we're going to draw them during hitstop anyway
                    (GGXXACPR.IsHitBox(hitbox) && (
                        p.Status.IsInRecovery &&
                        p.HitstopCounter == 0 
                        )) ||
                    hitbox.BoxTypeId != boxId)
                    { continue; }

                var pos = new PointInt(p.XPos + hitbox.XOffset * 100 * FlipVector(p), p.YPos + hitbox.YOffset * 100);

                PointInt coor = ScreenToWindow(WorldToScreen(pos, cam), windowDimensions);
                Dimensions boxDim = ScaleBoxDimensions(hitbox.Width, hitbox.Height, cam, windowDimensions);

                if (p.IsFacingRight)
                {
                    coor = new PointInt(coor.X - boxDim.Width, coor.Y);
                }

                g.OutlineFillRectangle(
                    outlineBrush,
                    fillBrush,
                    CreateRectangle(coor, boxDim),
                    1
                );
            }
        }

        internal static void DrawProjectileBoxes(Graphics g, SolidBrush outline, SolidBrush fill, int boxId, Projectile[] projectiles, Camera cam, Dimensions windowDimensions)
        {
            foreach(Projectile proj in projectiles)
            {
                foreach(Hitbox hitbox in proj.HitboxSet)
                {

                    if (!proj.IsActive && hitbox.BoxTypeId == 1 || hitbox.BoxTypeId != boxId)
                    { continue; }

                    var pos = new PointInt(proj.XPos + hitbox.XOffset * 100 * FlipVector(proj), proj.YPos + hitbox.YOffset * 100);

                    PointInt coor = ScreenToWindow(WorldToScreen(pos, cam), windowDimensions);
                    Dimensions boxDim = ScaleBoxDimensions(hitbox.Width, hitbox.Height, cam, windowDimensions);

                    if (proj.IsFacingRight)
                    {
                        coor = new PointInt(coor.X - boxDim.Width, coor.Y);
                    }

                    g.OutlineFillRectangle(
                        outline,
                        fill,
                        CreateRectangle(coor, boxDim),
                        1
                    );
                }
            }
        }

        internal static int FlipVector(Player p) { return p.IsFacingRight ? -1 : 1; }
        internal static int FlipVector(Projectile proj) { return proj.IsFacingRight ? -1 : 1; }

        // Converts the game coordinate space [0,-48000]:[64000:0] to pixel grid coordinate space [0,0]:[640,480]
        private static PointInt WorldToScreen(PointInt p, Camera cam)
        {
            float z = cam.Zoom;
            int x = (int)Math.Floor((p.X - cam.LeftEdge) * z / 100);
            int y = (int)Math.Floor((p.Y - cam.BottomEdge) * z / 100);
            y = y + GGXXACPR.SCREEN_HEIGHT_PIXELS - GGXXACPR.SCREEN_GROUND_PIXEL_OFFSET;
            return new PointInt(x, y);
        }

        // Converts the game's pixel grid coordinate space [0,0]:[640,480] to the game window coordinate space [0,0]:[window width, window height]
        private static PointInt ScreenToWindow(PointInt coor, Dimensions windowDimensions)
        {
            // Should math out to zero if not in widescreen
            int wideScreenOffset = (windowDimensions.Width - (windowDimensions.Height * 4 / 3)) / 2;

            return new PointInt(
                (coor.X * windowDimensions.Height / GGXXACPR.SCREEN_HEIGHT_PIXELS) + wideScreenOffset,
                coor.Y * windowDimensions.Height / GGXXACPR.SCREEN_HEIGHT_PIXELS
            );
        }

        private static Dimensions ScaleBoxDimensions(int width, int height, Camera cam, Dimensions windowDimensions)
        {
            return new Dimensions(
                (int)((width * windowDimensions.Height / GGXXACPR.SCREEN_HEIGHT_PIXELS) * cam.Zoom),
                (int)((height * windowDimensions.Height / GGXXACPR.SCREEN_HEIGHT_PIXELS) * cam.Zoom)
            );
        }
    }
}
