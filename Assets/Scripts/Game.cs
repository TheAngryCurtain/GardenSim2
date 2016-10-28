using UnityEngine;
using System.Collections;

[System.Serializable]
public class Game
{
    [SerializeField] private Player _player;
    public Player Player { get { return _player; } }

    [SerializeField] private int _worldSeed;
    public int WorldSeed { get { return _worldSeed; } }

    [SerializeField] private int _saveSlot;
    public int SaveSlot { get { return _saveSlot; } }

    public Game(int slot)
    {
        _saveSlot = slot;
        _player = new Player();
        _worldSeed = UnityEngine.Random.Range(0, int.MaxValue);
    }
}
