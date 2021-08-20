﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.Internal.DesignTime
{
#if NETSTANDARD2_0
    using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
    using IHostApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;
#else
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
#endif

    internal sealed class BundleBuilder
    {
        private sealed class HostingEnvironment : IWebHostEnvironment
        {
            public HostingEnvironment(PhysicalFileProvider outputFileProvider, string mode)
            {
                EnvironmentName = mode;
                ApplicationName = typeof(BundleBuilder).Assembly.GetName().Name;
                WebRootFileProvider = outputFileProvider;
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; }

            public string WebRootPath
            {
                get => (WebRootFileProvider as PhysicalFileProvider)?.Root;
                set => WebRootFileProvider = new PhysicalFileProvider(value);
            }

            public IFileProvider WebRootFileProvider { get; set; }

            public string ContentRootPath
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public IFileProvider ContentRootFileProvider
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
        }

        // DefaultBundleModelFactory schedules disposal of the bundle models for application shutdown;
        // we fake app shutdown using the cancellation token we get from the CLI tools
        // TODO: roll out a less ugly solution like introducing a dedicated interface?
        private sealed class Lifetime : IHostApplicationLifetime, IDisposable
        {
            private readonly CancellationTokenSource _lifetimeCts;
            private readonly CancellationTokenSource _linkedCts;

            public Lifetime(CancellationToken shutdownToken)
            {
                _lifetimeCts = new CancellationTokenSource();
                _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token, shutdownToken);

                ApplicationStopping = _linkedCts.Token;
            }

            public void Dispose()
            {
                _lifetimeCts.Cancel();

                _linkedCts.Dispose();
                _lifetimeCts.Dispose();
            }

            public CancellationToken ApplicationStarted => throw new NotSupportedException();
            public CancellationToken ApplicationStopping { get; }
            public CancellationToken ApplicationStopped => throw new NotSupportedException();

            public void StopApplication() => throw new NotSupportedException();
        }

        private static IServiceProvider BuildServiceProvider(DesignTimeBundlingConfiguration configuration, Action<int, string> logger, string mode,
            PhysicalFileProvider outputFileProvider, CancellationToken shutdownToken)
        {
            var services = new ServiceCollection();

            services
                .AddOptions()
                .AddLogging(builder =>
                {
                    // log filtering is performed by ConsoleReporter in the CLI
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddProvider(new LoggerProxyProvider(logger));
                });

            BundlingConfigurer configurer = services.AddBundlingCore((options, _) => options.Merge(configuration));

            services.AddSingleton<BundleBuilder>();

            services.AddSingleton<IWebHostEnvironment>(new HostingEnvironment(outputFileProvider, mode));
            services.AddSingleton<IHostApplicationLifetime>(new Lifetime(shutdownToken));

            if (mode == "Production")
                configurer.EnableMinification();

            if (configuration.Modules != null)
                foreach (IBundlingModule module in configuration.Modules)
                    module.Configure(configurer);

            return services.BuildServiceProvider();
        }

        private static string GetDefaultOutputBasePath(string projectDirPath)
        {
            var webRootPath = Path.Combine(projectDirPath, "wwwroot");
            return Directory.Exists(webRootPath) ? webRootPath : projectDirPath;
        }

        private static string EnsureOutputFilePath(string outputBasePath, PathString bundlesPath, PathString bundlePath)
        {
            var bundleFilePath = Path.Combine(outputBasePath,
                bundlesPath.HasValue ? bundlesPath.Value.Substring(1).Replace('/', Path.DirectorySeparatorChar) : string.Empty,
                bundlePath.Value.Substring(1).Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(bundleFilePath));
            return bundleFilePath;
        }

        private static IEnumerable<IBundlingModule> DiscoverModules(string compilationBasePath)
        {
            IEnumerable<string> wellKnownModuleAssemblies = typeof(BundleBuilder).Assembly.GetCustomAttributes<InternalsVisibleToAttribute>()
                .Select(attribute => attribute.AssemblyName)
                .Where(assemblyName => !assemblyName.EndsWith(".Test", StringComparison.Ordinal));

            IEnumerable<string> localModuleAssemblies = Directory.EnumerateFiles(compilationBasePath, "Karambolo.AspNetCore.Bundling.*.dll", SearchOption.TopDirectoryOnly)
                .Select(filePath => Path.ChangeExtension(Path.GetFileName(filePath), null));

            return new HashSet<string>(wellKnownModuleAssemblies.Concat(localModuleAssemblies), StringComparer.OrdinalIgnoreCase)
                .SelectMany(assemblyName =>
                {
                    Assembly assembly;

                    try { assembly = Assembly.Load(new AssemblyName(assemblyName)); }
                    catch (FileNotFoundException) { return Type.EmptyTypes; }

                    return assembly.GetTypes();
                })
                .Where(type =>
                    type.GetInterfaces().Any(intfType => intfType == typeof(IBundlingModule) &&
                    type.IsClass && !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null))
                .Select(type => (IBundlingModule)Activator.CreateInstance(type));
        }

        private static void SetupConfigFileConfiguration(ConfigFileConfiguration configuration, string configFilePath, string compilationBasePath)
        {
            configuration.ConfigFilePath = configFilePath;
            configuration.AddModules(DiscoverModules(compilationBasePath));
        }

        public static async Task ProcessAsync<TConfiguration>(Dictionary<string, object> settings, CancellationToken shutdownToken)
            where TConfiguration : DesignTimeBundlingConfiguration, new()
        {
            var configuration = new TConfiguration();

            if (configuration is ConfigFileConfiguration configFileConfiguration)
                SetupConfigFileConfiguration(configFileConfiguration, (string)settings["ConfigFilePath"], (string)settings["CompilationBasePath"]);

            var projectDirPath = settings.TryGetValue("ProjectDirPath", out var valueObj) ?
                (string)valueObj :
                (string)settings["ProjectFilePath"]; // for backward compatibility

            var loggerAction = (Action<int, string>)settings["Logger"];

            // TODO: outputBasePath from CLI tools?
            var outputBasePath = configuration.OutputBasePath ?? GetDefaultOutputBasePath(projectDirPath);

            var outputFileProvider = new PhysicalFileProvider(outputBasePath);

            IServiceProvider serviceProvider = BuildServiceProvider(configuration, loggerAction, (string)settings["Mode"], outputFileProvider, shutdownToken);

            using (serviceProvider.GetRequiredService<IHostApplicationLifetime>() as IDisposable)
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                IFileProvider sourceFileProvider = configuration.SourceFileProvider ?? outputFileProvider;

                var bundles = new BundleCollection(
                    configuration.BundlesPathPrefix ?? BundlingConfiguration.DefaultBundlesPathPrefix,
                    sourceFileProvider,
                    configuration.CaseSensitiveSourceFilePaths ?? AbstractionFile.GetDefaultCaseSensitiveFilePaths(sourceFileProvider));

                configuration.Configure(new BundleCollectionConfigurer(bundles, scope.ServiceProvider));

                var bundlingContext = new BundlingContext
                {
                    BundlesPathPrefix = configuration.BundlesPathPrefix ?? BundlingConfiguration.DefaultBundlesPathPrefix,
                    StaticFilesPathPrefix = configuration.StaticFilesPathPrefix ?? PathString.Empty,
                };

                BundleBuilder bundleBuilder = scope.ServiceProvider.GetRequiredService<BundleBuilder>();

                await bundleBuilder.ProduceBundlesAsync(bundles, configuration.AppBasePath ?? PathString.Empty, bundlingContext, outputBasePath, shutdownToken);
            }
        }

        private readonly IEnumerable<IBundleModelFactory> _modelFactories;
        private readonly ILogger _logger;

        public BundleBuilder(IEnumerable<IBundleModelFactory> modelFactories, ILogger<BundleBuilder> logger)
        {
            _modelFactories = modelFactories;
            _logger = logger;
        }

        private IBundleModel CreateModel(Bundle bundle)
        {
            return
                _modelFactories.Select(f => f.Create(bundle)).FirstOrDefault(m => m != null) ??
                throw ErrorHelper.ModelFactoryNotAvailable(bundle.GetType());
        }

        private async Task<string> BuildBundleAsync(IBundleModel bundle, PathString appBasePath, BundlingContext bundlingContext, CancellationToken shutdownToken)
        {
            // TODO: support params?
            var builderContext = new BundleBuilderContext
            {
                BundlingContext = bundlingContext,
                AppBasePath = appBasePath,
                Bundle = bundle,
                CancellationToken = shutdownToken
            };

            bundle.OnBuilding(builderContext);

            await bundle.Builder.BuildAsync(builderContext);

            bundle.OnBuilt(builderContext);

            return builderContext.Result;
        }

        private async Task ProduceBundlesAsync(BundleCollection bundles, string appBasePath, BundlingContext bundlingContext, string outputBasePath, CancellationToken shutdownToken)
        {
            IBundleModel[] bundleModels = bundles.Select(CreateModel).ToArray();

            foreach (IBundleModel bundle in bundleModels)
            {
                var outputFilePath = EnsureOutputFilePath(outputBasePath, bundlingContext.BundlesPathPrefix, bundle.Path);

                using (var writer = new StreamWriter(outputFilePath, append: false, encoding: bundle.OutputEncoding))
                {
                    long startTicks = Stopwatch.GetTimestamp();

                    string bundleContent;
                    try { bundleContent = await BuildBundleAsync(bundle, appBasePath, bundlingContext, shutdownToken); }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Bundle '{PATH}' was not built. Build was cancelled.", bundle.Path);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Bundle '{PATH}' was not built. Build failed.", bundle.Path);
                        if (ex is BundlingErrorException)
                            throw new BundlingErrorException($"Bundle '{bundle.Path}' could not be built.");
                        else
                            throw;
                    }

                    long endTicks = Stopwatch.GetTimestamp();

                    long elapsedMs = (endTicks - startTicks) / (Stopwatch.Frequency / 1000);
                    _logger.LogInformation("Bundle '{PATH}' was built in {ELAPSED}ms.", bundle.Path, elapsedMs);

                    await writer.WriteAsync(bundleContent);
                    await writer.FlushAsync();

                    _logger.LogInformation("Bundle '{PATH}' was written to {FILEPATH}", bundle.Path, outputFilePath);
                }
            }
        }
    }
}
