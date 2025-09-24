using System;

namespace PowerThreadPool.Works
{
    public sealed class WorkIDString :
        WorkID
    {
        private readonly string _string;
        private readonly int _hash;

        internal WorkIDString(string s)
        {
            _string = s;
            _hash = ComputeHash(WorkIdKind.String, 0L, default, s);
        }

        internal override WorkIdKind Kind => WorkIdKind.String;

        internal override long Long => default;

        internal override Guid Guid => default;

        internal override string String => _string;

        internal override int Hash => _hash;
    }
}
