using UnityEngine;
using System.Collections;

[System.Serializable]
public class ToolData : ItemData
{
    [SerializeField] protected eToolType _toolType;
    [SerializeField] protected int _toolLevel;

    public enum eToolType
    {
        None = -1,
        Shovel,
        WateringCan,
        Seeds,
        Axe,
        Decor
    }

    public eToolType ToolType { get { return _toolType; } }
    public int ToolLevel { get { return _toolLevel; } }

    public ToolData(string name, eItemType type, string desc, int value, eToolType tType) : base(name, type, desc, value)
    {
        _name = name;
        _type = type;
        _description = desc;
        _value = value;
        _quantity = 1;
        _toolType = tType;
    }
}
