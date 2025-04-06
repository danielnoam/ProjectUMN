using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VInspector;

public class CreditsText : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private float scrollSpeed = 0.2f;
    [SerializeField] private float scrollSpeedMultiplier = 1f;
    [SerializeField, Min(0)] private float spacing = 0.5f;
    [SerializeField, Min(0)] private float categorySpacing = 1f;
    [SerializeField] private Vector3 scrollDirection = Vector3.up;
    [SerializeField] private Vector3 startingOffset = Vector3.down; 
    
    [Header("Loop")]
    [SerializeField] private bool loopCredits;
    [SerializeField] private float loopDelay = 1f;
    [SerializeField] private float resetDistance = 15f;
    
    [Header("Fade")]
    [SerializeField] private bool fadeEnabled = true;
    [SerializeField] private float fadeSpeed = 0.2f;
    [SerializeField ,Tooltip("Distance where alpha = 1")] private float minFadeDistance; 
    [SerializeField ,Tooltip("Distance where alpha = 0")] private float maxFadeDistance = 5f;
    
    [Header("Credits")]
    [SerializeField] private string titleText = "Credits";
    [SerializeField] private string attributionText = "A game by Daniel Noam";
    [SerializeField] private SOCredit[] credits;
    
    [Header("References")]
    [SerializeField] private TextMeshProUGUI creditsTextPrefab;
    [SerializeField] private TextMeshProUGUI creditsHeaderPrefab;
    
    
    private readonly List<TextMeshProUGUI> _creditTexts = new List<TextMeshProUGUI>();
    private bool _isPaused;
    private bool _useScrollSpeedMultiplier;
    private Vector3 _normalizedDirection;
    private Coroutine _loopCoroutine;
    
    private void Start()
    {
        if (!creditsTextPrefab || !creditsHeaderPrefab || credits.Length == 0)
        {
            Debug.LogError("Credits system is missing required components!");
            return;
        }
        
        // Normalize the direction vector
        _normalizedDirection = scrollDirection.normalized;
        
        // Create all credit text objects
        if (autoStart) CreateCreditsText();
    }
    
    private void CreateCreditsText()
    {
        // Clear any existing texts
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        _creditTexts.Clear();
        
        // Set initial position with the offset from transform's position
        Vector3 currentPosition = transform.position + startingOffset;
        float currentYOffset = 0f;
        
        // Add main title at the beginning
        TextMeshProUGUI mainTitle = Instantiate(creditsHeaderPrefab, transform);
        mainTitle.text = titleText;
        mainTitle.fontSize *= 1.5f; // Make the font larger based on multiplier
        
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
        
        currentYOffset += spacing * 2; // Add extra spacing after main title
        
        _creditTexts.Add(mainTitle);
        
        // Group credits by category
        Dictionary<CreditsCategories, List<SOCredit>> groupedCredits = new Dictionary<CreditsCategories, List<SOCredit>>();
        
        foreach (SOCredit credit in credits)
        {
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
        
        // Position with extra spacing
        RectTransform gameByRect = gameByText.GetComponent<RectTransform>();
        currentYOffset += spacing * 2; // Apply the requested extra spacing
        
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
    
    private void Update()
    {
        if (_isPaused) return;
        
        MoveCredits();
        ApplyFadeEffect();
        
        // Check if we need to loop
        if (loopCredits && _creditTexts.Count > 0)
        {
            CheckForLooping();
        }
    }
    
    private void CheckForLooping()
    {
        // Use the last text (attribution) as our reference point for when credits have scrolled past
        TextMeshProUGUI lastText = _creditTexts[^1];
        
        if (!lastText) return;
        
        // Calculate distance from center for the last text element
        float distance = Vector3.Distance(lastText.transform.position, transform.position);
        
        // If the last text has passed the reset distance, and we're not already restarting
        if (distance > resetDistance && _loopCoroutine == null)
        {
            _loopCoroutine = StartCoroutine(LoopCreditsAfterDelay());
        }
    }
    
    private IEnumerator LoopCreditsAfterDelay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(loopDelay);
        
        // Restart the credits
        RestartCredits();
        
        // Reset the coroutine reference
        _loopCoroutine = null;
    }
    
    private void MoveCredits()
    {
        foreach (TextMeshProUGUI text in _creditTexts)
        {
            if (!text) continue;
            
            RectTransform textRT = text.GetComponent<RectTransform>();
            if (!textRT) continue;
            
            // Move the credit in the scroll direction with multiplier
            float speed = _useScrollSpeedMultiplier ? scrollSpeed * scrollSpeedMultiplier : scrollSpeed;
            Vector3 newPosition = textRT.position + _normalizedDirection * (speed * Time.deltaTime);
            textRT.position = newPosition;
        }
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
            
            // Calculate alpha based on distance using our helper function
            float alpha = CalculateAlphaFromDistance(distance);
            
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
    
    [Button]
    public void RestartCredits()
    {
        // Stop any existing loop coroutine
        if (_loopCoroutine != null)
        {
            StopCoroutine(_loopCoroutine);
            _loopCoroutine = null;
        }
        
        // Reset all credits to initial positions
        CreateCreditsText();
        
        // Ensure credits are playing
        _isPaused = false;
    }
    
    [Button]
    public void TogglePause()
    {
        _isPaused = !_isPaused;
    }
    
    
    [Button]
    public void ToggleScrollSpeedMultiplier()
    {
        _useScrollSpeedMultiplier = !_useScrollSpeedMultiplier;
    }
    
    [Button]
    public void SetScrollDirection(Vector3 newDirection)
    {
        scrollDirection = newDirection;
        _normalizedDirection = scrollDirection.normalized;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!fadeEnabled)
            return;
            
        // Define colors for visualization
        Color minFadeColor = new Color(0, 1, 0, 0.8f);    // Green - full visibility
        Color maxFadeColor = new Color(1, 0, 0, 0.8f);    // Red - no visibility
        Color startPositionColor = new Color(1, 1, 0, 0.8f); // Yellow - start position
        Color resetDistanceColor = new Color(0, 0.5f, 1, 0.8f); // Blue - reset distance for looping
        
        // Get the center of this object
        Vector3 center = transform.position;
        
        // Draw min fade distance (alpha = 1)
        Gizmos.color = minFadeColor;
        DrawCircle(center, minFadeDistance, 32);
        
        // Draw max fade distance (alpha = 0)
        Gizmos.color = maxFadeColor;
        DrawCircle(center, maxFadeDistance, 32);
        
        // Draw reset distance for looping (if enabled)
        if (loopCredits)
        {
            Gizmos.color = resetDistanceColor;
            DrawCircle(center, resetDistance, 32);
        }
        
        // Draw start position
        Gizmos.color = startPositionColor;
        DrawCircle(center + startingOffset, 0.5f, 32);
        
        // Draw labels
        DrawLabel(center + new Vector3(0, minFadeDistance, 0), "Min Fade (α=1)");
        DrawLabel(center + new Vector3(0, maxFadeDistance, 0), "Max Fade (α=0)");
        if (loopCredits)
        {
            DrawLabel(center + new Vector3(0, resetDistance, 0), "Reset Distance");
        }
        DrawLabel(center + startingOffset, "Start Position");
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
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        UnityEditor.Handles.Label(position, text, style);
    }
#endif
}