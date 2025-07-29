using System.Numerics;
using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    internal class Overlay : IDisposable
    {
        public static Overlay? Instance { get; private set; }

        private readonly Graphics _graphics;
        private readonly GraphicsResources _resources;

        private delegate void DrawDelegate(Player p1, Player p2, Camera cam);
        private readonly DrawDelegate? DrawFunctions;

        public Overlay(Graphics graphics)
        {
            _graphics = graphics;
            _resources = new GraphicsResources();
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


        public void RenderFrame(nint devicePointer)
        {
            if (!GGXXACPR.GGXXACPR.ShouldRender) return;
            _graphics.UpdateDevice(devicePointer);

            var p1 = GGXXACPR.GGXXACPR.Player1;
            var p2 = GGXXACPR.GGXXACPR.Player2;
            var cam = GGXXACPR.GGXXACPR.Camera;

            if (!p1.IsValid || !p2.IsValid) return;

            _graphics.BeginScene();
            if (Settings.WidescreenClipping) _graphics.SetScissorRect(GGXXACPR.GGXXACPR.GetGameRegion());

            // TODO: Test this more
            //GGXXACPR.GGXXACPR.RenderText("TEST!", 212, 368, 0xFF);

            if (Settings.DisplayBoxes) 
                DrawFunctions?.Invoke(p1, p2, cam);

            if (Settings.DisplayHSDMeter)
            {
                RenderComboTimeMeter(p2);
                RenderUntechTimeMeter(p2);
                RenderComboTimeMeter(p1);
                RenderUntechTimeMeter(p1);
            }

            _graphics.EndScene();
        }

        private unsafe void RenderComboTimeMeter(Player p)
        {
            _graphics.SetDeviceContext(GraphicsContext.ComboTime, p.Extra.ComboTime);
            _graphics.DrawRectangles(
                p.PlayerIndex == 0 ? _resources.ComboTimeMeterP2 : _resources.ComboTimeMeterP1,
                GGXXACPR.GGXXACPR.GetWidescreenCorrectionTransform());
        }
        private unsafe void RenderUntechTimeMeter(Player p)
        {
            _graphics.SetDeviceContext(GraphicsContext.Meter, p.Extra.UntechTimer + 1, 60.0f);
            _graphics.DrawRectangles(
                p.PlayerIndex == 0 ? _resources.UntechTimeMeterP2 : _resources.UntechTimeMeterP1,
                GGXXACPR.GGXXACPR.GetWidescreenCorrectionTransform());
        }

        private void RenderPlayerBoxes(Player p, Camera cam, BoxId boxType)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetModelTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
            using var rentalSlice = GGXXACPR.GGXXACPR.GetHitboxes(boxType, p);
            using var rentalArray = Drawing.GetHitboxPrimitives(rentalSlice.Span);
            _graphics.DrawRectangles(rentalArray.Span, transform);
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
            if (!Root.IsValid) return;

            Entity iEntity = Root.Next;

            while (!iEntity.Equals(Root))
            {
                if (!iEntity.IsValid) return;
                if (iEntity.PlayerIndex == playerIndexFilter)
                {
                    Matrix4x4 transform = GGXXACPR.GGXXACPR.GetModelTransform(iEntity) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
                    using var rentalHitboxArraySlice = GGXXACPR.GGXXACPR.GetHitboxes(boxType, iEntity);
                    using var rentalColorRectangleArray = Drawing.GetHitboxPrimitives(rentalHitboxArraySlice.Span);
                    _graphics.DrawRectangles(rentalColorRectangleArray.Span, transform);
                }

                iEntity = iEntity.Next;
            }
        }

        private void RenderPivot(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Pivot);
            using var rental = Drawing.GetPivot(p, GGXXACPR.GGXXACPR.WorldCoorPerViewPixel(cam));
            _graphics.DrawRectangles(rental.Span, GGXXACPR.GGXXACPR.GetProjectionTransform(cam));
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
