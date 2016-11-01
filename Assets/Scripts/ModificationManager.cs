using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ModificationManager : MonoBehaviour
{
    public static ModificationManager Instance;

    public int RemainingActions { get { return _actions.Count; } }

    [SerializeField]
    private List<ModificationAction> _actions;

    void Awake()
    {
        Instance = this;

        _actions = new List<ModificationAction>();
    }

    public void RecordAction(ModificationAction action)
    {
        _actions.Add(action);
    }

    public ModificationAction RetreiveAction(int index)
    {
        return _actions[index];
    }

    public void RemoveAction(ModificationAction action)
    {
        _actions.Remove(action);
    }
}
