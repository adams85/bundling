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
        static readonly IStylizer stylizer = new PlainStylizer();
        static readonly ILogger logger = NullLogger.Instance;

        public ILessEngine Create(string fileBasePath, string virtualBasePath, IFileProvider fileProvider)
        {
            if (fileBasePath == null)
                throw new ArgumentNullException(nameof(fileBasePath));

            if (virtualBasePath == null)
                throw new ArgumentNullException(nameof(virtualBasePath));

            if (!virtualBasePath.EndsWith('/'))
                virtualBasePath += '/';

            var fileReader = fileProvider != null ? new FileProviderFileReader(fileProvider) : FileProviderFileReader.Null;

            var importer = new Importer(fileReader, disableUrlReWriting: false, rootPath: virtualBasePath, inlineCssFiles: true, importAllFilesAsLess: true);

            var parser = new Parser(stylizer, importer);

            return new LessEngine(parser, logger, compress: false, debug: false) { CurrentDirectory = fileBasePath };
        }
    }
}
