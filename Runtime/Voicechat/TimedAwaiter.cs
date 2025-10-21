using System;
using System.Threading.Tasks;

namespace AnyVR.Voicechat
{
    public class TimedAwaiter<T>
    {
        private readonly T _canceledValue;

        private readonly T _timeoutValue;
        private TaskCompletionSource<T> _tcs;

        public TimedAwaiter(T timeoutValue, T canceledValue)
        {
            _timeoutValue = timeoutValue;
            _canceledValue = canceledValue;
        }

        public Task<T> WaitForResult(TimeSpan? timeout = null)
        {
            if (_tcs != null && !_tcs.Task.IsCompleted)
            {
                // Cancel previous task
                _tcs.TrySetResult(_canceledValue);
            }

            _tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            Task delay = Task.Delay(timeout ?? TimeSpan.FromSeconds(10));

            _ = Task.WhenAny(_tcs.Task, delay).ContinueWith(t =>
            {
                if (t.Result == delay && !_tcs.Task.IsCompleted)
                    _tcs.TrySetResult(_timeoutValue);
            });

            return _tcs.Task;
        }

        public void Complete(T result)
        {
            _tcs?.TrySetResult(result);
        }
    }
}
