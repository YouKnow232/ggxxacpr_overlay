using System.Collections.Concurrent;

namespace GGXXACPROverlay
{
    /// <summary>
    /// Queues tasks to be executed at a later time in a specfici context. This is needed to efficiently and safely interact with
    /// the D3D9 device on a one-time basis by offloading tasks to the owning thread (main game thread).
    /// </summary>
    internal static class TaskQueues
    {
        /// <summary>
        /// Executes tasks on the render thread before the overlay is rendered.
        /// </summary>
        public static readonly TaskQueue<nint> RenderThreadTaskQueue = new();
        /// <summary>
        /// Executes tasks in the message loop before PeekMessage is called.
        /// </summary>
        public static readonly TaskQueue PeekMessageTaskQueue = new();
    }

    /// <summary>
    /// Wrapper for ConcurrentQueue<Action<T>>
    /// </summary>
    internal class TaskQueue<T>
    {
        private readonly ConcurrentQueue<Action<T>> _taskQueue = new();

        /// <summary>
        /// Queues the given task to be exectued in the render thread at the next opportunity.
        /// </summary>
        internal void Enqueue(Action<T> task)
        {
            _taskQueue.Enqueue(task);
        }

        /// <summary>
        /// Executes pending tasks. This should only be called in the present hook.
        /// </summary>
        internal void ExecutePending(T input)
        {
            while (_taskQueue.TryDequeue(out var task))
            {
                task(input);
            }
        }
    }

    /// <summary>
    /// Wrapper for ConcurrentQueue<Action>
    /// </summary>
    internal class TaskQueue
    {
        private readonly ConcurrentQueue<Action> _taskQueue = new();

        /// <summary>
        /// Queues the given task to be exectued in the render thread at the next opportunity.
        /// </summary>
        internal void Enqueue(Action task)
        {
            _taskQueue.Enqueue(task);
        }

        /// <summary>
        /// Executes pending tasks. This should only be called in the present hook.
        /// </summary>
        internal void ExecutePending()
        {
            while (_taskQueue.TryDequeue(out var task))
            {
                task();
            }
        }
    }
}
