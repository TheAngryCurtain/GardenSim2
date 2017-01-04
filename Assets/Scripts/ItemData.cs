using UnityEngine;
using System.Collections;

[System.Serializable]
public class ItemData
{
    public enum eItemType
    {
        // seed? tool? 
        // consumable? upgrade?
		Tool
    }

	public enum eToolType
	{
		None = -1,
		Shovel,
		WateringCan,
		Seeds,
		Axe,
		Decor
	}

    [SerializeField] private string _name;
    [SerializeField] private eItemType _type;
    [SerializeField] private string _description;
    [SerializeField] private int _value;
    [SerializeField] private int _quantity;
	[SerializeField] private eToolType _toolType;

    public string Name { get { return _name; } }
    public eItemType Type { get { return _type; } }
    public string Description { get { return _description; } }
    public int Value { get { return _value; } }
    public int Quantity { get { return _quantity; } }
    public eToolType ToolType { get { return _toolType; } }

    public ItemData(string name, eItemType type, string desc, int value)
    {
        _name = name;
        _type = type;
        _description = desc;
        _value = value;
        _quantity = 1;
        _toolType = eToolType.None;
    }

    public ItemData(string name, eItemType type, string desc, int value, eToolType tType)
    {
        _name = name;
        _type = type;
        _description = desc;
        _value = value;
        _quantity = 1;
        _toolType = tType;
    }
}
