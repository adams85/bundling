using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Test.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Karambolo.AspNetCore.Bundling.Internal.Caching
{
    public abstract class BundleCacheTest
    {
        protected virtual ISystemClock Clock { get; } = new SystemClock();
        protected abstract IBundleCache Cache { get; }
        protected abstract bool ProvidesPhysicalFiles { get; }

        protected virtual void Setup(TimeSpan? expirationScanFrequency = null) { }
        protected virtual void Teardown() { }

        [Fact]
        public virtual async Task GetOrAdd()
        {
            Setup();
            try
            {
                var data = new BundleCacheData
                {
                    Version = "1",
                    Timestamp = DateTimeOffset.UtcNow,
                    Content = Encoding.UTF8.GetBytes("html { }")
                };

                var key = new BundleCacheKey(0, "/test.css", QueryString.Empty);
                var factoryRun = false;
                IBundleCacheItem addItem = await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(data);
                }, CancellationToken.None, BundleCacheOptions.Default, lockFile: true);

                Assert.True(factoryRun);

                if (addItem.FileReleaser != NullDisposable.Instance)
                {
                    // file is not released so the next call must block (using a parametrized key will force acquiring a writer lock)

                    var key2 = new BundleCacheKey(key.ManagerId, key.Path, new QueryString("?q"));
                    Task<IBundleCacheItem> getItemTask = Cache.GetOrAddAsync(key2, ct => throw new ApplicationException(), CancellationToken.None, BundleCacheOptions.Default, lockFile: false);

                    await AsyncHelper.NeverCompletesAsync(getItemTask);

                    // releasing file

                    addItem.FileReleaser.Dispose();
                    try { await getItemTask; }
                    catch (ApplicationException) { }
                }

                // re-checking

                factoryRun = false;
                IBundleCacheItem getItem = await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(new BundleCacheData());
                }, CancellationToken.None, BundleCacheOptions.Default, lockFile: false);

                Assert.False(factoryRun);

                Assert.Equal(data.Version, getItem.Version);
                Assert.Equal(data.Timestamp, getItem.Timestamp);
                Assert.True(!ProvidesPhysicalFiles ^ getItem.FileInfo is PhysicalFileInfo);
                Assert.Equal(data.Content, await FileHelper.GetContentAsync(getItem.FileInfo));

                // file lock was not requested so next call must not block

                factoryRun = false;
                getItem = await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(new BundleCacheData());
                }, CancellationToken.None, BundleCacheOptions.Default, lockFile: false);

                Assert.False(factoryRun);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        public virtual async Task Remove()
        {
            Setup();
            try
            {
                var data = new BundleCacheData
                {
                    Version = "1",
                    Timestamp = DateTimeOffset.UtcNow,
                    Content = Encoding.UTF8.GetBytes("html { }")
                };

                var key = new BundleCacheKey(0, "/test.css", QueryString.Empty);
                var factoryRun = false;
                IBundleCacheItem addItem = await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(data);
                }, CancellationToken.None, BundleCacheOptions.Default, lockFile: true);

                Assert.True(factoryRun);

                if (addItem.FileReleaser != NullDisposable.Instance)
                {
                    // file is not released so the next call must block (remove should acquire a writer lock)

                    Task removeItemTask = Cache.RemoveAsync(key, CancellationToken.None);

                    await AsyncHelper.NeverCompletesAsync(removeItemTask);

                    // releasing file

                    addItem.FileReleaser.Dispose();
                    try { await removeItemTask; }
                    catch (ApplicationException) { }
                }

                // re-checking

                await Cache.RemoveAsync(key, CancellationToken.None);

                factoryRun = false;
                await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(data);
                }, CancellationToken.None, BundleCacheOptions.Default, lockFile: false);

                Assert.True(factoryRun);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        public virtual async Task RemoveAll()
        {
            Setup();
            try
            {
                var data = new BundleCacheData
                {
                    Version = "1",
                    Timestamp = DateTimeOffset.UtcNow,
                    Content = Encoding.UTF8.GetBytes("html { }")
                };

                var key1 = new BundleCacheKey(0, "/test.css", QueryString.Empty);

                var factoryRun = false;
                IBundleCacheItem addItem1 = await Cache.GetOrAddAsync(key1, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(data);
                }, CancellationToken.None, BundleCacheOptions.Default, lockFile: true);

                Assert.True(factoryRun);

                var key2 = new BundleCacheKey(key1.ManagerId, key1.Path, new QueryString("?q"));

                if (addItem1.FileReleaser != NullDisposable.Instance)
                {
                    // file1 is not released so the next call must block (remove should acquire a writer lock)

                    Task removeAllItemTask = Cache.RemoveAllAsync(key1.ManagerId, key1.Path, CancellationToken.None);

                    await AsyncHelper.NeverCompletesAsync(removeAllItemTask);

                    // releasing file

                    addItem1.FileReleaser.Dispose();
                    try { await removeAllItemTask; }
                    catch (ApplicationException) { }

                    // re-adding without locking

                    factoryRun = false;
                    await Cache.GetOrAddAsync(key1, ct =>
                    {
                        factoryRun = true;
                        return Task.FromResult(data);
                    }, CancellationToken.None, BundleCacheOptions.Default, lockFile: false);

                    Assert.True(factoryRun);

                    factoryRun = false;
                    await Cache.GetOrAddAsync(key2, ct =>
                    {
                        factoryRun = true;
                        return Task.FromResult(data);
                    }, CancellationToken.None, BundleCacheOptions.Default, lockFile: false);

                    Assert.True(factoryRun);
                }

                // re-checking

                await Cache.RemoveAllAsync(key1.ManagerId, key1.Path, CancellationToken.None);

                factoryRun = false;
                await Cache.GetOrAddAsync(key1, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(data);
                }, CancellationToken.None, BundleCacheOptions.Default, lockFile: false);

                Assert.True(factoryRun);

                factoryRun = false;
                await Cache.GetOrAddAsync(key2, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(data);
                }, CancellationToken.None, BundleCacheOptions.Default, lockFile: false);

                Assert.True(factoryRun);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        public virtual async Task Options_AbsoluteExpiration()
        {
            Setup(expirationScanFrequency: TimeSpan.FromSeconds(0.1));
            try
            {
                var data = new BundleCacheData
                {
                    Version = "1",
                    Timestamp = DateTimeOffset.UtcNow,
                    Content = Encoding.UTF8.GetBytes("html { }")
                };

                var key = new BundleCacheKey(0, "/test.css", QueryString.Empty);

                var factoryRun = false;
                await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(data);
                }, CancellationToken.None, new BundleCacheOptions { AbsoluteExpiration = Clock.UtcNow.AddSeconds(1) }, lockFile: false);

                Assert.True(factoryRun);

                factoryRun = false;
                await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(new BundleCacheData());
                }, CancellationToken.None, BundleCacheOptions.Default, lockFile: false);

                Assert.False(factoryRun);

                await Task.Delay(TimeSpan.FromSeconds(3));

                factoryRun = false;
                await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(data);
                }, CancellationToken.None, new BundleCacheOptions { AbsoluteExpiration = Clock.UtcNow.AddSeconds(1) }, lockFile: false);

                Assert.True(factoryRun);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        public virtual async Task Options_SlidingExpiration()
        {
            Setup(expirationScanFrequency: TimeSpan.FromSeconds(0.1));
            try
            {
                var data = new BundleCacheData
                {
                    Version = "1",
                    Timestamp = DateTimeOffset.UtcNow,
                    Content = Encoding.UTF8.GetBytes("html { }")
                };

                var key = new BundleCacheKey(0, "/test.css", QueryString.Empty);

                var factoryRun = false;
                await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(data);
                }, CancellationToken.None, new BundleCacheOptions { SlidingExpiration = TimeSpan.FromSeconds(2) }, lockFile: false);

                Assert.True(factoryRun);

                await Task.Delay(TimeSpan.FromSeconds(1));

                factoryRun = false;
                await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(new BundleCacheData());
                }, CancellationToken.None, BundleCacheOptions.Default, lockFile: false);

                Assert.False(factoryRun);

                await Task.Delay(TimeSpan.FromSeconds(1.5));

                factoryRun = false;
                await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(new BundleCacheData());
                }, CancellationToken.None, BundleCacheOptions.Default, lockFile: false);

                Assert.False(factoryRun);


                await Task.Delay(TimeSpan.FromSeconds(2.5));

                factoryRun = false;
                await Cache.GetOrAddAsync(key, ct =>
                {
                    factoryRun = true;
                    return Task.FromResult(data);
                }, CancellationToken.None, new BundleCacheOptions { AbsoluteExpiration = Clock.UtcNow.AddSeconds(1) }, lockFile: false);

                Assert.True(factoryRun);
            }
            finally
            {
                Teardown();
            }
        }
    }
}
