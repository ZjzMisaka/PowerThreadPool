using System;
using System.Diagnostics;
using System.Threading;

namespace PowerThreadPool.Helpers
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class InterlockedFlag<T> where T : Enum
    {
        private long _innerValue;

        public T InterlockedValue
        {
            get => Get();
            set => Set(value);
        }

        public T Value => InnerValueToT(_innerValue);

        private string TypeName { get; } = typeof(T).Name;

        internal string DebuggerDisplay => $"{TypeName}.{InterlockedValue}";

        private InterlockedFlag(T initialValue)
        {
            Set(initialValue);
        }

        private void Set(T value)
            => Interlocked.Exchange(ref _innerValue, Convert.ToInt64(value));

        public T Get()
            => InnerValueToT(Interlocked.Read(ref _innerValue));

        public bool TrySet(T value, T comparand)
            => TrySet(value, comparand, out _);

        public bool TrySet(T value, T comparand, out T origValue)
        {
            var origInnerValue = Interlocked.CompareExchange(ref _innerValue, Convert.ToInt64(value), Convert.ToInt64(comparand));

            origValue = InnerValueToT(origInnerValue);

            return origInnerValue == Convert.ToInt64(comparand);
        }

        public static bool operator ==(InterlockedFlag<T> flag1, InterlockedFlag<T> flag2)
        {
            if (ReferenceEquals(flag1, null))
            {
                return ReferenceEquals(flag2, null);
            }
            else if (ReferenceEquals(flag2, null))
            {
                return ReferenceEquals(flag1, null);
            }

            return Interlocked.Read(ref flag1._innerValue) == Interlocked.Read(ref flag2._innerValue);
        }

        public static bool operator !=(InterlockedFlag<T> flag1, InterlockedFlag<T> flag2)
            => !(flag1 == flag2);

        public static bool operator ==(InterlockedFlag<T> flag1, T flag2)
        {
            if (ReferenceEquals(flag1, null))
            {
                return ReferenceEquals(flag2, null);
            }

            return Interlocked.Read(ref flag1._innerValue) == Convert.ToInt64(flag2);
        }

        public static bool operator !=(InterlockedFlag<T> flag1, T flag2)
            => !(flag1 == flag2);

        public static implicit operator InterlockedFlag<T>(T value)
            => new InterlockedFlag<T>(value);

        public static implicit operator T(InterlockedFlag<T> flag)
            => flag.InterlockedValue;

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                if (obj is InterlockedFlag<T> otherFlag)
                {
                    return this == otherFlag;
                }
                else if (obj is T otherValue)
                {
                    return this == otherValue;
                }
            }

            return false;
        }

        public override int GetHashCode() => _innerValue.GetHashCode();

        private static T InnerValueToT(long innerValue)
            => (T)Enum.ToObject(typeof(T), innerValue);
    }
}
