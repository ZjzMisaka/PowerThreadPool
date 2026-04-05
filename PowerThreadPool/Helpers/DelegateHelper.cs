using System;
using System.Threading;
using System.Threading.Tasks;

namespace PowerThreadPool.Helpers
{
    internal static class DelegateHelper
    {
        internal static Action ToNormalAction(Action<object[]> action, object[] param)
        {
            void wrapper()
            {
                action(param);
            }
            return wrapper;
        }

        internal static Action<CancellationTokenSource> ToNormalAction(Action<object[], CancellationTokenSource> action, object[] param)
        {
            void wrapper(CancellationTokenSource cts)
            {
                action(param, cts);
            }
            return wrapper;
        }

        internal static Func<TResult> ToNormalFunc<TResult>(Func<object[], TResult> function, object[] param)
        {
            TResult func()
            {
                return function(param);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, TResult> ToNormalFunc<TResult>(Func<object[], CancellationTokenSource, TResult> function, object[] param)
        {
            TResult func(CancellationTokenSource cts)
            {
                return function(param, cts);
            }
            return func;
        }

        internal static Action ToNormalAction<T1>(Action<T1> action, T1 param1)
        {
            void wrapper()
            {
                action(param1);
            }
            return wrapper;
        }

        internal static Action<CancellationTokenSource> ToNormalAction<T1, CancellationTokenSource>(Action<T1, CancellationTokenSource> action, T1 param1)
        {
            void wrapper(CancellationTokenSource cts)
            {
                action(param1, cts);
            }
            return wrapper;
        }

        internal static Func<TResult> ToNormalFunc<T1, TResult>(Func<T1, TResult> function, T1 param1)
        {
            TResult func()
            {
                return function(param1);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, TResult> ToNormalFunc<T1, CancellationTokenSource, TResult>(Func<T1, CancellationTokenSource, TResult> function, T1 param1)
        {
            TResult func(CancellationTokenSource cts)
            {
                return function(param1, cts);
            }
            return func;
        }

        internal static Action ToNormalAction<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2)
        {
            void wrapper()
            {
                action(param1, param2);
            }
            return wrapper;
        }

        internal static Action<CancellationTokenSource> ToNormalAction<T1, T2, CancellationTokenSource>(Action<T1, T2, CancellationTokenSource> action, T1 param1, T2 param2)
        {
            void wrapper(CancellationTokenSource cts)
            {
                action(param1, param2, cts);
            }
            return wrapper;
        }

        internal static Func<TResult> ToNormalFunc<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2)
        {
            TResult func()
            {
                return function(param1, param2);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, TResult> ToNormalFunc<T1, T2, CancellationTokenSource, TResult>(Func<T1, T2, CancellationTokenSource, TResult> function, T1 param1, T2 param2)
        {
            TResult func(CancellationTokenSource cts)
            {
                return function(param1, param2, cts);
            }
            return func;
        }

        internal static Action ToNormalAction<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
        {
            void wrapper()
            {
                action(param1, param2, param3);
            }
            return wrapper;
        }

        internal static Action<CancellationTokenSource> ToNormalAction<T1, T2, T3, CancellationTokenSource>(Action<T1, T2, T3, CancellationTokenSource> action, T1 param1, T2 param2, T3 param3)
        {
            void wrapper(CancellationTokenSource cts)
            {
                action(param1, param2, param3, cts);
            }
            return wrapper;
        }

        internal static Func<TResult> ToNormalFunc<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3)
        {
            TResult func()
            {
                return function(param1, param2, param3);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, TResult> ToNormalFunc<T1, T2, T3, CancellationTokenSource, TResult>(Func<T1, T2, T3, CancellationTokenSource, TResult> function, T1 param1, T2 param2, T3 param3)
        {
            TResult func(CancellationTokenSource cts)
            {
                return function(param1, param2, param3, cts);
            }
            return func;
        }

        internal static Action ToNormalAction<T1, T2, T3, T4>(
            Action<T1, T2, T3, T4> action,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4)
        {
            void wrapper()
            {
                action(param1, param2, param3, param4);
            }
            return wrapper;
        }

        internal static Action<CancellationTokenSource> ToNormalAction<T1, T2, T3, T4, CancellationTokenSource>(
            Action<T1, T2, T3, T4, CancellationTokenSource> action,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4)
        {
            void wrapper(CancellationTokenSource cts)
            {
                action(param1, param2, param3, param4, cts);
            }
            return wrapper;
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

        internal static Func<CancellationTokenSource, TResult> ToNormalFunc<T1, T2, T3, T4, CancellationTokenSource, TResult>(
            Func<T1, T2, T3, T4, CancellationTokenSource, TResult> function,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4)
        {
            TResult func(CancellationTokenSource cts)
            {
                return function(param1, param2, param3, param4, cts);
            }
            return func;
        }

        internal static Action ToNormalAction<T1, T2, T3, T4, T5>(
            Action<T1, T2, T3, T4, T5> action,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5)
        {
            void wrapper()
            {
                action(param1, param2, param3, param4, param5);
            }
            return wrapper;
        }

        internal static Action<CancellationTokenSource> ToNormalAction<T1, T2, T3, T4, T5, CancellationTokenSource>(
            Action<T1, T2, T3, T4, T5, CancellationTokenSource> action,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5)
        {
            void wrapper(CancellationTokenSource cts)
            {
                action(param1, param2, param3, param4, param5, cts);
            }
            return wrapper;
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

        internal static Func<CancellationTokenSource, TResult> ToNormalFunc<T1, T2, T3, T4, T5, CancellationTokenSource, TResult>(
            Func<T1, T2, T3, T4, T5, CancellationTokenSource, TResult> function,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5)
        {
            TResult func(CancellationTokenSource cts)
            {
                return function(param1, param2, param3, param4, param5, cts);
            }
            return func;
        }

        internal static Func<Task> ToNormalFunc(
            Func<object[], Task> asyncFunc,
            object[] param)
        {
            Task func()
            {
                return asyncFunc(param);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, Task> ToNormalFunc(
            Func<object[], CancellationTokenSource, Task> asyncFunc,
            object[] param)
        {
            Task func(CancellationTokenSource cts)
            {
                return asyncFunc(param, cts);
            }
            return func;
        }

        internal static Func<Task<TResult>> ToNormalFunc<TResult>(
            Func<object[], Task<TResult>> asyncFunc,
            object[] param)
        {
            Task<TResult> func()
            {
                return asyncFunc(param);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, Task<TResult>> ToNormalFuncT<CancellationTokenSource, TResult>(
            Func<object[], CancellationTokenSource, Task<TResult>> asyncFunc,
            object[] param)
        {
            Task<TResult> func(CancellationTokenSource cts)
            {
                return asyncFunc(param, cts);
            }
            return func;
        }

        internal static Func<Task> ToNormalFunc<T1>(
            Func<T1, Task> asyncFunc,
            T1 param1)
        {
            Task func()
            {
                return asyncFunc(param1);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, Task> ToNormalFunc<T1>(
            Func<T1, CancellationTokenSource, Task> asyncFunc,
            T1 param1)
        {
            Task func(CancellationTokenSource cts)
            {
                return asyncFunc(param1, cts);
            }
            return func;
        }

        internal static Func<Task<TResult>> ToNormalFunc<T1, TResult>(
            Func<T1, Task<TResult>> asyncFunc,
            T1 param1)
        {
            Task<TResult> func()
            {
                return asyncFunc(param1);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, Task<TResult>> ToNormalFunc<T1, TResult>(
            Func<T1, CancellationTokenSource, Task<TResult>> asyncFunc,
            T1 param1)
        {
            Task<TResult> func(CancellationTokenSource cts)
            {
                return asyncFunc(param1, cts);
            }
            return func;
        }

        internal static Func<Task> ToNormalFunc<T1, T2>(
            Func<T1, T2, Task> asyncFunc,
            T1 param1,
            T2 param2)
        {
            Task func()
            {
                return asyncFunc(param1, param2);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, Task> ToNormalFunc<T1, T2, CancellationTokenSource>(
            Func<T1, T2, CancellationTokenSource, Task> asyncFunc,
            T1 param1,
            T2 param2)
        {
            Task func(CancellationTokenSource cts)
            {
                return asyncFunc(param1, param2, cts);
            }
            return func;
        }

        internal static Func<Task<TResult>> ToNormalFunc<T1, T2, TResult>(
            Func<T1, T2, Task<TResult>> asyncFunc,
            T1 param1,
            T2 param2)
        {
            Task<TResult> func()
            {
                return asyncFunc(param1, param2);
            }
            return func;
        }
        internal static Func<CancellationTokenSource, Task<TResult>> ToNormalFunc<T1, T2, TResult>(
            Func<T1, T2, CancellationTokenSource, Task<TResult>> asyncFunc,
            T1 param1,
            T2 param2)
        {
            Task<TResult> func(CancellationTokenSource cts)
            {
                return asyncFunc(param1, param2, cts);
            }
            return func;
        }

        internal static Func<Task> ToNormalFunc<T1, T2, T3>(
            Func<T1, T2, T3, Task> asyncFunc,
            T1 param1,
            T2 param2,
            T3 param3)
        {
            Task func()
            {
                return asyncFunc(param1, param2, param3);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, Task> ToNormalFunc<T1, T2, T3>(
           Func<T1, T2, T3, CancellationTokenSource, Task> asyncFunc,
           T1 param1,
           T2 param2,
           T3 param3)
        {
            Task func(CancellationTokenSource cts)
            {
                return asyncFunc(param1, param2, param3, cts);
            }
            return func;
        }

        internal static Func<Task<TResult>> ToNormalFunc<T1, T2, T3, TResult>(
            Func<T1, T2, T3, Task<TResult>> asyncFunc,
            T1 param1,
            T2 param2,
            T3 param3)
        {
            Task<TResult> func()
            {
                return asyncFunc(param1, param2, param3);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, Task<TResult>> ToNormalFunc<T1, T2, T3, TResult>(
           Func<T1, T2, T3, CancellationTokenSource, Task<TResult>> asyncFunc,
           T1 param1,
           T2 param2,
           T3 param3)
        {
            Task<TResult> func(CancellationTokenSource cts)
            {
                return asyncFunc(param1, param2, param3, cts);
            }
            return func;
        }

        internal static Func<Task> ToNormalFunc<T1, T2, T3, T4>(
            Func<T1, T2, T3, T4, Task> asyncFunc,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4)
        {
            Task func()
            {
                return asyncFunc(param1, param2, param3, param4);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, Task> ToNormalFunc<T1, T2, T3, T4>(
          Func<T1, T2, T3, T4, CancellationTokenSource, Task> asyncFunc,
          T1 param1,
          T2 param2,
          T3 param3,
          T4 param4)
        {
            Task func(CancellationTokenSource cts)
            {
                return asyncFunc(param1, param2, param3, param4, cts);
            }
            return func;
        }

        internal static Func<Task<TResult>> ToNormalFunc<T1, T2, T3, T4, TResult>(
            Func<T1, T2, T3, T4, Task<TResult>> asyncFunc,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4)
        {
            Task<TResult> func()
            {
                return asyncFunc(param1, param2, param3, param4);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, Task<TResult>> ToNormalFunc<T1, T2, T3, T4, TResult>(
           Func<T1, T2, T3, T4, CancellationTokenSource, Task<TResult>> asyncFunc,
           T1 param1,
           T2 param2,
           T3 param3,
           T4 param4)
        {
            Task<TResult> func(CancellationTokenSource cts)
            {
                return asyncFunc(param1, param2, param3, param4, cts);
            }
            return func;
        }

        internal static Func<Task> ToNormalFunc<T1, T2, T3, T4, T5>(
            Func<T1, T2, T3, T4, T5, Task> asyncFunc,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5)
        {
            Task func()
            {
                return asyncFunc(param1, param2, param3, param4, param5);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, Task> ToNormalFunc<T1, T2, T3, T4, T5>(
           Func<T1, T2, T3, T4, T5, CancellationTokenSource, Task> asyncFunc,
           T1 param1,
           T2 param2,
           T3 param3,
           T4 param4,
           T5 param5)
        {
            Task func(CancellationTokenSource cts)
            {
                return asyncFunc(param1, param2, param3, param4, param5, cts);
            }
            return func;
        }

        internal static Func<Task<TResult>> ToNormalFunc<T1, T2, T3, T4, T5, TResult>(
            Func<T1, T2, T3, T4, T5, Task<TResult>> asyncFunc,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5)
        {
            Task<TResult> func()
            {
                return asyncFunc(param1, param2, param3, param4, param5);
            }
            return func;
        }

        internal static Func<CancellationTokenSource, Task<TResult>> ToNormalFunc<T1, T2, T3, T4, T5, TResult>(
           Func<T1, T2, T3, T4, T5, CancellationTokenSource, Task<TResult>> asyncFunc,
           T1 param1,
           T2 param2,
           T3 param3,
           T4 param4,
           T5 param5)
        {
            Task<TResult> func(CancellationTokenSource cts)
            {
                return asyncFunc(param1, param2, param3, param4, param5, cts);
            }
            return func;
        }
    }
}
