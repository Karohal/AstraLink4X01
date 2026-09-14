using Game.Procedural;

namespace Game.FogOfWar
{
    public sealed class BuildingConstructedEvent
    {
        public Planet Planet { get; }
        public int X { get; }
        public int Y { get; }
        public int Radius { get; }

        public BuildingConstructedEvent(Planet planet, int x, int y, int radius)
        {
            Planet = planet;
            X = x;
            Y = y;
            Radius = radius;
        }
    }

    // Écoute générique consommée par BuildingPlacementService (US2, T027) : découple la
    // dissipation du brouillard de guerre (US1) de la mécanique de construction complète, qui
    // n'existe pas encore à ce stade de l'implémentation.
    public sealed class FogOfWarConstructionListener
    {
        private readonly IFogOfWarService _fogOfWarService;

        public FogOfWarConstructionListener(IFogOfWarService fogOfWarService)
        {
            _fogOfWarService = fogOfWarService;
        }

        public void OnBuildingConstructed(BuildingConstructedEvent constructedEvent)
        {
            _fogOfWarService.RevealAround(
                constructedEvent.Planet,
                constructedEvent.X,
                constructedEvent.Y,
                constructedEvent.Radius); // FR-004
        }
    }
}
