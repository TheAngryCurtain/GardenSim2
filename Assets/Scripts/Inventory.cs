using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Inventory
{
    [SerializeField] private List<ToolData> _tools;
    [SerializeField] private List<ItemData> _items;

    private ToolData _currentTool = null;
    public ToolData CurrentTool { get { return _currentTool; } }

    public Inventory(int size)
    {
        _tools = new List<ToolData>()
        {
            // shovel
            // watering can
            // - axe is a later unlock?
            // - decorations is a later unlock? --> submenu?
            // - seeds are acquired through tutorial? --> submenu?
            new ToolData("Shovel", ItemData.eItemType.Tool, "A simple spade.", -1, ToolData.eToolType.Shovel),
            new ToolData("Watering Can", ItemData.eItemType.Tool, "A basic watering can.", -1, ToolData.eToolType.WateringCan),
            new ToolData("Seeds", ItemData.eItemType.Tool, "A seed pouch.", -1, ToolData.eToolType.Seeds),
            new ToolData("Axe", ItemData.eItemType.Tool, "A sturdy axe.", -1, ToolData.eToolType.Axe),
            new ToolData("Decorator", ItemData.eItemType.Tool, "er.. no idea.", -1, ToolData.eToolType.Decor)
        };
        _items = new List<ItemData>(size);
    }

    public void SetCurrentTool(int toolIndex)
    {
        if (toolIndex == -1)
        {
            // empty hand
            _currentTool = null;
        }
        else
        {
            _currentTool = _tools[toolIndex];
            Debug.LogFormat("Tool: {0}, desc: {1}, value: {2}", _currentTool.Name, _currentTool.Description, _currentTool.Value);
        }
    }
}
