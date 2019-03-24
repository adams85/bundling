using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IFileBundleItemTransformContext : IBundleItemTransformContext
    {
        IFileProvider FileProvider { get; }
        string FilePath { get; }
        IFileInfo FileInfo { get; }
        ISet<string> AdditionalSourceFilePaths { get; set; }
    }

    public class FileBundleItemTransformContext : BundleItemTransformContext, IFileBundleItemTransformContext
    {
        public FileBundleItemTransformContext(IBundleBuildContext buildContext)
            : base(buildContext) { }

        public IFileProvider FileProvider { get; set; }
        public string FilePath { get; set; }
        public IFileInfo FileInfo { get; set; }
        public ISet<string> AdditionalSourceFilePaths { get; set; }
    }
}
