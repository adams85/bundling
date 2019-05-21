using System;
using dotless.Core;
using dotless.Core.Importers;
using dotless.Core.Loggers;
using dotless.Core.Parser;
using dotless.Core.Stylizers;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Less
{
    public interface ILessEngineFactory
    {
        ILessEngine Create(string fileBasePath, string virtualBasePath, IFileProvider fileProvider);
    }

    public class LessEngineFactory : ILessEngineFactory
    {
        private static readonly IStylizer s_stylizer = new PlainStylizer();
        private static readonly ILogger s_logger = NullLogger.Instance;

        public ILessEngine Create(string fileBasePath, string virtualBasePath, IFileProvider fileProvider)
        {
            if (fileBasePath == null)
                throw new ArgumentNullException(nameof(fileBasePath));

            if (virtualBasePath == null)
                throw new ArgumentNullException(nameof(virtualBasePath));

            if (!virtualBasePath.EndsWith("/"))
                virtualBasePath += '/';

            FileProviderFileReader fileReader = fileProvider != null ? new FileProviderFileReader(fileProvider) : FileProviderFileReader.Null;

            var importer = new Importer(fileReader, disableUrlReWriting: false, rootPath: virtualBasePath, inlineCssFiles: true, importAllFilesAsLess: true);

            var parser = new Parser(s_stylizer, importer);

            return new LessEngine(parser, s_logger, compress: false, debug: false) { CurrentDirectory = fileBasePath };
        }
    }
}
