using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

public class CreditsText : MonoBehaviour
{
    [Header("Credits")]
    [SerializeField] private Vector3 startingOffset = Vector3.down; 
    [SerializeField, Min(0)] private float spacing = 0.5f;
    [SerializeField, Min(0)] private float categorySpacing = 1f;
    [SerializeField] private float titleFontSizeMultiplier = 1.5f;
    [SerializeField] private float titleExtraSpacing = 2f;
    [SerializeField] private float attributionExtraSpacing = 2f;
    [SerializeField] private string titleText = "Credits";
    [SerializeField] private string attributionText = "A game by Daniel Noam";
    [SerializeField, Range(0.01f, 0.5f)] private float attributionPauseDurationRatio = 0.1f;
    [SerializeField] private SOCredit[] credits;
    
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
    [SerializeField, Range(0.01f, 0.99f)] private float sequenceEventTriggerThreshold = 0.8f;
    [SerializeField] private UnityEvent onSequenceEventThresholdReached = new UnityEvent();
    
    [Header("References")]
    [SerializeField] private TextMeshProUGUI creditsTextPrefab;
    [SerializeField] private TextMeshProUGUI creditsHeaderPrefab;
    
    private readonly List<TextMeshProUGUI> _creditTexts = new List<TextMeshProUGUI>();
    private readonly Vector3 _normalizedDirection = Vector3.up; // Fixed upward scrolling
    private TextMeshProUGUI _attributionTextObject;
    private bool _attributionReachedCenter;
    private float _creditsStartTime;
    private float _effectiveDuration;
    private float _totalScrollDistance;
    private bool _hasTriggeredSequenceEvent;
    private float _calculatedScrollSpeed;
    
