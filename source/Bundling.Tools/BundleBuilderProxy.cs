using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Tools.Infrastructure;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    internal class BundleBuilderProxy
    {
        public const string BundlingAssemblyName = "Karambolo.AspNetCore.Bundling";

        private readonly AssemblyLoadContext _assemblyLoadContext;
        private readonly Type _bundlingConfigurationType;
        private readonly Type _configFileConfigurationType;
        private readonly MethodInfo _processMethodDefinition;
        private readonly IReporter _reporter;

        public BundleBuilderProxy(AssemblyLoadContext assemblyLoadContext, IReporter reporter)
        {
            if (assemblyLoadContext == null)
                throw new ArgumentNullException(nameof(assemblyLoadContext));

            if (reporter == null)
                throw new ArgumentNullException(nameof(reporter));

            _assemblyLoadContext = assemblyLoadContext;
            _reporter = reporter;

            Assembly bundlingAssembly;
            try { bundlingAssembly = assemblyLoadContext.LoadFromAssemblyName(new AssemblyName(BundlingAssemblyName)); }
            catch (Exception ex) { throw new InvalidOperationException($"Failed to load the {BundlingAssemblyName} assembly.", ex); }

            Type bundleBuilderClass;
            if ((_bundlingConfigurationType = bundlingAssembly.GetType("Karambolo.AspNetCore.Bundling.DesignTimeBundlingConfiguration", throwOnError: false, ignoreCase: false)) == null ||
                (_configFileConfigurationType = bundlingAssembly.GetType("Karambolo.AspNetCore.Bundling.Internal.DesignTime.ConfigFileConfiguration", throwOnError: false, ignoreCase: false)) == null ||
                (bundleBuilderClass = bundlingAssembly.GetType("Karambolo.AspNetCore.Bundling.Internal.DesignTime.BundleBuilder", throwOnError: false, ignoreCase: false)) == null ||
                (_processMethodDefinition = bundleBuilderClass.GetMethod("ProcessAsync",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(Dictionary<string, object>), typeof(CancellationToken) },
                    null)) == null)
            {
                throw new NotSupportedException($"The version of {BundlingAssemblyName} referenced by the application does not support design-time bundling. Please update the bundling library's NuGet packages and CLI tools to the latest version.");
            }
        }

        private void ScanAssemblyForConfigurations(List<Type> configurationTypes, string assemblyPath)
        {
            Assembly assembly = _assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);

            IEnumerable<Type> types = assembly.GetTypes()
                .Where(type =>
                    type.IsSubclassOf(_bundlingConfigurationType) &&
                    type.IsClass && !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null);

            configurationTypes.AddRange(types);
        }

        private async Task ProcessConfigurationAsync(Type configurationType, Dictionary<string, object> settings, CancellationToken cancellationToken)
        {
            MethodInfo buildMethod = _processMethodDefinition.MakeGenericMethod(configurationType);

            try { await (Task)buildMethod.Invoke(null, new object[] { settings, cancellationToken }); }
            catch (TargetInvocationException ex) when (ex.InnerException != null) { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); }
        }

        public async Task ProcessConfigurationsAsync(IEnumerable<string> assemblyFilePaths, Dictionary<string, object> settings, CancellationToken cancellationToken)
        {
            if (assemblyFilePaths == null)
                throw new ArgumentNullException(nameof(assemblyFilePaths));

            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var configurationTypes = new List<Type>();

            if (settings.TryGetValue("ConfigFilePath", out object configFilePath))
                configurationTypes.Add(_configFileConfigurationType);

            foreach (var path in assemblyFilePaths)
                ScanAssemblyForConfigurations(configurationTypes, path);

            _reporter.Output($"Found {configurationTypes.Count} bundling configuration(s).");

            if (configurationTypes.Count > 0)
            {
                var startTicks = Stopwatch.GetTimestamp();

                // TODO: parallelize?
                for (int i = 0, n = configurationTypes.Count; i < n; i++)
                {
                    Type configurationType = configurationTypes[i];

                    _reporter.Output(string.Empty);
                    _reporter.Output($"Processing configuration '{(configurationType == _configFileConfigurationType ? configFilePath : configurationType)}'...");

                    await ProcessConfigurationAsync(configurationType, settings, cancellationToken);
                }

                var endTicks = Stopwatch.GetTimestamp();

                var elapsedMs = (endTicks - startTicks) / (Stopwatch.Frequency / 1000);

                _reporter.Output(string.Empty);
                _reporter.Output($"Finished bundling in {elapsedMs}ms.");
            }
        }
    }
}
