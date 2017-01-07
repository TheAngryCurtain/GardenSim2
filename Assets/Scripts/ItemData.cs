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

    [SerializeField] protected string _name;
    [SerializeField] protected eItemType _type;
    [SerializeField] protected string _description;
    [SerializeField] protected int _value;
    [SerializeField] protected int _quantity;

    public string Name { get { return _name; } }
    public eItemType Type { get { return _type; } }
    public string Description { get { return _description; } }
    public int Value { get { return _value; } }
    public int Quantity { get { return _quantity; } }

    public ItemData(string name, eItemType type, string desc, int value)
    {
        _name = name;
        _type = type;
        _description = desc;
        _value = value;
        _quantity = 1;
    }
}
