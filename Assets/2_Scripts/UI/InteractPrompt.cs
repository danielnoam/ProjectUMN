using PrimeTween;
using Shapes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractPrompt : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private ShapeGroup promptBackgroundRectangle;
    [SerializeField] private ShapeGroup promptBackgroundCircle;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private SOInputReader inputReader;

    [Header("Prompt Animation")]
    [SerializeField] private float promptFadeDuration = 0.3f;
    [SerializeField] private Ease promptFadeEase = Ease.OutSine;
    
    
    [Header("Rotation To Camera")]
    [SerializeField] private bool rotateX = true;
    [SerializeField] private bool rotateY = true;
    [SerializeField] private bool rotateZ = false;
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    
    [Header("Distance Scaling Based On Camera")]
    [SerializeField] private bool smoothScaling = true;
    [SerializeField] private float minDistance = 7f;
    [SerializeField] private float maxDistance = 18f;
    [SerializeField] private float scalingSpeed = 10f;
    
    private bool _lookAtCamera;
    private bool _scaleWithDistance;
    private Camera _targetCamera;
    private Vector3 _originalScale;
    private float _originalXRotation;
    private float _originalZRotation;
    private Color _defaultBackgroundColor;
    private float _defaultTextAlpha;
    private Sequence _promptSequence;
    private ShapeGroup _previousBackGroup;
    private ShapeGroup _activeBackGroup;
    

    private void Awake()
    {
        Vector3 currentRotation = transform.rotation.eulerAngles;
        _originalXRotation = currentRotation.x;
        _originalZRotation = currentRotation.z;
    
        _originalScale = transform.localScale;
    
        if (promptBackgroundRectangle)
        {
            _defaultBackgroundColor = promptBackgroundRectangle.Color;
            SetAlpha(promptBackgroundRectangle, 0f);
        }
    
        if (promptBackgroundCircle)
        {
            if (_defaultBackgroundColor == null || _defaultBackgroundColor == Color.clear)
                _defaultBackgroundColor = promptBackgroundCircle.Color;
            SetAlpha(promptBackgroundCircle, 0f);
        }
    
        if (promptText)
        {
            _defaultTextAlpha = promptText.color.a;
            SetAlpha(promptText, 0f);
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
        UpdateInteractPrompt(true);
    }

    private void Start()
    {
        if (!_targetCamera)
        {
            _targetCamera = Camera.main;
        }
    }

    public void UpdateInteractPrompt(bool isPlayer)
    {
        _previousBackGroup = _activeBackGroup;
    
        if (inputReader)
        {
            switch (inputReader.CurrentControlScheme)
            {
                case ControlType.KeyboardMouse:
                    promptText.text = isPlayer ? "E" : "R";
                    _activeBackGroup = promptBackgroundRectangle;
                    break;
                case ControlType.Gamepad:
                    promptText.text = isPlayer ? "X" : "Y";
                    _activeBackGroup = promptBackgroundCircle;
                    break;
            }
        }
        else
        {
            promptText.text = isPlayer ? "E" : "R";
            _activeBackGroup = promptBackgroundRectangle;
        }
    
        // If we're switching backgrounds and a prompt is already visible
        if (_previousBackGroup && _previousBackGroup != _activeBackGroup && promptText.color.a > 0)
        {
            // Hide the previous background
            SetAlpha(_previousBackGroup, 0f);
        
            // Show the new background with the same alpha as the text
            if (_activeBackGroup)
            {
                Color activeColor = _defaultBackgroundColor;
                activeColor.a = promptText.color.a;
                _activeBackGroup.Color = activeColor;
            }
        }
    }
    

    
    public void FadePrompt(bool fadeIn)
    {
        _promptSequence.Stop();
        _promptSequence = Sequence.Create();
    
        // Only animate the active background group
        if (_activeBackGroup)
        {
            Color targetAlpha = fadeIn ? _defaultBackgroundColor : Color.clear;
        
            _promptSequence.Group(
                Tween.Custom(
                    onValueChange: newColor => _activeBackGroup.Color = newColor,
                    startValue: _activeBackGroup.Color,
                    endValue: targetAlpha,
                    duration: promptFadeDuration,
                    ease: promptFadeEase
                )
            );
        }
    
        if (promptText)
        {
            float targetAlpha = fadeIn ? _defaultTextAlpha : 0f;
        
            if (fadeIn)
            {
                _scaleWithDistance = true;
                _lookAtCamera = true;
            } 
        
            _promptSequence.Group(
                Tween.Alpha(
                        promptText,
                        startValue: promptText.color.a,
                        endValue: targetAlpha,
                        duration: promptFadeDuration,
                        ease: promptFadeEase
                    )
                    .OnComplete( () =>
                    {
                        if (!fadeIn)
                        {
                            _scaleWithDistance = false;
                            _lookAtCamera = false;
                        } 
                    })
            );
        }
    }
    
    private void LateUpdate()
    {
        if (!_targetCamera) return;

        // Get the direction from the object to the camera
        Vector3 directionToCamera = _targetCamera.transform.position - transform.position;
        
        // Calculate distance to camera for scaling
        float distanceToCamera = directionToCamera.magnitude;
        
        
        if (_lookAtCamera)
        {
            // Create a rotation that looks at the camera
            Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
        
            // Extract the Euler angles
            Vector3 eulerRotation = lookRotation.eulerAngles;
        
            // Only keep the rotations for axes we want to rotate
            Vector3 currentRotation = transform.rotation.eulerAngles;
        
            // Selectively apply rotations based on which axes we want to rotate
            if (!rotateX) eulerRotation.x = _originalXRotation;
            if (!rotateY) eulerRotation.y = currentRotation.y;
            if (!rotateZ) eulerRotation.z = _originalZRotation;
        
            // Apply any rotation offset
            eulerRotation += rotationOffset;
        
            // Convert back to quaternion
            Quaternion targetRotation = Quaternion.Euler(eulerRotation);
        
            // Apply the rotation (with or without smoothing)
            if (smoothRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }

        // Apply distance-based scaling if enabled
        if (_scaleWithDistance)
        {
            // Calculate scale factor based on distance (clamped between min and max distance)
            float normalizedDistance = Mathf.Clamp(distanceToCamera, minDistance, maxDistance);
            float scaleFactor = normalizedDistance / minDistance;
            
            // Calculate target scale
            Vector3 targetScale = _originalScale * scaleFactor;
            
            // Apply scaling with or without smoothing
            if (smoothScaling)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scalingSpeed * Time.deltaTime);
            }
            else
            {
                transform.localScale = targetScale;
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