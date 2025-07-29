namespace GGXXACPROverlay.FrameMeter
{
    internal class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int index = 0;

        public CircularBuffer(int size)
        {
            _buffer = new T[size];
        }

        public int Length => _buffer.Length;

        public void UpdateFrameIndex(int offset)
        {
            index += offset;
        }

        public T this[int key]
        {
            get => _buffer[WrappingIndex(key + index, _buffer.Length)];
            set => _buffer[WrappingIndex(key + index, _buffer.Length)] = value;
        }

        private static int WrappingIndex(int index, int size)
        {
            int r = index % size;
            return r < 0 ? r + size : r;
        }
    }
}
