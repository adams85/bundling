using System.Collections.Generic;
using Karambolo.PO;

namespace JsTranslation.Infrastructure.Localization
{
    public interface IPOLocalizationProvider : ILocalizationProvider
    {
        IReadOnlyDictionary<string, POCatalog> TextCatalogs { get; }
    }
}