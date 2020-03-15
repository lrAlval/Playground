using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Playground.TaskExtensions;
using Playground.Utils;

namespace Playground.Benchmarks.TaskExtensions
{
    [SimpleJob(launchCount: 1, warmupCount: 3, targetCount: 5, invocationCount: 50, id: "QuickJob")]
    [ThreadingDiagnoser]
    [MemoryDiagnoser]
    [BenchmarkCategory("InvokerBench")]
    public class TaskExtensionsBenchs
    {
        private TimeSpan _timeoutSpan;
        private const int MinTimeoutInMs = 300;
        private const int MaxTimeoutInMs = 500;
               
        [GlobalSetup]
        public void SetUp() => _timeoutSpan = TimeSpan.FromMilliseconds(250);

        [Benchmark]
        public async Task TimeoutHitTimer()
        {
            try
            {
                await ScheduleWithReturn(MinTimeoutInMs, MaxTimeoutInMs).Unwrap().TimeoutAfterWithTimer(_timeoutSpan);
            }
            catch (TimeoutException e)
            {
            }
        }

        [Benchmark]
        public async Task AwaitAwaitTimeoutHitTimer()
        {
            try
            {
                await await ScheduleWithReturn(MinTimeoutInMs, MaxTimeoutInMs).TimeoutAfterWithTimer(_timeoutSpan);
            }
            catch (TimeoutException e)
            {
            }
        }


        [Benchmark]
        public async Task TimeoutHitWithTPL()
        {
            try
            {
                await ScheduleWithReturn(MinTimeoutInMs, MaxTimeoutInMs).Unwrap().TimeoutAfterWithTPL(_timeoutSpan);
            }
            catch (TimeoutException e)
            {
            }
        }

        [Benchmark]
        public async Task AwaitAwaitTimeoutHitWithTPL()
        {
            try
            {
                await await ScheduleWithReturn(MinTimeoutInMs, MaxTimeoutInMs).TimeoutAfterWithTPL(_timeoutSpan);
            }
            catch (TimeoutException e)
            {
            }
        }

        private static Task<Task<int>> ScheduleWithReturn(int minSecond, int maxSecond) =>
            Task.Factory.StartNew(() => FakeAsync(minSecond, maxSecond), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        private static async Task<int> FakeAsync(int minSecond, int maxSecond)
        {
            await Task.Delay(RandomTimeSpan.Between(minSecond, maxSecond));

            return 0;
        }
    }
}