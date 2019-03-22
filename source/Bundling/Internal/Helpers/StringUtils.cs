namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    internal static class StringUtils
    {
        public static string RemoveQuotes(ref string value)
        {
            if (value.StartsWith("'"))
                if (value.EndsWith("'"))
                {
                    value = value.Substring(1, value.Length - 2);
                    return "'";
                }
                else
                    return null;

            if (value.StartsWith("\""))
                if (value.EndsWith("\""))
                {
                    value = value.Substring(1, value.Length - 2);
                    return "\"";
                }
                else
                    return null;

            return string.Empty;
        }
    }
}
