using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public static class FileManager
{
    public static System.Action OnGamesLoaded;

    private static string _fileExt = "sav";
    private static string _fileName = "GardenSim";
    private static string _lastLoadedKey = "LastLoaded-";
    private static List<Game> _savedGames = new List<Game>();

    public static List<Game> SavedGames { get { return _savedGames; } }
    public static int NumSavedGames { get { return _savedGames.Count; } }

    public static void LoadSavedGames()
    {
        string fileName = GetFileString();
        if (File.Exists(fileName))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(fileName, FileMode.Open);
            _savedGames = (List<Game>)bf.Deserialize(file);
            file.Close();

            Debug.Log("Load Successful");
        }
        else
        {
            Debug.Log("No save file found");
        }

        if (OnGamesLoaded != null)
        {
            OnGamesLoaded();
        }
    }

    private static void SaveGames()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(GetFileString());
        bf.Serialize(file, _savedGames);
        file.Close();

        Debug.Log("Save Successful");
    }

    private static string GetFileString()
    {
        // C:\Users\<user_name>\AppData\LocalLow\<company_name>\<product_name>
        return string.Format("{0}/{1}.{2}", Application.persistentDataPath, _fileName, _fileExt);
    }

    public static void SaveGame(Game game)
    {
        if (_savedGames.Contains(game))
        {
            _savedGames[game.SaveSlot] = game;
        }
        else
        {
            _savedGames.Add(game);
        }

        SaveGames();
    }

    public static Game NewGame(string name)
    {
        int newIndex = _savedGames.Count;
        Game game = new Game(name, newIndex);
        SetLastLoadedIndex(newIndex);
        SaveGame(game);

        return game;
    }

    public static Game LoadGame(int index)
    {
        if (index < 0 || index >= _savedGames.Count)
        {
            Debug.Log("Invalid index to load");
            return null;
        }

        SetLastLoadedIndex(index);
        return _savedGames[index];
    }

    public static Game CopyGame(int index)
    {
        if (index >= _savedGames.Count)
        {
            Debug.Log("Invalid index to copy");
            return null;
        }

        Game toCopy = _savedGames[index];
        Game copy = new Game(string.Format("{0} (Copy)", toCopy.GameName), _savedGames.Count, true, toCopy.Player, toCopy.WorldSeed);
        SaveGame(copy);

        return copy;
    }

    public static void DeleteGame(int index)
    {
        int count = _savedGames.Count;
        if (index >= count)
        {
            Debug.Log("Invalid index to delete");
            return;
        }

        _savedGames.RemoveAt(index);
        if (GetLastLoadedIndex() == index)
        {
            if (count > 0)
            {
                SetLastLoadedIndex(count - 1);
            }
            else
            {
                SetLastLoadedIndex(0);
            }
        }

        SaveGames();
    }

    public static int GetLastLoadedIndex()
    {
        return PlayerPrefs.GetInt(_lastLoadedKey);
    }

    public static void SetLastLoadedIndex(int index)
    {
        PlayerPrefs.SetInt(_lastLoadedKey, index);
    }
}
