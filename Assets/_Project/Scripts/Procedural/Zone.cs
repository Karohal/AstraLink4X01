using System;

namespace Game.Procedural
{
    public enum TerrainType
    {
        Plains,
        Hills,
        Mountains,
        Water
    }

    public sealed class Zone
    {
        public GridPosition Position { get; }
        public TerrainType Terrain { get; }
        public bool IsBuildable { get; }
        public bool IsRevealed { get; private set; }
        public Deposit Deposit { get; }
        public Guid? BuildingId { get; set; }

        public Zone(GridPosition position, TerrainType terrain, bool isBuildable, Deposit deposit = null)
        {
            Position = position;
            Terrain = terrain;
            IsBuildable = isBuildable;
            Deposit = deposit;
            IsRevealed = false;
        }

        public void Reveal()
        {
            IsRevealed = true; // FR-004 : pas de ré-obscurcissement en Phase 1
        }
    }
}
