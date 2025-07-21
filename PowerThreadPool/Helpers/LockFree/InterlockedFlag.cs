using System;
using System.Diagnostics;
#if NET5_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using System.Threading;

namespace PowerThreadPool.Helpers.LockFree
{
    /// <summary>
    /// Provide support for lock-free algorithms.
    /// Use enumeration as the status flag of the lock-free algorithm and implement thread-safe state switching through atomic operations.
    /// </summary>
    /// <typeparam name="T">Enumeration used to represent status</typeparam>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class InterlockedFlag<T> where T : Enum
    {
        private int _innerValue;

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

#if NET5_0_OR_GREATER
        private void Set(T value)
            => Interlocked.Exchange(ref _innerValue, Unsafe.As<T, int>(ref value));
#else
        private void Set(T value)
            => Interlocked.Exchange(ref _innerValue, (int)(object)value);
# endif

        public T Get()
            => InnerValueToT(_innerValue);

        public bool TrySet(T value, T comparand)
            => TrySet(value, comparand, out _);

        public bool TrySet(T value, T comparand, out T origValue)
        {
#if NET5_0_OR_GREATER
            int valueAsInt = Unsafe.As<T, int>(ref value);
            int comparandAsInt = Unsafe.As<T, int>(ref comparand);
#else
            int valueAsInt = (int)(object)value;
            int comparandAsInt = (int)(object)comparand;
#endif

            int origInnerValue = Interlocked.CompareExchange(ref _innerValue, valueAsInt, comparandAsInt);

            origValue = InnerValueToT(origInnerValue);

            return origInnerValue == comparandAsInt;
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

            return flag1._innerValue == flag2._innerValue;
        }

        public static bool operator !=(InterlockedFlag<T> flag1, InterlockedFlag<T> flag2)
            => !(flag1 == flag2);

        public static bool operator ==(InterlockedFlag<T> flag1, T flag2)
        {
            if (ReferenceEquals(flag1, null))
            {
                return ReferenceEquals(flag2, null);
            }

#if NET5_0_OR_GREATER
            return flag1._innerValue == Unsafe.As<T, int>(ref flag2);
#else
            return flag1._innerValue == (int)(object)flag2;
#endif
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

        private static T InnerValueToT(int innerValue)
#if NET5_0_OR_GREATER
            => Unsafe.As<int, T>(ref innerValue);
#else
            => (T)(object)innerValue;
#endif
    }
}
