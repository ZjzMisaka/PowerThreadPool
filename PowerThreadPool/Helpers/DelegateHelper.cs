using System;

namespace PowerThreadPool.Helpers
{
    internal static class DelegateHelper<TResult>
    {
        internal static Func<object[], TResult> ToNormalFunc(Func<TResult> function)
        {
            Func<object[], TResult> func = (param) => { return function(); };
            return func;
        }

        internal static Func<object[], TResult> ToNormalFunc(Action action)
        {
            Func<TResult> func = () => { action(); return default(TResult); };
            return DelegateHelper<TResult>.ToNormalFunc(func);
        }
        

        internal static Func<object[], TResult> ToNormalFunc(Action<object[]> action, object[] param)
        {
            Func<object[], TResult> func = (p) => { action(p); return default(TResult); };
            return func;
        }
    }

    internal static class DelegateHelper<T1, TResult>
    {
        internal static Func<object[], TResult> ToNormalFunc(Action<T1> action, T1 param1)
        {
            Func<T1, TResult> func = (p) => { action(p); return default(TResult); };
            return DelegateHelper<T1, TResult>.ToNormalFunc(func, param1);
        }

        internal static Func<object[], TResult> ToNormalFunc(Func<T1, TResult> function, T1 param1)
        {
            Func<TResult> func = () => { return function(param1); };
            return DelegateHelper<TResult>.ToNormalFunc(func);
        }
    }

    internal static class DelegateHelper<T1, T2, TResult>
    {
        internal static Func<object[], TResult> ToNormalFunc(Action<T1, T2> action, T1 param1, T2 param2)
        {
            Func<T1, T2, TResult> func = (p1, p2) => { action(p1, p2); return default(TResult); };
            return DelegateHelper<T1, T2, TResult>.ToNormalFunc(func, param1, param2);
        }

        internal static Func<object[], TResult> ToNormalFunc(Func<T1, T2, TResult> function, T1 param1, T2 param2)
        {
            Func<TResult> func = () => { return function(param1, param2); };
            return DelegateHelper<TResult>.ToNormalFunc(func);
        }
    }

    internal static class DelegateHelper<T1, T2, T3, TResult>
    {
        internal static Func<object[], TResult> ToNormalFunc(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
        {
            Func<T1, T2, T3, TResult> func = (p1, p2, p3) => { action(p1, p2, p3); return default(TResult); };
            return DelegateHelper<T1, T2, T3, TResult>.ToNormalFunc(func, param1, param2, param3);
        }

        internal static Func<object[], TResult> ToNormalFunc(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3)
        {
            Func<TResult> func = () => { return function(param1, param2, param3); };
            return DelegateHelper<TResult>.ToNormalFunc(func);
        }
    }

    internal static class DelegateHelper<T1, T2, T3, T4, TResult>
    {
        internal static Func<object[], TResult> ToNormalFunc(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            Func<T1, T2, T3, T4, TResult> func = (p1, p2, p3, p4) => { action(p1, p2, p3, p4); return default(TResult); };
            return DelegateHelper<T1, T2, T3, T4, TResult>.ToNormalFunc(func, param1, param2, param3, param4);
        }

        internal static Func<object[], TResult> ToNormalFunc(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            Func<TResult> func = () => { return function(param1, param2, param3, param4); };
            return DelegateHelper<TResult>.ToNormalFunc(func);
        }
    }

    internal static class DelegateHelper<T1, T2, T3, T4, T5, TResult>
    {
        internal static Func<object[], TResult> ToNormalFunc(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            Func<T1, T2, T3, T4, T5, TResult> func = (p1, p2, p3, p4, p5) => { action(p1, p2, p3, p4, p5); return default(TResult); };
            return DelegateHelper<T1, T2, T3, T4, T5, TResult>.ToNormalFunc(func, param1, param2, param3, param4, param5);
        }

        internal static Func<object[], TResult> ToNormalFunc(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            Func<TResult> func = () => { return function(param1, param2, param3, param4, param5); };
            return DelegateHelper<TResult>.ToNormalFunc(func);
        }
    }
}
