using System.Collections.Generic;
using System.Text;
using Karambolo.AspNetCore.Bundling;
using Karambolo.Common;
using Microsoft.Extensions.Localization;

namespace JsTranslation.Infrastructure.Bundling
{
    // this is a crude implementation for replacing js strings with localized texts looked up through IStringLocalizer
    // it includes basic support for escaping but tries to localize every js string indiscriminately
    public class JsTranslatorTransform : BundleItemTransform
    {
        static int FindNextString(string content, ref int endIndex)
        {
            var quote = '"';
            var startIndex = content.IndexOf(quote, endIndex + 1);

            if (startIndex < 0)
            {
                quote = '\'';
                startIndex = content.IndexOf(quote, endIndex + 1);
            }

            if (startIndex < 0)
                return -1;

            endIndex = content.IndexOfEscaped('\\', quote, startIndex + 1);

            if (endIndex < 0)
                return -1;

            return startIndex;
        }

        readonly IStringLocalizer _stringLocalizer;

        public JsTranslatorTransform(IStringLocalizer stringLocalizer)
        {
            _stringLocalizer = stringLocalizer;
        }

        public override void Transform(IBundleItemTransformContext context)
        {
            int startIndex, endIndex = -1;

            var stringLocations = new List<(int startIndex, int count)>();
            while ((startIndex = FindNextString(context.Content, ref endIndex)) >= 0)
                stringLocations.Add((startIndex + 1, endIndex - startIndex - 1));

            if (stringLocations.Count == 0)
                return;

            var sb = new StringBuilder(context.Content);

            for (var i = stringLocations.Count - 1; i >= 0; i--)
            {
                var stringLocation = stringLocations[i];

                sb.Remove(stringLocation.startIndex, stringLocation.count);

                var value = context.Content
                    .Substring(stringLocation.startIndex, stringLocation.count)
                    .Unescape('\\', '\'', '"');

                value = _stringLocalizer[value].Value.Escape('\\', '\'', '"');

                sb.Insert(stringLocation.startIndex, value);
            }

            context.Content = sb.ToString();
        }
    }
}
