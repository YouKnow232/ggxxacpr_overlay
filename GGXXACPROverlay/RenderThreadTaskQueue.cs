using System.Collections.Concurrent;

namespace GGXXACPROverlay
{
    /// <summary>
    /// Queues task to be executed in the render thread. This is needed to efficiently and safely interact with
    /// the D3D9 device on a one-time basis by offloading tasks to the owning thread (main game thread).
    /// </summary>
    internal static class RenderThreadTaskQueue
    {
        private static readonly ConcurrentQueue<Action<nint>> _taskQueue = new();

        /// <summary>
        /// Queues the given task to be exectued in the render thread at the next opportunity.
        /// </summary>
        internal static void Enqueue(Action<nint> task)
        {
            _taskQueue.Enqueue(task);
        }

        /// <summary>
        /// Executes pending tasks. This should only be called in the present hook.
        /// </summary>
        internal static void ExecutePending(nint device)
        {
            while (_taskQueue.TryDequeue(out var task))
            {
                task(device);
            }
        }
    }
}
