using System;
using UnityEngine;

namespace Game.Procedural
{
    [Serializable]
    public struct TerrainSpeedEntry
    {
        public TerrainType Terrain;
        public float SpeedModifier;
    }

    // Catalogue de contenu (FR-044) : modificateur de vitesse de transport par type de terrain
    // (biome, montée/descente). Principe fixé ici ; les valeurs exactes par terrain sont de
    // l'équilibrage à affiner ultérieurement.
    [CreateAssetMenu(menuName = "AstraLink/Procedural/Terrain Speed Catalog", fileName = "TerrainSpeedCatalog")]
    public sealed class TerrainSpeedCatalog : ScriptableObject
    {
        [SerializeField] private TerrainSpeedEntry[] _entries;
        [SerializeField] private float _defaultSpeedModifier = 1f;

        public float GetSpeedModifier(TerrainType terrain)
        {
            if (_entries != null)
            {
                foreach (var entry in _entries)
                {
                    if (entry.Terrain == terrain)
                        return entry.SpeedModifier;
                }
            }

            return _defaultSpeedModifier;
        }

        // Construction programmatique (tests, imports de contenu).
        public void Initialize(TerrainSpeedEntry[] entries, float defaultSpeedModifier = 1f)
        {
            _entries = entries;
            _defaultSpeedModifier = defaultSpeedModifier;
        }
    }
}
