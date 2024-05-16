using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerThreadPool.Helpers;
using PowerThreadPool.Constants;

namespace UnitTest
{
    public class InterlockedFlagTest
    {
        InterlockedFlag<WorkerGettedFlags> gettedLock0 = WorkerGettedFlags.Unlocked;
        InterlockedFlag<WorkerGettedFlags> gettedLock1 = WorkerGettedFlags.Unlocked;

        private void InitFlags()
        {
            gettedLock0 = WorkerGettedFlags.Unlocked;
            gettedLock1 = WorkerGettedFlags.Unlocked;
        }

        [Fact]
        public void TestGetSet()
        {
            InitFlags();

            gettedLock0.InterlockedValue = WorkerGettedFlags.Disabled;
            Assert.Equal(WorkerGettedFlags.Disabled, gettedLock0.InterlockedValue);
        }

        [Fact]
        public void TestValue()
        {
            InitFlags();

            gettedLock0.InterlockedValue = WorkerGettedFlags.Disabled;
            Assert.Equal(WorkerGettedFlags.Disabled, gettedLock0.Value);
        }

        [Fact]
        public void TestDebuggerDisplay()
        {
            InitFlags();

            string dd = gettedLock0.DebuggerDisplay;
            Assert.Equal("WorkerGettedFlags.Unlocked", dd);
        }

        [Fact]
        public void TestGet()
        {
            InitFlags();

            gettedLock0.InterlockedValue = WorkerGettedFlags.Disabled;
            Assert.Equal(WorkerGettedFlags.Disabled, gettedLock0.Get());
        }

        [Fact]
        public void TestTrySet()
        {
            InitFlags();

            bool res;
            res = gettedLock0.TrySet(WorkerGettedFlags.Disabled, WorkerGettedFlags.Unlocked);
            Assert.Equal(WorkerGettedFlags.Disabled, gettedLock0.Get());
            Assert.True(res);

            res = gettedLock0.TrySet(WorkerGettedFlags.Disabled, WorkerGettedFlags.Unlocked);
            Assert.Equal(WorkerGettedFlags.Disabled, gettedLock0.Get());
            Assert.False(res);
        }

        [Fact]
        public void TestTrySetWithOrigValueParam()
        {
            InitFlags();

            WorkerGettedFlags orig;
            bool res;
            res = gettedLock0.TrySet(WorkerGettedFlags.Disabled, WorkerGettedFlags.Unlocked, out orig);
            Assert.Equal(WorkerGettedFlags.Disabled, gettedLock0.Get());
            Assert.Equal(WorkerGettedFlags.Unlocked, orig);
            Assert.True(res);

            res = gettedLock0.TrySet(WorkerGettedFlags.Disabled, WorkerGettedFlags.Unlocked, out orig);
            Assert.Equal(WorkerGettedFlags.Disabled, orig);
            Assert.False(res);
        }

        [Fact]
        public void TestOperator1()
        {
            InitFlags();

            bool res;
            res = gettedLock0 == gettedLock1;
            Assert.True(res);
            res = gettedLock0 != gettedLock1;
            Assert.False(res);
            gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            res = gettedLock0 == gettedLock1;
            Assert.False(res);
            res = gettedLock0 == gettedLock1;
            Assert.False(res);
            gettedLock0 = null;
            res = gettedLock0 == gettedLock1;
            Assert.False(res);
            gettedLock1 = null;
            res = gettedLock0 == gettedLock1;
            Assert.True(res);
            gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            res = gettedLock0 == gettedLock1;
            Assert.False(res);
        }

        [Fact]
        public void TestOperator2()
        {
            InitFlags();

            bool res;
            res = gettedLock0 == WorkerGettedFlags.Unlocked;
            Assert.True(res);
            res = gettedLock0 != WorkerGettedFlags.Unlocked;
            Assert.False(res);
            gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            res = gettedLock0 == WorkerGettedFlags.Unlocked;
            Assert.False(res);
            res = gettedLock0 == WorkerGettedFlags.Unlocked;
            Assert.False(res);
            gettedLock0 = null;
            res = gettedLock0 == WorkerGettedFlags.Unlocked;
            Assert.False(res);
        }

        [Fact]
        public void Testimplicit()
        {
            InitFlags();

            gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            WorkerGettedFlags f = gettedLock0;
            Assert.Equal(WorkerGettedFlags.ToBeDisabled, gettedLock0.Value);
            Assert.Equal(WorkerGettedFlags.ToBeDisabled, f);
        }

        [Fact]
        public void TestEquals1()
        {
            InitFlags();

            bool res;
            res = gettedLock0.Equals(gettedLock1);
            Assert.True(res);
            gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            res = gettedLock0.Equals(gettedLock1);
            Assert.False(res);
            res = gettedLock0.Equals(gettedLock1);
            Assert.False(res);
        }

        [Fact]
        public void TestEquals2()
        {
            InitFlags();

            bool res;
            res = gettedLock0.Equals(WorkerGettedFlags.Unlocked);
            Assert.True(res);
            gettedLock0 = WorkerGettedFlags.ToBeDisabled;
            res = gettedLock0.Equals(WorkerGettedFlags.Unlocked);
            Assert.False(res);
            res = gettedLock0.Equals(WorkerGettedFlags.Unlocked);
            Assert.False(res);
        }

        [Fact]
        public void TestEquals3()
        {
            InitFlags();

            bool res;
            res = gettedLock0.Equals(null);
            Assert.False(res);
            res = gettedLock0.Equals(new PowerThreadPool.PowerPool());
            Assert.False(res);
        }

        [Fact]
        public void TestGetHashCode()
        {
            InitFlags();

            int hash0 = gettedLock0.GetHashCode();
            int hash1 = gettedLock0.GetHashCode();
            int hash2 = gettedLock1.GetHashCode();
            gettedLock1 = WorkerGettedFlags.ToBeDisabled;
            int hash3 = gettedLock1.GetHashCode();
            Assert.Equal(hash0, hash1);
            Assert.Equal(hash1, hash2);
            Assert.NotEqual(hash2, hash3);
        }
    }
}
