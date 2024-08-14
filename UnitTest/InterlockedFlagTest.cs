using PowerThreadPool.Constants;
using PowerThreadPool.Helpers;

namespace UnitTest
{
    public class InterlockedFlagTest
    {
        InterlockedFlag<WorkerGettedFlags> _gettedLock0 = WorkerGettedFlags.Free;
        InterlockedFlag<WorkerGettedFlags> _gettedLock1 = WorkerGettedFlags.Free;

        private void InitFlags()
        {
            _gettedLock0 = WorkerGettedFlags.Free;
            _gettedLock1 = WorkerGettedFlags.Free;
        }

        [Fact]
        public void TestGetSet()
        {
            InitFlags();

            _gettedLock0.InterlockedValue = WorkerGettedFlags.Disabled;
            Assert.Equal(WorkerGettedFlags.Disabled, _gettedLock0.InterlockedValue);
        }

        [Fact]
        public void TestValue()
        {
            InitFlags();

            _gettedLock0.InterlockedValue = WorkerGettedFlags.Disabled;
            Assert.Equal(WorkerGettedFlags.Disabled, _gettedLock0.Value);
        }

        [Fact]
        public void TestDebuggerDisplay()
        {
            InitFlags();

            string dd = _gettedLock0.DebuggerDisplay;
            Assert.Equal("WorkerGettedFlags.Free", dd);
        }

        [Fact]
        public void TestGet()
        {
            InitFlags();

            _gettedLock0.InterlockedValue = WorkerGettedFlags.Disabled;
            Assert.Equal(WorkerGettedFlags.Disabled, _gettedLock0.Get());
        }

        [Fact]
        public void TestTrySet()
        {
            InitFlags();

            bool res;
            res = _gettedLock0.TrySet(WorkerGettedFlags.Disabled, WorkerGettedFlags.Free);
            Assert.Equal(WorkerGettedFlags.Disabled, _gettedLock0.Get());
            Assert.True(res);

            res = _gettedLock0.TrySet(WorkerGettedFlags.Disabled, WorkerGettedFlags.Free);
            Assert.Equal(WorkerGettedFlags.Disabled, _gettedLock0.Get());
            Assert.False(res);
        }

        [Fact]
        public void TestTrySetWithOrigValueParam()
        {
            InitFlags();

            WorkerGettedFlags orig;
            bool res;
            res = _gettedLock0.TrySet(WorkerGettedFlags.Disabled, WorkerGettedFlags.Free, out orig);
            Assert.Equal(WorkerGettedFlags.Disabled, _gettedLock0.Get());
            Assert.Equal(WorkerGettedFlags.Free, orig);
            Assert.True(res);

            res = _gettedLock0.TrySet(WorkerGettedFlags.Disabled, WorkerGettedFlags.Free, out orig);
            Assert.Equal(WorkerGettedFlags.Disabled, orig);
            Assert.False(res);
        }

        [Fact]
        public void TestOperator1()
        {
            InitFlags();

            bool res;
            res = _gettedLock0 == _gettedLock1;
            Assert.True(res);
            res = _gettedLock0 != _gettedLock1;
            Assert.False(res);
            _gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            res = _gettedLock0 == _gettedLock1;
            Assert.False(res);
            res = _gettedLock0 == _gettedLock1;
            Assert.False(res);
            _gettedLock0 = null;
            res = _gettedLock0 == _gettedLock1;
            Assert.False(res);
            _gettedLock1 = null;
            res = _gettedLock0 == _gettedLock1;
            Assert.True(res);
            _gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            res = _gettedLock0 == _gettedLock1;
            Assert.False(res);
        }

        [Fact]
        public void TestOperator2()
        {
            InitFlags();

            bool res;
            res = _gettedLock0 == WorkerGettedFlags.Free;
            Assert.True(res);
            res = _gettedLock0 != WorkerGettedFlags.Free;
            Assert.False(res);
            _gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            res = _gettedLock0 == WorkerGettedFlags.Free;
            Assert.False(res);
            res = _gettedLock0 == WorkerGettedFlags.Free;
            Assert.False(res);
            _gettedLock0 = null;
            res = _gettedLock0 == WorkerGettedFlags.Free;
            Assert.False(res);
        }

        [Fact]
        public void Testimplicit()
        {
            InitFlags();

            _gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            WorkerGettedFlags f = _gettedLock0;
            Assert.Equal(WorkerGettedFlags.ToBeDisabled, _gettedLock0.Value);
            Assert.Equal(WorkerGettedFlags.ToBeDisabled, f);
        }

        [Fact]
        public void TestEquals1()
        {
            InitFlags();

            bool res;
            res = _gettedLock0.Equals(_gettedLock1);
            Assert.True(res);
            _gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            res = _gettedLock0.Equals(_gettedLock1);
            Assert.False(res);
            res = _gettedLock0.Equals(_gettedLock1);
            Assert.False(res);
        }

        [Fact]
        public void TestEquals2()
        {
            InitFlags();

            bool res;
            res = _gettedLock0.Equals(WorkerGettedFlags.Free);
            Assert.True(res);
            _gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            res = _gettedLock0.Equals(WorkerGettedFlags.Free);
            Assert.False(res);
            res = _gettedLock0.Equals(WorkerGettedFlags.Free);
            Assert.False(res);
        }

        [Fact]
        public void TestEquals3()
        {
            InitFlags();

            bool res;
            res = _gettedLock0.Equals(null);
            Assert.False(res);
            res = _gettedLock0.Equals(new PowerThreadPool.PowerPool());
            Assert.False(res);
        }

        [Fact]
        public void TestGetHashCode()
        {
            InitFlags();

            int hash0 = _gettedLock0.GetHashCode();
            int hash1 = _gettedLock0.GetHashCode();
            int hash2 = _gettedLock1.GetHashCode();
            _gettedLock1 = WorkerGettedFlags.ToBeDisabled;
            int hash3 = _gettedLock1.GetHashCode();
            Assert.Equal(hash0, hash1);
            Assert.Equal(hash1, hash2);
            Assert.NotEqual(hash2, hash3);
        }
    }
}
