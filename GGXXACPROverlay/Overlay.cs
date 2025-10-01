using System.Numerics;
using GGXXACPROverlay.GGXXACPR;
using GGXXACPROverlay.Rendering;

namespace GGXXACPROverlay
{
    internal class Overlay : IDisposable
    {
        public static Overlay? Instance { get; private set; }

        private readonly Graphics _graphics;
        private readonly GraphicsResources _resources;

        private delegate void DrawDelegate(Player p1, Player p2, Camera cam);
        private readonly DrawDelegate? BoxDrawFunctions;

        private FrameMeter.FrameMeter _frameMeter;

        private bool _rebuildFrameMeter = false;

        public Overlay(Graphics graphics, GraphicsResources resources, FrameMeter.FrameMeter frameMeter)
        {
            _graphics = graphics;
            _resources = resources;
            _frameMeter = frameMeter;
            Instance = this;

            var DrawOpMap = new Dictionary<DrawOperation, DrawDelegate>()
            {
                { DrawOperation.Push,       RenderPushboxes },
                { DrawOperation.Grab,       RenderGrabboxes },
                { DrawOperation.Hurt,       RenderHurtboxes },
                { DrawOperation.Hit,        RenderHitboxes },
                { DrawOperation.CleanHit,   RenderCleanHitboxes },
                { DrawOperation.Pivot,      RenderPivots },
                { DrawOperation.MiscRange,  RenderRangeBoxes },
            };
            foreach (DrawOperation op in Settings.Hitboxes.DrawOrder)
            {
                if (DrawOpMap.TryGetValue(op, out DrawDelegate? drawDelegate))
                    BoxDrawFunctions += drawDelegate;
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

            if (Settings.Hitboxes.WidescreenClipping)
                _graphics.SetScissorRect(GGXXACPR.GGXXACPR.GetGameRegion());

            // TODO: Test this more
            //GGXXACPR.GGXXACPR.RenderText("TEST!", 212, 368, 0xFF);

            if (Settings.Hitboxes.DisplayBoxes)
                BoxDrawFunctions?.Invoke(p1, p2, cam);

            if (Settings.Misc.DisplayHSDMeter)
            {
                RenderComboTimeMeter(p2);
                RenderComboTimeMeter(p1);
                RenderUntechTimeMeter(p2);
                RenderUntechTimeMeter(p1);
            }

            if (Settings.FrameMeter.Display)
                RenderFrameMeter();

            if (Settings.Misc.DisplayHelpDialog)
                RenderHelpScreen();

            _graphics.EndScene();
        }

        private void RenderHelpScreen()
        {
            using var glyphs = Drawing.GetLegendPrimitives(_graphics.ViewPort, _resources.TextAtlas, out var backDrop);
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetViewPortProjectionTransform();
            _graphics.SetDeviceContext(GraphicsContext.HUD);
            _graphics.DrawRectangles(backDrop, transform);
            _graphics.DrawText(glyphs, transform);
            backDrop.Dispose();
        }

        private void RenderFrameMeter()
        {
            _graphics.SetDeviceContext(GraphicsContext.HUD);
            using var primitives = Drawing.GetFrameMeterPrimitives(_frameMeter, _graphics.ViewPort, _resources.TextAtlas, out var glyphs);
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetViewPortProjectionTransform();
            _graphics.DrawRectangles(primitives, transform);
            _graphics.DrawText(glyphs, transform);

            glyphs.Dispose();
        }

        private void RenderComboTimeMeter(Player p)
        {
            _graphics.SetDeviceContext(GraphicsContext.ComboTime, p.Extra.ComboTime);
            _graphics.DrawRectangles(
                p.PlayerIndex == 0 ? _resources.ComboTimeMeterP2 : _resources.ComboTimeMeterP1,
                GGXXACPR.GGXXACPR.GetWidescreenCorrectionTransform());
        }
        private void RenderUntechTimeMeter(Player p)
        {
            _graphics.SetDeviceContext(GraphicsContext.Meter, p.Extra.UntechTimer + 1, 60.0f);
            _graphics.DrawRectangles(
                p.PlayerIndex == 0 ? _resources.UntechTimeMeterP2 : _resources.UntechTimeMeterP1,
                GGXXACPR.GGXXACPR.GetWidescreenCorrectionTransform());
        }

        private void RenderPlayerBoxes(Player p, Camera cam, BoxId boxType)
        {
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetModelTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
            using var hitboxes = GGXXACPR.GGXXACPR.GetHitboxes(boxType, p);

            if (hitboxes.Length == 0) return;

            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            if (Settings.Hitboxes.CombineBoxes && hitboxes.Length > 1)
            {
                using var combinedGeometry = Drawing.GetCombinedHitboxPrimitives(hitboxes);
                using var borderGeometry = Drawing.GetBorderPrimitives(hitboxes);
                _graphics.DrawTriangles(combinedGeometry, transform);
                _graphics.SetDeviceContext(GraphicsContext.Basic);
                _graphics.DrawTriangles(borderGeometry, transform);
            }
            else
            {
                using var hitboxPrimitives = Drawing.GetHitboxPrimitives(hitboxes);
                _graphics.DrawRectangles(hitboxPrimitives, transform);
            }
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

            while (iEntity.IsValid && !iEntity.Equals(Root))
            {
                if ((boxType == BoxId.HIT && iEntity.Status.HasFlag(ActionState.DisableHitboxes) &&
                        // Discard if disabled hitboxes is flagged and not ignoring that flag in settings
                        iEntity.Status.HasFlag(ActionState.DisableHitboxes) && !Settings.Misc.IgnoreDisableHitboxFlag &&
                        // but only if not in hitstop
                        !(iEntity.HitstopCounter > 0 && iEntity.AttackFlags.HasFlag(AttackState.HasConnected))) ||
                    boxType == BoxId.HURT && iEntity.Status.HasFlag(ActionState.DisableHurtboxes) ||
                    boxType == BoxId.HURT && iEntity.Status.HasFlag(ActionState.StrikeInvuln))
                {
                    iEntity = iEntity.Next;
                    continue;
                }

                if (iEntity.PlayerIndex == playerIndexFilter)
                {
                    Matrix4x4 transform = GGXXACPR.GGXXACPR.GetModelTransform(iEntity) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
                    using var hitboxes = GGXXACPR.GGXXACPR.GetHitboxes(boxType, iEntity);

                    if (hitboxes.Length == 0)
                    {
                        iEntity = iEntity.Next;
                        continue;
                    }

                    if (Settings.Hitboxes.CombineBoxes && hitboxes.Length > 1)
                    {
                        using var combinedGeometry = Drawing.GetCombinedHitboxPrimitives(hitboxes);
                        using var borderGeometry = Drawing.GetBorderPrimitives(hitboxes);
                        _graphics.DrawTriangles(combinedGeometry, transform);
                        _graphics.SetDeviceContext(GraphicsContext.Basic);
                        _graphics.DrawTriangles(borderGeometry, transform);
                    }
                    else
                    {
                        using var primitives = Drawing.GetHitboxPrimitives(hitboxes.Span);
                        _graphics.DrawRectangles(primitives, transform);
                    }
                }

                iEntity = iEntity.Next;
            }
        }

        private void RenderPivot(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Pivot);
            float coordinateRatio = GGXXACPR.GGXXACPR.WorldCoorPerViewPixel(cam);
            var transform = GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
            using var rental = Drawing.GetPivot(p, coordinateRatio);
            _graphics.DrawRectangles(rental.Span, transform);

            Entity root = GGXXACPR.GGXXACPR.RootEntity;
            if (!root.IsValid) return;

            Entity iEntity = root.Next;

            while (iEntity.IsValid && !iEntity.Equals(root))
            {
                if (iEntity.PlayerIndex == p.PlayerIndex)
                {
                    using var eRental = Drawing.GetPivot(iEntity, coordinateRatio);
                    _graphics.DrawRectangles(eRental.Span, transform);
                }

                iEntity = iEntity.Next;
            }
        }

