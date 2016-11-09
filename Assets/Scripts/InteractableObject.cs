using UnityEngine;
using System.Collections;

public class InteractableObject : MonoBehaviour
{
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private float _defaultAlpha;

    private Color _colorComponent;
    private bool _interactable = false;

    protected virtual void Start()
    {
        UpdateAlpha(_defaultAlpha);
    }

    public virtual void OnMouseEnter()
    {
        if (_interactable)
        {
            UpdateAlpha(1f);
        }
    }

    public virtual void OnMouseExit()
    {
        if (_interactable)
        {
            UpdateAlpha(_defaultAlpha);
        }
    }

    public void OnMouseDown()
    {
        if (_interactable)
        {
            ClickAction();
        }
    }

    protected virtual void ClickAction()
    {

    }

    protected void EnableInteraction(bool enable)
    {
        _interactable = enable;
    }

    private void UpdateAlpha(float value)
    {
        _colorComponent = _renderer.material.color;
        _colorComponent.a = value;
        _renderer.material.color = _colorComponent;
    }
}
