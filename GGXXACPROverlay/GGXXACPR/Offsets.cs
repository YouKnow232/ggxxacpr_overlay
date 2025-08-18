namespace GGXXACPROverlay.GGXXACPR
{
    /// <summary>
    /// Memory location constants. These will have to be updated whenever +R is.
    /// </summary>
    internal static class Offsets
    {
        public const nint IN_GAME_FLAG = 0x7101F4;
        public const nint GAME_VER_FLAG = 0x6D0538;   // 0=AC, 1=+R

        // Injection Addresses
        public const nint MESSAGE_LOOP_END = 0x222413;
        public const nint PRESENT_CALL_INSTRUCTION = 0x2271D8;
        public const nint GRAPHICS_HOOK_BREAKPOINT = 0x2271DA;
        public const nint PEEK_MESSAGE_FUNCTION_POINTER = 0x3BD348;
        public const nint MESSAGE_LOOP_REL_JMP_OFFSET_BYTE_ADDR = 0x222414;

        // Hack Addresses
        public const nint FIX_BACKGROUND_STATE_INSTRUCTION = 0x21C363;
        public const nint BACKGROUND_STATE = 0x6D6420;  // see enum BackgroundState

        // DirectX
        public const nint DIRECT3D9_DEVICE = 0x710580;
        public const nint RENDER_TEXT = 0x1E9610;

        // Entities
        public const nint PLAYER_1_PTR = 0x6D1378;
        public const nint PLAYER_2_PTR = 0x6D4C84;
        public const nint ENTITY_ARR_HEAD_TAIL_PTR = 0x6D27A8;
        public const nint ENTITY_LIST_PTR = 0x6D137C;

        // Camera
        public const nint CAMERA = 0x6D5CD4;
        public const nint VIEW_HEIGHT = 0x6C118C;
        public const nint VIEW_WIDTH = 0x6C14E4;
        public const nint WINDOW_MODE = 0x6C1510; // 0=Window 1=Full 2=Borderless

        // Pushboxes
        public const nint PUSHBOX_STANDING_WIDTH_ARRAY = 0x571564;
        public const nint PUSHBOX_STANDING_HEIGHT_ARRAY = 0x571E6C;
        public const nint PUSHBOX_CROUCHING_WIDTH_ARRAY = 0x573154;
        public const nint PUSHBOX_CROUCHING_HEIGHT_ARRAY = 0x573B38;
        public const nint PUSHBOX_AIR_WIDTH_ARRAY = 0x573B6C;
        public const nint PUSHBOX_AIR_HEIGHT_ARRAY = 0x573BA0;
        // Y offset values for Airborne pushboxes (Almost always equal to abs(YPos)+4000 except for Kliff)
        public const nint PUSHBOX_P1_JUMP_OFFSET = 0x6D6378;
        public const nint PUSHBOX_P2_JUMP_OFFSET = 0x6D637C;
        public const nint PUSHBOX_EDGE_DISTANCE = 0x6D638C;

        // Throws
        public const nint PLUSR_GROUND_THROW_RANGE_ARRAY = 0x57005C;
        public const nint AC_GROUND_THROW_RANGE_ARRAY = 0x56FF6C;
        public const nint PLUSR_AIR_THROW_HORIZONTAL_RANGE_ARRAY = 0x5708DC;
        public const nint AC_AIR_THROW_HORIZONTAL_RANGE_ARRAY = 0x570174;
        public const nint AIR_THROW_LOWER_RANGE_ARRAY = 0x5709B4;
        public const nint AIR_THROW_UPPER_RANGE_ARRAY = 0x570A8C;
        public const nint COMMAND_GRAB_ID_P1 = 0x6D6384;
        public const nint COMMAND_GRAB_ID_P2 = 0x6D6388;
        public const nint COMMAND_GRAB_RANGE_LOOKUP_TABLE = 0x572110;
        // one byte [P1Throwable, P2Throwable, P1ThrowActive P2ThrowActive]
        public const nint GLOBAL_THROW_FLAGS = 0x6D5D7C;

        // Pause Menus
        // 0 = not paused, 1 or 2 = paused (not sure the difference between 1 and 2)
        public const nint TRAINING_MODE_PAUSE_STATE = 0x7109E4;
        public const nint TRAINING_MODE_PAUSE_DISPLAY = 0x6CBD20;

        // Replay
        // 1 = normal, 0 = do not simulate, -1 = rewinding (stays at 0 for frame stepping)
        public const nint GLOBAL_REPLAY_SIMULATE = 0x7D5788;
    }
}
