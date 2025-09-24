using System;

namespace PowerThreadPool.Works
{
    public sealed class WorkIDGuid :
        WorkID
    {
        private readonly Guid _guid;
        private readonly int _hash;

        internal WorkIDGuid(Guid g)
        {
            _guid = g;
            _hash = ComputeHash(WorkIdKind.Guid, 0L, g, null);
        }

        internal override WorkIdKind Kind => WorkIdKind.Guid;

        internal override long Long => default;

        internal override Guid Guid => _guid;

        internal override string String => default;

        internal override int Hash => _hash;
    }
}
