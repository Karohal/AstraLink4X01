using System;
using System.IO;
using Newtonsoft.Json;

namespace Game.Core
{
    public interface ISaveLoadService
    {
        void Save(GameStateSnapshot state, string slotName);
        GameStateSnapshot Load(string slotName);
    }

    public sealed class UnsupportedSaveVersionException : Exception
    {
        public UnsupportedSaveVersionException(int foundVersion, int supportedVersion)
            : base($"Save schemaVersion {foundVersion} is not supported (expected {supportedVersion}).")
        {
        }
    }

    // Le dossier de base (Application.persistentDataPath en jeu réel) est injecté plutôt que lu
    // directement, pour que cette classe reste indépendante d'Unity et testable en EditMode
    // (Principe II) sans scène chargée.
    public sealed class SaveLoadService : ISaveLoadService
    {
        public const int CurrentSchemaVersion = 1;

        private readonly string _baseDirectory;

        public SaveLoadService(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            Directory.CreateDirectory(_baseDirectory);
        }

        public void Save(GameStateSnapshot state, string slotName)
        {
            state.SchemaVersion = CurrentSchemaVersion;
            state.SavedAtUtc = DateTime.UtcNow;
            var json = JsonConvert.SerializeObject(state, Formatting.Indented);
            File.WriteAllText(GetPath(slotName), json);
        }

        public GameStateSnapshot Load(string slotName)
        {
            var path = GetPath(slotName);
            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path);
            var raw = JsonConvert.DeserializeObject<GameStateSnapshot>(json);

            if (raw.SchemaVersion != CurrentSchemaVersion)
            {
                // Refus explicite plutôt qu'échec silencieux (aucune migration définie à ce stade).
                throw new UnsupportedSaveVersionException(raw.SchemaVersion, CurrentSchemaVersion);
            }

            return raw;
        }

        private string GetPath(string slotName) => Path.Combine(_baseDirectory, $"{slotName}.json");
    }
}
