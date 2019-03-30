using System;
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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.Internal.DesignTime
{
    internal class BundleBuilder
    {
        private class HostingEnvironment : IHostingEnvironment
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
        private class Lifetime : IApplicationLifetime
        {
            public Lifetime(CancellationToken cancellationToken)
            {
                ApplicationStopping = cancellationToken;
            }

            public CancellationToken ApplicationStarted => throw new NotSupportedException();
            public CancellationToken ApplicationStopping { get; }
            public CancellationToken ApplicationStopped => throw new NotSupportedException();
            public void StopApplication() => throw new NotSupportedException();
        }

        private static IServiceProvider BuildServiceProvider(DesignTimeBundlingConfiguration configuration, Action<int, string> logger, string mode, 
            PhysicalFileProvider outputFileProvider, CancellationToken cancellationToken)
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

            services.AddSingleton<IHostingEnvironment>(new HostingEnvironment(outputFileProvider, mode));
            services.AddSingleton<IApplicationLifetime>(new Lifetime(cancellationToken));

            configurer
                .AddCss()
                .AddJs();

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
                .Where(assemblyName => !assemblyName.EndsWith(".Test"));

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
            configuration.SetModules(DiscoverModules(compilationBasePath));
        }

        public static async Task ProcessAsync<TConfiguration>(Dictionary<string, object> settings, CancellationToken cancellationToken)
            where TConfiguration : DesignTimeBundlingConfiguration, new()
        {
            var configuration = new TConfiguration();

            if (configuration is ConfigFileConfiguration)
                SetupConfigFileConfiguration((ConfigFileConfiguration)(object)configuration, (string)settings["ConfigFilePath"], (string)settings["CompilationBasePath"]);

            var projectFilePath = (string)settings["ProjectFilePath"];
            var loggerAction = (Action<int, string>)settings["Logger"];

            // TODO: outputBasePath from CLI tools?
            var outputBasePath = configuration.OutputBasePath ?? GetDefaultOutputBasePath(Path.GetDirectoryName(projectFilePath));

            var outputFileProvider = new PhysicalFileProvider(outputBasePath);

            IServiceProvider serviceProvider = BuildServiceProvider(configuration, loggerAction, (string)settings["Mode"], outputFileProvider, cancellationToken);
            using (serviceProvider as IDisposable)
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var bundles = new BundleCollection(
                    configuration.BundlesPathPrefix ?? BundlingConfiguration.DefaultBundlesPathPrefix,
                    configuration.SourceFileProvider ?? outputFileProvider,
                    configuration.CaseSensitiveSourceFilePaths ?? AbstractionFile.GetDefaultCaseSensitiveFilePaths());

                configuration.Configure(new BundleCollectionConfigurer(bundles, scope.ServiceProvider));

                var bundlingContext = new BundlingContext
                {
                    BundlesPathPrefix = configuration.BundlesPathPrefix ?? BundlingConfiguration.DefaultBundlesPathPrefix,
                    StaticFilesPathPrefix = configuration.StaticFilesPathPrefix ?? PathString.Empty,
                };

                BundleBuilder bundleBuilder = scope.ServiceProvider.GetRequiredService<BundleBuilder>();

                await bundleBuilder.ProduceBundlesAsync(bundles, configuration.AppBasePath ?? PathString.Empty, bundlingContext, outputBasePath, cancellationToken);
            }

            await Task.CompletedTask;
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

        private async Task BuildBundleAsync(TextWriter writer, IBundleModel bundle, PathString appBasePath, BundlingContext bundlingContext, CancellationToken cancellationToken)
        {
            var startTicks = Stopwatch.GetTimestamp();

            // TODO: support params?
            var builderContext = new BundleBuilderContext
            {
                BundlingContext = bundlingContext,
                AppBasePath = appBasePath,
                Bundle = bundle,
                CancellationToken = cancellationToken
            };

            bundle.OnBuilding(builderContext);

            await bundle.Builder.BuildAsync(builderContext);

            await writer.WriteAsync(builderContext.Result);
            await writer.FlushAsync();

            bundle.OnBuilt(builderContext);

            var endTicks = Stopwatch.GetTimestamp();

            var elapsedMs = (endTicks - startTicks) / (Stopwatch.Frequency / 1000);
            _logger.LogInformation("Bundle {PATH} was built in {ELAPSED}ms.", bundle.Path, elapsedMs);
        }

        private async Task ProduceBundlesAsync(BundleCollection bundles, string appBasePath, BundlingContext bundlingContext, string outputBasePath, CancellationToken cancellationToken)
        {
            IBundleModel[] bundleModels = bundles.Select(CreateModel).ToArray();

            foreach (IBundleModel bundle in bundleModels)
            {
                var outputFilePath = EnsureOutputFilePath(outputBasePath, bundlingContext.BundlesPathPrefix, bundle.Path);

                using (var writer = new StreamWriter(outputFilePath, append: false, encoding: bundle.OutputEncoding))
                    await BuildBundleAsync(writer, bundle, appBasePath, bundlingContext, cancellationToken);

                _logger.LogInformation("Bundle {PATH} was written to {FILEPATH}", bundle.Path, outputFilePath);
            }
        }
    }
}
