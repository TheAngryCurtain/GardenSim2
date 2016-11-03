using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [System.Serializable]
    public class SubMenu
    {
        public GameObject Panel;
        public Text Title;
        public Button[] Buttons;
    }

    [SerializeField] private SubMenu[] _submenus;

    public void SetButtonListener(int menuIndex, int buttonIndex, UnityEngine.Events.UnityAction callback)
    {
        _submenus[menuIndex].Buttons[buttonIndex].onClick.AddListener(callback);
    }

    public void RemoveButtonListener(int menuIndex, int buttonIndex)
    {
        _submenus[menuIndex].Buttons[buttonIndex].onClick.RemoveAllListeners();
    }

    public void RemoveSubMenuListeners(int menuIndex)
    {
        for (int i = 0; i < _submenus[menuIndex].Buttons.Length; ++i)
        {
            RemoveButtonListener(menuIndex, i);
        }
    }

    public void ShowSubMenu(int menuIndex, bool show)
    {
        _submenus[menuIndex].Panel.SetActive(show);
    }

    public void ShowMenuButton(int menuIndex, int buttonIndex, bool show)
    {
        _submenus[menuIndex].Buttons[buttonIndex].gameObject.SetActive(show);
    }
}
