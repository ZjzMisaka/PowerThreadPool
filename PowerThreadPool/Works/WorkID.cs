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

    public readonly struct WorkID :
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

        public WorkIdKind Kind => _kind;

        public bool IsLong => _kind == WorkIdKind.Long;
        public bool IsGuid => _kind == WorkIdKind.Guid;
        public bool IsString => _kind == WorkIdKind.String;
        public bool IsEmpty => _kind == WorkIdKind.None;

        public static WorkID Empty => default;

        private WorkID(long value)
        {
            _kind = WorkIdKind.Long;
            _long = value;
            _guid = default;
            _string = null;
        }

        private WorkID(Guid value)
        {
            _kind = WorkIdKind.Guid;
            _long = default;
            _guid = value;
            _string = null;
        }

        private WorkID(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            _kind = WorkIdKind.String;
            _long = default;
            _guid = default;
            _string = value;
        }

        public static WorkID FromLong(long value) => new WorkID(value);
        public static WorkID FromGuid(Guid value) => new WorkID(value);
        public static WorkID FromString(string value) => new WorkID(value);

        public static implicit operator WorkID(long value) => new WorkID(value);
        public static implicit operator WorkID(Guid value) => new WorkID(value);

        public static explicit operator long(WorkID id)
        {
            if (id._kind != WorkIdKind.Long)
                throw new InvalidCastException("WorkID is not of type long.");
            return id._long;
        }

        public static explicit operator Guid(WorkID id)
        {
            if (id._kind != WorkIdKind.Guid)
                throw new InvalidCastException("WorkID is not of type Guid.");
            return id._guid;
        }

        public bool TryGetLong(out long value)
        {
            if (_kind == WorkIdKind.Long) { value = _long; return true; }
            value = default; return false;
        }

        public bool TryGetGuid(out Guid value)
        {
            if (_kind == WorkIdKind.Guid) { value = _guid; return true; }
            value = default; return false;
        }

        public bool TryGetString(out string value)
        {
            if (_kind == WorkIdKind.String) { value = _string; return true; }
            value = default; return false;
        }

        public override string ToString()
        {
            switch (_kind)
            {
                case WorkIdKind.Long: return _long.ToString();
                case WorkIdKind.Guid: return _guid.ToString("D");
                case WorkIdKind.String: return _string;
                default: return string.Empty;
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
                    return _guid.TryFormat(destination, out charsWritten, format);

                case WorkIdKind.String:
                    ReadOnlySpan<char> s = _string.AsSpan();
                    if (s.Length <= destination.Length)
                    {
                        s.CopyTo(destination);
                        charsWritten = s.Length;
                        return true;
                    }
                    charsWritten = 0;
                    return false;

                default:
                    charsWritten = 0;
                    return true;
            }
        }
#endif

        public bool Equals(WorkID other)
        {
            if (_kind != other._kind) return false;
            switch (_kind)
            {
                case WorkIdKind.Long: return _long == other._long;
                case WorkIdKind.Guid: return _guid.Equals(other._guid);
                case WorkIdKind.String: return string.Equals(_string, other._string, StringComparison.Ordinal);
                default: return true;
            }
        }

        public override bool Equals(object obj) => obj is WorkID other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (int)_kind;
                switch (_kind)
                {
                    case WorkIdKind.Long:
                        h = h * 31 + _long.GetHashCode();
                        break;
                    case WorkIdKind.Guid:
                        h = h * 31 + _guid.GetHashCode();
                        break;
                    case WorkIdKind.String:
                        h = h * 31 + (_string == null ? 0 : StringComparer.Ordinal.GetHashCode(_string));
                        break;
                }
                return h;
            }
        }

        public static bool operator ==(WorkID left, WorkID right) => left.Equals(right);
        public static bool operator !=(WorkID left, WorkID right) => !left.Equals(right);
    }
}
