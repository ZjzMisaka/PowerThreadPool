using PowerThreadPool.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    public class ConcurrentSetTest
    {
        [Fact]
        public void TestDefaultConstructor()
        {
            var set = new ConcurrentSet<int>();
            Assert.Equal(0, set.Count);
        }

        [Fact]
        public void TestAddAndCountMethods()
        {
            var set = new ConcurrentSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);

            Assert.Equal(3, set.Count);
        }

        [Fact]
        public void TestTryRemoveMethod()
        {
            var set = new ConcurrentSet<int>();
            set.Add(1);
            set.Add(2);
            set.Remove(1);

            Assert.Equal(1, set.Count);
            Assert.DoesNotContain(1, set);
        }

        [Fact]
        public void TestConstructorWithItems()
        {
            var items = Enumerable.Range(1, 3);
            var set = new ConcurrentSet<int>(items);

            Assert.Equal(3, set.Count);
            Assert.All(items, item => Assert.Contains(item, set));
        }

        [Fact]
        public void TestGetEnumerator()
        {
            var items = Enumerable.Range(1, 3);
            var set = new ConcurrentSet<int>(items);

            var enumeratedItems = new List<int>();
            foreach (var item in set)
            {
                enumeratedItems.Add(item);
            }

            Assert.Equal(items, enumeratedItems);
        }
    }
}