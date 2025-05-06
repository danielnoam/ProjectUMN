using UnityEngine;

public class HoverEffect : MonoBehaviour
{
    [Header("Hover Settings")]
    [Tooltip("Animation curve to define the hover path")]
    public AnimationCurve hoverCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("The height of the hover effect")]
    public float hoverHeight = 0.5f;
    
    [Tooltip("How fast the object moves through the curve")]
    public float hoverSpeed = 0.3f;
    
    // Private variables
    private Vector3 _startPosition;
    private float _timeOffset;
    
    private void Start()
    {
        _startPosition = transform.position;
        _timeOffset = Random.Range(0f, 1f);
    }
    
    private void Update()
    {
        float time = Mathf.PingPong(Time.time * hoverSpeed + _timeOffset, 1f);
        
        // Calculate hover position using the animation curve
        Vector3 newPosition = _startPosition;
        newPosition.y += hoverCurve.Evaluate(time) * hoverHeight;
        
        // Apply position
        transform.position = newPosition;
    }
}