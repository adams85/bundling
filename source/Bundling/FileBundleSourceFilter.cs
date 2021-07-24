using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IFileBundleSourceFilterItem
    {
        IFileProvider FileProvider { get; }
        bool CaseSensitiveFilePaths { get; }
        string FilePath { get; }
    }

    public interface IFileBundleSourceFilter
    {
        void Filter(List<IFileBundleSourceFilterItem> fileList, IBundleBuildContext context);
        Task FilterAsync(List<IFileBundleSourceFilterItem> fileList, IBundleBuildContext context);
    }

    public class FileBundleSourceFilter : IFileBundleSourceFilter
    {
        public virtual void Filter(List<IFileBundleSourceFilterItem> fileList, IBundleBuildContext context) { }

        public virtual Task FilterAsync(List<IFileBundleSourceFilterItem> fileList, IBundleBuildContext context)
        {
            return Task.CompletedTask;
        }
    }
}
