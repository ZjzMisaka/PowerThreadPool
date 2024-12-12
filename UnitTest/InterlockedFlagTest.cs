using System.Reflection;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers;
using Xunit.Abstractions;

namespace UnitTest
{
    public class InterlockedFlagTest
    {
        private readonly ITestOutputHelper _output;

        public InterlockedFlagTest(ITestOutputHelper output)
        {
            _output = output;
        }

        InterlockedFlag<CanGetWork> _canGetWork0 = CanGetWork.Allowed;
        InterlockedFlag<CanGetWork> _canGetWork1 = CanGetWork.Allowed;

        private void InitFlags()
        {
            _canGetWork0 = CanGetWork.Allowed;
            _canGetWork1 = CanGetWork.Allowed;
        }

        [Fact]
        public void TestGetSet()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            _canGetWork0.InterlockedValue = CanGetWork.Disabled;
            Assert.Equal(CanGetWork.Disabled, _canGetWork0.InterlockedValue);
        }

        [Fact]
        public void TestValue()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            _canGetWork0.InterlockedValue = CanGetWork.Disabled;
            Assert.Equal(CanGetWork.Disabled, _canGetWork0.Value);
        }

        [Fact]
        public void TestDebuggerDisplay()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            string dd = _canGetWork0.DebuggerDisplay;
            Assert.Equal("CanGetWork.Allowed", dd);
        }

        [Fact]
        public void TestGet()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            _canGetWork0.InterlockedValue = CanGetWork.Disabled;
            Assert.Equal(CanGetWork.Disabled, _canGetWork0.Get());
        }

        [Fact]
        public void TestTrySet()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            bool res;
            res = _canGetWork0.TrySet(CanGetWork.Disabled, CanGetWork.Allowed);
            Assert.Equal(CanGetWork.Disabled, _canGetWork0.Get());
            Assert.True(res);

            res = _canGetWork0.TrySet(CanGetWork.Disabled, CanGetWork.Allowed);
            Assert.Equal(CanGetWork.Disabled, _canGetWork0.Get());
            Assert.False(res);
        }

        [Fact]
        public void TestTrySetWithOrigValueParam()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            CanGetWork orig;
            bool res;
            res = _canGetWork0.TrySet(CanGetWork.Disabled, CanGetWork.Allowed, out orig);
            Assert.Equal(CanGetWork.Disabled, _canGetWork0.Get());
            Assert.Equal(CanGetWork.Allowed, orig);
            Assert.True(res);

            res = _canGetWork0.TrySet(CanGetWork.Disabled, CanGetWork.Allowed, out orig);
            Assert.Equal(CanGetWork.Disabled, orig);
            Assert.False(res);
        }

        [Fact]
        public void TestOperator1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            bool res;
            res = _canGetWork0 == _canGetWork1;
            Assert.True(res);
            res = _canGetWork0 != _canGetWork1;
            Assert.False(res);
            _canGetWork0 = CanGetWork.ToBeDisabled;
            res = _canGetWork0 == _canGetWork1;
            Assert.False(res);
            res = _canGetWork0 == _canGetWork1;
            Assert.False(res);
            _canGetWork0 = null;
            res = _canGetWork0 == _canGetWork1;
            Assert.False(res);
            _canGetWork1 = null;
            res = _canGetWork0 == _canGetWork1;
            Assert.True(res);
            _canGetWork0 = CanGetWork.ToBeDisabled;
            res = _canGetWork0 == _canGetWork1;
            Assert.False(res);
        }

        [Fact]
        public void TestOperator2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            bool res;
            res = _canGetWork0 == CanGetWork.Allowed;
            Assert.True(res);
            res = _canGetWork0 != CanGetWork.Allowed;
            Assert.False(res);
            _canGetWork0 = CanGetWork.ToBeDisabled;
            res = _canGetWork0 == CanGetWork.Allowed;
            Assert.False(res);
            res = _canGetWork0 == CanGetWork.Allowed;
            Assert.False(res);
            _canGetWork0 = null;
            res = _canGetWork0 == CanGetWork.Allowed;
            Assert.False(res);
        }

        [Fact]
        public void Testimplicit()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            _canGetWork0 = CanGetWork.ToBeDisabled;
            CanGetWork f = _canGetWork0;
            Assert.Equal(CanGetWork.ToBeDisabled, _canGetWork0.Value);
            Assert.Equal(CanGetWork.ToBeDisabled, f);
        }

        [Fact]
        public void TestEquals1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            bool res;
            res = _canGetWork0.Equals(_canGetWork1);
            Assert.True(res);
            _canGetWork0 = CanGetWork.ToBeDisabled;
            res = _canGetWork0.Equals(_canGetWork1);
            Assert.False(res);
            res = _canGetWork0.Equals(_canGetWork1);
            Assert.False(res);
        }

        [Fact]
        public void TestEquals2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            bool res;
            res = _canGetWork0.Equals(CanGetWork.Allowed);
            Assert.True(res);
            _canGetWork0 = CanGetWork.ToBeDisabled;
            res = _canGetWork0.Equals(CanGetWork.Allowed);
            Assert.False(res);
            res = _canGetWork0.Equals(CanGetWork.Allowed);
            Assert.False(res);
        }

        [Fact]
        public void TestEquals3()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            bool res;
            res = _canGetWork0.Equals(null);
            Assert.False(res);
            res = _canGetWork0.Equals(new PowerThreadPool.PowerPool());
            Assert.False(res);
        }

        [Fact]
        public void TestGetHashCode()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            InitFlags();

            int hash0 = _canGetWork0.GetHashCode();
            int hash1 = _canGetWork0.GetHashCode();
            int hash2 = _canGetWork1.GetHashCode();
            _canGetWork1 = CanGetWork.ToBeDisabled;
            int hash3 = _canGetWork1.GetHashCode();
            Assert.Equal(hash0, hash1);
            Assert.Equal(hash1, hash2);
            Assert.NotEqual(hash2, hash3);
        }
    }
}
