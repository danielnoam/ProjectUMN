using System;
using System.Collections;
using System.Collections.Generic;
using CustomAttribute;
using UnityEngine;
using PrimeTween;
using TMPEffects.Components;
using TMPro;
using UnityEngine.SceneManagement;

public class IntroSceneHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float animationDuration = 15f;
    [SerializeField] private float startDelay = 1f;
    
    [Header("Loading Text")]
    [SerializeField] private float loadingTextCycleInterval = 0.5f;
    [SerializeField] private string finishedLoadingText = "Finished Loading Simulation";
    [SerializeField] private string[] loadingTextArray = { "Loading Simulation", "Loading Simulation.", "Loading Simulation..", "Loading Simulation..." };
    

    [Header("Floating Text")]
    [SerializeField] private float floatingTextSpawnInterval = 1.5f;
    [SerializeField] private float floatingTextFadeDuration = 1.5f;
    [SerializeField] private float floatingTextMoveDuration = 3f;
    [SerializeField] private float floatingTextMoveDistance = 100f;
    [SerializeField, Range(0f, 1f)] private float textGenerationDurationPercentage = 0.8f;
    [SerializeField] private string[] textArray;
    
    
    [Header("References")]
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Transform textPosition;
    [SerializeField] private TextMeshProUGUI textPrefab;
    [SerializeField] private SceneField mainScene;


    private TMPWriter _loadingTextWriter;
    private Coroutine _loadingTextCycleCoroutine;
    private Coroutine _floatingTextCoroutine;
    private float _textGenerationEndTime;

    private void Awake()
    {
        if (mainScene == null) return;
        _loadingTextWriter = loadingText.GetComponent<TMPWriter>();
    }

    private void Start()
    {
        StartLoadingAnimation();
    }

    private void StartLoadingAnimation()
    {
        // Calculate when to stop generating text
        _textGenerationEndTime = startDelay + (animationDuration * textGenerationDurationPercentage);
        
        // Reset loading text
        loadingText.alpha = 0;
        loadingText.text = loadingTextArray[0];

        // Start the loading sequence
        StartCoroutine(LoadingSequence());
    }

    private IEnumerator LoadingSequence()
    {
        float startTime = Time.time;
        
        // Initial delay
        yield return new WaitForSeconds(startDelay);
        
        // Start the writer effect
        _loadingTextWriter.RestartWriter();
        
        // Fade in the loading text
        Tween.Alpha(loadingText, startValue: 0f, endValue: 1f, duration: 3f);
        
        // Wait for fade-in to complete
        yield return new WaitForSeconds(3f);
        
        // Disable the writer effect after initial animation
        _loadingTextWriter.enabled = false;
        
        // Start loading text cycle
        _loadingTextCycleCoroutine = StartCoroutine(CycleLoadingText());
        
        // Start floating text animation if we have text in the array
        if (textArray != null && textArray.Length > 0)
        {
            _floatingTextCoroutine = StartCoroutine(SpawnFloatingText());
        }
        
        // Wait until we reach the text generation end time
        float timeToWait = _textGenerationEndTime - (Time.time - startTime);
        if (timeToWait > 0)
        {
            yield return new WaitForSeconds(timeToWait);
        }
        
        // Stop text generation coroutines
        if (_floatingTextCoroutine != null)
        {
            StopCoroutine(_floatingTextCoroutine);
            _floatingTextCoroutine = null;
        }
        
        // Stop loading text cycle
        if (_loadingTextCycleCoroutine != null)
        {
            StopCoroutine(_loadingTextCycleCoroutine);
            _loadingTextCycleCoroutine = null;
        }
        
        yield return new WaitForSeconds(1f);
        
        // Fade out current loading text
        loadingText.text = finishedLoadingText;
        _loadingTextWriter.enabled = true;
        _loadingTextWriter.RestartWriter();
        
        yield return new WaitForSeconds(2f);
        Tween.Alpha(loadingText, startValue: 1f, endValue: 0f, duration: 1);
        yield return new WaitForSeconds(1f);
        
        // Wait for the remaining animation duration
        float remainingTime = animationDuration - (Time.time - startTime - startDelay);
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }
        
        // Load the next scene
        SceneManager.LoadScene(mainScene.BuildIndex);
    }

    private IEnumerator CycleLoadingText()
    {
        int index = 0;
        
        while (true)
        {
            // Cycle to the next loading text
            index = (index + 1) % loadingTextArray.Length;
            loadingText.text = loadingTextArray[index];
            
            // Wait for the next cycle
            yield return new WaitForSeconds(loadingTextCycleInterval);
        }
    }

    private IEnumerator SpawnFloatingText()
    {
        int index = 0;
        
        while (true)
        {
            // Create a new text instance
            TextMeshProUGUI newText = Instantiate(textPrefab, textPosition.position, Quaternion.identity, textPosition);
            newText.text = textArray[index];
            newText.alpha = 0;
            
            // Start animation for this text
            StartCoroutine(AnimateFloatingText(newText));
            
            // Move to next text in array
            index = (index + 1) % textArray.Length;
            
            // Wait before spawning the next text
            yield return new WaitForSeconds(floatingTextSpawnInterval);
        }
    }

    private IEnumerator AnimateFloatingText(TextMeshProUGUI text)
    {
        Vector3 startPosition = text.transform.position;
        Vector3 endPosition = startPosition + Vector3.up * floatingTextMoveDistance;
        
        // Fade in
        Tween.Alpha(text, startValue: 0f, endValue: 1f, duration: floatingTextFadeDuration);
        
        // Move upward
        Tween.Position(text.transform, startPosition, endPosition, floatingTextMoveDuration, Ease.OutQuad);
        
        // Wait until move is almost complete
        yield return new WaitForSeconds(floatingTextMoveDuration - floatingTextFadeDuration);
        
        // Fade out
        Tween.Alpha(text, startValue: 1f, endValue: 0f, duration: floatingTextFadeDuration);
        
        // Wait until fade out is complete
        yield return new WaitForSeconds(floatingTextFadeDuration + 0.1f);
        
        // Destroy the text object
        Destroy(text.gameObject);
    }
}