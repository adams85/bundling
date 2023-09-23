using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Karambolo.Common;
using Karambolo.PO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace JsTranslation.Infrastructure.Localization
{
    public class POLocalizationProvider : IPOLocalizationProvider
    {
        public const string BasePath = "/App_Data/Localization";

        readonly IWebHostEnvironment _env;
        readonly ILogger<POLocalizationProvider> _logger;

        public POLocalizationProvider(IWebHostEnvironment env, ILogger<POLocalizationProvider> logger)
        {
            _env = env;
            _logger = logger;

            Initialize();
        }

        public CultureInfo[] AvailableCultures { get; private set; }

        public IReadOnlyDictionary<string, POCatalog> TextCatalogs { get; private set; }

        void Initialize()
        {
            var cultures = _env.ContentRootFileProvider.GetDirectoryContents(BasePath)
                .Where(fi => fi.IsDirectory)
                .Select(fi => fi.Name)
                .ToArray();

            AvailableCultures = cultures.Select(c => new CultureInfo(c)).ToArray();

            var textCatalogFiles = cultures.SelectMany(
                c => _env.ContentRootFileProvider.GetDirectoryContents(Path.Combine(BasePath, c))
                    .Where(fi => !fi.IsDirectory && ".po".Equals(Path.GetExtension(fi.Name), StringComparison.OrdinalIgnoreCase)),
                (c, f) => (Culture: c, FileInfo: f));

            var textCatalogs = new List<(string FileName, string Culture, POCatalog Catalog)>();

            var parserSettings = new POParserSettings
            {
                SkipComments = true,
                SkipInfoHeaders = true,
            };

            Parallel.ForEach(textCatalogFiles,
                () => new POParser(parserSettings),
                (it, s, p) =>
                {
                    POParseResult result;
                    using (var stream = it.FileInfo.CreateReadStream())
                        result = p.Parse(new StreamReader(stream));

                    if (result.Success)
                    {
                        lock (textCatalogs)
                            textCatalogs.Add((it.FileInfo.Name, it.Culture, result.Catalog));
                    }
                    else
                        _logger.LogWarning("Translation file \"{FILE}\" has errors.", Path.Combine(BasePath, it.Culture, it.FileInfo.Name));

                    return p;
                },
                CachedDelegates.Noop<POParser>.Action);

            TextCatalogs = textCatalogs
                .GroupBy(it => it.Culture, it => (it.FileName, it.Catalog))
                .ToDictionary(g => g.Key, g => g
                    .OrderBy(it => it.FileName)
                    .Select(it => it.Catalog)
                    .Aggregate((acc, src) =>
                    {
                        foreach (var entry in src)
                            try { acc.Add(entry); }
                            catch (ArgumentException) { _logger.LogWarning("Multiple translations for key {KEY}.", POStringLocalizer.FormatKey(entry.Key)); }

                        return acc;
                    }));
        }
    }
}
