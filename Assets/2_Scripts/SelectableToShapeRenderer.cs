using System;
using System.Collections.Generic;
using Shapes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VInspector;

public class SelectableToShapeRenderer : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")] 
    [SerializeField] private bool checkForShapesOnAwake;
    [SerializeField] private ShapeRenderer[] shapeRenderers;

    private readonly Dictionary<ShapeRenderer, Color> _shapesOriginalColors = new Dictionary<ShapeRenderer, Color>();
    private SelectableWrapper _selectableWrapper;
    private Color _selectableHighlightedColor;
    private Color _selectablePressedColor;
    private Color _selectableDisabledColor;
    private Color _selectableSelectedColor;
    private bool _isSelected = false;
    private bool _isPressed = false;
    
    private void Awake()
    {
        if (!checkForShapesOnAwake && (shapeRenderers == null || shapeRenderers.Length == 0)) 
        {
            Debug.LogWarning("No shape renderers assigned to SelectableToShapeRenderer on " + gameObject.name);
            return;
        }
        
        if (checkForShapesOnAwake)
        {
            FindAllShapeRenderersInChildren();
            if (shapeRenderers == null || shapeRenderers.Length == 0)
            {
                Debug.LogWarning("No shape renderers found in children of " + gameObject.name);
                return;
            }
        }

        var selectable = GetComponent<Selectable>();
        if (!selectable)
        {
            Debug.LogError("No Selectable component found on " + gameObject.name);
            return;
        }
        
        // Create a wrapper for the selectable that provides events
        _selectableWrapper = new SelectableWrapper(selectable);
        _selectableWrapper.InteractableChanged += OnInteractableChanged;
        
        _selectableHighlightedColor = selectable.colors.highlightedColor;
        _selectablePressedColor = selectable.colors.pressedColor;
        _selectableDisabledColor = selectable.colors.disabledColor;
        _selectableSelectedColor = selectable.colors.selectedColor;
        
        foreach (var shapeRenderer in shapeRenderers)
        {
            if (shapeRenderer && !_shapesOriginalColors.ContainsKey(shapeRenderer))
            {
                // Ensure we capture the alpha component as well
                Color originalColor = shapeRenderer.Color;
                _shapesOriginalColors.Add(shapeRenderer, originalColor);
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (_selectableWrapper != null)
        {
            _selectableWrapper.InteractableChanged -= OnInteractableChanged;
        }
    }

    private void OnInteractableChanged(bool interactable)
    {
        if (!interactable)
        {
            // Set disabled color
            foreach (var shapeRenderer in shapeRenderers)
            {
                if (!shapeRenderer) continue;
                shapeRenderer.Color = _selectableDisabledColor;
            }
        }
        else
        {
            // Return to normal state based on current visual state
            UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        if (_selectableWrapper is not { Interactable: true }) return;
        
        if (_isPressed)
        {
            foreach (var shapeRenderer in shapeRenderers)
            {
                if (!shapeRenderer) continue;
                shapeRenderer.Color = _selectablePressedColor;
            }
        }
        else if (_isSelected)
        {
            foreach (var shapeRenderer in shapeRenderers)
            {
                if (!shapeRenderer) continue;
                shapeRenderer.Color = _selectableSelectedColor;
            }
        }
        else
        {
            foreach (var shapeRenderer in shapeRenderers)
            {
                if (!shapeRenderer) continue;
                if (_shapesOriginalColors.TryGetValue(shapeRenderer, out var originalColor))
                {
                    shapeRenderer.Color = originalColor;
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Check for null wrapper
        if (_selectableWrapper is not { Interactable: true }) return;
        
        _isPressed = true;
        foreach (var shapeRenderer in shapeRenderers)
        {
            if (shapeRenderer == null) continue;
            shapeRenderer.Color = _selectablePressedColor;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Check for null wrapper
        if (_selectableWrapper is not { Interactable: true }) return;
        
        _isPressed = false;
        UpdateVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Check for null wrapper
        if (_selectableWrapper == null || _isSelected || _isPressed || !_selectableWrapper.Interactable) return;
        
        foreach (var shapeRenderer in shapeRenderers)
        {
            if (shapeRenderer == null) continue;
            shapeRenderer.Color = _selectableHighlightedColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Check for null wrapper
        if (_selectableWrapper == null || _isSelected || _isPressed || !_selectableWrapper.Interactable) return;
        
        foreach (var shapeRenderer in shapeRenderers)
        {
            if (shapeRenderer == null) continue;
            if (_shapesOriginalColors.TryGetValue(shapeRenderer, out var originalColor))
            {
                shapeRenderer.Color = originalColor;
            }
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        // Check for null wrapper
        if (_selectableWrapper is not { Interactable: true }) return;
        
        _isSelected = true;
        
        // Only update visuals if not pressed
        if (!_isPressed)
        {
            foreach (var shapeRenderer in shapeRenderers)
            {
                if (shapeRenderer == null) continue;
                shapeRenderer.Color = _selectableSelectedColor;
            }
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // Check for null wrapper
        if (_selectableWrapper is not { Interactable: true }) return;
    
        _isSelected = false;
    
        // Only update visuals if not pressed
        if (!_isPressed)
        {
            foreach (var shapeRenderer in shapeRenderers)
            {
                if (shapeRenderer == null) continue;
                if (_shapesOriginalColors.TryGetValue(shapeRenderer, out var originalColor))
                {
                    shapeRenderer.Color = originalColor;
                }
            }
        }
    }
    
    [Button]
    private void FindAllShapeRenderersInChildren()
    {
        shapeRenderers = GetComponentsInChildren<ShapeRenderer>();
        // Filter out duplicates
        for (int i = 0; i < shapeRenderers.Length; i++)
        {
            for (int j = i + 1; j < shapeRenderers.Length; j++)
            {
                if (shapeRenderers[i] == shapeRenderers[j])
                {
                    shapeRenderers[j] = null;
                }
            }
        }
    }

    // Helper class to wrap Selectable and provide events
    private class SelectableWrapper
    {
        private readonly Selectable _selectable;
        private bool _interactable;

        public event Action<bool> InteractableChanged;

        public bool Interactable
        {
            get
            {
                if (_selectable == null) return false;
                
                bool newInteractable = _selectable.interactable;
                
                // If the value has changed, trigger the event
                if (_interactable != newInteractable)
                {
                    _interactable = newInteractable;
                    InteractableChanged?.Invoke(_interactable);
                }
                
                return _interactable;
            }
        }

        public SelectableWrapper(Selectable selectable)
        {
            _selectable = selectable;
            _interactable = selectable && selectable.interactable;
        }
    }
}