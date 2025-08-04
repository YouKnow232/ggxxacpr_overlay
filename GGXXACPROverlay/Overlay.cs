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
                { DrawOperation.MiscRange,  RenderRangeBoxes },
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
            Matrix4x4 transform = GGXXACPR.GGXXACPR.GetModelTransform(p) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
            using var hitboxes = GGXXACPR.GGXXACPR.GetHitboxes(boxType, p);

            if (hitboxes.Length == 0) return;

            _graphics.SetDeviceContext(GraphicsContext.Hitbox);
            if (Settings.CombineBoxes && hitboxes.Length > 1)
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
                        iEntity.Status.HasFlag(ActionState.DisableHitboxes) && !Settings.IgnoreDisableHitboxFlag &&
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

                    if (Settings.CombineBoxes && hitboxes.Length > 1)
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
                    transform = GGXXACPR.GGXXACPR.GetEntityTransform(iEntity) * GGXXACPR.GGXXACPR.GetProjectionTransform(cam);
                    _graphics.DrawRectangles(Drawing.GetPivotRangeBoxPrimitive(ebox), transform);
                }

                iEntity = iEntity.Next;
            }
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

        private void RenderRangeBoxes(Player p1, Player p2, Camera cam)
        {
            if (!Settings.HideP2) RenderRangeBoxes(p2, cam);
            if (!Settings.HideP1) RenderRangeBoxes(p1, cam);
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
