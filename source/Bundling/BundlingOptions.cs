using System.Runtime.InteropServices;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling
{
    public class BundlingOptions : StaticFileOptions
    {
        public static readonly BundlingOptions Default = new BundlingOptions();

        public BundlingOptions() : this(new SharedOptions()) { }

        public BundlingOptions(SharedOptions sharedOptions) : base(sharedOptions)
        {
            RequestPath = "/bundles";
            CaseSensitiveSourceFilePaths = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        public IBundleManager BundleManager { get; set; }
        public IFileProvider SourceFileProvider { get; set; }
        public bool CaseSensitiveSourceFilePaths { get; set; }
        public string StaticFilesRequestPath { get; set; }
    }
}
