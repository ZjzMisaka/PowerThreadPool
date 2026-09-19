using System.Diagnostics;

namespace PowerThreadPool.Helpers
{
    internal class StatusPingPongChecker
    {
        private int _pingPongThresholdDivisor = 20000;
        private Stopwatch _timeSinceLastIdle = new Stopwatch();
        private Stopwatch _spinWatch = new Stopwatch();
        private HitChecker _hitChecker = new HitChecker(10);
        private long _statusPingPongThresholdTicks;
        private long _statusPingPongSpinTicks;

        internal bool HasPingedPong { get; set; }

        internal bool CanSpin => _spinWatch.ElapsedTicks < _statusPingPongSpinTicks;

        internal StatusPingPongChecker()
        {
            _timeSinceLastIdle.Start();
            _statusPingPongThresholdTicks = Stopwatch.Frequency / _pingPongThresholdDivisor;
            _statusPingPongSpinTicks = _statusPingPongThresholdTicks * 2;
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
                    _statusPingPongSpinTicks = _statusPingPongThresholdTicks * 2;
                }
                else if (_hitChecker.MissCount <= 1 && _pingPongThresholdDivisor > 2000)
                {
                    _pingPongThresholdDivisor -= 500;
                    _statusPingPongThresholdTicks = Stopwatch.Frequency / _pingPongThresholdDivisor;
                    _statusPingPongSpinTicks = _statusPingPongThresholdTicks * 2;
                }
            }
        }
    }
}
