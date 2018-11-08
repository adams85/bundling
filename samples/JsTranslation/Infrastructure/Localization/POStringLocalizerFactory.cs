using System;
using System.Globalization;
using Karambolo.PO;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace JsTranslation.Infrastructure.Localization
{
    public class POStringLocalizerFactory : IStringLocalizerFactory
    {
        readonly IPOLocalizationProvider _localizationProvider;
        readonly ILoggerFactory _loggerFactory;

        public POStringLocalizerFactory(IPOLocalizationProvider localizationProvider, ILoggerFactory loggerFactory)
        {
            _localizationProvider = localizationProvider;
            _loggerFactory = loggerFactory;
        }

        POCatalog GetCatalogForCulture(CultureInfo culture)
        {
            while (culture != null && !culture.Equals(CultureInfo.InvariantCulture))
                if (_localizationProvider.TextCatalogs.TryGetValue(culture.Name, out var catalog))
                    return catalog;
                else
                    culture = culture.Parent;

            return null;
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            return new POStringLocalizer(CultureInfo.CurrentCulture, GetCatalogForCulture, _loggerFactory.CreateLogger<POStringLocalizer>());
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            return Create(null);
        }
    }
}
