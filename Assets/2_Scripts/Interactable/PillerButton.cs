
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

[SelectionBase]
[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(AudioSource))]
public class PillerButton : MonoBehaviour
{
    [Header("Button Feedback")]
    [SerializeField] private Transform buttonTransform;     
    [SerializeField] private SOAudioEvent sfxButtonPress;
    [SerializeField] private float buttonAnimationHeight = 0.1f;            
    [SerializeField] private float buttonAnimationSpeed = 5f;
    [SerializeField] private float autoReleaseDelay = 0.5f;
    [SerializeField] private CableState cableStateOnPress = CableState.Toggle;
    [SerializeField] private Cable[] connectedCables;
    
    [Header("Button Events")]
    [SerializeField] private UnityEvent onButtonPressed; 
    
    private bool _isPressed = false;
    private Vector3 _initialButtonPosition;                    
    private Vector3 _pressedButtonPosition;
    private Coroutine _releaseCoroutine;
    private AudioSource _audioSource;

    private void Awake()
    {
        _initialButtonPosition = buttonTransform.localPosition;
        _pressedButtonPosition = _initialButtonPosition - new Vector3(0, buttonAnimationHeight, 0);
        _audioSource = GetComponent<AudioSource>();
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
    
    [Button]
    public void PressButton()
    {
        if (_isPressed) return;

        _isPressed = true;
        

        sfxButtonPress?.Play(_audioSource);
        ToggleConnectedCables();
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