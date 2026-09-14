using System;
using UnityEngine;

namespace Game.Colonists
{
    [CreateAssetMenu(menuName = "AstraLink/Colonists/Ethnicity Definition", fileName = "EthnicityDefinition")]
    public sealed class EthnicityDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string[] _maleFirstNames;
        [SerializeField] private string[] _femaleFirstNames;
        [SerializeField] private string[] _lastNames;

        public string Id => _id;
        public string[] MaleFirstNames => _maleFirstNames ?? Array.Empty<string>();
        public string[] FemaleFirstNames => _femaleFirstNames ?? Array.Empty<string>();
        public string[] LastNames => _lastNames ?? Array.Empty<string>();

        // Construction programmatique (tests, imports de contenu) puisque les champs sont privés
        // et normalement renseignés via l'Inspector.
        public void Initialize(string id, string[] maleFirstNames, string[] femaleFirstNames, string[] lastNames)
        {
            _id = id;
            _maleFirstNames = maleFirstNames;
            _femaleFirstNames = femaleFirstNames;
            _lastNames = lastNames;
        }
    }
}
