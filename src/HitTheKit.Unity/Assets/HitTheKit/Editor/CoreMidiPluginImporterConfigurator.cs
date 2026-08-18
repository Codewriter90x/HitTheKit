using UnityEditor;

namespace HitTheKit.Unity.EditorTools
{
    public sealed class CoreMidiPluginImporterConfigurator : AssetPostprocessor
    {
        private const string PluginPath = "Assets/Plugins/macOS/HitTheKitCoreMidi.dylib";

        private void OnPreprocessAsset()
        {
            if (assetPath != PluginPath || !(assetImporter is PluginImporter importer)) return;
            Configure(importer);
        }

        [MenuItem("HitTheKit/Configure CoreMIDI Plug-in Importer")]
        public static void ConfigureGeneratedPlugin()
        {
            if (!(AssetImporter.GetAtPath(PluginPath) is PluginImporter importer)) return;
            Configure(importer);
            importer.SaveAndReimport();
        }

        internal static void Configure(PluginImporter importer)
        {
            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(true);
            importer.SetEditorData("OS", "OSX");
            importer.SetEditorData("CPU", "ARM64");
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, true);
            importer.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "ARM64");
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
        }
    }
}
