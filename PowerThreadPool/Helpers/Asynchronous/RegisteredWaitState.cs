using System.Threading;
using System.Threading.Tasks;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class RegisteredWaitState<T>
    {
        public TaskCompletionSource<T> Tcs { get; set; }
        public RegisteredWaitHandle Handle { get; set; }
        public T Res { get; set; }

        internal static void WaitCallback(object s, bool _)
        {
            var st = (RegisteredWaitState<T>)s;
            st.Tcs.TrySetResult(st.Res);
        }
    }
}
