using System.Diagnostics;

namespace PowerThreadPool.Helpers
{
    internal class StatusPingPongChecker
    {
        private int _pingPongThresholdDivisor = 20000;
        private Stopwatch _timeSinceLastIdle = new Stopwatch();
        private Stopwatch _spinWatch = new Stopwatch();
        private HitChecker _hitChecker;
        private long _statusPingPongThresholdTicks;

        internal bool HasPingedPong { get; set; }

        internal bool CanSpin => _spinWatch.ElapsedTicks < _statusPingPongThresholdTicks;

        internal StatusPingPongChecker(int hitCount)
        {
            _hitChecker = new HitChecker(hitCount);

            _timeSinceLastIdle.Start();
            _statusPingPongThresholdTicks = Stopwatch.Frequency / _pingPongThresholdDivisor;
        }

        internal void CheckIsPingedPong()
        {
            HasPingedPong = _timeSinceLastIdle.ElapsedTicks < _statusPingPongThresholdTicks;
        }

        internal void StartNewCheck()
        {
            _timeSinceLastIdle.Restart();
        }

        internal void StartSpin()
        {
            _spinWatch.Restart();
        }

        internal void HandleSpinRes(bool result)
        {
            if (result)
            {
                _hitChecker.Hit();
            }
            else
            {
                HasPingedPong = false;
                _hitChecker.Missed();
            }

            if (_hitChecker.Count == 10)
            {
                if (_hitChecker.MissCount > 2)
                {
                    _pingPongThresholdDivisor += 500;
                    _statusPingPongThresholdTicks = Stopwatch.Frequency / _pingPongThresholdDivisor;
                }
                else if (_hitChecker.MissCount <= 1 && _pingPongThresholdDivisor > 2000)
                {
                    _pingPongThresholdDivisor -= 500;
                    _statusPingPongThresholdTicks = Stopwatch.Frequency / _pingPongThresholdDivisor;
                }
            }
        }
    }
}
