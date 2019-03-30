using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace Karambolo.AspNetCore.Bundling.Tools.Infrastructure
{
    // https://github.com/dotnet/cli/issues/4057
    public static class DependencyContextResolver
    {
        public static DependencyContext Resolve(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            string[] depsFiles = Directory.EnumerateFiles(path, "*.deps.json", SearchOption.TopDirectoryOnly).ToArray();
            if (depsFiles.Length == 0)
                throw new FileNotFoundException("No '.deps.json' file was found in the application path.");

            if (depsFiles.Length > 1)
                throw new FileNotFoundException("Multiple '.deps.json' file was found in the application path.");

            var depsFile = depsFiles[0];

            using (var reader = new DependencyContextJsonReader())
            using (FileStream stream = File.OpenRead(depsFile))
                return reader.Read(stream);
        }
    }
}
