using System;

namespace PowerThreadPool.Works
{
    public enum WorkIDKind : byte
    {
        Long = 0,
        Guid = 1,
        String = 2,
    }

    /// <summary>
    /// An abstract class representing the ID of a work.
    ///
    /// Optimization already attempted: converting WorkID to a readonly struct
    /// (a union of kind/long/Guid/string fields, about 40 bytes).
    /// This eliminates one small heap allocation per work, but the ID is embedded by value
    /// in three heap objects per work (the Work instance, the _aliveWorkDic dictionary node,
    /// and the ExecuteResult), so one 32-byte shared object plus three 8-byte references
    /// becomes three 40-byte inline copies. Net effect: about +64 bytes per work,
    /// which increased total allocations by ~10% in the sync-short-work benchmark
    /// (622.67 MB -> 684.93 MB per 1,000,000 works) with no measurable time improvement.
    /// Therefore, keeping WorkID as a class.
    /// </summary>
    public abstract class WorkID :
        IEquatable<WorkID>
        , IFormattable
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        internal abstract WorkIDKind Kind { get; }
        internal abstract long Long { get; }
        internal abstract Guid Guid { get; }
        internal abstract string String { get; }

        internal abstract int Hash { get; }

        public bool IsLong { get { return Kind == WorkIDKind.Long; } }
        public bool IsGuid { get { return Kind == WorkIDKind.Guid; } }
        public bool IsString { get { return Kind == WorkIDKind.String; } }

        public static WorkID FromLong(long value)
        {
            return new WorkIDLong(value);
        }

        public static WorkID FromGuid(Guid value)
        {
            return new WorkIDGuid(value);
        }

        public static WorkID FromString(string value)
        {
            if (value == null) throw new ArgumentNullException("value");
            return new WorkIDString(value);
        }

        public static implicit operator WorkID(long value)
        {
            return FromLong(value);
        }

        public static implicit operator WorkID(Guid value)
        {
            return FromGuid(value);
        }

        public static implicit operator WorkID(string value)
        {
            return FromString(value);
        }

        public static explicit operator long(WorkID id)
        {
            if (id == null) throw new InvalidCastException("WorkID is null.");
            if (id.Kind != WorkIDKind.Long)
                throw new InvalidCastException("WorkID is not of type long.");
            return id.Long;
        }

        public static explicit operator Guid(WorkID id)
        {
            if (id == null) throw new InvalidCastException("WorkID is null.");
            if (id.Kind != WorkIDKind.Guid)
                throw new InvalidCastException("WorkID is not of type Guid.");
            return id.Guid;
        }

        public static explicit operator string(WorkID id)
        {
            if (id == null) throw new InvalidCastException("WorkID is null.");
            if (id.Kind != WorkIDKind.String)
                throw new InvalidCastException("WorkID is not of type string.");
            return id.String;
        }

        public bool TryGetLong(out long value)
        {
            value = Long;
            return IsLong;
        }

        public bool TryGetGuid(out Guid value)
        {
            value = Guid;
            return IsGuid;
        }

        public bool TryGetString(out string value)
        {
            value = String;
            return IsString;
        }

        public override string ToString()
        {
            string res = null;
            if (Kind == WorkIDKind.Long)
            {
                res = Long.ToString();
            }
            else if (Kind == WorkIDKind.Guid)
            {
                res = Guid.ToString("D");
            }
            else if (Kind == WorkIDKind.String)
            {
                res = String;
            }
            return res;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            string res = null;
            if (Kind == WorkIDKind.Long)
            {
                res = Long.ToString(format, formatProvider);
            }
            else if (Kind == WorkIDKind.Guid)
            {
                string f = string.IsNullOrEmpty(format) ? "D" : format;
                res = Guid.ToString(f, formatProvider);
            }
            else if (Kind == WorkIDKind.String)
            {
                res = String;
            }
            return res;
        }

#if NET6_0_OR_GREATER
        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider provider)
        {
            bool res = true;
            charsWritten = default;
            if (Kind == WorkIDKind.Long)
            {
                res = Long.TryFormat(destination, out charsWritten, format, provider);
            }
            else if (Kind == WorkIDKind.Guid)
            {
                if (format.Length == 0)
                {
                    res = Guid.TryFormat(destination, out charsWritten, "D");
                }
                else
                {
                    res = Guid.TryFormat(destination, out charsWritten, format);
                }
            }
            else if (Kind == WorkIDKind.String)
            {
                ReadOnlySpan<char> s = String.AsSpan();
                if (s.Length <= destination.Length)
                {
                    s.CopyTo(destination);
                    charsWritten = s.Length;
                    res = true;
                }
                else
                {
                    charsWritten = 0;
                    res = false;
                }
            }
            return res;
        }
#endif

        public bool Equals(WorkID other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if ((object)other == null)
                return false;

            if (Kind != other.Kind)
                return false;

            bool res = false;
            if (Kind == WorkIDKind.Long)
            {
                res = Long == other.Long;
            }
            else if (Kind == WorkIDKind.Guid)
            {
                res = Guid.Equals(other.Guid);
            }
            else if (Kind == WorkIDKind.String)
            {
                res = string.Equals(String, other.String, StringComparison.Ordinal);
            }
            return res;
        }

        public override bool Equals(object obj)
        {
            WorkID other = obj as WorkID;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Hash;
        }

        public static bool operator ==(WorkID left, WorkID right)
        {
            if (ReferenceEquals(left, right)) return true;
            if ((object)left == null || (object)right == null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(WorkID left, WorkID right)
        {
            return !(left == right);
        }

        internal static int ComputeHash(WorkIDKind kind, long l, Guid g, string s)
        {
            unchecked
            {
                int h = 17;
                h = (h * 31) + (int)kind;

                switch (kind)
                {
                    case WorkIDKind.Long:
                        h = (h * 31) + l.GetHashCode();
                        break;

                    case WorkIDKind.Guid:
                        h = (h * 31) + g.GetHashCode();
                        break;

                    case WorkIDKind.String:
                        h = (h * 31) + StringComparer.Ordinal.GetHashCode(s);
                        break;
                }

                return h;
            }
        }
    }
}
