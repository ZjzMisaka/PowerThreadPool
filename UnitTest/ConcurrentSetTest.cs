using PowerThreadPool.Collections;
using System.Collections;

namespace UnitTest
{
    public class ConcurrentSetTest
    {
        [Fact]
        public void TestDefaultConstructor()
        {
            ConcurrentSet<int> set = new ConcurrentSet<int>();
            Assert.Equal(0, set.Count);
        }

        [Fact]
        public void TestAddAndCountMethods()
        {
            ConcurrentSet<int> set = new ConcurrentSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);

            Assert.Equal(3, set.Count);
        }

        [Fact]
        public void TestTryRemoveMethod()
        {
            ConcurrentSet<int> set = new ConcurrentSet<int>();
            set.Add(1);
            set.Add(2);
            set.Remove(1);

            Assert.Equal(1, set.Count);
            Assert.DoesNotContain(1, set);
        }

        [Fact]
        public void TestConstructorWithItems()
        {
            IEnumerable<int> items = Enumerable.Range(1, 3);
            ConcurrentSet<int> set = new ConcurrentSet<int>(items);

            Assert.Equal(3, set.Count);
            Assert.All(items, item => Assert.Contains(item, set));
        }

        [Fact]
        public void TestGetEnumerator()
        {
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
    }
}