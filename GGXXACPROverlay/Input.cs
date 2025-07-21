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
                    Settings.DisplayBoxes = !Settings.DisplayBoxes;
                    break;
                case VirtualKeyCodes.VK_F2:
                    Settings.DisplayHSDMeter = !Settings.DisplayHSDMeter;
                    break;
                case VirtualKeyCodes.VK_F3:
                    Settings.AlwaysDrawThrowRange = !Settings.AlwaysDrawThrowRange;
                    break;
                case VirtualKeyCodes.VK_F4:
                    if (Settings.HideP1 && Settings.HideP2) { Settings.HideP1 = true; Settings.HideP2 = true; }
                    else if (!Settings.HideP1 && Settings.HideP2) { Settings.HideP1 = true; Settings.HideP2 = false; }
                    else if (Settings.HideP1 && !Settings.HideP2) { Settings.HideP1 = false; Settings.HideP2 = false; }
                    else if (!Settings.HideP1 && !Settings.HideP2) { Settings.HideP1 = false; Settings.HideP2 = true; }
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

            }
        }

    }
}
