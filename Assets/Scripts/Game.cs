using UnityEngine;
using System.Collections;

[System.Serializable]
public class Game
{
    [SerializeField] private Player _player;
    public Player Player { get { return _player; } }

    [SerializeField] private string _gameName;
    public string GameName { get { return _gameName; } }

    [SerializeField] private int _worldSeed;
    public int WorldSeed { get { return _worldSeed; } }

    [SerializeField] private int _saveSlot;
    public int SaveSlot { get { return _saveSlot; } }

    public Game(string name, int slot, bool isCopy = false, Player p = null, int worldSeed = 0)
    {
        _gameName = name;
        _saveSlot = slot;

        if (isCopy)
        {
            _player = p;
            _worldSeed = worldSeed;
        }
        else
        {
            _player = new Player(50);
            _worldSeed = UnityEngine.Random.Range(0, int.MaxValue);
        }
    }
}
