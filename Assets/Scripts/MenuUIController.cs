using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [SerializeField] GameObject _togglePrefab;

    [SerializeField] private Text _title;
    [SerializeField] private Button[] _buttons;
    [SerializeField] private ScrollRect _scrollView;
    [SerializeField] private InputField _input;

    private ToggleGroup _toggleGroup;
    private List<Toggle> _toggleItems;

    public void SetTitle(string title)
    {
        _title.text = title;
    }

    public int GetActiveToggleIndex()
    {
        if (_scrollView != null)
        {
            Transform group = _scrollView.content.GetChild(0);
            Toggle[] toggles = group.GetComponentsInChildren<Toggle>();
            int index = 0;
            for (int i = 0; i < toggles.Length; ++i)
            {
                if (toggles[i].isOn)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }
        else
        {
            Debug.Log("No Scroll View!");
            return -1;
        }
    }

    public string GetFieldInput()
    {
        if (_input != null)
        {
            return _input.text;
        }
        else
        {
            Debug.Log("No Input Field!");
            return string.Empty;
        }
    }

    public void RemoveScrolListener()
    {
        if (_scrollView != null)
        {
            _scrollView.onValueChanged.RemoveAllListeners();
        }
    }

    public void SetScrollListData(int menuIndex, List<Game> data)
    {
        if (_scrollView != null)
        {
            if (data.Count > 0)
            {
                _toggleItems = new List<Toggle>(data.Count);
                _toggleGroup = _scrollView.content.GetChild(0).GetComponent<ToggleGroup>();
                
                for (int i = 0; i < data.Count; ++i)
                {
                    CopyToggleItem(i, data[i].GameName);
                }

                // force first one on
                _toggleItems[0].isOn = true;
            }
        }
    }

    public void RemoveToggleItem(int index)
    {
        // copy the values up the list and remove the last one
        for (int i = index; i < _toggleItems.Count - 1; ++i)
        {
            Text current = _toggleItems[i].GetComponentInChildren<Text>();
            Text next = _toggleItems[i + 1].GetComponentInChildren<Text>();

            current.text = next.text;
        }

        GameObject lastToggle = _toggleItems[_toggleItems.Count - 1].gameObject;
        _toggleItems.RemoveAt(_toggleItems.Count - 1);
        Destroy(lastToggle);

        // force the first item on
        if (_toggleItems.Count > 0)
        {
            _toggleItems[0].isOn = true;
        }
    }

    public void CopyToggleItem(int index, string text)
    {
        Vector3 localPos = Vector3.zero;
        int toggleHeight = 30;
        int buffer = 5;

        GameObject toggleObj = (GameObject)Instantiate(_togglePrefab);
        Toggle toggle = toggleObj.GetComponent<Toggle>();
        Text toggleText = toggleObj.transform.GetComponentInChildren<Text>();

        toggle.transform.SetParent(_toggleGroup.transform, false);
        localPos.y = (toggleHeight + buffer) * -index;
        toggle.transform.localPosition = localPos;
        toggle.group = _toggleGroup;
        toggleText.text = text;

        _toggleItems.Add(toggle);
    }

    public void SetButtonListener(int buttonIndex, UnityEngine.Events.UnityAction callback)
    {
        _buttons[buttonIndex].onClick.AddListener(callback);
    }

    public void RemoveButtonListener(int buttonIndex)
    {
        _buttons[buttonIndex].onClick.RemoveAllListeners();
    }

    public void RemoveMenuListeners()
    {
        for (int i = 0; i < _buttons.Length; ++i)
        {
            RemoveButtonListener(i);
        }
    }

    public void ShowMenuButton(int buttonIndex, bool show)
    {
        _buttons[buttonIndex].gameObject.SetActive(show);
    }

    public void DisableMenuButton(int buttonIndex, bool disable)
    {
        _buttons[buttonIndex].interactable = disable;
    }
}
