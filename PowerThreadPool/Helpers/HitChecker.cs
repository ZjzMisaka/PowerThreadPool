namespace PowerThreadPool.Helpers
{
    public sealed class HitChecker
    {
        private int _bits = 0;
        private int _idx = 0;
        private int _filled = 0;
        private int _zeroCount = 0;

        private int _checkCount = 10;
        internal HitChecker(int checkCount)
        {
            _checkCount = checkCount;
        }

        public void Hit()
        {
            Add(1);
        }

        public void Missed()
        {
            Add(0);
        }

        private void Add(int value)
        {
            int v = value == 0 ? 0 : 1;

            if (_filled < _checkCount)
            {
                if (v == 1)
                {
                    _bits |= (1 << _idx);
                }
                if (v == 0)
                {
                    ++_zeroCount;
                }
                _idx = (_idx + 1) % _checkCount;
                _filled++;
            }
            else
            {
                int mask = 1 << _idx;
                int old = (_bits & mask) != 0 ? 1 : 0;
                if (old == 0)
                {
                    --_zeroCount;
                }

                if (v == 1) _bits |= mask;
                else _bits &= ~mask;

                if (v == 0)
                {
                    ++_zeroCount;
                }

                _idx = (_idx + 1) % _checkCount;
            }
        }

        public int MissCount => _zeroCount;
        public int Count => _filled;
    }
}
