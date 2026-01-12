using System.Collections;
using System.Reflection;
using PowerThreadPool.Collections;
using Xunit.Abstractions;

namespace UnitTest
{
    public class ConcurrentSetTest
    {
        private readonly ITestOutputHelper _output;

        public ConcurrentSetTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestDefaultConstructor()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentSet<int> set = new ConcurrentSet<int>();
            Assert.Empty(set);
        }

        [Fact]
        public void TestAddAndCountMethods()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentSet<int> set = new ConcurrentSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);

            Assert.Equal(3, set.Count);
        }

        [Fact]
        public void TestTryRemoveMethod()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentSet<int> set = new ConcurrentSet<int>();
            set.Add(1);
            set.Add(2);
            set.Remove(1);

            Assert.Single(set);
            Assert.DoesNotContain(1, set);
        }

        [Fact]
        public void TestConstructorWithItems()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            IEnumerable<int> items = Enumerable.Range(1, 3);
            ConcurrentSet<int> set = new ConcurrentSet<int>(items);

            Assert.Equal(3, set.Count);
            Assert.All(items, item => Assert.Contains(item, set));
        }

        [Fact]
        public void TestGetEnumerator()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            IEnumerable<int> items = Enumerable.Range(1, 3);
            ConcurrentSet<int> set = new ConcurrentSet<int>(items);

            List<int> enumeratedItems = new List<int>();
            foreach (int item in set)
            {
                enumeratedItems.Add(item);
            }

            Assert.Equal(items, enumeratedItems);
        }

        [Fact]
        public void TestNonGenericGetEnumerator()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            IEnumerable<int> items = Enumerable.Range(1, 3);
            ConcurrentSet<int> set = new ConcurrentSet<int>(items);

            IEnumerable nonGenericSet = set;
            List<int> enumeratedItems = new List<int>();
            foreach (int item in nonGenericSet)
            {
                enumeratedItems.Add((int)item);
            }

            Assert.Equal(items, enumeratedItems);
        }

        [Fact]
        public void TestICollectionAdd()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            IEnumerable<int> items = Enumerable.Range(1, 2);
            ICollection<int> set = new ConcurrentSet<int>(items);

            set.Add(3);

            Assert.Equal(3, set.Count);
        }

        [Fact]
        public void TestICollectionIsReadOnly()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ICollection<int> set = new ConcurrentSet<int>();

            Assert.False(set.IsReadOnly);
        }

        [Fact]
        public void TestICollectionCopyTo()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            IEnumerable<int> items = Enumerable.Range(1, 2);
            ICollection<int> set = new ConcurrentSet<int>(items);

            int[] array = new int[5];
            set.CopyTo(array, 2);

            Assert.Equal(0, array[0]);
            Assert.Equal(0, array[1]);
            Assert.Equal(1, array[2]);
            Assert.Equal(2, array[3]);
            Assert.Equal(0, array[4]);
        }
    }
}
