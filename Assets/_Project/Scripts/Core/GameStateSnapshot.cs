using System;
using System.Collections.Generic;

namespace Game.Core
{
    // Enveloppe de sauvegarde complète, per contracts/savegame-schema.md. Toutes les sections sont
    // définies dès la Foundational ; chaque story (US1-US10) est responsable de peupler/restaurer
    // la sienne dans SaveLoadService plutôt que de modifier cette classe.
    [Serializable]
    public sealed class GameStateSnapshot
    {
        public int SchemaVersion = SaveLoadService.CurrentSchemaVersion;
        public DateTime SavedAtUtc;
        public PlanetSnapshot Planet;
        public ColonySnapshot Colony;
        public List<ColonistSnapshot> Colonists = new List<ColonistSnapshot>();
        public List<BuildingSnapshot> Buildings = new List<BuildingSnapshot>();
        public List<TechnologySnapshot> Technologies = new List<TechnologySnapshot>();
        public List<TransportTaskSnapshot> TransportTasks = new List<TransportTaskSnapshot>();
        public TreasurySnapshot Treasury;
    }

    [Serializable]
    public sealed class PlanetSnapshot
    {
        public Guid Id;
        public int Seed;
        public int Width;
        public int Height;
        public List<ZoneSnapshot> Zones = new List<ZoneSnapshot>();
    }

    [Serializable]
    public sealed class ZoneSnapshot
    {
        public int X;
        public int Y;
        public string TerrainId;
        public bool Revealed;
        public DepositSnapshot Deposit;
        public Guid? BuildingId;
    }

    [Serializable]
    public sealed class DepositSnapshot
    {
        public string ResourceId;
        public float Remaining;
        public string State;
        public bool IsInfinite;
    }

    [Serializable]
    public sealed class ColonySnapshot
    {
        public string Name;
        public int DevelopmentLevel;
        public float DevelopmentProgress;
        public string State;
    }

    [Serializable]
    public sealed class ColonistSnapshot
    {
        public Guid Id;
        public string Name;
        public string Gender;
        public string EthnicityId;
        public string Biography;
        public float Health;
        public List<SkillSnapshot> Skills = new List<SkillSnapshot>();
        public string AssignmentMode;
        public AssignmentSnapshot CurrentAssignment;
        public Guid? HousingId;
    }

    [Serializable]
    public sealed class SkillSnapshot
    {
        public string JobId;
        public float Value;
        public string Source;
    }

    [Serializable]
    public sealed class AssignmentSnapshot
    {
        public string Type;
        public Guid TargetId;
    }

    [Serializable]
    public sealed class BuildingSnapshot
    {
        public Guid Id;
        public string DefinitionId;
        public int X;
        public int Y;
        public string State;
        public float ConstructionProgress;
        public bool IsStartingShelter;
        public ProductionRecipeStateSnapshot ProductionRecipeState;
        public HousingCohabitationSnapshot HousingCohabitation;
    }

    [Serializable]
    public sealed class ProductionRecipeStateSnapshot
    {
        public List<ResourceAmountSnapshot> InputBuffer = new List<ResourceAmountSnapshot>();
        public List<ResourceAmountSnapshot> OutputBuffer = new List<ResourceAmountSnapshot>();
    }

    [Serializable]
    public sealed class ResourceAmountSnapshot
    {
        public string ResourceId;
        public float Quantity;
    }

    [Serializable]
    public sealed class HousingCohabitationSnapshot
    {
        public float ContinuousDuration;
    }

    [Serializable]
    public sealed class TechnologySnapshot
    {
        public string Id;
        public float Progress;
        public bool Unlocked;
    }

    [Serializable]
    public sealed class TransportTaskSnapshot
    {
        public Guid Id;
        public string ResourceId;
        public Guid SourceBuildingId;
        public Guid DestinationBuildingId;
        public Guid? AssignedColonistId;
        public Guid? AssignedVehicleId;
        public string Phase;
        public float PhaseProgress;
        public float CarriedQuantity;
    }

    [Serializable]
    public sealed class TreasurySnapshot
    {
        public float Amount;
    }
}
