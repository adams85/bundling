using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IFileBundleItemTransformContext : IBundleItemTransformContext
    {
        string FilePath { get; }
        IFileProvider FileProvider { get; }
        IFileInfo FileInfo { get; }
    }

    public class FileBundleItemTransformContext : BundleItemTransformContext, IFileBundleItemTransformContext
    {
        public FileBundleItemTransformContext(IBundleBuildContext buildContext)
            : base(buildContext) { }

        public string FilePath { get; set; }
        public IFileProvider FileProvider { get; set; }
        public IFileInfo FileInfo { get; set; }
    }
}
