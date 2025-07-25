namespace GGXXACPROverlay.Hooks
{
    internal abstract class DisposableHook : IHook, IDisposable
    {
        public abstract bool IsInstalled { get; }

        public abstract void Install();

        public abstract void Uninstall();


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
                // dispose managed
            }
            // dispose unmanaged
            if (IsInstalled) Uninstall();

            _disposed = true;
        }
    }
}
