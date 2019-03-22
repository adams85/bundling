using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Test.Helpers;
using Xunit;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    // based on Stephen Cleary's AsyncReaderWriterLock unit tests
    // https://github.com/StephenCleary/AsyncEx/blob/master/test/AsyncEx.Coordination.UnitTests/AsyncReaderWriterLockUnitTests.cs
    public class AsyncKeyedLockTest
    {
        [Fact]
        public async Task DifferentKeys_DontInterfere()
        {
            var @lock = new AsyncKeyedLock<int>();
            await @lock.WriterLockAsync(0);
            await @lock.WriterLockAsync(1);
        }

        [Fact]
        public async Task WriteLocked_NoWaiters()
        {
            var @lock = new AsyncKeyedLock<int>();
            using (await @lock.WriterLockAsync(0)) { }
        }

        [Fact]
        public async Task Unlocked_PermitsWriterLock()
        {
            var @lock = new AsyncKeyedLock<int>();
            await @lock.WriterLockAsync(0);
        }

        [Fact]
        public async Task Unlocked_PermitsMultipleReaderLocks()
        {
            var @lock = new AsyncKeyedLock<int>();
            await @lock.ReaderLockAsync(0);
            await @lock.ReaderLockAsync(0);
        }

        [Fact]
        public async Task WriteLocked_PreventsAnotherWriterLock()
        {
            var @lock = new AsyncKeyedLock<int>();
            await @lock.WriterLockAsync(0);
            Task<IDisposable> task = @lock.WriterLockAsync(0);
            await AsyncHelper.NeverCompletesAsync(task);
        }

        [Fact]
        public async Task WriteLocked_PreventsReaderLock()
        {
            var @lock = new AsyncKeyedLock<int>();
            await @lock.WriterLockAsync(0);
            Task<IDisposable> task = @lock.ReaderLockAsync(0);
            await AsyncHelper.NeverCompletesAsync(task);
        }

        [Fact]
        public async Task WriteLocked_Unlocked_PermitsAnotherWriterLock()
        {
            var @lock = new AsyncKeyedLock<int>();
            var firstWriteLockTaken = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirstWriteLock = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var task = Task.Run(async () =>
            {
                using (await @lock.WriterLockAsync(0))
                {
                    firstWriteLockTaken.SetResult(null);
                    await releaseFirstWriteLock.Task;
                }
            });
            await firstWriteLockTaken.Task;
            Task<IDisposable> lockTask = @lock.WriterLockAsync(0);
            Assert.False(lockTask.IsCompleted);
            releaseFirstWriteLock.SetResult(null);
            await lockTask;
        }

        [Fact]
        public async Task ReadLocked_PreventsWriterLock()
        {
            var @lock = new AsyncKeyedLock<int>();
            await @lock.ReaderLockAsync(0);
            Task<IDisposable> task = @lock.WriterLockAsync(0);
            await AsyncHelper.NeverCompletesAsync(task);
        }

        [Fact]
        public async Task LockReleased_WriteTakesPriorityOverRead()
        {
            var @lock = new AsyncKeyedLock<int>();
            Task writeLock, readLock;
            using (await @lock.WriterLockAsync(0))
            {
                readLock = @lock.ReaderLockAsync(0);
                writeLock = @lock.WriterLockAsync(0);
            }

            await writeLock;
            await AsyncHelper.NeverCompletesAsync(readLock);
        }

        [Fact]
        public async Task ReaderLocked_ReaderReleased_ReaderAndWriterWaiting_DoesNotReleaseReaderOrWriter()
        {
            var @lock = new AsyncKeyedLock<int>();
            Task readLock, writeLock;
            await @lock.ReaderLockAsync(0);
            using (await @lock.ReaderLockAsync(0))
            {
                writeLock = @lock.WriterLockAsync(0);
                readLock = @lock.ReaderLockAsync(0);
            }

            await Task.WhenAll(AsyncHelper.NeverCompletesAsync(writeLock), AsyncHelper.NeverCompletesAsync(readLock));
        }

        [Fact]
        public async Task LoadTest()
        {
            var @lock = new AsyncKeyedLock<int>();

            var readReleasers = new List<IDisposable>();
            for (int i = 0; i != 1000; ++i)
                readReleasers.Add(@lock.ReaderLock(0));

            var writeTask = Task.Run(() => { @lock.WriterLock(0).Dispose(); });

            var readTasks = new List<Task>();
            for (int i = 0; i != 100; ++i)
                readTasks.Add(Task.Run(() => @lock.ReaderLock(0).Dispose()));

            await Task.Delay(1000);

            foreach (IDisposable readReleaser in readReleasers)
                readReleaser.Dispose();

            await writeTask;

            foreach (Task readTask in readTasks)
                await readTask;
        }
    }
}

