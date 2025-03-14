using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;


[SelectionBase]
[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(AudioSource))]
public class TouchPanel : MonoBehaviour
{

    [Header("Panel Settings")]
    [SerializeField] private float autoReleaseDelay = 0.5f;
    
    [Header("Panel Feedback")]
    [SerializeField] private Transform panelTransform;
    [SerializeField] private SOAudioEvent sfxPanelPress;

    [Header("Panel Events")]
    [SerializeField] private UnityEvent onPanelPressed; 
    
    private bool _isPressed = false;
    private Coroutine _releaseCoroutine;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public void PressPanel()
    {
        if (_isPressed) return;

        _isPressed = true;
        
        sfxPanelPress?.Play(_audioSource);

        onPanelPressed.Invoke();
        

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
