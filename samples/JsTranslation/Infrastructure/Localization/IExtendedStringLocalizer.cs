using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JsTranslation.Infrastructure.Localization
{
    public interface IExtendedStringLocalizer : IStringLocalizer
    {
        bool TryGetTranslation(string name, object[] arguments, out string value);
        new IExtendedStringLocalizer WithCulture(CultureInfo culture);
    }
}