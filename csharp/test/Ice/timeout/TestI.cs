//
// Copyright (c) ZeroC, Inc. All rights reserved.
//

using Test;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroC.Ice.Test.Timeout
{
    internal class Timeout : ITimeout
    {
        public void op(Current current)
        {
        }

        public void sendData(byte[] seq, Current current)
        {
        }

        public void sleep(int to, Current current)
        {
            if (current.Connection == null)
            {
                // Ensure the collocated dispatch is canceled when the invocation is canceled because of the invocation
                // timeout.
                try
                {
                    Task.Delay(to, current.CancellationToken).Wait();
                    TestHelper.Assert(false);
                }
                catch (TaskCanceledException)
                {
                }
            }
            else
            {
                Thread.Sleep(to);
            }
        }
    }

    internal class Controller : IController
    {
        private readonly TaskScheduler _scheduler;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        public Controller(TaskScheduler scheduler) => _scheduler = scheduler;

        public void holdAdapter(int to, Current current)
        {
            Task.Factory.StartNew(() => _semaphore.Wait(), default, TaskCreationOptions.None, _scheduler);
            if (to >= 0)
            {
                Task.Delay(to).ContinueWith(t => _semaphore.Release(), TaskScheduler.Default);
            }
        }

        public void resumeAdapter(Current current) => _ = _semaphore.Release();

        public void shutdown(Current current) => current.Adapter.Communicator.ShutdownAsync();
    }
}
