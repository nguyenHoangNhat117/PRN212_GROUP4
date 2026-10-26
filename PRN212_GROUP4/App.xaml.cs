using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace PRN212_GROUP4
{
    public partial class App : Application
    {
        static App()
        {
            var culture = new CultureInfo("vi-VN");

            culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
        }
    }
}