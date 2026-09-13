using System;
using System.Diagnostics;
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
using System.Runtime.CompilerServices;
#endif
using System.Threading;

namespace PowerThreadPool.Helpers.LockFree
{
    /// <summary>
    /// Provide support for lock-free algorithms.
    /// Use enumeration as the status flag of the lock-free algorithm and implement thread-safe state switching through atomic operations.
    ///
    /// This is a mutable struct ON PURPOSE: it must be held as a field of the owner object
    /// so that all threads share one storage location and the atomic operations below act
    /// on that location (this is what replaced a heap-allocated class: one object per flag
    /// used to be allocated per Work/Worker/PowerPool instance).
    /// NEVER copy it into a local/readonly field/property getter and then call mutating
    /// members (TrySet/Set/InterlockedValue setter) on the copy - the copy is a separate
    /// storage and the change would be silently lost.
    /// All flag enums are defined so that their 0 member is the initial state, therefore
    /// default(InterlockedFlag&lt;T&gt;) is a validly-initialized flag.
    /// </summary>
    /// <typeparam name="T">Enumeration used to represent status</typeparam>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal struct InterlockedFlag<T> where T : Enum
    {
        private int _innerValue;

        public T InterlockedValue
        {
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get => Get();
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            set => Set(value);
        }

        public T Value => InnerValueToT(_innerValue);

        internal string DebuggerDisplay => $"{typeof(T).Name}.{InterlockedValue}";

        internal InterlockedFlag(T initialValue)
        {
            _innerValue = ConvertToInt(initialValue);
        }

        // Class-style initialization used to go through the implicit conversion operator;
        // field initializers keep working through this path so existing call sites are unchanged.
        public static implicit operator InterlockedFlag<T>(T value)
        {
            return new InterlockedFlag<T>(value);
        }

        public static implicit operator T(InterlockedFlag<T> flag)
        {
            return flag.InterlockedValue;
        }

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void Set(T value)
#if NET5_0_OR_GREATER
            => Interlocked.Exchange(ref _innerValue, Unsafe.As<T, int>(ref value));
#else
            => Interlocked.Exchange(ref _innerValue, ConvertToInt(value));
#endif

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public T Get()
            => InnerValueToT(_innerValue);

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool TrySet(T value, T comparand)
            => TrySet(value, comparand, out _);

        public bool TrySet(T value, T comparand, out T origValue)
        {
#if NET5_0_OR_GREATER
            int valueAsInt = Unsafe.As<T, int>(ref value);
            int comparandAsInt = Unsafe.As<T, int>(ref comparand);
#else
            int valueAsInt = ConvertToInt(value);
            int comparandAsInt = ConvertToInt(comparand);
#endif

            int origInnerValue = Interlocked.CompareExchange(ref _innerValue, valueAsInt, comparandAsInt);

            origValue = InnerValueToT(origInnerValue);

            return origInnerValue == comparandAsInt;
        }

        public static bool operator ==(InterlockedFlag<T> flag1, InterlockedFlag<T> flag2)
            => flag1._innerValue == flag2._innerValue;

        public static bool operator !=(InterlockedFlag<T> flag1, InterlockedFlag<T> flag2)
            => !(flag1 == flag2);

        public static bool operator ==(InterlockedFlag<T> flag1, T flag2)
#if NET5_0_OR_GREATER
            => flag1._innerValue == Unsafe.As<T, int>(ref flag2);
#else
            => flag1._innerValue == ConvertToInt(flag2);
#endif

        public static bool operator !=(InterlockedFlag<T> flag1, T flag2)
            => !(flag1 == flag2);

        public override bool Equals(object obj)
        {
            if (obj is InterlockedFlag<T> otherFlag)
            {
                return this == otherFlag;
            }
            else if (obj is T otherValue)
            {
                return this == otherValue;
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

        private static int ConvertToInt(T value)
            => (int)(object)value;
    }
}
