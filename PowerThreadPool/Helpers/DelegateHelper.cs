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

        internal static Action<CancellationToken> ToNormalAction(Action<object[], CancellationToken> action, object[] param)
        {
            void wrapper(CancellationToken ct)
            {
                action(param, ct);
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

        internal static Func<CancellationToken, TResult> ToNormalFunc<TResult>(Func<object[], CancellationToken, TResult> function, object[] param)
        {
            TResult func(CancellationToken ct)
            {
                return function(param, ct);
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

        internal static Action<CancellationToken> ToNormalAction<T1>(Action<T1, CancellationToken> action, T1 param1)
        {
            void wrapper(CancellationToken ct)
            {
                action(param1, ct);
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

        internal static Func<CancellationToken, TResult> ToNormalFunc<T1, CancellationToken, TResult>(Func<T1, CancellationToken, TResult> function, T1 param1)
        {
            TResult func(CancellationToken ct)
            {
                return function(param1, ct);
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

        internal static Action<CancellationToken> ToNormalAction<T1, T2>(Action<T1, T2, CancellationToken> action, T1 param1, T2 param2)
        {
            void wrapper(CancellationToken ct)
            {
                action(param1, param2, ct);
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

        internal static Func<CancellationToken, TResult> ToNormalFunc<T1, T2, CancellationToken, TResult>(Func<T1, T2, CancellationToken, TResult> function, T1 param1, T2 param2)
        {
            TResult func(CancellationToken ct)
            {
                return function(param1, param2, ct);
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

        internal static Action<CancellationToken> ToNormalAction<T1, T2, T3>(Action<T1, T2, T3, CancellationToken> action, T1 param1, T2 param2, T3 param3)
        {
            void wrapper(CancellationToken ct)
            {
                action(param1, param2, param3, ct);
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

        internal static Func<CancellationToken, TResult> ToNormalFunc<T1, T2, T3, CancellationToken, TResult>(Func<T1, T2, T3, CancellationToken, TResult> function, T1 param1, T2 param2, T3 param3)
        {
            TResult func(CancellationToken ct)
            {
                return function(param1, param2, param3, ct);
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

        internal static Action<CancellationToken> ToNormalAction<T1, T2, T3, T4>(
            Action<T1, T2, T3, T4, CancellationToken> action,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4)
        {
            void wrapper(CancellationToken ct)
            {
                action(param1, param2, param3, param4, ct);
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

        internal static Func<CancellationToken, TResult> ToNormalFunc<T1, T2, T3, T4, CancellationToken, TResult>(
            Func<T1, T2, T3, T4, CancellationToken, TResult> function,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4)
        {
            TResult func(CancellationToken ct)
            {
                return function(param1, param2, param3, param4, ct);
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

        internal static Action<CancellationToken> ToNormalAction<T1, T2, T3, T4, T5>(
            Action<T1, T2, T3, T4, T5, CancellationToken> action,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5)
        {
            void wrapper(CancellationToken ct)
            {
                action(param1, param2, param3, param4, param5, ct);
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

        internal static Func<CancellationToken, TResult> ToNormalFunc<T1, T2, T3, T4, T5, TResult>(
            Func<T1, T2, T3, T4, T5, CancellationToken, TResult> function,
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5)
        {
            TResult func(CancellationToken ct)
            {
                return function(param1, param2, param3, param4, param5, ct);
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

        internal static Func<CancellationToken, Task> ToNormalFunc(
            Func<object[], CancellationToken, Task> asyncFunc,
            object[] param)
        {
            Task func(CancellationToken ct)
            {
                return asyncFunc(param, ct);
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

        internal static Func<CancellationToken, Task<TResult>> ToNormalFuncT<CancellationToken, TResult>(
            Func<object[], CancellationToken, Task<TResult>> asyncFunc,
            object[] param)
        {
            Task<TResult> func(CancellationToken ct)
            {
                return asyncFunc(param, ct);
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

        internal static Func<CancellationToken, Task> ToNormalFunc<T1>(
            Func<T1, CancellationToken, Task> asyncFunc,
            T1 param1)
        {
            Task func(CancellationToken ct)
            {
                return asyncFunc(param1, ct);
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

        internal static Func<CancellationToken, Task<TResult>> ToNormalFunc<T1, TResult>(
            Func<T1, CancellationToken, Task<TResult>> asyncFunc,
            T1 param1)
        {
            Task<TResult> func(CancellationToken ct)
            {
                return asyncFunc(param1, ct);
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

        internal static Func<CancellationToken, Task> ToNormalFunc<T1, T2>(
            Func<T1, T2, CancellationToken, Task> asyncFunc,
            T1 param1,
            T2 param2)
        {
            Task func(CancellationToken ct)
            {
                return asyncFunc(param1, param2, ct);
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
        internal static Func<CancellationToken, Task<TResult>> ToNormalFunc<T1, T2, TResult>(
            Func<T1, T2, CancellationToken, Task<TResult>> asyncFunc,
            T1 param1,
            T2 param2)
        {
            Task<TResult> func(CancellationToken ct)
            {
                return asyncFunc(param1, param2, ct);
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

        internal static Func<CancellationToken, Task> ToNormalFunc<T1, T2, T3>(
           Func<T1, T2, T3, CancellationToken, Task> asyncFunc,
           T1 param1,
           T2 param2,
           T3 param3)
        {
            Task func(CancellationToken ct)
            {
                return asyncFunc(param1, param2, param3, ct);
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

        internal static Func<CancellationToken, Task<TResult>> ToNormalFunc<T1, T2, T3, TResult>(
           Func<T1, T2, T3, CancellationToken, Task<TResult>> asyncFunc,
           T1 param1,
           T2 param2,
           T3 param3)
        {
            Task<TResult> func(CancellationToken ct)
            {
                return asyncFunc(param1, param2, param3, ct);
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

        internal static Func<CancellationToken, Task> ToNormalFunc<T1, T2, T3, T4>(
          Func<T1, T2, T3, T4, CancellationToken, Task> asyncFunc,
          T1 param1,
          T2 param2,
          T3 param3,
          T4 param4)
        {
            Task func(CancellationToken ct)
            {
                return asyncFunc(param1, param2, param3, param4, ct);
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

        internal static Func<CancellationToken, Task<TResult>> ToNormalFunc<T1, T2, T3, T4, TResult>(
           Func<T1, T2, T3, T4, CancellationToken, Task<TResult>> asyncFunc,
           T1 param1,
           T2 param2,
           T3 param3,
           T4 param4)
        {
            Task<TResult> func(CancellationToken ct)
            {
                return asyncFunc(param1, param2, param3, param4, ct);
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

        internal static Func<CancellationToken, Task> ToNormalFunc<T1, T2, T3, T4, T5>(
           Func<T1, T2, T3, T4, T5, CancellationToken, Task> asyncFunc,
           T1 param1,
           T2 param2,
           T3 param3,
           T4 param4,
           T5 param5)
        {
            Task func(CancellationToken ct)
            {
                return asyncFunc(param1, param2, param3, param4, param5, ct);
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

        internal static Func<CancellationToken, Task<TResult>> ToNormalFunc<T1, T2, T3, T4, T5, TResult>(
           Func<T1, T2, T3, T4, T5, CancellationToken, Task<TResult>> asyncFunc,
           T1 param1,
           T2 param2,
           T3 param3,
           T4 param4,
           T5 param5)
        {
            Task<TResult> func(CancellationToken ct)
            {
                return asyncFunc(param1, param2, param3, param4, param5, ct);
            }
            return func;
        }
    }
}
