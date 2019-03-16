using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    /// Based on <see cref="https://blogs.msdn.microsoft.com/pfxteam/2012/02/12/building-async-coordination-primitives-part-7-asyncreaderwriterlock/"/>
    /// TODO: support cancellation?
    class AsyncKeyedLock<TKey>
    {
        class LockState
        {
            public Queue<TaskCompletionSource<IDisposable>> WaitingWriters;
            public TaskCompletionSource<IDisposable> WaitingReader;
            public int WaitingReaderCount;
            public int Status;
        }

        struct Releaser : IDisposable
        {
            AsyncKeyedLock<TKey> _owner;
            readonly TKey _key;
            readonly bool _isWriter;

            public Releaser(AsyncKeyedLock<TKey> owner, TKey key, bool isWriter)
            {
                _owner = owner;
                _key = key;
                _isWriter = isWriter;
            }

            public void Dispose()
            {
                if (_owner != null)
                {
                    if (_isWriter)
                        _owner.WriterRelease(_key);
                    else
                        _owner.ReaderRelease(_key);

                    _owner = null;
                }
            }
        }

        readonly Dictionary<TKey, LockState> _locks = new Dictionary<TKey, LockState>();

        LockState GetOrCreate(TKey key)
        {
            if (!_locks.TryGetValue(key, out LockState lockState))
                _locks[key] = lockState = new LockState
                {
                    WaitingWriters = new Queue<TaskCompletionSource<IDisposable>>(),
                    WaitingReader = new TaskCompletionSource<IDisposable>(TaskCreationOptions.RunContinuationsAsynchronously)
                };

            return lockState;
        }

        void Remove(TKey key)
        {
            _locks.Remove(key);
        }

        public Task<IDisposable> ReaderLockAsync(TKey key)
        {
            lock (_locks)
            {
                var @lock = GetOrCreate(key);

                if (@lock.Status >= 0 && @lock.WaitingWriters.Count == 0)
                {
                    ++@lock.Status;
                    _locks[key] = @lock;

                    return Task.FromResult<IDisposable>(new Releaser(this, key, isWriter: false));
                }
                else
                {
                    ++@lock.WaitingReaderCount;
                    _locks[key] = @lock;

                    // continuation: ensuring that all awaiters will be able to run concurrently rather than getting serialized
                    return @lock.WaitingReader.Task.ContinueWith(t => t.Result);
                }
            }
        }

        public IDisposable ReaderLock(TKey key)
        {
            return ReaderLockAsync(key).GetAwaiter().GetResult();
        }

        public Task<IDisposable> WriterLockAsync(TKey key)
        {
            lock (_locks)
            {
                var @lock = GetOrCreate(key);

                if (@lock.Status == 0)
                {
                    @lock.Status = -1;
                    _locks[key] = @lock;

                    return Task.FromResult<IDisposable>(new Releaser(this, key, isWriter: true));
                }
                else
                {
                    var waiter = new TaskCompletionSource<IDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);
                    @lock.WaitingWriters.Enqueue(waiter);

                    return waiter.Task;
                }
            }
        }

        public IDisposable WriterLock(TKey key)
        {
            return WriterLockAsync(key).GetAwaiter().GetResult();
        }

        void ReaderRelease(TKey key)
        {
            TaskCompletionSource<IDisposable> toWake;

            lock (_locks)
            {
                var @lock = GetOrCreate(key);

                --@lock.Status;
                if (@lock.Status != 0)
                {
                    _locks[key] = @lock;
                    return;
                }
                else if (@lock.WaitingWriters.Count > 0)
                {
                    @lock.Status = -1;
                    _locks[key] = @lock;

                    toWake = @lock.WaitingWriters.Dequeue();
                }
                else
                {
                    Remove(key);
                    return;
                }
            }

            toWake.SetResult(new Releaser(this, key, isWriter: true));
        }

        void WriterRelease(TKey key)
        {
            TaskCompletionSource<IDisposable> toWake;
            var toWakeIsWriter = false;

            lock (_locks)
            {
                var @lock = GetOrCreate(key);

                if (@lock.WaitingWriters.Count > 0)
                {
                    toWake = @lock.WaitingWriters.Dequeue();
                    toWakeIsWriter = true;
                }
                else if (@lock.WaitingReaderCount > 0)
                {
                    toWake = @lock.WaitingReader;
                    @lock.Status = @lock.WaitingReaderCount;
                    @lock.WaitingReaderCount = 0;
                    @lock.WaitingReader = new TaskCompletionSource<IDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _locks[key] = @lock;
                }
                else
                {
                    Remove(key);
                    return;
                }
            }

            toWake.SetResult(new Releaser(this, key, toWakeIsWriter));
        }
    }
}
