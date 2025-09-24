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

    public sealed class WorkID :
        IEquatable<WorkID>
        , IFormattable
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private readonly WorkIdKind _kind;
        private readonly long _long;
        private readonly Guid _guid;
        private readonly string _string;

        private readonly int _hash;

        public WorkIdKind Kind { get { return _kind; } }

        public bool IsLong { get { return _kind == WorkIdKind.Long; } }
        public bool IsGuid { get { return _kind == WorkIdKind.Guid; } }
        public bool IsString { get { return _kind == WorkIdKind.String; } }

        private WorkID(WorkIdKind kind, long l, Guid g, string s)
        {
            _kind = kind;
            _long = l;
            _guid = g;
            _string = s;
            _hash = ComputeHash(kind, l, g, s);
        }

        public static WorkID FromLong(long value)
        {
            return new WorkID(WorkIdKind.Long, value, default(Guid), null);
        }

        public static WorkID FromGuid(Guid value)
        {
            return new WorkID(WorkIdKind.Guid, 0L, value, null);
        }

        public static WorkID FromString(string value)
        {
            if (value == null) throw new ArgumentNullException("value");
            return new WorkID(WorkIdKind.String, 0L, default(Guid), value);
        }

        // 隐式：long/Guid -> WorkID
        public static implicit operator WorkID(long value)
        {
            return FromLong(value);
        }

        public static implicit operator WorkID(Guid value)
        {
            return FromGuid(value);
        }

        // 显式：WorkID -> long/Guid
        public static explicit operator long(WorkID id)
        {
            if (id == null) throw new InvalidCastException("WorkID is null.");
            if (id._kind != WorkIdKind.Long)
                throw new InvalidCastException("WorkID is not of type long.");
            return id._long;
        }

        public static explicit operator Guid(WorkID id)
        {
            if (id == null) throw new InvalidCastException("WorkID is null.");
            if (id._kind != WorkIdKind.Guid)
                throw new InvalidCastException("WorkID is not of type Guid.");
            return id._guid;
        }

        // TryGetXxx
        public bool TryGetLong(out long value)
        {
            if (_kind == WorkIdKind.Long)
            {
                value = _long;
                return true;
            }
            value = default(long);
            return false;
        }

        public bool TryGetGuid(out Guid value)
        {
            if (_kind == WorkIdKind.Guid)
            {
                value = _guid;
                return true;
            }
            value = default(Guid);
            return false;
        }

        public bool TryGetString(out string value)
        {
            if (_kind == WorkIdKind.String)
            {
                value = _string;
                return true;
            }
            value = null;
            return false;
        }

        public override string ToString()
        {
            switch (_kind)
            {
                case WorkIdKind.Long:
                    return _long.ToString();
                case WorkIdKind.Guid:
                    return _guid.ToString("D");
                case WorkIdKind.String:
                    return _string;
                default:
                    return string.Empty;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            switch (_kind)
            {
                case WorkIdKind.Long:
                    return _long.ToString(format, formatProvider);
                case WorkIdKind.Guid:
                    string f = string.IsNullOrEmpty(format) ? "D" : format;
                    return _guid.ToString(f, formatProvider);
                case WorkIdKind.String:
                    return _string;
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
            switch (_kind)
            {
                case WorkIdKind.Long:
                    return _long.TryFormat(destination, out charsWritten, format, provider);

                case WorkIdKind.Guid:
                    // Guid.TryFormat 不使用 provider；format 为空等价于 "D"
                    if (format.Length == 0)
                    {
                        return _guid.TryFormat(destination, out charsWritten, "D");
                    }
                    return _guid.TryFormat(destination, out charsWritten, format);

                case WorkIdKind.String:
                    {
                        ReadOnlySpan<char> s = _string.AsSpan();
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

            if (_kind != other._kind) return false;

            switch (_kind)
            {
                case WorkIdKind.Long:
                    return _long == other._long;

                case WorkIdKind.Guid:
                    return _guid.Equals(other._guid);

                case WorkIdKind.String:
                    return string.Equals(_string, other._string, StringComparison.Ordinal);

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
            return _hash;
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

        private static int ComputeHash(WorkIdKind kind, long l, Guid g, string s)
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
