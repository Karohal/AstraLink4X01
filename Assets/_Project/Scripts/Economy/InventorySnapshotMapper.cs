using System.Collections.Generic;
using Game.Core;

namespace Game.Economy
{
    // FR-031 tranche US2 : traduction Inventory <-> liste de ResourceAmountSnapshot.
    public static class InventorySnapshotMapper
    {
        public static List<ResourceAmountSnapshot> ToSnapshot(Inventory inventory)
        {
            var list = new List<ResourceAmountSnapshot>();
            foreach (var kvp in inventory.Quantities)
                list.Add(new ResourceAmountSnapshot { ResourceId = kvp.Key, Quantity = kvp.Value });
            return list;
        }

        public static Inventory FromSnapshot(List<ResourceAmountSnapshot> snapshot)
        {
            var inventory = new Inventory();
            if (snapshot == null) return inventory;

            foreach (var entry in snapshot)
                inventory.SetQuantity(entry.ResourceId, entry.Quantity);

            return inventory;
        }
    }
}