    private float CurrentCreditsDuration
    {
        get
        {
            if (_calculatedScrollSpeed <= 0f) return 0f;
            return _totalScrollDistance / _calculatedScrollSpeed;
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
    
    private void OnCreditsSequenceStart()
    {
        // Restart the credits to sync with the sequence
        CreateCreditsText();

        // Use the duration from TestManager if available
        if (TestManager.Instance != null && TestManager.Instance.CreditsSequenceDuration > 0f)
        {
            AdjustSpeedForDuration(TestManager.Instance.CreditsSequenceDuration);
        }

        // Reset flags
        _attributionReachedCenter = false;
        _hasTriggeredSequenceEvent = false;
        _creditsStartTime = Time.time;
        
        Debug.Log("Credits sequence started");
    }

    private void Start()
    {
        // Validate required components
        if (!creditsTextPrefab || !creditsHeaderPrefab || credits == null || credits.Length == 0)
        {
            Debug.LogError("Credits system is missing required components!");
            return;
        }
        
        // If using duration mode, adjust the scroll speed and start the credits
        if (useDurationMode && creditsDuration > 0f && autoStart)
        {
            CreateCreditsText();
            AdjustSpeedForDuration(creditsDuration);
            _creditsStartTime = Time.time;
        }
    }
    
    private void Update()
    {
        // Check how we should update the credits movement
        if (useDurationMode)
        {
            UpdateCreditsWithDurationMode();
        }
        else if (TestManager.Instance != null && TestManager.Instance.IsCreditsSequenceActive)
        {
            UpdateCreditsWithSequenceState();
        }
        
        ApplyFadeEffect();
    }
    
    private void UpdateCreditsWithDurationMode()
    {
        float elapsedTime = Time.time - _creditsStartTime;
        
        // If we've reached the end of the total duration
        if (elapsedTime >= creditsDuration)
        {
            // Time is up, restart
            RestartCredits();
            return;
        }
        
        // Calculate progress based on elapsed time
        float progress = elapsedTime / creditsDuration;
        
        // Check if we've reached the custom threshold
        if (progress >= sequenceEventTriggerThreshold && !_hasTriggeredSequenceEvent)
        {
            onSequenceEventThresholdReached?.Invoke();
            _hasTriggeredSequenceEvent = true;
        }
        
        UpdateCreditsPositioning(progress);
    }
    
    private void UpdateCreditsWithSequenceState()
    {
        if (TestManager.Instance == null) return;
        
        // Convert sequence state (which goes from 1 to 0) to progress (0 to 1)
        float progress = 1f - TestManager.Instance.CreditsSequenceState;
        
        // Check if we've reached the custom threshold
        if (TestManager.Instance.CreditsSequenceState <= sequenceEventTriggerThreshold && !_hasTriggeredSequenceEvent)
        {
            // Fire the event when reaching the threshold
            onSequenceEventThresholdReached?.Invoke();
            _hasTriggeredSequenceEvent = true;
        }
        
        UpdateCreditsPositioning(progress);
    }
    
    // Unified method to handle credits positioning based on progress
    private void UpdateCreditsPositioning(float progress)
    {
        // Calculate when to pause attribution text
        float pauseStartRatio = 1 - attributionPauseDurationRatio;
        
        // Ensure we don't divide by zero
        if (pauseStartRatio <= 0f)
        {
            pauseStartRatio = 0.01f;
        }
        
        // If we've reached the pause point
        if (progress >= pauseStartRatio)
        {
            // Position attribution text at center ONCE when we first reach this point
            if (!_attributionReachedCenter)
            {
                PositionAttributionTextAtCenter();
                _attributionReachedCenter = true;
                Debug.Log($"Attribution reached center at progress: {progress}");
            }
            
            // Keep all other credits in their positions at the pause point
            // This prevents them from continuing to scroll off-screen
            if (progress > pauseStartRatio)
            {
                // Don't move any further during pause phase
                return;
            }
        }
        
        // Scale progress to account for the pause ratio
        float scaledProgress = Mathf.Min(progress / pauseStartRatio, 1.0f);
        
        // Move credits with scaled progress
        MoveCreditsWithProgress(scaledProgress);
    }
    
    private void MoveCreditsWithProgress(float progress)
    {
        // Clamp progress to 0-1 range
        progress = Mathf.Clamp01(progress);

        // The starting position for all texts
        Vector3 basePosition = transform.position + startingOffset;
        
        // Get center position
        Vector3 centerPosition = transform.position;

        // Process each text element
        for (int i = 0; i < _creditTexts.Count; i++)
        {
            TextMeshProUGUI text = _creditTexts[i];
            if (text == null) continue;
            
            RectTransform textRT = text.GetComponent<RectTransform>();
            if (textRT == null) continue;
            
            // Special handling for attribution text
            if (text == _attributionTextObject)
            {
                // For the attribution text, we want to move it directly toward the center
                // Calculate its target position (center)
                Vector3 startPos = basePosition + new Vector3(0, -CalculateAttributionInitialOffset(), 0);
                
                // Lerp between start position and center position based on progress
                Vector3 newPosition = Vector3.Lerp(startPos, centerPosition, progress);
                
                // Apply the new position
                textRT.position = newPosition;
                
                // If we're at full progress, ensure it's exactly at center
                if (progress >= 0.99f)
                {
                    textRT.position = centerPosition;
                }
            }
            else
            {
                // For other texts, calculate standard scrolling positions
                // Get the reversed index - this makes the first item (title) positioned on top
                int reversedIndex = (_creditTexts.Count - 1) - i;
                
                // Calculate the offset based on index (vertical spacing)
                float offsetDistance = reversedIndex * spacing;
                
                // Add extra spacing for the title (first element)
                if (i == 0)
                {
                    offsetDistance += titleExtraSpacing * spacing;
                }
                
                // Calculate the distance to move based on progress
                float moveDistance = _totalScrollDistance * progress;
                
                // Set the new position
                Vector3 newPosition = basePosition + (_normalizedDirection * (moveDistance - offsetDistance));
                textRT.position = newPosition;
            }
        }
    }
    
    // Helper method to calculate initial vertical offset for attribution text
    private float CalculateAttributionInitialOffset()
    {
        float offset = 0f;
        
        // Count number of texts
        int textCount = _creditTexts.Count;
        
        // Find attribution text index
        int attributionIndex = -1;
        for (int i = 0; i < textCount; i++)
        {
            if (_creditTexts[i] == _attributionTextObject)
            {
                attributionIndex = i;
                break;
            }
        }
        
        if (attributionIndex >= 0)
        {
            // Get the reversed index - for positioning calculation
            int reversedIndex = (textCount - 1) - attributionIndex;
            
            // Calculate offset based on position in the list
            offset = reversedIndex * spacing + attributionExtraSpacing;
        }
        
        return offset;
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
            
            Debug.Log("Attribution text positioned at center");
        }
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
        Vector3 startPosition = transform.position + startingOffset;
        float currentYOffset = 0f;
        
        // We'll collect all text elements first, then add them in reverse order
        List<TextMeshProUGUI> allTexts = new List<TextMeshProUGUI>();
        
        // Add main title first (but it will be added last to _creditTexts to appear at the top)
        TextMeshProUGUI mainTitle = Instantiate(creditsHeaderPrefab, transform);
        mainTitle.text = titleText;
        mainTitle.fontSize *= titleFontSizeMultiplier;
        
        // Position the title
        RectTransform titleRect = mainTitle.GetComponent<RectTransform>();
        if (titleRect != null)
        {
            titleRect.position = new Vector3(
                startPosition.x, 
                startPosition.y - currentYOffset,
                startPosition.z
            );
            
            // Set initial alpha if fading is enabled
            if (fadeEnabled)
            {
                float distance = Vector3.Distance(titleRect.position, transform.position);
                float alpha = CalculateAlphaFromDistance(distance);
                
                Color textColor = mainTitle.color;
                textColor.a = alpha;
                mainTitle.color = textColor;
            }
        }
        
        currentYOffset += spacing * titleExtraSpacing;
        allTexts.Add(mainTitle);
        
        // Group credits by category
        Dictionary<CreditsCategories, List<SOCredit>> groupedCredits = new Dictionary<CreditsCategories, List<SOCredit>>();
        
        foreach (SOCredit credit in credits)
        {
            if (credit == null) continue;
            
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
            
            // Position the category header
            RectTransform headerRect = categoryHeader.GetComponent<RectTransform>();
            if (headerRect != null)
            {
                headerRect.position = new Vector3(
                    startPosition.x, 
                    startPosition.y - currentYOffset,
                    startPosition.z
                );
                
                // Set initial alpha if fading is enabled
                if (fadeEnabled)
                {
                    float distance = Vector3.Distance(headerRect.position, transform.position);
                    float alpha = CalculateAlphaFromDistance(distance);
                    
                    Color textColor = categoryHeader.color;
                    textColor.a = alpha;
                    categoryHeader.color = textColor;
                }
            }
            
            currentYOffset += spacing;
            allTexts.Add(categoryHeader);
            
            // Add all credits in this category
            foreach (SOCredit credit in groupedCredits[category])
            {
                TextMeshProUGUI creditText = Instantiate(creditsTextPrefab, transform);
                creditText.text = credit.CreditString;
                
                // Position the credit text
                RectTransform textRect = creditText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.position = new Vector3(
                        startPosition.x, 
                        startPosition.y - currentYOffset,
                        startPosition.z
                    );
                    
                    // Set initial alpha if fading is enabled
                    if (fadeEnabled)
                    {
                        float distance = Vector3.Distance(textRect.position, transform.position);
                        float alpha = CalculateAlphaFromDistance(distance);
                        
                        Color textColor = creditText.color;
                        textColor.a = alpha;
                        creditText.color = textColor;
                    }
                }
                
                currentYOffset += spacing;
                allTexts.Add(creditText);
            }
            
            // Add extra spacing between categories
            currentYOffset += categorySpacing;
        }
        
        // Add attribution text at the end
        TextMeshProUGUI gameByText = Instantiate(creditsTextPrefab, transform);
        gameByText.text = attributionText;
        _attributionTextObject = gameByText;
        
        // Position with extra spacing
        RectTransform gameByRect = gameByText.GetComponent<RectTransform>();
        if (gameByRect != null)
        {
            currentYOffset += attributionExtraSpacing;
            
            gameByRect.position = new Vector3(
                startPosition.x, 
                startPosition.y - currentYOffset,
                startPosition.z
            );
            
            // Set initial alpha if fading is enabled
            if (fadeEnabled)
            {
                float distance = Vector3.Distance(gameByRect.position, transform.position);
                float alpha = CalculateAlphaFromDistance(distance);
                
                Color textColor = gameByText.color;
                textColor.a = alpha;
                gameByText.color = textColor;
            }
        }
        
        allTexts.Add(gameByText);
        
        // Now, add all texts to _creditTexts in REVERSE order
        // This will make the attribution appear at the bottom and the title at the top
        for (int i = allTexts.Count - 1; i >= 0; i--)
        {
            _creditTexts.Add(allTexts[i]);
        }
        
        // Calculate total scroll distance
        _totalScrollDistance = CalculateTotalScrollDistance();
        
        Debug.Log($"Created {_creditTexts.Count} credit texts with total scroll distance: {_totalScrollDistance}");
    }
    
