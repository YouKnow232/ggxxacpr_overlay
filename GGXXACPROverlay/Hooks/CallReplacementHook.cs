namespace GGXXACPROverlay.Hooks
{
    internal class CallReplacementHook : DisposableHook
    {
        private bool _isInstalled = false;
        public override bool IsInstalled => _isInstalled;

        public CallReplacementHook()
        {
            //
        }

        public override void Install()
        {
            throw new NotImplementedException();
            _isInstalled = true;
        }

        public override void Uninstall()
        {
            throw new NotImplementedException();
            _isInstalled = false;
        }


        ~CallReplacementHook()
        {
            if (IsInstalled)
            {
                Dispose(false);
            }
        }
    }
}
