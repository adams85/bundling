using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Karambolo.AspNetCore.Bundling.Internal.Configuration
{
    public interface IConfigFileManager
    {
        void Load(BundleCollection bundles, TextReader reader, ConfigFilePathMapper pathMapper = null);
    }

    public delegate PathString ConfigFilePathMapper(string filePath, PathString pathPrefix, bool output);

    public class ConfigFileManager : IConfigFileManager
    {
        private readonly IEnumerable<IExtensionMapper> _extensionMappers;

        public ConfigFileManager(IEnumerable<IExtensionMapper> extensionMappers)
        {
            if (extensionMappers == null)
                throw new ArgumentNullException(nameof(extensionMappers));

            _extensionMappers = extensionMappers;
        }

        public const string DefaultPathPrefix = "/wwwroot";

        private static PathString DefaultMapPath(string filePath, PathString pathPrefix, bool output)
        {
            PathString result = filePath;

            if (result.StartsWithSegments(DefaultPathPrefix, out result) && output)
                result.StartsWithSegments(pathPrefix, out result);

            return result;
        }

        public void Load(BundleCollection bundles, TextReader reader, ConfigFilePathMapper pathMapper)
        {
            if (bundles == null)
                throw new ArgumentNullException(nameof(bundles));

            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (bundles.SourceFileProvider == null)
                throw ErrorHelper.PropertyCannotBeNull(nameof(bundles), nameof(bundles.SourceFileProvider));

            if (pathMapper == null)
                pathMapper = DefaultMapPath;

            var serializer = JsonSerializer.CreateDefault();
            BundleData[] items = SerializationHelper.Deserialize<BundleData[]>(reader);

            var n = items.Length;
            for (var i = 0; i < n; i++)
            {
                BundleData item = items[i];

                PathString outputPath = pathMapper(UrlUtils.NormalizePath(item.OutputFileName), bundles.PathPrefix, output: true);
                if (!outputPath.HasValue)
                    throw ErrorHelper.PathMappingNotPossible(item.OutputFileName, nameof(pathMapper));

                var extension = Path.GetExtension(outputPath);

                IBundleConfiguration outputConfig = _extensionMappers.Select(em => em.MapOutput(extension)).FirstOrDefault(cfg => cfg != null);
                if (outputConfig == null)
                    throw ErrorHelper.ExtensionNotRecognized(extension);

                var bundle = new Bundle(outputPath, outputConfig);
                var bundleSource = new FileBundleSource(bundles.SourceFileProvider, bundles.CaseSensitiveSourceFilePaths, bundle);

                bundle.Transforms = outputConfig.ConfigurationHelper.SetDefaultTransforms(bundle.Transforms);

                if (item.Minify.Any(kvp => "enabled".Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) && kvp.Value is bool boolValue && boolValue))
                    bundle.Transforms = outputConfig.ConfigurationHelper.EnableMinification(bundle.Transforms);

                var m = item.InputFiles.Count;
                for (var j = 0; j < m; j++)
                {
                    var inputFile = item.InputFiles[j];

                    bool exclude;
                    if (inputFile.StartsWith("!"))
                    {
                        inputFile = inputFile.Substring(1);
                        exclude = true;
                    }
                    else
                        exclude = false;

                    PathString inputPath = pathMapper(UrlUtils.NormalizePath(inputFile), PathString.Empty, output: false);
                    extension = Path.GetExtension(inputPath);

                    IBundleConfiguration inputConfig = _extensionMappers.Select(em => em.MapInput(extension)).FirstOrDefault(cfg => cfg != null);
                    if (inputConfig == null)
                        throw ErrorHelper.ExtensionNotRecognized(extension);

                    var bundleSourceItem = new FileBundleSourceItem(inputPath, bundleSource) { Exclude = exclude };

                    bundleSourceItem.ItemTransforms = inputConfig.ConfigurationHelper.SetDefaultItemTransforms(bundleSourceItem.ItemTransforms);

                    bundleSource.Items.Add(bundleSourceItem);
                }

                bundle.Sources.Add(bundleSource);
                bundles.Add(bundle);
            }
        }
    }
}