    private void ApplyFadeEffect()
    {
        if (!fadeEnabled) return;
        
        foreach (TextMeshProUGUI text in _creditTexts)
        {
            if (text == null) continue;
            
            RectTransform textRT = text.GetComponent<RectTransform>();
            if (textRT == null) continue;
            
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
    
    // Calculate the total distance for credits scrolling
    private float CalculateTotalScrollDistance()
    {
        // Focus on calculating the distance needed for attribution text to reach center
        if (_attributionTextObject == null)
        {
            Debug.LogError("Attribution text is null! Cannot calculate distance.");
            return 10f; // Default fallback
        }
        
        Vector3 attributionStartPos = _attributionTextObject.transform.position;
        Vector3 centerPos = transform.position;
        
        // Calculate direct distance from attribution text to center
        float attributionToCenterDistance = Vector3.Distance(attributionStartPos, centerPos);
        
        Debug.Log($"Attribution text distance to center: {attributionToCenterDistance}");
        
        return attributionToCenterDistance;
    }
    
    // Adjust the speed for duration calculation
    private void AdjustSpeedForDuration(float durationInSeconds)
    {
        if (durationInSeconds <= 0f)
        {
            Debug.LogError("Duration must be greater than zero!");
            return;
        }
        
        // Protect against potential division by zero
        float pauseRatio = Mathf.Clamp01(attributionPauseDurationRatio);
        if (pauseRatio >= 0.99f)
        {
            pauseRatio = 0.99f;
        }
        
        // Calculate the actual scrolling time (excluding pause time)
        float activeScrollTime = durationInSeconds * (1f - pauseRatio);
        
        // Get the distance from attribution text to center
        float attributionDistance = CalculateTotalScrollDistance();
        _totalScrollDistance = attributionDistance;
        
        // Ensure we don't divide by zero
        if (activeScrollTime <= 0.01f)
        {
            activeScrollTime = 0.01f;
        }
        
        // Calculate the required speed to have attribution text reach center at right time
        _calculatedScrollSpeed = attributionDistance / activeScrollTime;
        _effectiveDuration = durationInSeconds; // Store the full duration including pause
        
        Debug.Log($"Adjusted scroll speed: {_calculatedScrollSpeed} units/sec for duration: {durationInSeconds}s (active scroll: {activeScrollTime}s)");
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
        
        // Reset flags
        _attributionReachedCenter = false;
        _hasTriggeredSequenceEvent = false;
        
        // Record the start time
        _creditsStartTime = Time.time;
        
        Debug.Log("Credits restarted");
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