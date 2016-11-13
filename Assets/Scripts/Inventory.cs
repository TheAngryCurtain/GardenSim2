using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Inventory
{
    [SerializeField] private List<ItemData> _tools;
    [SerializeField] private List<ItemData> _items;

    public Inventory(int size)
    {
        _tools = new List<ItemData>()
        {
            // shovel
            // watering can
            // - axe is a later unlock?
            // - decorations is a later unlock? --> submenu?
            // - seeds are acquired through tutorial? --> submenu?
        };
        _items = new List<ItemData>(size);
    }
}
