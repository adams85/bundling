using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;

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

        public static readonly PathString DefaultPathPrefix = "/wwwroot";

        private static PathString DefaultMapPath(string filePath, PathString pathPrefix, bool output)
        {
            var result = new PathString(filePath);

            if (result.StartsWithSegments(DefaultPathPrefix, out PathString remaining))
                result = remaining;

            if (output)
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

            BundleData[] items = SerializationHelper.Deserialize<BundleData[]>(reader);

            for (int i = 0, n = items.Length; i < n; i++)
            {
                BundleData item = items[i];

                PathString outputPath = pathMapper(UrlUtils.NormalizePath(item.OutputFileName), bundles.PathPrefix, output: true);
                if (!outputPath.HasValue)
                    throw ErrorHelper.PathMappingNotPossible(item.OutputFileName, nameof(pathMapper));

                var extension = Path.GetExtension(outputPath.Value);

                IBundleConfiguration outputConfig = _extensionMappers.Select(em => em.MapOutput(extension)).FirstOrDefault(cfg => cfg != null);
                if (outputConfig == null)
                    throw ErrorHelper.ExtensionNotRecognized(extension);

                var bundle = new Bundle(outputPath, outputConfig);
                var bundleSource = new FileBundleSource(bundles.SourceFileProvider, bundles.CaseSensitiveSourceFilePaths, bundle);

                bundle.Transforms = outputConfig.ConfigurationHelper.SetDefaultTransforms(bundle.Transforms);

                if (item.Minify == null ||
                    !item.Minify.Any(kvp => "enabled".Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) || kvp.Value is bool boolValue && boolValue))
                    bundle.Transforms = outputConfig.ConfigurationHelper.EnableMinification(bundle.Transforms);

                if (item.InputFiles != null)
                    for (int j = 0, m = item.InputFiles.Count; j < m; j++)
                    {
                        var inputFile = item.InputFiles[j];

                        bool exclude;
                        if (inputFile.StartsWith("!", StringComparison.Ordinal))
                        {
                            inputFile = inputFile.Substring(1);
                            exclude = true;
                        }
                        else
                            exclude = false;

                        PathString inputPath = pathMapper(UrlUtils.NormalizePath(inputFile), PathString.Empty, output: false);
                        if (!inputPath.HasValue)
                            throw ErrorHelper.PathMappingNotPossible(inputFile, nameof(pathMapper));

                        extension = Path.GetExtension(inputPath.Value);

                        IBundleConfiguration inputConfig = _extensionMappers.Select(em => em.MapInput(extension)).FirstOrDefault(cfg => cfg != null);
                        if (inputConfig == null)
                            throw ErrorHelper.ExtensionNotRecognized(extension);

                        var bundleSourceItem = new FileBundleSourceItem(inputPath.Value, bundleSource) { Exclude = exclude };

                        bundleSourceItem.ItemTransforms = inputConfig.ConfigurationHelper.SetDefaultItemTransforms(bundleSourceItem.ItemTransforms);

                        bundleSource.Items.Add(bundleSourceItem);
                    }

                bundle.Sources.Add(bundleSource);
                bundles.Add(bundle);
            }
        }
    }
}
