using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HitTheKit.Unity.Devices;
using UnityEngine;

namespace HitTheKit.Unity.DeviceSetup
{
    public sealed class AtomicUserKitConfigurationStore : IUserKitConfigurationStore
    {
        private const long MaximumDocumentBytes = 256 * 1024;
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false, true);
        private readonly string path;
        private readonly UserKitConfigurationSerializer serializer = new UserKitConfigurationSerializer();
        private readonly UserKitConfigurationLoader loader = new UserKitConfigurationLoader();

        public AtomicUserKitConfigurationStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A configuration path is required.", nameof(path));
            this.path = Path.GetFullPath(path);
        }

        public void Save(UserKitConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            byte[] bytes = Utf8NoBom.GetBytes(serializer.Serialize(configuration));
            if (bytes.Length > MaximumDocumentBytes)
                throw new InvalidOperationException("Kit configuration exceeds the maximum supported size.");

            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Kit configuration path requires a directory.");
            Directory.CreateDirectory(directory);
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, bytes);
                if (File.Exists(path)) File.Replace(temporaryPath, path, null);
                else File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        public bool TryLoad(string configurationId, out UserKitConfiguration configuration)
        {
            configuration = null;
            if (!File.Exists(path)) return false;
            var info = new FileInfo(path);
            if (info.Length <= 0 || info.Length > MaximumDocumentBytes)
                throw new InvalidOperationException("Saved kit configuration size is outside the supported range.");
            UserKitConfiguration loaded = loader.Load(File.ReadAllText(path, Utf8NoBom));
            if (!string.Equals(loaded.ConfigurationId, configurationId, StringComparison.Ordinal)) return false;
            configuration = loaded;
            return true;
        }

        public IReadOnlyList<UserKitConfiguration> List()
        {
            if (!File.Exists(path)) return Array.AsReadOnly(Array.Empty<UserKitConfiguration>());
            var info = new FileInfo(path);
            if (info.Length <= 0 || info.Length > MaximumDocumentBytes)
                throw new InvalidOperationException("Saved kit configuration size is outside the supported range.");
            return Array.AsReadOnly(new[] { loader.Load(File.ReadAllText(path, Utf8NoBom)) });
        }
    }

    public static class DeviceSetupConfigurationRuntime
    {
        public const string ConfigurationId = "user.device-setup";
        public static string DefaultPath => Path.Combine(
            Application.persistentDataPath,
            "HitTheKit",
            "device-configuration.json");

        public static AtomicUserKitConfigurationStore CreateDefaultStore() =>
            new AtomicUserKitConfigurationStore(DefaultPath);

        public static void DeleteSavedConfiguration()
        {
            if (File.Exists(DefaultPath)) File.Delete(DefaultPath);
        }

        public static bool TryLoadComplete(out UserKitConfiguration configuration)
        {
            try
            {
                if (CreateDefaultStore().TryLoad(ConfigurationId, out configuration) && configuration.IsComplete)
                    return true;
            }
            catch (Exception)
            {
                // A corrupt or obsolete local file must not prevent keyboard/generic MIDI play.
            }
            configuration = null;
            return false;
        }
    }
}
