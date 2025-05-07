using System;
using System.Collections;
using System.Collections.Generic;
using CustomAttribute;
using UnityEngine;
using PrimeTween;
using TMPEffects.CharacterData;
using TMPEffects.Components;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class IntroSceneHandler : MonoBehaviour
{
    [Header("Settings")] [SerializeField] private float animationDuration = 15f;
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private SOAudioEvent writerStartSfx;

    [Header("Loading Text")] [SerializeField]
    private float loadingTextCycleInterval = 0.5f;

    [SerializeField] private string finishedLoadingText = "Finished Loading Simulation.";
    [SerializeField] private string startingSimulationText = "Starting Simulation.";

    [SerializeField] private string[] loadingTextArray =
        { "Loading Simulation", "Loading Simulation.", "Loading Simulation..", "Loading Simulation..." };

    [Header("Floating Text")] [SerializeField]
    private float floatingTextSpawnInterval = 2f;

    [SerializeField] private float floatingTextFadeDuration = 3f;
    [SerializeField] private float floatingTextMoveDuration = 7f;
    [SerializeField] private float floatingTextMoveDistance = 200f;
    [SerializeField, Range(0f, 1f)] private float textGenerationDurationPercentage = 0.8f;
    [SerializeField] private string[] textArray;


    [Header("References")] [SerializeField]
    private AudioSource audioSource;

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

        _loadingTextWriter.OnStartWriter.AddListener(OnStartWriter);
        
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDisable()
    {
        _loadingTextWriter.OnStartWriter.RemoveListener(OnStartWriter);
    }

    private void Start()
    {
        StartLoadingAnimation();
    }

    private void Update()
    {
        // Skip the loading animation if the user presses the space key
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)) {
            SceneManager.LoadScene(mainScene.BuildIndex);
        }
    }

    private void OnStartWriter(TMPWriter writer)
    {
        if (!audioSource) return;
        writerStartSfx?.Play(audioSource);
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
        
        yield return new WaitForSeconds(3f);
        loadingText.text = finishedLoadingText;
        _loadingTextWriter.enabled = true;
        _loadingTextWriter.RestartWriter();
        yield return new WaitForSeconds(3f);
        _loadingTextWriter.enabled = false;
        loadingText.text = startingSimulationText;
        _loadingTextWriter.enabled = true;
        _loadingTextWriter.RestartWriter();
        yield return new WaitForSeconds(1f);
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
            
            // Get the writer component and play the sound
            TMPWriter writer = newText.GetComponent<TMPWriter>();
            if (writer)
            {
                writer.OnStartWriter.AddListener(OnStartWriter);
            }
            
            // Start animation for this text
            StartCoroutine(AnimateFloatingText(newText, writer));
            
            // Move to next text in array
            index = (index + 1) % textArray.Length;
            
            // Wait before spawning the next text
            yield return new WaitForSeconds(floatingTextSpawnInterval);
        }
    }

    private IEnumerator AnimateFloatingText(TextMeshProUGUI text, TMPWriter textWriter)
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
        
        // Remove the writer listener to prevent memory leaks
        if (textWriter)
        {
            textWriter.OnStartWriter.RemoveListener(OnStartWriter);
        }
        Destroy(text.gameObject);
    }
}