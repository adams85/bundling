using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IFileBundleItemTransformContext : IBundleItemTransformContext
    {
        IFileProvider FileProvider { get; }
        bool CaseSensitiveFilePaths { get; }
        string FilePath { get; }
        IFileInfo FileInfo { get; }
    }

    public class FileBundleItemTransformContext : BundleItemTransformContext, IFileBundleItemTransformContext
    {
        public FileBundleItemTransformContext(IBundleBuildContext buildContext)
            : base(buildContext) { }

        public IFileProvider FileProvider { get; set; }
        public bool CaseSensitiveFilePaths { get; set; }
        public string FilePath { get; set; }
        public IFileInfo FileInfo { get; set; }
    }
}
