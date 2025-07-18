namespace GGXXACPROverlay.Hooks
{
    public interface IHook
    {
        public bool IsInstalled { get; }

        public void Install();
        public void Uninstall();
    }
}
