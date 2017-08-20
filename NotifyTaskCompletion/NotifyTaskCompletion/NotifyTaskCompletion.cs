using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace NotifyTaskCompletion
{
    public sealed class NotifyTaskCompletion<TResult> : INotifyPropertyChanged
    {
        private readonly Func<Task<TResult>> _taskFunction;
        private bool _isFirstRun;

        public NotifyTaskCompletion(Func<Task<TResult>> taskFunction)
        {
            _isFirstRun = true;
            if (taskFunction == null)
            {
                throw new ArgumentNullException(nameof(taskFunction));
            }

            _taskFunction = taskFunction;
        }

        private void InvokePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Watch()
        {
            if (!_isFirstRun)
            {
                throw new InvalidOperationException("can only watch once");
            }
            _isFirstRun = false;
            var _ = WatchAsync();
        }

        private async Task WatchAsync()
        {
            try
            {
                Task = _taskFunction();
                await Task; //await does not wrap an exception into an aggregate exception
                InvokePropertyChanged(nameof(Result));
            }
            catch (OperationCanceledException)
            {
                InvokePropertyChanged(nameof(IsCanceled));
            }
            catch
            {
                try
                {
                    Task.Wait(); //-> aggregate exception
                }
                catch (AggregateException)
                {
                    InvokePropertyChanged(nameof(IsFaulted));
                    InvokePropertyChanged(nameof(AggregateException));
                }
            }

            InvokePropertyChanged(nameof(Status));
            InvokePropertyChanged(nameof(IsCompleted));
        }

        public Task<TResult> Task { get; private set; }
        public TResult Result => 
            (Task.Status == TaskStatus.RanToCompletion) ? Task.Result : default(TResult);
        public TaskStatus Status => Task.Status;
        //https://msdn.microsoft.com/de-de/library/system.threading.tasks.task.iscompleted(v=vs.110).aspx
        //IsCompleted will return true when the task is in one of the three final states: RanToCompletion, Faulted, or Canceled.
        public bool IsCompleted => Task.IsCompleted;
        public bool IsCanceled => Task.IsCanceled;
        public bool IsFaulted => Task.IsFaulted;
        public AggregateException AggregateException => Task.Exception;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}