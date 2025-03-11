using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[SelectionBase]
[RequireComponent(typeof(Interactable))]
public class PillerButton : MonoBehaviour
{
    [Header("Button Visuals")]
    [SerializeField] private Transform buttonTransform;            
    [SerializeField, Min(0.1f)] private float buttonAnimationHeight = 0.1f;            
    [SerializeField] private float buttonAnimationSpeed = 5f;
    [SerializeField] private float autoReleaseDelay = 0.5f;
    
    [Header("Button Events")]
    [SerializeField] private UnityEvent onButtonPressed; 
    
    private bool _isPressed = false;
    private Vector3 _initialButtonPosition;                    
    private Vector3 _pressedButtonPosition;
    private Coroutine _releaseCoroutine;

    private void Awake()
    {
        _initialButtonPosition = buttonTransform.localPosition;
        _pressedButtonPosition = _initialButtonPosition - new Vector3(0, buttonAnimationHeight, 0);
    }

    private void Update()
    {
        // Animate plate position based on activation state
        Vector3 targetPosition = _isPressed ? _pressedButtonPosition : _initialButtonPosition;
        buttonTransform.localPosition = Vector3.Lerp(
            buttonTransform.localPosition,
            targetPosition,
            Time.deltaTime * buttonAnimationSpeed
        );
    }
    
    public void PressButton()
    {
        if (_isPressed) return;

        _isPressed = true;
        

        onButtonPressed.Invoke();
        

        if (_releaseCoroutine != null) StopCoroutine(_releaseCoroutine);
            
        _releaseCoroutine = StartCoroutine(ReleaseButtonAfterDelay());
    }
    
    private IEnumerator ReleaseButtonAfterDelay()
    {
        // Wait for the specified delay time
        yield return new WaitForSeconds(autoReleaseDelay);
        
        // Release the button
        _isPressed = false;
        _releaseCoroutine = null;
    }
}