using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoadManager : MonoBehaviour
{
	public Sprite LoadingIcon;

	private AsyncOperation loadLevel;

	void Start()
	{
		loadLevel = SceneManager.LoadSceneAsync("Scenes/Garden");
	}

	void Update()
	{
		if (!loadLevel.isDone)
		{
			Debug.LogFormat("{0}% Complete", loadLevel.progress * 100);
		}
	}
}
