using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using VInspector;

public class CreditsText : MonoBehaviour
{
    [Header("Text")]
    [SerializeField, Min(0)] private float spacing = 0.5f;
    [SerializeField, Min(0)] private float categorySpacing = 1f;
    [SerializeField] private float titleFontSizeMultiplier = 1.5f;
    [SerializeField] private float titleSpacing = 3f;
    [SerializeField] private float attributionSpacing = 2f;
    [SerializeField] private string titleText = "Credits";
    [SerializeField] private string attributionText = "A game by Daniel Noam";
    [SerializeField, Range(0.01f, 0.5f)] private float attributionPauseDurationRatio = 0.1f;
    [SerializeField] private SOCredit[] credits;
    
    [Header("Movement")]
    [SerializeField] private Vector3 startingOffset = Vector3.down; 
    [SerializeField] private float scrollSpeed = 0.2f;
    
    [Header("Fade")]
    [SerializeField] private bool fadeEnabled = true;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField, Tooltip("Distance where alpha = 1")] private float minFadeDistance; 
    [SerializeField, Tooltip("Distance where alpha = 0")] private float maxFadeDistance = 5f;
    
    [Header("Duration")]
    [SerializeField] private bool useDurationMode;
    [ShowIf("useDurationMode")]
    [SerializeField] private float creditsDuration = 60f;
    [SerializeField] private bool autoStart = true;
    [EndIf]
    
    [Header("Events")]
    [SerializeField, Range(0.01f, 0.99f)] private float sequenceEventTriggerThreshold = 0.2f;
    [SerializeField] private UnityEvent onSequenceEventThresholdReached = new UnityEvent();
    
    [Header("References")]
    [SerializeField] private TextMeshProUGUI creditsTextPrefab;
    [SerializeField] private TextMeshProUGUI creditsHeaderPrefab;
    
    private readonly List<TextMeshProUGUI> _creditTexts = new List<TextMeshProUGUI>();
    private readonly Vector3 _normalizedDirection = Vector3.up;
    private TextMeshProUGUI _attributionTextObject;
    private bool _attributionReachedCenter;
    private float _creditsStartTime;
    private bool _isPaused;
    private bool _hasTriggeredSequenceEvent;
    
    private float CurrentCreditsDuration
    {
        get
        {
            float distance = CalculateTotalScrollDistance();
            if (scrollSpeed <= 0f) return 0f;
            return distance / scrollSpeed;
        }
    }

    private void OnEnable()
    {
        if (TestManager.Instance != null)
        {
            TestManager.Instance.onCreditsSequenceStart.AddListener(OnCreditsSequenceStart);
        }
    }
    
    private void OnDisable()
    {
        if (TestManager.Instance != null)
        {
            TestManager.Instance.onCreditsSequenceStart.RemoveListener(OnCreditsSequenceStart);
        }
    }
    
    private void Start()
    {
        // Validate required components
        if (!creditsTextPrefab || !creditsHeaderPrefab || credits == null || credits.Length == 0)
        {
            Debug.LogError("Credits system is missing required components!");
            return;
        }
        
        // Create all credit text objects
        if (autoStart)
        {
            CreateCreditsText();
            
            // If using duration mode, adjust the scroll speed
            if (useDurationMode && creditsDuration > 0f)
            {
                AdjustSpeedForDuration(creditsDuration);
                _creditsStartTime = Time.time;
            }
        }
    }
    
    private void Update()
    {
        if (_isPaused) return;
        
        // Check if we're using duration mode
        if (useDurationMode)
        {
            UpdateWithDurationMode();
        }
        else if (TestManager.Instance && TestManager.Instance.IsCreditsSequenceActive)
        {
            UpdateWithTestManager();
        }
        else
        {
            // Standard movement
            MoveCredits();
        }
        
        ApplyFadeEffect();
        CheckAttributionPosition();
    }
    
    private void UpdateWithDurationMode()
    {
        float elapsedTime = Time.time - _creditsStartTime;
        
        // If we've reached the end of the total duration
        if (elapsedTime >= creditsDuration)
        {
            // Time is up, restart
            RestartCredits();
            return;
        }
        
        // Calculate progress
        float progress = elapsedTime / creditsDuration;
        
        // Check if we should trigger the event
        if (progress >= sequenceEventTriggerThreshold && !_hasTriggeredSequenceEvent)
        {
            onSequenceEventThresholdReached?.Invoke();
            _hasTriggeredSequenceEvent = true;
        }
        
        // Calculate when we should pause the attribution text
        float pauseStartTime = creditsDuration * (1 - attributionPauseDurationRatio);
        
        // Check if we should pause the attribution text
        if (!_attributionReachedCenter && elapsedTime >= pauseStartTime)
        {
            // Force attribution to center
            PositionAttributionTextAtCenter();
            _attributionReachedCenter = true;
        }
        
        // Standard movement for all other credit texts
        MoveCredits();
    }
    
    private void UpdateWithTestManager()
    {
        if (TestManager.Instance == null) return;
    
        // The CreditsSequenceState goes from 1.0 to 0.0 as the sequence progresses
        float sequenceState = TestManager.Instance.CreditsSequenceState;
    
        // Debug the values to see what's happening
        Debug.Log($"Sequence State: {sequenceState}, Threshold: {sequenceEventTriggerThreshold}, Triggered: {_hasTriggeredSequenceEvent}");
    
        // Check if we should trigger the event - we need to compare directly with the sequenceEventTriggerThreshold
        // The comparison needs to match your threshold definition (less than or equal to threshold)
        if (sequenceState <= sequenceEventTriggerThreshold && !_hasTriggeredSequenceEvent)
        {
            Debug.Log($"Triggering sequence event at state: {sequenceState}");
            onSequenceEventThresholdReached?.Invoke();
            _hasTriggeredSequenceEvent = true;
        }
    
        // Convert sequence state to progress (0 to 1) for other calculations
        float progress = 1f - sequenceState;
    
        // Calculate if we should be pausing now
        float pauseThreshold = 1f - attributionPauseDurationRatio;
    
        if (!_attributionReachedCenter && progress >= pauseThreshold)
        {
            // Force attribution to center
            PositionAttributionTextAtCenter();
            _attributionReachedCenter = true;
        }
    
        // Adjust speed based on sequence state to ensure smooth movement
        // Avoid division by zero and extremely high speeds near the end
        float speedAdjustment = 1f;
        if (sequenceState > 0.05f)
        {
            speedAdjustment = 1f / sequenceState;
            speedAdjustment = Mathf.Clamp(speedAdjustment, 0.1f, 10f); // Prevent extremely high speeds
        }
    
        // Move credits with adjusted speed
        MoveCreditsWithSpeedAdjustment(speedAdjustment);
    }
    
    private void MoveCredits()
    {
        foreach (var text in _creditTexts)
        {
            if (!text) continue;
            
            RectTransform textRT = text.GetComponent<RectTransform>();
            if (!textRT) continue;
            
            // If this is the attribution text, and it has reached the center, don't move it
            if (_attributionReachedCenter && text == _attributionTextObject)
                continue;
            
            Vector3 newPosition = textRT.position + _normalizedDirection * (scrollSpeed * Time.deltaTime);
            textRT.position = newPosition;
        }
    }
    
    private void MoveCreditsWithSpeedAdjustment(float speedMultiplier)
    {
        foreach (var text in _creditTexts)
        {
            if (!text) continue;
            
            RectTransform textRT = text.GetComponent<RectTransform>();
            if (!textRT) continue;
            
            // If this is the attribution text, and it has reached the center, don't move it
            if (_attributionReachedCenter && text == _attributionTextObject)
                continue;
            
            Vector3 newPosition = textRT.position + _normalizedDirection * (scrollSpeed * speedMultiplier * Time.deltaTime);
            textRT.position = newPosition;
        }
    }
    
    private void CheckAttributionPosition()
    {
        if (!_attributionTextObject || _attributionReachedCenter)
            return;
        
        // Get the attribution text position and check if it's at the center
        RectTransform attributionRect = _attributionTextObject.GetComponent<RectTransform>();
        if (attributionRect)
        {
            // Calculate the distance between attribution text and the center on the scroll axis
            float dotProduct = Vector3.Dot(attributionRect.position - transform.position, _normalizedDirection);
            
            // If attribution text is at or past the center (considering the direction)
            if (Mathf.Abs(dotProduct) < 0.1f) // Small threshold for "center"
            {
                _attributionReachedCenter = true;
                PositionAttributionTextAtCenter(); // Make sure it's exactly at center
            }
        }
    }
    
    private void PositionAttributionTextAtCenter()
    {
        if (_attributionTextObject == null) return;
        
        RectTransform attributionRect = _attributionTextObject.GetComponent<RectTransform>();
        if (attributionRect != null)
        {
            // Set the attribution text exactly at the center
            attributionRect.position = transform.position;
            
            // Ensure full visibility
            Color textColor = _attributionTextObject.color;
            textColor.a = 1.0f;
            _attributionTextObject.color = textColor;
            
            _attributionReachedCenter = true;
            Debug.Log("Attribution text positioned at center");
        }
    }
    
    private void OnCreditsSequenceStart()
    {
        // Set duration mode to true
        useDurationMode = true;
    
        // Restart the credits
        CreateCreditsText();
    
        // Use the duration from TestManager, not the local creditsDuration
        if (TestManager.Instance != null && TestManager.Instance.CreditsSequenceDuration > 0f)
        {
            AdjustSpeedForDuration(TestManager.Instance.CreditsSequenceDuration);
        }
    
        // Ensure credits are playing
        _isPaused = false;
        _attributionReachedCenter = false;
        _hasTriggeredSequenceEvent = false;
        _creditsStartTime = Time.time;
        
        Debug.Log("Credits sequence started");
    }

    private void CreateCreditsText()
    {
        // Clear any existing texts
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        _creditTexts.Clear();
        _attributionReachedCenter = false;
        
        // Set initial position with the offset from transform's position
        Vector3 currentPosition = transform.position + startingOffset;
        float currentYOffset = 0f;
        
        // Add main title at the beginning
        TextMeshProUGUI mainTitle = Instantiate(creditsHeaderPrefab, transform);
        mainTitle.text = titleText;
        mainTitle.fontSize *= titleFontSizeMultiplier;
        
        // Position the main title with the current position
        RectTransform titleRect = mainTitle.GetComponent<RectTransform>();
        titleRect.position = new Vector3(
            currentPosition.x, 
            currentPosition.y - currentYOffset,
            currentPosition.z
        );
        
        // Set initial alpha if fading is enabled
        if (fadeEnabled)
        {
            // Calculate the distance from center to determine initial alpha
            float distance = Vector3.Distance(titleRect.position, transform.position);
            float alpha = CalculateAlphaFromDistance(distance);
            
            Color textColor = mainTitle.color;
            textColor.a = alpha;
            mainTitle.color = textColor;
        }
        
        currentYOffset += titleSpacing;
        
        _creditTexts.Add(mainTitle);
        
        // Group credits by category
        Dictionary<CreditsCategories, List<SOCredit>> groupedCredits = new Dictionary<CreditsCategories, List<SOCredit>>();
        
        foreach (SOCredit credit in credits)
        {
            if (!credit) continue;
            
            if (!groupedCredits.ContainsKey(credit.CreditCategory))
            {
                groupedCredits[credit.CreditCategory] = new List<SOCredit>();
            }
            
            groupedCredits[credit.CreditCategory].Add(credit);
        }
        
        // Create the credit texts, organized by category
        foreach (var category in groupedCredits.Keys)
        {
            // Add category header
            TextMeshProUGUI categoryHeader = Instantiate(creditsHeaderPrefab, transform);
            categoryHeader.text = category.ToString().ToUpper();
            
            // Position the category header with the current position
            RectTransform headerRect = categoryHeader.GetComponent<RectTransform>();
            headerRect.position = new Vector3(
                currentPosition.x, 
                currentPosition.y - currentYOffset,
                currentPosition.z
            );
            
            // Set initial alpha if fading is enabled
            if (fadeEnabled)
            {
                // Calculate the distance from center to determine initial alpha
                float distance = Vector3.Distance(headerRect.position, transform.position);
                float alpha = CalculateAlphaFromDistance(distance);
                
                Color textColor = categoryHeader.color;
                textColor.a = alpha;
                categoryHeader.color = textColor;
            }
            
            currentYOffset += spacing;
            
            _creditTexts.Add(categoryHeader);
            
            // Add all credits in this category
            foreach (SOCredit credit in groupedCredits[category])
            {
                TextMeshProUGUI creditText = Instantiate(creditsTextPrefab, transform);
                creditText.text = credit.CreditString;
                
                // Position the credit text with the current position
                RectTransform textRect = creditText.GetComponent<RectTransform>();
                textRect.position = new Vector3(
                    currentPosition.x, 
                    currentPosition.y - currentYOffset,
                    currentPosition.z
                );
                
                // Set initial alpha if fading is enabled
                if (fadeEnabled)
                {
                    // Calculate the distance from center to determine initial alpha
                    float distance = Vector3.Distance(textRect.position, transform.position);
                    float alpha = CalculateAlphaFromDistance(distance);
                    
                    Color textColor = creditText.color;
                    textColor.a = alpha;
                    creditText.color = textColor;
                }
                
                currentYOffset += spacing;
                
                _creditTexts.Add(creditText);
            }
            
            // Add extra spacing between categories
            currentYOffset += categorySpacing;
        }
        
        // Add attribution text at the end with extra spacing
        TextMeshProUGUI gameByText = Instantiate(creditsTextPrefab, transform);
        gameByText.text = attributionText;
        _attributionTextObject = gameByText; // Store reference to the attribution text object
        
        // Position with extra spacing
        RectTransform gameByRect = gameByText.GetComponent<RectTransform>();
        currentYOffset += attributionSpacing;
        
        gameByRect.position = new Vector3(
            currentPosition.x, 
            currentPosition.y - currentYOffset,
            currentPosition.z
        );
        
        // Set initial alpha if fading is enabled
        if (fadeEnabled)
        {
            // Calculate the distance from center to determine initial alpha
            float distance = Vector3.Distance(gameByRect.position, transform.position);
            float alpha = CalculateAlphaFromDistance(distance);
            
            Color textColor = gameByText.color;
            textColor.a = alpha;
            gameByText.color = textColor;
        }
        
        _creditTexts.Add(gameByText);
    }
    
    private void ApplyFadeEffect()
    {
        if (!fadeEnabled) return;
        
        foreach (TextMeshProUGUI text in _creditTexts)
        {
            if (!text) continue;
            
            RectTransform textRT = text.GetComponent<RectTransform>();
            if (!textRT) continue;
            
            // Calculate absolute distance from center
            float distance = Vector3.Distance(textRT.position, transform.position);
            
            // Calculate alpha based on distance
            float alpha = CalculateAlphaFromDistance(distance);
            
            // If this is the attribution text, and it has reached center, keep it fully visible
            if (_attributionReachedCenter && text == _attributionTextObject)
            {
                alpha = 1.0f;
            }
            
            // Apply fade to this specific credit text
            Color textColor = text.color;
            textColor.a = Mathf.Lerp(textColor.a, alpha, Time.deltaTime * fadeSpeed);
            text.color = textColor;
        }
    }
    
    // Helper method to calculate alpha based on distance
    private float CalculateAlphaFromDistance(float distance)
    {
        if (distance <= minFadeDistance)
        {
            return 1.0f; // Within min fade distance - fully visible
        }
        else if (distance >= maxFadeDistance)
        {
            return 0.0f; // Beyond max fade distance - fully transparent
        }
        else
        {
            // Between min and max - calculate fade
            return 1.0f - ((distance - minFadeDistance) / (maxFadeDistance - minFadeDistance));
        }
    }
    
    // Calculate the total distance the credits need to travel
    private float CalculateTotalScrollDistance()
    {
        if (_creditTexts.Count == 0)
        {
            return 0f;
        }
        
        // Get first and last credit positions
        Vector3 firstPosition = _creditTexts[0].transform.position;
        Vector3 lastPosition = _creditTexts[^1].transform.position;
        
        // Add the height of the last credit to ensure it scrolls completely off-screen
        if (_creditTexts[^1].TryGetComponent<RectTransform>(out RectTransform lastRect))
        {
            lastPosition += _normalizedDirection * lastRect.rect.height;
        }
        
        // Calculate the position where the last credit should end (the center position plus max fade distance)
        Vector3 endPosition = transform.position + (_normalizedDirection * maxFadeDistance);
        
        // Calculate total distance: from first credit to the point where last credit is fully offscreen
        float totalDistance = Vector3.Distance(firstPosition, endPosition) + 
                              Vector3.Distance(lastPosition, firstPosition);
        
        return totalDistance;
    }
    
    // Adjust the scroll speed based on the desired duration
    private void AdjustSpeedForDuration(float durationInSeconds)
    {
        if (durationInSeconds <= 0f)
        {
            Debug.LogError("Duration must be greater than zero!");
            return;
        }
        
        // Account for the attribution pause time when calculating speed
        // We want the credits to reach their final positions after (1-attributionPauseDurationRatio) of the total time
        float effectiveDuration = durationInSeconds * (1 - attributionPauseDurationRatio);
        
        float totalDistance = CalculateTotalScrollDistance();
        
        // Calculate the required speed based on the distance and effective duration
        float requiredSpeed = totalDistance / effectiveDuration;
        
        // Set the scroll speed
        scrollSpeed = requiredSpeed;
        
        Debug.Log($"Adjusted scroll speed to {scrollSpeed} units/sec for duration: {durationInSeconds}s");
    }
    
    [Button]
    private void RestartCredits()
    {
        // Reset all credits to initial positions
        CreateCreditsText();
        
        // If using duration mode, adjust the scroll speed
        if (useDurationMode && creditsDuration > 0f)
        {
            AdjustSpeedForDuration(creditsDuration);
        }
        
        // Reset the attribution position flag
        _attributionReachedCenter = false;
        _hasTriggeredSequenceEvent = false;
        
        // Record the start time
        _creditsStartTime = Time.time;
        
        // Ensure credits are playing
        _isPaused = false;
    }
    
    [Button]
    private void TogglePause()
    {
        _isPaused = !_isPaused;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!fadeEnabled) return;
            
        // Define colors for visualization
        Color minFadeColor = new Color(0, 1, 0, 0.8f);    // Green - full visibility
        Color maxFadeColor = new Color(1, 0, 0, 0.8f);    // Red - no visibility
        Color startPositionColor = new Color(1, 1, 0, 0.8f); // Yellow - start position
        
        // Get the center of this object
        Vector3 center = transform.position;
        
        // Draw min fade distance (alpha = 1)
        Gizmos.color = minFadeColor;
        DrawCircle(center, minFadeDistance, 32);
        
        // Draw max fade distance (alpha = 0)
        Gizmos.color = maxFadeColor;
        DrawCircle(center, maxFadeDistance, 32);
        
        // Draw start position
        Gizmos.color = startPositionColor;
        DrawCircle(center + startingOffset, 0.5f, 32);
        
        // Draw labels
        DrawLabel(center + new Vector3(0, minFadeDistance, 0), "Min Fade (α=1)");
        DrawLabel(center + new Vector3(0, maxFadeDistance, 0), "Max Fade (α=0)");
        DrawLabel(center + startingOffset, "Start Position");
        
        // Draw current duration information if using duration mode
        if (useDurationMode)
        {
            string durationInfo = $"Duration: {CurrentCreditsDuration:F1}s";
            string attributionInfo = $"Attribution Pause: {creditsDuration * attributionPauseDurationRatio:F1}s";
            DrawLabel(center + new Vector3(0, -2, 0), durationInfo);
            DrawLabel(center + new Vector3(0, -2.5f, 0), attributionInfo);
        }
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        // Draw a circle in the scene view
        float angle = 0f;
        float angleStep = 360f / segments;
        
        Vector3 prevPoint = center + new Vector3(Mathf.Cos(Mathf.Deg2Rad * angle) * radius, Mathf.Sin(Mathf.Deg2Rad * angle) * radius, 0);
        
        for (int i = 0; i < segments + 1; i++)
        {
            angle += angleStep;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(Mathf.Deg2Rad * angle) * radius, Mathf.Sin(Mathf.Deg2Rad * angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
    
    private void DrawLabel(Vector3 position, string text)
    {
        // Set label style
        GUIStyle style = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            normal =
            {
                textColor = Color.white
            }
        };
        UnityEditor.Handles.Label(position, text, style);
    }
#endif
}