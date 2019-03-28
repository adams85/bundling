using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.EcmaScript
{
    public sealed class ModuleFile : AbstractionFile
    {
        public ModuleFile()
            : this(NullFileProvider, null) { }

        public ModuleFile(IFileProvider fileProvider, string filePath, bool caseSensitiveFilePaths = true)
            : base(fileProvider, filePath, caseSensitiveFilePaths) { }

        public ModuleFile(ModuleFile other, string filePath) : this(other.FileProvider, filePath, other.CaseSensitiveFilePaths) { }

        /// <remarks>
        /// This property is not included in the equality check. It can be safely changed even when the instance is used a dictionary key.
        /// </remarks>
        public string Content { get; set; }
    }
}
