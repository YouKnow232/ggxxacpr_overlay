using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    internal static class Input
    {
        private const uint PREV_KEY_STATE_BIT = 0x40000000;
        private static bool IsKeyRepeat(nint lParam)
            => (lParam & PREV_KEY_STATE_BIT) != 0;


        public static void HandleKeyDownEvent(nint lParam, VirtualKeyCodes keyCode)
        {
            if (IsKeyRepeat(lParam)) return;

            switch (keyCode)
            {
                case VirtualKeyCodes.VK_F1:
                    Settings.Hitboxes.DisplayBoxes = !Settings.Hitboxes.DisplayBoxes;
                    break;
                case VirtualKeyCodes.VK_F2:
                    Settings.Misc.DisplayHSDMeter = !Settings.Misc.DisplayHSDMeter;
                    break;
                case VirtualKeyCodes.VK_F3:
                    Settings.FrameMeter.Display = !Settings.FrameMeter.Display;
                    break;
                case VirtualKeyCodes.VK_F4:
                    Settings.Hitboxes.AlwaysDrawThrowRange = !Settings.Hitboxes.AlwaysDrawThrowRange;
                    break;
                case VirtualKeyCodes.VK_F5:
                    if (GGXXACPR.GGXXACPR.IsInGame) Hacks.TogglePauseNoMenu();
                    break;
                case VirtualKeyCodes.VK_F6:
                    if (GGXXACPR.GGXXACPR.IsInGame) Hacks.FrameStepFromPause();
                    break;
                case VirtualKeyCodes.VK_F7:
                    if (GGXXACPR.GGXXACPR.IsInGame) Hacks.ToggleBlackBG();
                    break;
                case VirtualKeyCodes.VK_F8:
                    Settings.ReloadSettings();
                    break;
                case VirtualKeyCodes.VK_F9:
                    Settings.Misc.DisplayHelpDialog = !Settings.Misc.DisplayHelpDialog;
                    break;
            }
        }

    }
}
