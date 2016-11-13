using UnityEngine;
using System.Collections;

[System.Serializable]
public class Item : MonoBehaviour
{
    private ItemData _itemData;
    public ItemData Data { get { return _itemData; } }

    public virtual void Init(ItemData data)
    {
        _itemData = data;
    }

    public virtual void Init(string name, ItemData.eItemType type, string desc, int value)
    {
        _itemData = new ItemData(name, type, desc, value);
    }
}
