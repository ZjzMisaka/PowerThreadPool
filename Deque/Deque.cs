using System.Threading;
using DequeUtility;

namespace System.Collections.Concurrent
{
    internal sealed class ChaseLevDeque<T>
    {
        private T[] _array;

        private long _top;
        private long _bottom;

        public ChaseLevDeque(int capacity = 32)
        {
            if (capacity < 2) capacity = 2;
            int pow2 = Utility.ClosestPowerOfTwoGreaterThan(capacity);
            _array = new T[pow2];
            _top = 0;
            _bottom = 0;
        }

        public int ApproximateCount
        {
            get
            {
                long b = VolatileRead(ref _bottom);
                long t = VolatileRead(ref _top);
                long n = b - t;
                if (n <= 0) return 0;
                if (n > int.MaxValue) return int.MaxValue;
                return (int)n;
            }
        }

        public int Capacity => _array.Length;

        public bool IsEmpty => VolatileRead(ref _bottom) <= VolatileRead(ref _top);

        private long VolatileRead(ref long value)
        {
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            return Volatile.Read(ref value);
#else
            return Thread.VolatileRead(ref value);
#endif
        }

        private void VolatileWrite(ref long value, long newValue)
        {
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            Volatile.Write(ref value, newValue);
#else
            Thread.VolatileWrite(ref value, newValue);
#endif
        }

        public void PushBottom(T item)
        {
            var a = _array;
            long b = _bottom;
            long t = VolatileRead(ref _top);
            long size = b - t;
            if (size >= a.Length - 1)
            {
                Grow(a, t, b);
                a = _array;
            }

            a[(int)(b & (a.Length - 1))] = item;
            VolatileWrite(ref _bottom, b + 1);
        }

        public bool TryPopBottom(out T item)
        {
            long b = _bottom - 1;
            VolatileWrite(ref _bottom, b);

            long t = VolatileRead(ref _top);
            var a = _array;

            if (t <= b)
            {
                int idx = (int)(b & (a.Length - 1));
                item = a[idx];

                bool success = true;
                if (t == b)
                {
                    if (Interlocked.CompareExchange(ref _top, t + 1, t) != t)
                    {
                        success = false;
                        item = default;
                    }
                    VolatileWrite(ref _bottom, t + 1);
                }

                if (success)
                {
                    a[idx] = default;
                    return true;
                }

                item = default;
                return false;
            }
            else
            {
                VolatileWrite(ref _bottom, t);
                item = default;
                return false;
            }
        }

        public bool TrySteal(out T item)
        {
            while (true)
            {
                long t = VolatileRead(ref _top);
                long b = VolatileRead(ref _bottom);

                if (t >= b)
                {
                    item = default;
                    return false;
                }

                var a = _array;
                int idx = (int)(t & (a.Length - 1));
                T result = a[idx];

                if (Interlocked.CompareExchange(ref _top, t + 1, t) == t)
                {
                    a[idx] = default;
                    item = result;
                    return true;
                }

                Thread.SpinWait(1);
            }
        }

        private void Grow(T[] oldArray, long t, long b)
        {
            int oldLen = oldArray.Length;
            int newLen = oldLen << 1;
            T[] newArray = new T[newLen];
            int oldMask = oldLen - 1;
            int newMask = newLen - 1;

            for (long i = t; i < b; i++)
            {
                newArray[(int)(i & newMask)] = oldArray[(int)(i & oldMask)];
            }

            _array = newArray;
        }
    }
}