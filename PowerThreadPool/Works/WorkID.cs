using System;

namespace PowerThreadPool.Works
{
    public enum WorkIdKind : byte
    {
        None = 0,
        Long = 1,
        Guid = 2,
        String = 3,
    }

    public abstract class WorkID :
        IEquatable<WorkID>
        , IFormattable
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        internal abstract WorkIdKind Kind { get; }
        internal abstract long Long { get; }
        internal abstract Guid Guid { get; }
        internal abstract string String { get; }

        internal abstract int Hash { get; }

        public bool IsLong { get { return Kind == WorkIdKind.Long; } }
        public bool IsGuid { get { return Kind == WorkIdKind.Guid; } }
        public bool IsString { get { return Kind == WorkIdKind.String; } }

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

        public static explicit operator long(WorkID id)
        {
            if (id == null) throw new InvalidCastException("WorkID is null.");
            if (id.Kind != WorkIdKind.Long)
                throw new InvalidCastException("WorkID is not of type long.");
            return id.Long;
        }

        public static explicit operator Guid(WorkID id)
        {
            if (id == null) throw new InvalidCastException("WorkID is null.");
            if (id.Kind != WorkIdKind.Guid)
                throw new InvalidCastException("WorkID is not of type Guid.");
            return id.Guid;
        }

        public bool TryGetLong(out long value)
        {
            if (Kind == WorkIdKind.Long)
            {
                value = Long;
                return true;
            }
            value = default(long);
            return false;
        }

        public bool TryGetGuid(out Guid value)
        {
            if (Kind == WorkIdKind.Guid)
            {
                value = Guid;
                return true;
            }
            value = default(Guid);
            return false;
        }

        public bool TryGetString(out string value)
        {
            if (Kind == WorkIdKind.String)
            {
                value = String;
                return true;
            }
            value = null;
            return false;
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case WorkIdKind.Long:
                    return Long.ToString();
                case WorkIdKind.Guid:
                    return Guid.ToString("D");
                case WorkIdKind.String:
                    return String;
                default:
                    return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            switch (Kind)
            {
                case WorkIdKind.Long:
                    return Long.ToString(format, formatProvider);
                case WorkIdKind.Guid:
                    string f = string.IsNullOrEmpty(format) ? "D" : format;
                    return Guid.ToString(f, formatProvider);
                case WorkIdKind.String:
                    return String;
                default:
                    return string.Empty;
            }
        }

#if NET6_0_OR_GREATER
        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider provider)
        {
            switch (Kind)
            {
                case WorkIdKind.Long:
                    return Long.TryFormat(destination, out charsWritten, format, provider);

                case WorkIdKind.Guid:
                    // Guid.TryFormat 不使用 provider；format 为空等价于 "D"
                    if (format.Length == 0)
                    {
                        return Guid.TryFormat(destination, out charsWritten, "D");
                    }
                    return Guid.TryFormat(destination, out charsWritten, format);

                case WorkIdKind.String:
                    {
                        ReadOnlySpan<char> s = String.AsSpan();
                        if (s.Length <= destination.Length)
                        {
                            s.CopyTo(destination);
                            charsWritten = s.Length;
                            return true;
                        }
                        charsWritten = 0;
                        return false;
                    }

                default:
                    charsWritten = 0;
                    return true;
            }
        }
#endif

        public bool Equals(WorkID other)
        {
            if (ReferenceEquals(this, other)) return true;
            if ((object)other == null) return false;

            if (Kind != other.Kind) return false;

            switch (Kind)
            {
                case WorkIdKind.Long:
                    return Long == other.Long;

                case WorkIdKind.Guid:
                    return Guid.Equals(other.Guid);

                case WorkIdKind.String:
                    return string.Equals(String, other.String, StringComparison.Ordinal);

                default:
                    return true;
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as WorkID;
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

        internal static int ComputeHash(WorkIdKind kind, long l, Guid g, string s)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (int)kind;

                switch (kind)
                {
                    case WorkIdKind.Long:
                        h = h * 31 + l.GetHashCode();
                        break;

                    case WorkIdKind.Guid:
                        h = h * 31 + g.GetHashCode();
                        break;

                    case WorkIdKind.String:
                        h = h * 31 + (s == null ? 0 : StringComparer.Ordinal.GetHashCode(s));
                        break;
                }

                return h;
            }
        }
    }
}
