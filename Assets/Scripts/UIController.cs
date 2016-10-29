using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance;
    
    [SerializeField] Text _flattenButtonText;

    void Awake()
    {
        Instance = this;
    }

    public void OnButtonClicked(int id)
    {
        switch (id)
        {
            case 0: // flatten terrain
                bool state = GameManager.Instance.TerrainManager.ToggleInteractMode();
                SetFlattenText(state);
                break;
        }
    }

    private void SetFlattenText(bool state)
    {
        _flattenButtonText.text = (state ? "Select" : "Flatten");
    }
}
