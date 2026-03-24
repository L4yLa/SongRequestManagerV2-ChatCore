using BGLib.Polyglot;
using Zenject;

namespace SongRequestManagerV2.Localizes
{
    /// <summary>
    /// SiraLocalizer復活したらこっちでの実装も考える。
    /// </summary>
    public class LocalizeController : IInitializable
    {
        public void Initialize()
        {
            // ILocalizer (SiraUtil.Interfaces) was removed in SiraUtil 3.x.
            // Use BGLib.Polyglot.LocalizationImporter directly.
            var stream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("SongRequestManagerV2.Resources.localize.csv");
            if (stream != null) {
                using var reader = new System.IO.StreamReader(stream);
                LocalizationImporter.ImportDataFromCSV(reader.ReadToEnd(), LocalizationImporter.GetLanguageCodes());
            }
        }
    }
}