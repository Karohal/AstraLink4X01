using System;
using UnityEngine;

namespace Game.Core
{
    // Marqueur posé sur chaque GameObject bâtiment généré par Phase1GameView, pour retrouver son
    // identifiant lors d'un raycast souris (Principe II : aucune logique métier ici).
    public sealed class BuildingMarker : MonoBehaviour
    {
        public Guid BuildingId;
        public int X;
        public int Y;
    }
}
