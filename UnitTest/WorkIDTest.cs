using System.Globalization;
using PowerThreadPool.Works;

namespace UnitTest
{
    public class WorkIDTest
    {
        [Fact]
        public void TestEmptyDefaultState()
        {
            WorkID empty = WorkID.Empty;
            Assert.Equal(WorkIdKind.None, empty.Kind);
            Assert.False(empty.IsLong);
            Assert.False(empty.IsGuid);
            Assert.False(empty.IsString);

            Assert.False(empty.TryGetLong(out _));
            Assert.False(empty.TryGetGuid(out _));
            Assert.False(empty.TryGetString(out _));

            Assert.Equal(string.Empty, empty.ToString());
            Assert.Equal(string.Empty, empty.ToString(null, CultureInfo.InvariantCulture));

            Assert.True(empty.Equals(WorkID.Empty));
            Assert.True(empty == WorkID.Empty);
            Assert.False(empty != WorkID.Empty);

            _ = empty.GetHashCode();
        }

        [Fact]
        public void TestFromLongBasicAndImplicitAndExplicit()
        {
            long v = 1234567890123456789;
            WorkID a = WorkID.FromLong(v);
            WorkID b = v;

            Assert.Equal(WorkIdKind.Long, a.Kind);
            Assert.True(a.IsLong);
            Assert.False(a.IsGuid);
            Assert.False(a.IsString);

            Assert.True(a.TryGetLong(out long got));
            Assert.Equal(v, got);

            Assert.Equal(v.ToString(), a.ToString());
            Assert.Equal(v.ToString("N0", CultureInfo.InvariantCulture), a.ToString("N0", CultureInfo.InvariantCulture));

            Span<char> buf = stackalloc char[64];
            Assert.True(a.TryFormat(buf, out int written, default, CultureInfo.InvariantCulture));
            Assert.Equal(v.ToString(), new string(buf[..written]));

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());

            long v2 = (long)a;
            Assert.Equal(v, v2);

            WorkID g = WorkID.FromGuid(Guid.NewGuid());
            Assert.Throws<InvalidCastException>(() => { long _ = (long)g; });

            WorkID s = WorkID.FromString("x");
            Assert.Throws<InvalidCastException>(() => { long _ = (long)s; });

            WorkID none = WorkID.Empty;
            Assert.Throws<InvalidCastException>(() => { long _ = (long)none; });
        }

        [Fact]
        public void TestFromGuidBasicAndImplicitAndExplicit()
        {
            Guid id = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
            WorkID a = WorkID.FromGuid(id);
            WorkID b = id;

            Assert.Equal(WorkIdKind.Guid, a.Kind);
            Assert.True(a.IsGuid);
            Assert.False(a.IsLong);
            Assert.False(a.IsString);

            Assert.True(a.TryGetGuid(out Guid got));
            Assert.Equal(id, got);

            Assert.Equal(id.ToString("D"), a.ToString());
            Assert.Equal(id.ToString("D", CultureInfo.InvariantCulture), a.ToString(null, CultureInfo.InvariantCulture));
            Assert.Equal(id.ToString("D", CultureInfo.InvariantCulture), a.ToString(string.Empty, CultureInfo.InvariantCulture));
            Assert.Equal(id.ToString("N", CultureInfo.InvariantCulture), a.ToString("N", CultureInfo.InvariantCulture));

            Span<char> buf = stackalloc char[64];
            Assert.True(a.TryFormat(buf, out int wD, "D".AsSpan(), CultureInfo.InvariantCulture));
            Assert.Equal(id.ToString("D"), new string(buf[..wD]));

            Assert.True(a.TryFormat(buf, out int wN, "N".AsSpan(), CultureInfo.InvariantCulture));
            Assert.Equal(id.ToString("N"), new string(buf[..wN]));

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());

            Guid id2 = (Guid)a;
            Assert.Equal(id, id2);

