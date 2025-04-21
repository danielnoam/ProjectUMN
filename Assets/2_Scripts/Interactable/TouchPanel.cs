using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VInspector;
using PrimeTween;



[SelectionBase]
[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(AudioSource))]
public class TouchPanel : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] private float autoReleaseDelay = 0.5f;
    
    [Header("Panel Feedback")]
    [SerializeField] private SOAudioEvent sfxPanelPress;
    [SerializeField] private Renderer panelRenderer;
    [SerializeField] private Color punchEmissionColor = Color.white;
    [SerializeField] private float punchDuration = 0.5f;
    [SerializeField] private Cable[] connectedCables;
    [SerializeField] private CableState cableStateOnPress = CableState.Toggle;
    
    [Header("Panel Events")]
    [SerializeField] private UnityEvent onPanelPressed; 
    
    private bool _isPressed = false;
    private Coroutine _releaseCoroutine;
    private AudioSource _audioSource;
    private Material _panelMaterial;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        // Get the material from the panel renderer
        if (panelRenderer)
        {
            // Create a material instance to avoid changing the shared material
            _panelMaterial = new Material(panelRenderer.material);
            panelRenderer.material = _panelMaterial;
            
            // Enable emission on the material
            _panelMaterial.EnableKeyword("_EMISSION");
        }
    }

    [Button]
    public void PressPanel()
    {
        if (_isPressed) return;

        _isPressed = true;
        
        sfxPanelPress?.Play(_audioSource);
        
        PunchEmissionColor();

        ToggleConnectedCables();
        onPanelPressed?.Invoke();

        if (_releaseCoroutine != null) StopCoroutine(_releaseCoroutine);
            
        _releaseCoroutine = StartCoroutine(ReleaseButtonAfterDelay());
    }
    
    private void PunchEmissionColor()
    {
        if (_panelMaterial)
        {
            // Get the current emission color from the material
            Color currentEmissionColor = _panelMaterial.GetColor(EmissionColor);
            
            // Create a sequence to change the emission color to white and back
            Sequence.Create()
                // First, change to white (or to a brighter version of the current color)
                .Chain(Tween.Custom(
                    startValue: currentEmissionColor,
                    endValue: punchEmissionColor,
                    duration: punchDuration * 0.3f,
                    ease: Ease.OutQuad,
                    onValueChange: newColor => _panelMaterial.SetColor(EmissionColor, newColor)
                ))
                // Then, revert back to the original emission color
                .Chain(Tween.Custom(
                    startValue: punchEmissionColor,
                    endValue: currentEmissionColor,
                    duration: punchDuration * 0.7f,
                    ease: Ease.InOutQuad,
                    onValueChange: newColor => _panelMaterial.SetColor(EmissionColor, newColor)
                ));
        }
    }
    
    private IEnumerator ReleaseButtonAfterDelay()
    {
        // Wait for the specified delay time
        yield return new WaitForSeconds(autoReleaseDelay);
        
        // Release the button
        _isPressed = false;
        _releaseCoroutine = null;
    }
    
    private void ToggleConnectedCables()
    {
        if (connectedCables == null || connectedCables.Length == 0) return;

        foreach (var cable in connectedCables)
        {
            if (cable == null) continue;
            switch (cableStateOnPress)
            {
                case CableState.On:
                    cable.SetState(true);
                    break;
                case CableState.Off:
                    cable.SetState(false);
                    break;
                case CableState.Toggle:
                    cable.Toggle();
                    break;
            }
        }
    }
}