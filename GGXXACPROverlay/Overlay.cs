using System.Numerics;
using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    internal class Overlay
    {
        public static Overlay? Instance { get; private set; }

        private readonly Graphics _graphics;

        public Overlay(Graphics graphics)
        {
            _graphics = graphics;

            Instance = this;
        }

        public void Dispose()
        {
            _graphics.Dispose();
            Instance = null;
        }

        public void RenderFrame()
        {
            if (!GGXXACPR.GGXXACPR.ShouldRender()) return;

            var p1 = GGXXACPR.GGXXACPR.Player1;
            var p2 = GGXXACPR.GGXXACPR.Player2;
            var cam = GGXXACPR.GGXXACPR.Camera;

            _graphics.BeginScene();

            // TODO: custom draw order
            RenderHitboxes(p2, cam);
            RenderHitboxes(p1, cam);
            //RenderPushbox(p2);
            //RenderPushbox(p1);
            RenderPivot(p2, cam);
            RenderPivot(p1, cam);

            _graphics.EndScene();
        }

        private void RenderHitboxes(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetModelTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
            _graphics.DrawRectangles(Drawing.GetHitboxPrimitives(p), transform);
            _graphics.DrawRectangles([Drawing.GetCLHitBox(p)], transform);
        }

        private void RenderPivot(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Pivot);
            _graphics.DrawRectangles(Drawing.GetPivot(p, GGXXACPR.GGXXACPR.WorldCoorPerGamePixel(cam)), GGXXACPR.GGXXACPR.GetProjectionTransform(cam));
        }

        // TODO
        private void RenderPushbox(Player p)
        {
            throw new NotImplementedException();
        }
    }
}
