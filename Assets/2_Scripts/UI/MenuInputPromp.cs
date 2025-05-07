using PrimeTween;
using Shapes;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MenuInputPromp : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private string keyboardMouseText;
    [SerializeField] private string gamepadText;
    
    [Header("References")]
    [SerializeField] private ShapeGroup promptBackgroundRectangle;
    [SerializeField] private ShapeGroup promptBackgroundCircle;
    [SerializeField] private TextMeshProUGUI inputText;
    [SerializeField] private SOInputReader inputReader;
    
    

    
    private Color _defaultBackgroundColor;
    private float _defaultTextAlpha;
    private Sequence _promptSequence;
    private ShapeGroup _previousBackGroup;
    private ShapeGroup _activeBackGroup;
    

    private void Awake()
    {
        
        if (promptBackgroundRectangle && promptBackgroundCircle)
        {
            _defaultBackgroundColor = promptBackgroundRectangle.Color;
        }
        
        if (inputText)
        {
            _defaultTextAlpha = inputText.color.a;
        }
    }
    
    private void OnEnable()
    {
        if (!inputReader) return;
        
        inputReader.ControlSchemeChangedEvent += OnControlSchemeChanged;
    }
    
    private void OnDisable()
    {
        if (!inputReader) return;
        
        inputReader.ControlSchemeChangedEvent -= OnControlSchemeChanged;
    }

    private void OnControlSchemeChanged(ControlType type)
    {
        UpdateInteractPrompt();
    }
    

    private void UpdateInteractPrompt()
    {
        _previousBackGroup = _activeBackGroup;
    
        if (inputReader)
        {
            switch (inputReader.CurrentControlScheme)
            {
                case ControlType.KeyboardMouse:
                    inputText.text = keyboardMouseText;
                    _activeBackGroup = promptBackgroundRectangle;
                    break;
                case ControlType.Gamepad:
                    inputText.text = gamepadText;
                    _activeBackGroup = promptBackgroundCircle;
                    break;
            }
        }
        else
        {
            inputText.text = keyboardMouseText;
            _activeBackGroup = promptBackgroundRectangle;
        }
    
        // If we're switching backgrounds and a prompt is already visible
        if (_previousBackGroup && _previousBackGroup != _activeBackGroup && inputText.color.a > 0)
        {
            // Hide the previous background
            SetAlpha(_previousBackGroup, 0f);
        
            // Show the new background with the same alpha as the text
            if (_activeBackGroup)
            {
                Color activeColor = _defaultBackgroundColor;
                activeColor.a = inputText.color.a;
                _activeBackGroup.Color = activeColor;
            }
        }
    }
    

    
    private void SetAlpha(ShapeGroup graphic, float alpha)
    {
        if (!graphic) return;
        
        Color color = graphic.Color;
        color.a = alpha;
        graphic.Color = color;
    }
    
    private void SetAlpha(Graphic graphic, float alpha)
    {
        if (!graphic) return;
        
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

}