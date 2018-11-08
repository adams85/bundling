using System;
using System.Collections.Generic;
using System.Globalization;
using Karambolo.Common;
using Karambolo.Common.Localization;
using Karambolo.PO;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace JsTranslation.Infrastructure.Localization
{
    public class POStringLocalizer : IExtendedStringLocalizer
    {
        public static string FormatKey(POKey key)
        {
            var result = string.Concat("'", key.Id, "'");
            if (key.PluralId != null)
                result = string.Concat(result, "-'", key.PluralId, "'");
            if (key.ContextId != null)
                result = string.Concat(result, "@'", key.ContextId, "'");

            return result;
        }

        readonly POCatalog _catalog;
        readonly Func<CultureInfo, POCatalog> _getCatalogForCulture;
        readonly ILogger<POStringLocalizer> _logger;

        public POStringLocalizer(CultureInfo culture, Func<CultureInfo, POCatalog> getCatalogForCulture, ILogger<POStringLocalizer> logger)
        {
            _catalog = getCatalogForCulture(culture);
            _getCatalogForCulture = getCatalogForCulture;
            _logger = logger;
        }

        public LocalizedString this[string name] => Localize(name, null);

        public LocalizedString this[string name, params object[] arguments] => Localize(name, arguments);

        LocalizedString Localize(string name, object[] arguments)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            bool notFound;
            if (notFound = !TryGetTranslation(name, arguments, out string translation) && translation == null)
                translation = name;

            var value = ArrayUtils.IsNullOrEmpty(arguments) ? translation : string.Format(translation, arguments);
            return new LocalizedString(name, value, notFound);
        }

        public bool TryGetTranslation(string name, object[] arguments, out string value)
        {
            var plural = default(Plural);
            var context = default(TextContext);

            if (!ArrayUtils.IsNullOrEmpty(arguments))
            {
                var pluralIndex = Array.FindIndex(arguments, a => a is Plural);
                if (pluralIndex >= 0)
                    plural = (Plural)arguments[pluralIndex];

                var contextIndex = arguments.Length - 1;
                object contextArg;
                if (pluralIndex != contextIndex && (contextArg = arguments[contextIndex]) is TextContext)
                    context = (TextContext)contextArg;
            }

            return TryGetTranslation(name, plural, context, out value);
        }

        bool TryGetTranslation(string name, Plural plural, TextContext context, out string value)
        {
            var key = new POKey(name, plural.Id, context.Id);
            if (!TryGetTranslation(key, plural.Count, out string translation))
            {
                _logger.LogTrace("No translation for key {KEY}.", POStringLocalizer.FormatKey(key));

                TryGetTranslationFallback(name, plural, context, out value);
                return false;
            }

            value = translation;
            return true;
        }

        bool TryGetTranslation(POKey key, int pluralCount, out string value)
        {
            if (_catalog != null)
            {
                var translation = _catalog.GetTranslation(key, pluralCount);
                if (translation != null)
                {
                    value = translation;
                    return true;
                }
            }

            value = null;
            return false;
        }

        bool TryGetTranslationFallback(string name, Plural plural, TextContext context, out string value)
        {
            value = plural.Id == null || plural.Count == 1 ? name : plural.Id;
            return true;
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotSupportedException();
        }

        public IExtendedStringLocalizer WithCulture(CultureInfo culture)
        {
            return new POStringLocalizer(culture, _getCatalogForCulture, _logger);
        }

        IStringLocalizer IStringLocalizer.WithCulture(CultureInfo culture)
        {
            return WithCulture(culture);
        }
    }
}