using System;
using Game.Core;

namespace Game.Procedural
{
    // Traduction Planet <-> PlanetSnapshot pour la persistance (FR-031 tranche US1). SaveLoadService
    // reste générique (sérialise/désérialise l'enveloppe) ; chaque module fournit son propre mapper.
    public static class PlanetSnapshotMapper
    {
        public static PlanetSnapshot ToSnapshot(Planet planet)
        {
            var snapshot = new PlanetSnapshot
            {
                Id = planet.Id,
                Seed = planet.Seed,
                Width = planet.Width,
                Height = planet.Height
            };

            for (var x = 0; x < planet.Width; x++)
            {
                for (var y = 0; y < planet.Height; y++)
                {
                    var zone = planet.Zones[x, y];
                    var zoneSnapshot = new ZoneSnapshot
                    {
                        X = x,
                        Y = y,
                        TerrainId = zone.Terrain.ToString(),
                        Revealed = zone.IsRevealed,
                        BuildingId = zone.BuildingId
                    };

                    if (zone.Deposit != null)
                    {
                        zoneSnapshot.Deposit = new DepositSnapshot
                        {
                            ResourceId = zone.Deposit.ResourceId,
                            Remaining = zone.Deposit.RemainingQuantity,
                            State = zone.Deposit.State.ToString(),
                            IsInfinite = zone.Deposit.IsInfinite
                        };
                    }

                    snapshot.Zones.Add(zoneSnapshot);
                }
            }

            return snapshot;
        }

        public static Planet FromSnapshot(PlanetSnapshot snapshot)
        {
            var zones = new Zone[snapshot.Width, snapshot.Height];

            foreach (var zoneSnapshot in snapshot.Zones)
            {
                var terrain = (TerrainType)Enum.Parse(typeof(TerrainType), zoneSnapshot.TerrainId);
                Deposit deposit = null;
                if (zoneSnapshot.Deposit != null)
                {
                    deposit = new Deposit(zoneSnapshot.Deposit.ResourceId, zoneSnapshot.Deposit.Remaining, zoneSnapshot.Deposit.IsInfinite);
                    RestoreDepositState(deposit, zoneSnapshot.Deposit.State);
                }

                var zone = new Zone(new GridPosition(zoneSnapshot.X, zoneSnapshot.Y), terrain, terrain != TerrainType.Water, deposit);
                if (zoneSnapshot.Revealed) zone.Reveal();
                zone.BuildingId = zoneSnapshot.BuildingId;

                zones[zoneSnapshot.X, zoneSnapshot.Y] = zone;
            }

            return new Planet(snapshot.Id, snapshot.Seed, snapshot.Width, snapshot.Height, zones);
        }

        private static void RestoreDepositState(Deposit deposit, string stateName)
        {
            var state = (DepositState)Enum.Parse(typeof(DepositState), stateName);
            switch (state)
            {
                case DepositState.ReadyForExtractor:
                    deposit.UnlockTechnology();
                    break;
                case DepositState.Extracting:
                    deposit.UnlockTechnology();
                    deposit.StartExtraction();
                    break;
                case DepositState.Depleted:
                    deposit.UnlockTechnology();
                    deposit.StartExtraction();
                    deposit.Extract(deposit.RemainingQuantity + 1f);
                    break;
            }
        }
    }
}
