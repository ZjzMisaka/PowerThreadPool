using System;

namespace PowerThreadPool.Works
{
    public sealed class WorkIDLong :
        WorkID
    {
        private readonly long _long;
        private readonly int _hash;

        internal WorkIDLong(long l)
        {
            _long = l;
            _hash = ComputeHash(WorkIDKind.Long, l, default, null);
        }

        internal override WorkIDKind Kind => WorkIDKind.Long;

        internal override long Long => _long;

        internal override Guid Guid => default;

        internal override string String => default;

        internal override int Hash => _hash;
    }
}
