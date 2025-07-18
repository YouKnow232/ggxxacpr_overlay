using System.Numerics;
using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    internal class Overlay : IDisposable
    {
        public static Overlay? Instance { get; private set; }

        private readonly Graphics _graphics;

        private delegate void DrawDelegate(Player p1, Player p2, Camera cam);
        private readonly DrawDelegate? DrawFunctions;

        public Overlay(Graphics graphics)
        {
            _graphics = graphics;
            Instance = this;

            var DrawOpMap = new Dictionary<DrawOperation, DrawDelegate>()
            {
                { DrawOperation.Push,       RenderPushboxes },
                { DrawOperation.Grab,       RenderGrabboxes },
                { DrawOperation.Hurt,       RenderHurtboxes },
                { DrawOperation.Hit,        RenderHitboxes },
                { DrawOperation.CleanHit,   RenderCleanHitboxes },
                { DrawOperation.Pivot,      RenderPivots },
            };
            foreach (DrawOperation op in Settings.DrawOrder)
            {
                if (op == DrawOperation.None) continue;
                DrawFunctions += DrawOpMap[op];
            }
        }


        // TODO: Take device pointer as input? Something about thread safety issues when exposing it via the static Overlay instance.
        public void RenderFrame(nint devicePointer)
        {
            if (!GGXXACPR.GGXXACPR.ShouldRender) return;
            _graphics.UpdateDevice(devicePointer);  // TEMP

            var p1 = GGXXACPR.GGXXACPR.Player1;
            var p2 = GGXXACPR.GGXXACPR.Player2;
            var cam = GGXXACPR.GGXXACPR.Camera;

            _graphics.BeginScene();
            if (Settings.WidescreenClipping) _graphics.SetScissorRect(GGXXACPR.GGXXACPR.GetGameRegion());

            // TODO: Test this more
            //GGXXACPR.GGXXACPR.RenderText("TEST!", 212, 368, 0xFF);

            // See constructor and Settings.DrawOrder
            if (Settings.DisplayBoxes) 
                DrawFunctions?.Invoke(p1, p2, cam);

            if (Settings.DisplayHSDMeter)
            {
                RenderComboTimeMeter(p2);
                RenderUntechTimeMeter(p2);
            }

            _graphics.EndScene();
        }

        private unsafe void RenderComboTimeMeter(Player p)
        {
            _graphics.SetDeviceContext(GraphicsContext.ComboTime, p.Extra.ComboTime);
            _graphics.DrawRectangles(Drawing.ComboTimeMeter);
        }
        private unsafe void RenderUntechTimeMeter(Player p)
        {
            _graphics.SetDeviceContext(GraphicsContext.Meter, p.Extra.UntechTimer + 1, 60.0f);
            _graphics.DrawRectangles(Drawing.UntechTimeMeter);
        }

        private void RenderPlayerBoxes(Player p, Camera cam, BoxId boxType)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetModelTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
            _graphics.DrawRectangles(
                Drawing.GetHitboxPrimitives(GGXXACPR.GGXXACPR.GetHitboxes(boxType, p)),
                transform);
        }
        private void RenderCleanHitbox(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetModelTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
            _graphics.DrawRectangles(Drawing.GetCLHitBox(p), transform);
        }
        private void RenderEntityBoxes(int playerIndexFilter, Camera cam, BoxId boxType)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            Entity Root = GGXXACPR.GGXXACPR.RootEntity;
            Entity iEntity = Root.Next;

            while (!iEntity.Equals(Root))
            {
                if (iEntity.PlayerIndex == playerIndexFilter)
                {
                    Matrix4x4 transform = GGXXACPR.GGXXACPR.GetModelTransform(iEntity) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
                    _graphics.DrawRectangles(
                        Drawing.GetHitboxPrimitives(GGXXACPR.GGXXACPR.GetHitboxes(boxType, iEntity)),
                        transform);
                }

                iEntity = iEntity.Next;
            }
        }

        private void RenderPivot(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Pivot);
            _graphics.DrawRectangles(
                Drawing.GetPivot(p, GGXXACPR.GGXXACPR.WorldCoorPerGamePixel(cam)),
                GGXXACPR.GGXXACPR.GetProjectionTransform(cam));
        }

        private void RenderPushbox(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetPlayerTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
            _graphics.DrawRectangles(Drawing.GetPushboxPrimitives(p), transform);
        }

        private void RenderGrabBox(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetPlayerTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
            _graphics.DrawRectangles(Drawing.GetGrabboxPrimitives(p), transform);
        }

        private void RenderCommandGrabBox(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetPlayerTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
            _graphics.DrawRectangles(Drawing.GetCommnadGrabboxPrimitives(p), transform);
        }

        //private void RenderPushAndGrabBox(Player p, Camera cam)
        //{
        //    _graphics.SetDeviceContext(GraphicsContext.Hitbox);
        //    Matrix4x4 transform = GGXXACPR.GGXXACPR.GetPlayerTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
        //    _graphics.DrawRectangles(Drawing.GetPushAndGrabPrimitives(p), transform);
        //}

        private void RenderMiscProximityBoxes(Camera cam)
        {
            throw new NotImplementedException();
        }

        #region WrapperDelegates
        private void RenderPivots(Player p1, Player p2, Camera cam)
        {
            if (!Settings.HideP2) RenderPivot(p2, cam);
            if (!Settings.HideP1) RenderPivot(p1, cam);
        }
        private void RenderCleanHitboxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.HideP2) RenderCleanHitbox(p2, cam);
            if (!Settings.HideP1) RenderCleanHitbox(p1, cam);
        }
        private void RenderHitboxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.HideP2)
            {
                RenderPlayerBoxes(p2, cam, BoxId.HIT);
                RenderEntityBoxes(1, cam, BoxId.HIT);
            }
            if (!Settings.HideP1)
            {
                RenderPlayerBoxes(p1, cam, BoxId.HIT);
                RenderEntityBoxes(0, cam, BoxId.HIT);
            }
        }
        private void RenderHurtboxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.HideP2)
            {
                RenderPlayerBoxes(p2, cam, BoxId.HURT);
                RenderEntityBoxes(1, cam, BoxId.HURT);
            }
            if (!Settings.HideP1)
            {
                RenderPlayerBoxes(p1, cam, BoxId.HURT);
                RenderEntityBoxes(0, cam, BoxId.HURT);
            }
        }
        private void RenderGrabboxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.HideP2)
            {
                if (GGXXACPR.GGXXACPR.IsCommandThrowActive(p2))
                {
                    RenderCommandGrabBox(p2, cam);
                }
                else if (GGXXACPR.GGXXACPR.IsThrowActive(p2) || Settings.AlwaysDrawThrowRange)
                {
                    RenderGrabBox(p2, cam);
                }
            }
            if (!Settings.HideP1)
            {
                if (GGXXACPR.GGXXACPR.IsCommandThrowActive(p1))
                {
                    RenderCommandGrabBox(p1, cam);
                }
                else if (GGXXACPR.GGXXACPR.IsThrowActive(p1) || Settings.AlwaysDrawThrowRange)
                {
                    RenderGrabBox(p1, cam);
                }
            }
        }
        private void RenderPushboxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.HideP2) RenderPushbox(p2, cam);
            if (!Settings.HideP1) RenderPushbox(p1, cam);
        }
        #endregion


        private bool _disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _graphics.Dispose();
                Instance = null;
            }
            // dispose unmanaged here
            // blank
            Debug.Log("[WARNING] Overlay Disposed!");
            _disposed = true;
        }
    }
}
