namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        internal readonly struct ExportName
        {
            public static readonly ExportName None = default;
            public static readonly ExportName Default = new ExportName("default");

            public ExportName(string value, string rawValue = null)
            {
                Value = value;
                RawValue = rawValue;
            }

            public string Value { get; }
            public bool HasValue => Value != null;

            public string RawValue { get; } // unescaped string literal value in the case of literals
            public bool IsLiteral => RawValue != null;
        }
    }
}
