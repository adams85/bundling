using System;
using System.Collections.Generic;
using System.Threading;
using dotless.Core;
using dotless.Core.Importers;
using dotless.Core.Input;
using dotless.Core.Loggers;
using dotless.Core.Parser;
using dotless.Core.Stylizers;
using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Less
{
    public interface ILessEngineFactory
    {
        ILessEngine Create(string fileBasePath, PathString virtualPathPrefix, IFileProvider fileProvider, PathString outputPath, CancellationToken token);
    }

    public class LessEngineFactory : ILessEngineFactory
    {
        internal sealed class EnhancedImporter : Importer, IImporter
        {
            private readonly PathString _outputPath;

            public EnhancedImporter(IFileReader fileReader, bool disableUrlReWriting, string rootPath, bool inlineCssFiles, bool importAllFilesAsLess, PathString outputPath)
                : base(fileReader, disableUrlReWriting, rootPath, inlineCssFiles, importAllFilesAsLess)
            {
                _outputPath = outputPath;
            }

            string IImporter.AlterUrl(string url, List<string> pathList)
            {
                if (IsUrlRewritingDisabled || !UrlUtils.IsRelative(url))
                    return url;

                if (pathList.Count > 0)
                    url = GetAdjustedFilePath(url, pathList);

                return CssRewriteUrlTransform.RebaseUrlCore(url, CurrentDirectory, RootPath, _outputPath);
            }
        }

        private static readonly IStylizer s_stylizer = new PlainStylizer();
        private static readonly ILogger s_logger = NullLogger.Instance;

        public ILessEngine Create(string fileBasePath, PathString virtualPathPrefix, IFileProvider fileProvider, PathString outputPath, CancellationToken token)
        {
            if (fileBasePath == null)
                throw new ArgumentNullException(nameof(fileBasePath));

            fileBasePath = UrlUtils.NormalizePath(fileBasePath, trailingNormalization: PathNormalization.ExcludeSlash);
            var rootPath = UrlUtils.NormalizePath(virtualPathPrefix, trailingNormalization: PathNormalization.ExcludeSlash);

            var fileReader = new FileProviderFileReader(fileProvider ?? AbstractionFile.NullFileProvider, token);

            var importer = new EnhancedImporter(fileReader, disableUrlReWriting: false, rootPath, inlineCssFiles: true, importAllFilesAsLess: true, outputPath);

            var parser = new Parser(s_stylizer, importer);

            return new LessEngine(parser, s_logger, compress: false, debug: false) { CurrentDirectory = fileBasePath };
        }
    }
}