        private void RenderPushbox(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            _graphics.DrawRectangles(
                Drawing.GetPushboxPrimitives(p),
                GGXXACPR.GGXXACPR.GetPlayerTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam));
        }

        private void RenderGrabBox(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            _graphics.DrawRectangles(
                Drawing.GetGrabboxPrimitives(p),
                GGXXACPR.GGXXACPR.GetPlayerTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam));
        }

        private void RenderCommandGrabBox(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            _graphics.DrawRectangles(
                Drawing.GetCommnadGrabboxPrimitives(p),
                GGXXACPR.GGXXACPR.GetPlayerTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam));
        }

        private void RenderRangeBoxes(Player p, Camera cam)
        {
            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetPlayerTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
            GGXXACPR.GGXXACPR.GetProximityBox(p, out var box);
            _graphics.DrawRectangles(Drawing.GetPushRangeBoxPrimitive(box), transform);

            Entity root = GGXXACPR.GGXXACPR.RootEntity;

            if (!root.IsValid) return;

            Entity iEntity = root.Next;

            while (iEntity.IsValid && !iEntity.Equals(root))
            {
                if (iEntity.PlayerIndex == p.PlayerIndex && GGXXACPR.GGXXACPR.GetProximityBox(iEntity, out var ebox))
                {
                    _graphics.DrawRectangles(
                        Drawing.GetPivotRangeBoxPrimitive(ebox),
                        GGXXACPR.GGXXACPR.GetEntityTransform(iEntity) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam));
                }

                iEntity = iEntity.Next;
            }
        }

        #region WrapperDelegates
        private void RenderPivots(Player p1, Player p2, Camera cam)
        {
            if (!Settings.Hitboxes.HideP2) RenderPivot(p2, cam);
            if (!Settings.Hitboxes.HideP1) RenderPivot(p1, cam);
        }
        private void RenderCleanHitboxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.Hitboxes.HideP2) RenderCleanHitbox(p2, cam);
            if (!Settings.Hitboxes.HideP1) RenderCleanHitbox(p1, cam);
        }
        private void RenderHitboxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.Hitboxes.HideP2)
            {
                RenderPlayerBoxes(p2, cam, BoxId.HIT);
                RenderEntityBoxes(1, cam, BoxId.HIT);
            }
            if (!Settings.Hitboxes.HideP1)
            {
                RenderPlayerBoxes(p1, cam, BoxId.HIT);
                RenderEntityBoxes(0, cam, BoxId.HIT);
            }
        }
        private void RenderHurtboxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.Hitboxes.HideP2)
            {
                RenderPlayerBoxes(p2, cam, BoxId.HURT);
                RenderEntityBoxes(1, cam, BoxId.HURT);
            }
            if (!Settings.Hitboxes.HideP1)
            {
                RenderPlayerBoxes(p1, cam, BoxId.HURT);
                RenderEntityBoxes(0, cam, BoxId.HURT);
            }
        }
        private void RenderGrabboxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.Hitboxes.HideP2)
            {
                if (GGXXACPR.GGXXACPR.IsCommandThrowActive(p2))
                {
                    RenderCommandGrabBox(p2, cam);
                }
                else if (GGXXACPR.GGXXACPR.IsThrowActive(p2) || Settings.Hitboxes.AlwaysDrawThrowRange)
                {
                    RenderGrabBox(p2, cam);
                }
            }
            if (!Settings.Hitboxes.HideP1)
            {
                if (GGXXACPR.GGXXACPR.IsCommandThrowActive(p1))
                {
                    RenderCommandGrabBox(p1, cam);
                }
                else if (GGXXACPR.GGXXACPR.IsThrowActive(p1) || Settings.Hitboxes.AlwaysDrawThrowRange)
                {
                    RenderGrabBox(p1, cam);
                }
            }
        }
        private void RenderPushboxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.Hitboxes.HideP2) RenderPushbox(p2, cam);
            if (!Settings.Hitboxes.HideP1) RenderPushbox(p1, cam);
        }

        private void RenderRangeBoxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.Hitboxes.HideP2) RenderRangeBoxes(p2, cam);
            if (!Settings.Hitboxes.HideP1) RenderRangeBoxes(p1, cam);
        }
        #endregion

        public void UpdateGameState()
        {
            if (!GGXXACPR.GGXXACPR.IsInGame)
            {
                _rebuildFrameMeter = true;
                return;
            }

            if (_rebuildFrameMeter)
            {
                Debug.Log("[DEBUG] restarting Frame Meter");
                _frameMeter = new FrameMeter.FrameMeter();
                _rebuildFrameMeter = false;
            }

            _frameMeter.Update();
        }

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
