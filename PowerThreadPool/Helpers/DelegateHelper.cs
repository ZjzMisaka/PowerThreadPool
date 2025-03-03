using System;

namespace PowerThreadPool.Helpers
{
    internal static class DelegateHelper
    {
        internal static Func<TResult> ToNormalFunc<TResult>(Action action)
        {
            TResult func()
            {
                action();
                return default;
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<TResult>(Action<object[]> action, object[] param)
        {
            TResult func()
            {
                action(param);
                return default;
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<TResult>(Func<object[], TResult> function, object[] param)
        {
            TResult func()
            {
                return function(param);
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<T1, TResult>(Action<T1> action, T1 param1)
        {
            TResult func()
            {
                action(param1);
                return default;
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<T1, TResult>(Func<T1, TResult> function, T1 param1)
        {
            TResult func()
            {
                return function(param1);
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<T1, T2, TResult>(Action<T1, T2> action, T1 param1, T2 param2)
        {
            TResult func()
            {
                action(param1, param2);
                return default;
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2)
        {
            TResult func()
            {
                return function(param1, param2);
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<T1, T2, T3, TResult>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
        {
            TResult func()
            {
                action(param1, param2, param3);
                return default;
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3)
        {
            TResult func()
            {
                return function(param1, param2, param3);
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<T1, T2, T3, T4, TResult>(
            Action<T1, T2, T3, T4> action,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4)
        {
            TResult func()
            {
                action(param1, param2, param3, param4);
                return default;
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<T1, T2, T3, T4, TResult>(
            Func<T1, T2, T3, T4, TResult> function,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4)
        {
            TResult func()
            {
                return function(param1, param2, param3, param4);
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<T1, T2, T3, T4, T5, TResult>(
            Action<T1, T2, T3, T4, T5> action,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5)
        {
            TResult func()
            {
                action(param1, param2, param3, param4, param5);
                return default;
            }
            return func;
        }

        internal static Func<TResult> ToNormalFunc<T1, T2, T3, T4, T5, TResult>(
            Func<T1, T2, T3, T4, T5, TResult> function,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5)
        {
            TResult func()
            {
                return function(param1, param2, param3, param4, param5);
            }
            return func;
        }
    }
}