            WorkID l = WorkID.FromLong(1);
            Assert.Throws<InvalidCastException>(() => { Guid _ = (Guid)l; });
            WorkID s = WorkID.FromString("x");
            Assert.Throws<InvalidCastException>(() => { Guid _ = (Guid)s; });
            WorkID none = WorkID.Empty;
            Assert.Throws<InvalidCastException>(() => { Guid _ = (Guid)none; });
        }

        [Fact]
        public void TestFromStringBasicAndNullThrows()
        {
            string txt = "hello";
            WorkID a = WorkID.FromString(txt);

            Assert.Equal(WorkIdKind.String, a.Kind);
            Assert.True(a.IsString);
            Assert.False(a.IsLong);
            Assert.False(a.IsGuid);

            Assert.True(a.TryGetString(out string got));
            Assert.Same(txt, got);

            Assert.Equal(txt, a.ToString());
            Assert.Equal(txt, a.ToString(null, CultureInfo.InvariantCulture));
            Assert.Equal(txt, a.ToString("ignored", null));

            Span<char> small = stackalloc char[2];
            Assert.False(a.TryFormat(small, out int writtenSmall, default, CultureInfo.InvariantCulture));
            Assert.Equal(0, writtenSmall);

            Span<char> big = stackalloc char[8];
            Assert.True(a.TryFormat(big, out int writtenBig, default, CultureInfo.InvariantCulture));
            Assert.Equal(txt.Length, writtenBig);
            Assert.Equal(txt, new string(big[..writtenBig]));

            Assert.Throws<ArgumentNullException>(() => WorkID.FromString(null));
        }

        [Fact]
        public void TestTryGetFailurePathsForOtherKinds()
        {
            WorkID l = WorkID.FromLong(7);
            Assert.False(l.TryGetGuid(out _));
            Assert.False(l.TryGetString(out _));

            WorkID g = WorkID.FromGuid(Guid.NewGuid());
            Assert.False(g.TryGetLong(out _));
            Assert.False(g.TryGetString(out _));

            WorkID s = WorkID.FromString("x");
            Assert.False(s.TryGetLong(out _));
            Assert.False(s.TryGetGuid(out _));
        }

        [Fact]
        public void TestEqualityMatrixAndOperators()
        {
            WorkID l1 = WorkID.FromLong(42);
            WorkID l2 = WorkID.FromLong(42);
            WorkID l3 = WorkID.FromLong(100);

            WorkID g1 = WorkID.FromGuid(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff"));
            WorkID g2 = WorkID.FromGuid(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff"));
            WorkID g3 = WorkID.FromGuid(Guid.Parse("00000000-0000-0000-0000-000000000000"));

            WorkID s1 = WorkID.FromString("abc");
            WorkID s2 = WorkID.FromString("abc");
            WorkID s3 = WorkID.FromString("ABC");

            WorkID n = WorkID.Empty;

            Assert.True(l1.Equals(l2));
            Assert.True(g1.Equals(g2));
            Assert.True(s1.Equals(s2));
            Assert.True(l1 == l2);
            Assert.True(g1 == g2);
            Assert.True(s1 == s2);
            Assert.False(l1 != l2);

            Assert.False(l1.Equals(l3));
            Assert.False(g1.Equals(g3));
            Assert.False(s1.Equals(s3));
            Assert.True(l1 != l3);

            Assert.False(l1.Equals(g1));
            Assert.False(l1.Equals(s1));
            Assert.False(g1.Equals(s1));
            Assert.True(l1 != g1);
            Assert.True(s1 != g1);

            Assert.True(n.Equals(WorkID.Empty));
            Assert.False(n.Equals(l1));
            Assert.True(n == WorkID.Empty);
            Assert.True(n != l1);

            object o = l2;
            Assert.True(l1.Equals((object)l2));
            Assert.False(l1.Equals((object)l3));
            Assert.False(l1.Equals(new object()));

            Assert.Equal(l1.GetHashCode(), l2.GetHashCode());
            Assert.Equal(g1.GetHashCode(), g2.GetHashCode());
            Assert.Equal(s1.GetHashCode(), s2.GetHashCode());
        }

        [Fact]
        public void TestTryFormatDefaultCaseForNone()
        {
            WorkID n = WorkID.Empty;
            Span<char> buf = stackalloc char[1];
            bool ok = n.TryFormat(buf, out int written, default, CultureInfo.InvariantCulture);
            Assert.True(ok);
            Assert.Equal(0, written);
        }
    }
}
