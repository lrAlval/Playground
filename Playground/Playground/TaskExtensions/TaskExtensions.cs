using System;
using System.Threading;
using System.Threading.Tasks;

namespace Playground.TaskExtensions
{
    //https://devblogs.microsoft.com/pfxteam/crafting-a-task-timeoutafter-method/
    public static class TaskExtensions
    {
        public static Task<TResult> TimeoutAfterWithTimer<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            // Short-circuit #1: infinite timeout or task already completed
            if (task.IsCompleted || (timeout == TimeSpan.MaxValue))
            {
                // Either the task has already completed or timeout will never occur.
                // No proxy necessary.
                return task;
            }

            // tcs.Task will be returned as a proxy to the caller
            TaskCompletionSource<TResult> tcs =
                new TaskCompletionSource<TResult>();

            // Short-circuit #2: zero timeout
            if (timeout == TimeSpan.Zero)
            {
                // We've already timed out.
                tcs.SetException(new TimeoutException());
                return tcs.Task;
            }

            // Set up a timer to complete after the specified timeout period
            Timer timer = new Timer(
                state =>
                {
                    // Recover your state information
                    var myTcs = (TaskCompletionSource<TResult>)state;

                    // Fault our proxy with a TimeoutException
                    myTcs.TrySetException(new TimeoutException());
                },
                tcs,
                timeout,
                Timeout.InfiniteTimeSpan);

            // Wire up the logic for what happens when source task completes
            task.ContinueWith(
                (antecedent, state) =>
                {
                    // Recover our state data
                    var tuple =
                        (Tuple<Timer, TaskCompletionSource<TResult>>)state;

                    // Cancel the Timer
                    tuple.Item1.Dispose();

                    // Marshal results to proxy
                    MarshalTaskResults(antecedent, tuple.Item2);
                },
                Tuple.Create(timer, tcs),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return tcs.Task;
        }

        public static Task TimeoutAfterWithTimer(this Task task, TimeSpan timeout)
        {
            // Short-circuit #1: infinite timeout or task already completed
            if (task.IsCompleted || (timeout == TimeSpan.MaxValue))
            {
                // Either the task has already completed or timeout will never occur.
                // No proxy necessary.
                return task;
            }

            // tcs.Task will be returned as a proxy to the caller
            TaskCompletionSource<VoidTaskResult> tcs =
                new TaskCompletionSource<VoidTaskResult>();

            // Short-circuit #2: zero timeout
            if (timeout == TimeSpan.Zero)
            {
                // We've already timed out.
                tcs.SetException(new TimeoutException());
                return tcs.Task;
            }

            // Set up a timer to complete after the specified timeout period
            Timer timer = new Timer(
                state =>
                {
                    // Recover your state information
                    var myTcs = (TaskCompletionSource<VoidTaskResult>)state;

                    // Fault our proxy with a TimeoutException
                    myTcs.TrySetException(new TimeoutException());
                },
                tcs,
                timeout,
                Timeout.InfiniteTimeSpan);

            // Wire up the logic for what happens when source task completes
            task.ContinueWith(
                (antecedent, state) =>
                {
                    // Recover our state data
                    //var tuple = (Tuple<Timer, TaskCompletionSource<VoidTaskResult>>)state;

                    var (timerState, taskSource) = (Tuple<Timer, TaskCompletionSource<VoidTaskResult>>)state;


                    // Cancel the Timer
                    timerState.Dispose();

                    // Marshal results to proxy
                    MarshalTaskResults(antecedent, taskSource);
                },
                Tuple.Create(timer, tcs),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return tcs.Task;
        }

        public static async Task TimeoutAfterWithTPL(this Task originalTask, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(originalTask, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == originalTask)
                {
                    timeoutCancellationTokenSource.Cancel();
                    await originalTask.ConfigureAwait(false);
                }
                else
                {
                    throw new TimeoutException();
                }
            }
        }

        public static async Task<TResult> TimeoutAfterWithTPL<TResult>(this Task<TResult> originalTask, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(originalTask, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == originalTask)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await originalTask.ConfigureAwait(false);
                }

                throw new TimeoutException();
            }
        }

        internal struct VoidTaskResult
        {
        }

        internal static void MarshalTaskResults<TResult>(
            Task source,
            TaskCompletionSource<TResult> proxy)
        {
            switch (source.Status)
            {
                case TaskStatus.Faulted:
                    proxy.TrySetException(source.Exception);
                    break;
                case TaskStatus.Canceled:
                    proxy.TrySetCanceled();
                    break;
                case TaskStatus.RanToCompletion:
                    Task<TResult> castedSource = source as Task<TResult>;
                    proxy.TrySetResult(
                        castedSource == null
                            ? default
                            : //source is a Task
                            castedSource.Result); // source is a Task<TResult>
                    break;
            }
        }
    }
}
