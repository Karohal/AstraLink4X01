using UnityEngine;

namespace Game.Core
{
    // Marqueur posé sur chaque GameObject tuile généré par Phase1GameView, pour retrouver ses
    // coordonnées de grille lors d'un raycast souris (Principe II : aucune logique métier ici).
    public sealed class TileMarker : MonoBehaviour
    {
        public int X;
        public int Y;
    }
}
