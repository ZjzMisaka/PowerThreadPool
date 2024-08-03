using PowerThreadPool.Helpers;
using System.Reflection;

namespace UnitTest
{
    public class VersionBasedExecutorTest
    {
        [Fact]
        public void TestVersionBasedExecutorMaxValue()
        {
            bool actionCalled = false;
            Action action = () => actionCalled = true;
            var executor = new VersionBasedExecutor(action);

            FieldInfo updatedVersionField = typeof(VersionBasedExecutor)
                .GetField("_updatedVersion", BindingFlags.NonPublic | BindingFlags.Instance);
            updatedVersionField.SetValue(executor, long.MaxValue);

            executor.UpdateVersion();

            long updatedVersion = (long)updatedVersionField.GetValue(executor);
            Assert.Equal(long.MinValue + 1, updatedVersion);

            Assert.False(actionCalled);
        }
    }
}
