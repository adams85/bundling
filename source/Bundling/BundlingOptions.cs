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
        }

        public IBundleManager BundleManager { get; set; }
        public IFileProvider SourceFileProvider { get; set; }
        public string StaticFilesRequestPath { get; set; }
    }
}
