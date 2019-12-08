using System;

namespace Microsoft.DotNet.Cli.CommandLine
{
    internal static partial class CommandLineApplicationExtensions
    {
        public static bool TryParse<TEnum>(this CommandOption option, TEnum defaultValue, out TEnum result)
            where TEnum : struct, Enum
        {
            if (option.Value() == null)
            {
                result = defaultValue;
                return true;
            }

            return Enum.TryParse(option.Value(), ignoreCase: true, result: out result);
        }

        public static string GetEnumValues<TEnum>(this CommandLineApplication app)
            where TEnum : struct, Enum
        {
            return string.Join(", ", Enum.GetNames(typeof(TEnum)));
        }
    }
}
