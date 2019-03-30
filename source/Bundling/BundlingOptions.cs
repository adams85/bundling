using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling
{
    public class BundlingOptions : StaticFileOptions
    {
        public BundlingOptions() : this(new SharedOptions()) { }

        public BundlingOptions(SharedOptions sharedOptions) : base(sharedOptions)
        {
            RequestPath = BundlingConfiguration.DefaultBundlesPathPrefix;
        }

        internal BundlingOptions(BundlingOptions options, BundlingConfiguration configuration) : base(new SharedOptions())
        {
            BundleManager = options.BundleManager;
            SourceFileProvider = configuration.SourceFileProvider ?? options.SourceFileProvider;
            CaseSensitiveSourceFilePaths = configuration.CaseSensitiveSourceFilePaths ?? options.CaseSensitiveSourceFilePaths;
            StaticFilesRequestPath = configuration.StaticFilesPathPrefix ?? options.StaticFilesRequestPath;

            ContentTypeProvider = options.ContentTypeProvider;
            DefaultContentType = options.DefaultContentType;
            ServeUnknownFileTypes = options.ServeUnknownFileTypes;
            OnPrepareResponse = options.OnPrepareResponse;

            RequestPath = configuration.BundlesPathPrefix ?? options.RequestPath;
            FileProvider = options.FileProvider;
        }

        public IBundleManager BundleManager { get; set; }
        public IFileProvider SourceFileProvider { get; set; }
        public bool? CaseSensitiveSourceFilePaths { get; set; }
        public string StaticFilesRequestPath { get; set; }
    }
}
