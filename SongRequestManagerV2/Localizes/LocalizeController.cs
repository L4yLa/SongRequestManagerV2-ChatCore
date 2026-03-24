using System;
using System.Collections.Generic;
using BGLib.Polyglot;
using UnityEngine;
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
            var stream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("SongRequestManagerV2.Resources.localize.csv");
            if (stream != null) {
                using var reader = new System.IO.StreamReader(stream);
                var csvText = reader.ReadToEnd();
                var localizationAsset = (LocalizationAsset)Activator.CreateInstance(
                    typeof(LocalizationAsset),
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                    null,
                    new object[] { new TextAsset(csvText) },
                    null);
                var inputFiles = new List<LocalizationAsset>(Localization.Instance.inputFiles)
                {
                    localizationAsset
                };
                LocalizationImporter.ImportFromFiles(inputFiles);
            }
        }
    }
}