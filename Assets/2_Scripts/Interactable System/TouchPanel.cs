using System.Collections;
using UnityEngine;
using UnityEngine.Events;


public class TouchPanel : MonoBehaviour
{

    [Header("Panel Visuals")]
    [SerializeField] private Transform panelTransform;            
    [SerializeField] private float animationSpeed = 5f;
    [SerializeField] private float autoReleaseDelay = 0.5f;
    

    [Header("Panel Events")]
    [SerializeField] private UnityEvent onPanelPressed; 
    
    private bool _isPressed = false;
    private Coroutine _releaseCoroutine;
    
    
    public void PressPanel()
    {
        if (_isPressed) return;

        _isPressed = true;
        

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
