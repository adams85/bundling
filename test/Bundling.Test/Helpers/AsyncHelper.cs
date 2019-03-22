using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling.Test.Helpers
{
    public static class AsyncHelper
    {
        public static async Task NeverCompletesAsync(Task task, int timeout = 1000)
        {
            // Wait for the task to complete, or the timeout to fire.
            Task completedTask = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
            if (completedTask == task)
                throw new ApplicationException("Task completed unexpectedly.");

            // If the task didn't complete, attach a continuation that will raise an exception on a random thread pool thread if it ever does complete.
            try
            {
                throw new ApplicationException("Task completed unexpectedly.");
            }
            catch (Exception ex)
            {
                var info = ExceptionDispatchInfo.Capture(ex);
                Task __ = task.ContinueWith(_ => info.Throw(), TaskScheduler.Default);
            }
        }
    }
}
