using System;
using System.Collections.Generic;

namespace Game.Economy
{
    public sealed class Inventory
    {
        private readonly Dictionary<string, float> _quantities = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _capacities = new Dictionary<string, float>();

        public IReadOnlyDictionary<string, float> Quantities => _quantities;

        public float GetQuantity(string resourceId) => _quantities.TryGetValue(resourceId, out var q) ? q : 0f;

        public float GetCapacity(string resourceId) => _capacities.TryGetValue(resourceId, out var c) ? c : float.MaxValue;

        public void SetCapacity(string resourceId, float capacity)
        {
            _capacities[resourceId] = capacity;
        }

        public bool HasAtLeast(string resourceId, float amount) => GetQuantity(resourceId) >= amount;

        public bool TryAdd(string resourceId, float amount)
        {
            if (amount <= 0f) return true;

            var current = GetQuantity(resourceId);
            var capacity = GetCapacity(resourceId);
            var room = capacity - current;
            if (room <= 0f) return false;

            var added = Math.Min(amount, room);
            _quantities[resourceId] = current + added;
            return added >= amount - 0.0001f;
        }

        public bool TryRemove(string resourceId, float amount)
        {
            if (amount <= 0f) return true;

            var current = GetQuantity(resourceId);
            if (current < amount) return false;

            _quantities[resourceId] = current - amount;
            return true;
        }

        public void SetQuantity(string resourceId, float quantity)
        {
            _quantities[resourceId] = quantity;
        }
    }
}
