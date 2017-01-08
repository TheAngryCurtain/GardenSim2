using UnityEngine;
using System.Collections;

public class InteractableObject : MonoBehaviour
{
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private float _defaultAlpha;

    private Color _colorComponent;
    private bool _interactable = false;

    private bool _isHovered = false;
    public bool IsHovered { get { return _isHovered; } }

    protected virtual void Start()
    {
        UpdateAlpha(_defaultAlpha);
    }

    public virtual void OnMouseEnter()
    {
        if (_interactable)
        {
            OnObjectHoverEnter();
            _isHovered = true;
        }
    }

    public virtual void OnMouseExit()
    {
        if (_interactable)
        {
            OnObjectHoverExit();
            _isHovered = false;
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

    protected virtual void OnObjectHoverEnter()
    {
        UpdateAlpha(1f);
    }

    protected virtual void OnObjectHoverExit()
    {
        UpdateAlpha(_defaultAlpha);
    }

    protected void EnableInteraction(bool enable)
    {
        _interactable = enable;
    }

    protected void UpdateAlpha(float value)
    {
        _colorComponent = _renderer.material.color;
        _colorComponent.a = value;
        _renderer.material.color = _colorComponent;
    }
}
