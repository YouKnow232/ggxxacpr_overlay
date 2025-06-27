namespace GGXXACPROverlay.GGXXACPR
{
    internal static class Offsets
    {
        public const nint IN_GAME_FLAG  = 0x007101F4;
        public const nint GAME_VER_FLAG = 0x006D0538;   // 0=AC, 1=+R

        // DirectX
        public const nint DIRECT3D9_DEVICE_OFFSET = 0x710580;
        public const nint GRAPHICS_HOOK_BREAKPOINT_OFFSET = 0x2271DA;
        public const uint RENDER_TEXT_OFFSET = 0x1E9610;


        public const nint PLAYER_1_PTR_ADDR = 0x006D1378;
        public const nint PLAYER_2_PTR_ADDR = 0x006D4C84;
        public const nint CAMERA_ADDR = 0x006D5CD4;

        public const nint PUSHBOX_STANDING_WIDTH_ARRAY   = 0x00571564;
        public const nint PUSHBOX_STANDING_HEIGHT_ARRAY  = 0x00571E6C;
        public const nint PUSHBOX_CROUCHING_WIDTH_ARRAY  = 0x00573154;
        public const nint PUSHBOX_CROUCHING_HEIGHT_ARRAY = 0x00573B38;
        public const nint PUSHBOX_AIR_WIDTH_ARRAY        = 0x00573B6C;
        public const nint PUSHBOX_AIR_HEIGHT_ARRAY       = 0x00573BA0;

        // Y offset values for Airborne pushboxes (Almost always equal to abs(YPos)+4000 except for Kliff)
        public const nint PUSHBOX_P1_JUMP_OFFSET_ADDRESS = 0x006D6378;
        public const nint PUSHBOX_P2_JUMP_OFFSET_ADDRESS = 0x006D637C;

        public const nint PLUSR_GROUND_THROW_RANGE_ARRAY         = 0x0057005C;
        public const nint AC_GROUND_THROW_RANGE_ARRAY            = 0x0056FF6C;
        public const nint PLUSR_AIR_THROW_HORIZONTAL_RANGE_ARRAY = 0x005708DC;
        public const nint AC_AIR_THROW_HORIZONTAL_RANGE_ARRAY    = 0x00570174;
        public const nint AIR_THROW_LOWER_RANGE_ARRAY            = 0x005709B4;
        public const nint AIR_THROW_UPPER_RANGE_ARRAY            = 0x00570A8C;

        #region global data (structless afaik)
        // one byte [P1Throwable, P2Throwable, P1ThrowActive P2ThrowActive]
        public const nint GLOBAL_THROW_FLAGS_ADDR = 0x006D5D7C;
        public const nint COMMAND_GRAB_ID_ADDR = 0x006D6384;
        public const nint COMMAND_GRAB_RANGE_LOOKUP_TABLE = 0x00572110;
        // 1 = normal, 0 = do not simulate, -1 = rewinding (stays at 0 for frame stepping)
        public const nint GLOBAL_REPLAY_SIMULATE_ADDR = 0x007D5788;
        // 0 = not paused, 1 or 2 = paused (not sure the difference between 1 and 2)
        public const nint GLOBAL_PAUSE_VAR_ADDR = 0x007109E4;
        #endregion
    }
}
