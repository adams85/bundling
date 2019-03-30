using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Karambolo.AspNetCore.Bundling.Tools.Infrastructure
{
    // https://stackoverflow.com/questions/37895278/how-to-load-assemblies-located-in-a-folder-in-net-core-console-app
    // https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/assemblyloadcontext.md
    // https://natemcmaster.com/blog/2018/07/25/netcore-plugins/
    public class AssemblyLoader : AssemblyLoadContext
    {
        private readonly DependencyContext _dependencyContext;
        private readonly ICompilationAssemblyResolver _assemblyResolver;
        private readonly Func<AssemblyName, Assembly> _onResolve;
        private readonly ConcurrentDictionary<string, IntPtr> _nativeLibraries;

        public AssemblyLoader(string basePath, Func<AssemblyName, Assembly> onResolve = null)
        {
            BasePath = basePath;

            _onResolve = onResolve ?? (_ => null);
            if (basePath != null)
            {
                _dependencyContext = DependencyContextResolver.Resolve(basePath);
                _assemblyResolver = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
                {
                    new AppBaseCompilationAssemblyResolver(basePath),
                    new ReferenceAssemblyPathResolver(),
                    new PackageCompilationAssemblyResolver()
                });
            }
            else
            {
                _dependencyContext = DependencyContext.Default;
                _assemblyResolver = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
                {
                    new ReferenceAssemblyPathResolver(),
                    new PackageCompilationAssemblyResolver()
                });
            }

            _nativeLibraries = new ConcurrentDictionary<string, IntPtr>();
        }

        public string BasePath { get; }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            Assembly assembly = _onResolve(assemblyName);
            if (assembly != null)
                return assembly;

            RuntimeLibrary runtimeLibrary = _dependencyContext.RuntimeLibraries.FirstOrDefault(
                lib => string.Equals(lib.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

            CompilationLibrary library;
            if (runtimeLibrary != null)
            {
                library = new CompilationLibrary(
                    runtimeLibrary.Type,
                    runtimeLibrary.Name,
                    runtimeLibrary.Version,
                    runtimeLibrary.Hash,
                    runtimeLibrary.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                    runtimeLibrary.Dependencies,
                    runtimeLibrary.Serviceable);
            }
            else
                library = _dependencyContext.CompileLibraries.FirstOrDefault(lib => string.Equals(lib.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

            if (library == null)
                return null;

            var assemblies = new List<string>();

            _assemblyResolver.TryResolveAssemblyPaths(library, assemblies);

            return assemblies.Count > 0 ? LoadFromAssemblyPath(assemblies[0]) : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            return _nativeLibraries.GetOrAdd(unmanagedDllName, key =>
            {
                string architecture, extension;
                if ((architecture = GetArchitecture()) == null ||
                    (extension = GetExtension()) == null)
                    return default;

                var unmanagedDllPath = Path.Combine(BasePath, architecture, Path.ChangeExtension(key, extension));
                return File.Exists(unmanagedDllPath) ? LoadUnmanagedDllFromPath(unmanagedDllPath) : default;
            });

            string GetArchitecture()
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86: return "x86";
                    case Architecture.X64: return "x64";
                    case Architecture.Arm: return "arm";
                    case Architecture.Arm64: return "arm64";
                    default: return null;
                }
            }

            string GetExtension()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return ".dll";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return ".so";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return ".dylib";
                else
                    return null;
            }
        }
    }
}
