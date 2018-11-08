using System.Globalization;

namespace JsTranslation.Infrastructure.Localization
{
    public interface ILocalizationProvider
    {
        CultureInfo[] AvailableCultures { get; }
    }
}