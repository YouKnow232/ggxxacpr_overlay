namespace GGXXACPROverlay.FrameMeter
{
    internal class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _index = 0;

        public CircularBuffer(int size)
        {
            _buffer = new T[size];
        }

        public int Length => _buffer.Length;
        public int Index => _index;
        public int MaxIndex => Math.Max(_index, _buffer.Length - 1);
        public int MinIndex => Math.Min(_index - _buffer.Length - 1, 0);

        private void UpdateFrameIndex(int offset)
        {
            _index += offset;
        }

        public void RollBack(int index)
        {
            _index -= index;
        }
        public void Add(T element)
        {
            this[_index++] = element;
        }
        public T Get(int index)
        {
            return this[index];
        }
        private T this[int key]
        {
            get => _buffer[WrappingIndex(key, _buffer.Length)];
            set => _buffer[WrappingIndex(key, _buffer.Length)] = value;
        }

        private static int WrappingIndex(int index, int size)
        {
            int r = index % size;
            return r < 0 ? r + size : r;
        }
    }
}
