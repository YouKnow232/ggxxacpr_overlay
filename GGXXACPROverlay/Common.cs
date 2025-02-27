namespace GGXXACPROverlay
{
    public struct OverlaySettings()
    {
        // Implemented
        public bool ShouldRender = false;
        public bool DisplayHitBoxes = true;
        public bool DisplayFrameMeter = true;
        public bool AlwaysDrawThrowRange = false;
        // TODO
        public bool DisplayFrameMeterLegend = false;
        public bool RecordDuringHitstop = false;
        public bool RecordDuringSuperFlash = false;
        public bool AdvancedMode = false;
    }

    internal static class Constants
    {
        public const string CONSOLE_BETA_WARNING =
            "This is a beta build. It has known issues and may even have some unknown ones.\n" +
            "You can help report issues here https://github.com/YouKnow232/ggxxacpr_overlay/issues\n";
        public const string CONSOLE_NETPLAY_NOTICE =
            "Please close the overlay during netplay.\n";
        public const string CONSOLE_KNOWN_ISSUES =
            "Known Issues:\n" +
            "- PLACE HOLDER\n"+
            "- Update this in release branch\n";
        public const string CONSOLE_CONTROLS =
            "In this console window:\n" +
            "Press '1' to toggle hitbox display\n" +
            "Press '2' to toggle always-on throw range display\n" +
            " *Air throw boxes only check for the pushbox's bottom edge highlighted in yellow\n\n" +
            "Press '3' to toggle frame meter display\n" +
            "Press '4' to display frame meter legend\n" +
            "Press '5' to toggle frame meter hitstop pausing\n" +
            "Press '6' to toggle frame meter super flash pausing\n" +
            "\nPress 'q' to exit\n";
        public const string CONSOLE_EXIT_PROMPT =
            "Press any key to exit\n";
    }
}
