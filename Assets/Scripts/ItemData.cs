using UnityEngine;
using System.Collections;

public class ItemData
{
    public enum eItemType
    {
        // seed? tool? 
        // consumable? upgrade?
    }

    private string _name;
    private eItemType _type;
    private string _description;
    private int _value;
    private int _quantity;

    public ItemData(string name, eItemType type, string desc, int value)
    {
        _name = name;
        _type = type;
        _description = desc;
        _value = value;
        _quantity = 1;
    }
}
