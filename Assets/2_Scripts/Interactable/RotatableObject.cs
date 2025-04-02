using UnityEngine;
using PrimeTween;
using VInspector;
using System;

[RequireComponent(typeof(AudioSource))]
public class RotatableObject : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField, Tooltip("Rotation mode")] private RotationMode rotationMode = RotationMode.Direct;
    [SerializeField, Tooltip("Enable or disable rotation")] private bool canRotate = true;
    
    [ShowIf("rotationMode", RotationMode.Modifier)]
    [Header("Modifiers")]
    [SerializeField] private Vector3 rotationDirection = Vector3.up;
    [SerializeField] private float modifier1 = 0f;
    [SerializeField] private float modifier2 = 0f;
    [SerializeField] private float modifier3 = 0f;
    [EndIf]
    
    [Header("Feedback")]
    [SerializeField, Tooltip("How long is the rotation")] private float rotationTime = 0.5f;
    [SerializeField, Tooltip("Rotation ease")] private Ease rotationEase = Ease.Linear;
    [SerializeField, Tooltip("Rotation SFX")] private SOAudioEvent rotationSfx;
    
    public enum RotationMode { Direct, Modifier }
    private AudioSource _audioSource;
    private Vector3 _defaultRotation;
    private Tween _rotateTween;
    private bool _modifiersChanged = false;
    
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _defaultRotation = transform.rotation.eulerAngles;
    }

    private void OnValidate()
    {
        rotationDirection = new Vector3(
            Mathf.Clamp(rotationDirection.x, -1, 1),
            Mathf.Clamp(rotationDirection.y, -1, 1),
            Mathf.Clamp(rotationDirection.z, -1, 1)
        );
    }

    #region General Methods -------------------------------------

    [Button]
    public void ResetRotation()
    {
        if (rotationMode == RotationMode.Modifier)
        {
            ResetModifiers();
        }
        else
        {
            RotateTo(Quaternion.Euler(_defaultRotation));
        }
    }
    
    private void RotateTo(Quaternion targetRotation)
    {
        if (!CanRotate()) return;
        
        _rotateTween = Tween.Rotation(transform, targetRotation, rotationTime, rotationEase);
        
        if (_audioSource) 
        {
            rotationSfx?.Play(_audioSource);
        }
    }
    
    private bool CanRotate()
    {
        return canRotate && !_rotateTween.isAlive && Application.isPlaying;
    }
    
    public bool RotationEnabled
    {
        get => canRotate;
        set => canRotate = value;
    }
    
    public RotationMode CurrentRotationMode
    {
        get => rotationMode;
        set => rotationMode = value;
    }

    #endregion General Methods -------------------------------------

    #region Direct Rotation Methods -------------------------------------
    
    public void SetRotationOnX(float degrees)
    {
        if (rotationMode != RotationMode.Direct) return;
        Vector3 currentEuler = transform.rotation.eulerAngles;
        RotateTo(Quaternion.Euler(degrees, currentEuler.y, currentEuler.z));
    }
    
    public void SetRotationOnY(float degrees)
    {
        if (rotationMode != RotationMode.Direct) return;
        
        Vector3 currentEuler = transform.rotation.eulerAngles;
        RotateTo(Quaternion.Euler(currentEuler.x, degrees, currentEuler.z));
    }
    
    public void SetRotationOnZ(float degrees)
    {
        if (rotationMode != RotationMode.Direct) return;
        Vector3 currentEuler = transform.rotation.eulerAngles;
        RotateTo(Quaternion.Euler(currentEuler.x, currentEuler.y, degrees));
    }
    
    public void RotateOnXBy(float degrees)
    {
        if (rotationMode != RotationMode.Direct) return;
        RotateTo(transform.rotation * Quaternion.AngleAxis(degrees, Vector3.right));
    }
    
    public void RotateOnYBy(float degrees)
    {
        if (rotationMode != RotationMode.Direct) return;
        RotateTo(transform.rotation * Quaternion.AngleAxis(degrees, Vector3.up));
    }
    
    public void RotateOnZBy(float degrees)
    {
        if (rotationMode != RotationMode.Direct) return;
        RotateTo(transform.rotation * Quaternion.AngleAxis(degrees, Vector3.forward));
    }
    
    #endregion Direct Rotation Methods -------------------------------------

    #region Modifier Rotation Methods -------------------------------------
    
    public void SetRotationDirection(Vector3 direction)
    {
        rotationDirection = direction.normalized;
        _modifiersChanged = true;
        
        if (rotationMode == RotationMode.Modifier)
        {
            ApplyModifiers();
        }
    }
    
    public void SetModifier1(float value)
    {
        modifier1 = value;
        _modifiersChanged = true;
        
        if (rotationMode == RotationMode.Modifier)
        {
            ApplyModifiers();
        }
    }
    
    public void SetModifier2(float value)
    {
        modifier2 = value;
        _modifiersChanged = true;
        
        if (rotationMode == RotationMode.Modifier)
        {
            ApplyModifiers();
        }
    }
    
    public void SetModifier3(float value)
    {
        modifier3 = value;
        _modifiersChanged = true;
        
        if (rotationMode == RotationMode.Modifier)
        {
            ApplyModifiers();
        }
    }
    
    public void ResetModifiers()
    {
        modifier1 = 0f;
        modifier2 = 0f;
        modifier3 = 0f;
        _modifiersChanged = true;
        
        if (rotationMode == RotationMode.Modifier)
        {
            ApplyModifiers();
        }
    }
    
    public void ApplyModifiers()
    {
        if (!_modifiersChanged) return;
        
        // Calculate the combined modifier value
        float combinedModifier = modifier1 + modifier2 + modifier3;
        
        // Apply rotation around the specified direction vector
        Quaternion targetRotation;
        
        if (combinedModifier == 0f)
        {
            // If combined modifier is zero, return to default rotation
            targetRotation = Quaternion.Euler(_defaultRotation);
        }
        else
        {
            // Normalize the direction vector to ensure consistent rotation
            Vector3 normalizedDirection = rotationDirection.normalized;
            
            // Create rotation around the direction vector by the combined amount
            Quaternion modifierRotation = Quaternion.AngleAxis(combinedModifier, normalizedDirection);
            
            // Apply to the default rotation
            targetRotation = Quaternion.Euler(_defaultRotation) * modifierRotation;
        }
        
        RotateTo(targetRotation);
        _modifiersChanged = false;
    }
    
    #endregion Modifier Rotation Methods -------------------------------------
}