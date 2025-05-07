using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VInspector;

public class TestMusicHandler : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private SOAudioEvent[] extraThemes;
    
    [Header("References")]
    [SerializeField] private TestManager testManager;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, ReadOnly] private List<SOAudioEvent> _availableThemes = new List<SOAudioEvent>();
    private bool _isChangingThemes = false;
    private Coroutine _themeMonitorCoroutine;
    
    private void Awake()
    {
        RefreshAvailableThemes();
    }
    
    private void OnEnable()
    {
        testManager?.onTestStartUnloading.AddListener(OnTestStartUnloading);
        testManager?.onTestLoaded.AddListener(OnTestLoaded);
    }

    private void OnDisable()
    {
        testManager?.onTestStartUnloading.RemoveListener(OnTestStartUnloading);
        testManager?.onTestLoaded.RemoveListener(OnTestLoaded);
        StopThemeMonitor();
    }
    
    private void OnTestStartUnloading(SOTest test)
    {
        if (testManager.CurrentTheme) 
        {
            _isChangingThemes = true;
            StopThemeMonitor();
            StartCoroutine(testManager.CurrentTheme?.FadeOutRoutine(audioSource, test.GetTimeToUnload()));
        }
    }
    
    private void OnTestLoaded(SOTest test)
    {
        RefreshAvailableThemes();
        _isChangingThemes = false;
        testManager.CurrentTheme?.Play(audioSource);
        StartThemeMonitor();
    }
    
    private void StartThemeMonitor()
    {
        StopThemeMonitor();
        _themeMonitorCoroutine = StartCoroutine(MonitorThemePlayback());
    }
    
    private void StopThemeMonitor()
    {
        if (_themeMonitorCoroutine != null)
        {
            StopCoroutine(_themeMonitorCoroutine);
            _themeMonitorCoroutine = null;
        }
    }
    
    private IEnumerator MonitorThemePlayback()
    {
        // Wait until the audio source is actually playing
        yield return new WaitUntil(() => audioSource.isPlaying);
        
        // Get the length of the current clip
        float clipLength = audioSource.clip.length;
        
        // Wait until the clip is almost finished
        yield return new WaitForSeconds(clipLength - 0.1f);
        
        // Check if we're still playing (hasn't been interrupted)
        if (audioSource.isPlaying && !_isChangingThemes)
        {
            PlayNextTheme();
        }
    }
    
    [Button]
    private void PlayNextTheme()
    {
        // If we've run out of available themes, refresh the list
        if (_availableThemes.Count == 0)
        {
            RefreshAvailableThemes();
            
            // If we still have no themes, return
            if (_availableThemes.Count == 0)
                return;
        }
        
        // Pick a random theme from the available themes
        int randomIndex = Random.Range(0, _availableThemes.Count);
        SOAudioEvent nextTheme = _availableThemes[randomIndex];
        
        // Remove the chosen theme from the available list
        _availableThemes.RemoveAt(randomIndex);
        
        // Play the new theme
        _isChangingThemes = true;
        
        StartCoroutine(CrossfadeToNextTheme(nextTheme));
    }
    
    private IEnumerator CrossfadeToNextTheme(SOAudioEvent nextTheme)
    {
        // Fade out the current theme if it's playing
        if (audioSource.isPlaying && testManager.CurrentTheme)
        {
            yield return StartCoroutine(testManager.CurrentTheme.FadeOutRoutine(audioSource, 1.0f));
        }
        
        
        // Play the next theme
        nextTheme.Play(audioSource);
        
        _isChangingThemes = false;
        
        // Start monitoring the new theme
        StartThemeMonitor();
    }
    
    private void RefreshAvailableThemes()
    {
        _availableThemes.Clear();
        
        // Add all extra themes to the available list
        foreach (SOAudioEvent theme in extraThemes)
        {
            if (theme) _availableThemes.Add(theme);
        }
    }
}