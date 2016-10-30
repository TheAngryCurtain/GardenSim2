using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance;
    
    [SerializeField] Text _flattenButtonText;

    [SerializeField] Button _undoButton;

    private bool _isModifying = false;

    void Awake()
    {
        Instance = this;
    }

    public void OnButtonClicked(int id)
    {
        switch (id)
        {
            case 0: // flatten terrain
                _isModifying = GameManager.Instance.TerrainManager.ToggleInteractMode();
                SetFlattenText(_isModifying);
                break;

            case 1: // undo terrain modify
                GameManager.Instance.TerrainManager.UndoLastModify();
                break;
        }

        _undoButton.gameObject.SetActive(_isModifying);
    }

    private void SetFlattenText(bool state)
    {
        _flattenButtonText.text = (state ? "Select" : "Flatten");
    }

    public void OnTerrainModified(int index)
    {
        _undoButton.interactable = (index > 0);
    }
}
