using System;
using UnityEngine;
using PrimeTween;
using TMPEffects.Components;
using TMPro;
using UnityEngine.Serialization;
using VInspector;

public class TestInfoTextHandler : MonoBehaviour
{

    [Header("Animation")] 
    [SerializeField] private float textFadeInDuration = 0.7f;
    [SerializeField] private float textFadeOutDuration = 0.3f;
    [SerializeField] private float textTransitionDelay = 1f;
    [SerializeField] private float initialDelay = 2f;
    [SerializeField] private float displayDuration = 0.5f;
    [SerializeField,ReadOnly] private float animationTime;
    
    [Header("References")] 
    [SerializeField] private PlayerStateMachine player;
    [SerializeField] private TextMeshProUGUI testNameText;
    [SerializeField] private TextMeshProUGUI testDescriptionText;
    [SerializeField] private TextMeshProUGUI musicNameText;
    [SerializeField] private TextMeshProUGUI musicAuthorText;

    private Sequence _textSequence;
    private TMPWriter _testNameWriter;
    private TMPWriter _testDescriptionWriter;
    private TMPWriter _musicNameWriter;
    private TMPWriter _musicAuthorWriter;
    
    private void Awake()
    {
        _testNameWriter = testNameText.GetComponent<TMPWriter>();
        _testDescriptionWriter = testDescriptionText.GetComponent<TMPWriter>();
        _musicNameWriter = musicNameText.GetComponent<TMPWriter>();
        _musicAuthorWriter = musicAuthorText.GetComponent<TMPWriter>();
        testNameText.alpha = 0f;
        testDescriptionText.alpha = 0f;
        musicNameText.alpha = 0f;
        musicAuthorText.alpha = 0f;
    }

    private void OnEnable()
    {
        TestManager.Instance?.onTestLoaded.AddListener(OnTestLoaded);
        TestManager.Instance?.onTestStartUnloading.AddListener(OnTestStartUnloading);
        TestManager.Instance?.onTestStartLoading.AddListener(OnTestStartLoading);
        TestManager.Instance?.onIntroSequenceStart.AddListener(ForceHideAnimation);
        
        player?.onPlayerSpawned.AddListener(OnPlayerSpawnedAnimation);
        player?.onPlayerOpenedMenu.AddListener(ForceHideAnimation);
    }
    

    private void OnDisable()
    {
        TestManager.Instance?.onTestLoaded.RemoveListener(OnTestLoaded);
        TestManager.Instance?.onTestStartLoading.RemoveListener(OnTestStartLoading);
        TestManager.Instance?.onTestStartUnloading.RemoveListener(OnTestStartUnloading);
        TestManager.Instance?.onIntroSequenceStart.RemoveListener(ForceHideAnimation);
        
        player?.onPlayerSpawned.RemoveListener(OnPlayerSpawnedAnimation);
        player?.onPlayerOpenedMenu.RemoveListener(ForceHideAnimation);
    }
    
    private void OnTestStartUnloading(SOTest test)
    {
        
        if (_textSequence.isAlive)
        {
            ForceHideAnimation();
        }
        
        float duration = test.GetTimeToUnload();
        if (duration <= 1f) return;
        
        testNameText.alpha = 0f;
        testNameText.text = "Unloading test...";

        _textSequence = Sequence.Create()
            // Show text
            .ChainCallback(() => { _testNameWriter.RestartWriter(); })
            .Chain(Tween.Alpha(testNameText, startValue: 0f, endValue: 1f, duration: duration/4))
            // Wait between show and hide
            .ChainDelay(duration/3)
            // Hide text 
            .Chain(Tween.Alpha(testNameText, endValue: 0f, duration: duration/4));
    }
    
    private void OnTestStartLoading(SOTest test)
    {
        if (_textSequence.isAlive)
        {
            ForceHideAnimation();
        } 
        
        float duration = test.GetTimeToLoad();
        if (duration <= 1f) return;
        testNameText.alpha = 0f;
        testNameText.text = "Loading test...";

        
        _textSequence = Sequence.Create()
            // Show text
            .ChainCallback(() => { _testNameWriter.RestartWriter(); })
            .Chain(Tween.Alpha(testNameText, startValue: 0f, endValue: 1f, duration: duration/4))
            // Wait between show and hide
            .ChainDelay(duration/3)
            // Hide text 
            .Chain(Tween.Alpha(testNameText, endValue: 0f, duration: duration/4));
    }
    
    
    private void OnTestLoaded(SOTest test)
    {
        if (test.ShowTestInfo)
        {
            string prfix = "";
            string suffix = "";
            testNameText.text = $"{prfix}{test.Name}{suffix}";
            testDescriptionText.text = $"{prfix}{test.Description}{suffix}";
            musicNameText.text = $"{prfix}'{test.GetTheme().aoName}'{suffix}";
            musicAuthorText.text = $"{prfix}By {test.GetTheme().aoAuthor}{suffix}";
        }
        else
        {
            testNameText.text = "";
            testDescriptionText.text = "";
            musicNameText.text = "";
            musicAuthorText.text = "";
        }
    }
    

    

    [Button]
    private void OnPlayerSpawnedAnimation(ISpawnPoint spawnPoint)
    {
        if (_textSequence.isAlive)
        {
            _textSequence.Stop();
        }
        
        testNameText.alpha = 0f;
        testDescriptionText.alpha = 0f;
        musicNameText.alpha = 0f;
        musicAuthorText.alpha = 0f;
        
        _textSequence = Sequence.Create()
            
            .ChainDelay(initialDelay)
            
            // Show text
            .ChainCallback(() => { _testNameWriter.RestartWriter(); })
            .Chain(Tween.Alpha(testNameText, startValue: 0f, endValue: 1f, duration: textFadeInDuration))
            .ChainDelay(textTransitionDelay)
            .ChainCallback(() => { _testDescriptionWriter.RestartWriter(); })
            .Chain(Tween.Alpha(testDescriptionText, startValue: 0f, endValue: 1f, duration: textFadeInDuration))
            .ChainDelay(textTransitionDelay)
            .ChainCallback(() => { _musicNameWriter.RestartWriter(); })
            .Chain(Tween.Alpha(musicNameText, startValue: 0f, endValue: 1f, duration: textFadeInDuration))
            .ChainDelay(textTransitionDelay)
            .ChainCallback(() => { _musicAuthorWriter.RestartWriter(); })
            .Chain(Tween.Alpha(musicAuthorText, startValue: 0f, endValue: 1f, duration: textFadeInDuration))
            
            // Wait between show and hide
            .ChainDelay(displayDuration)
            
            // Hide text 
            .Chain(Tween.Alpha(testNameText, endValue: 0f, duration: textFadeOutDuration))
            .ChainDelay(textTransitionDelay)
            .Chain(Tween.Alpha(testDescriptionText, endValue: 0f, duration: textFadeOutDuration))
            .ChainDelay(textTransitionDelay)
            .Chain(Tween.Alpha(musicNameText, endValue: 0f, duration: textFadeOutDuration))
            .ChainDelay(textTransitionDelay)
            .Chain(Tween.Alpha(musicAuthorText, endValue: 0f, duration: textFadeOutDuration));
    }

    [Button]
    private void ForceHideAnimation()
    {
        if (!_textSequence.isAlive)
        {
            return;
        }

        _textSequence.Stop();
        
        _textSequence = Sequence.Create()
            .Chain(Tween.Alpha(testNameText, endValue: 0f, duration: textFadeOutDuration/2))
            .ChainDelay(textTransitionDelay/2)
            .Chain(Tween.Alpha(testDescriptionText, endValue: 0f, duration: textFadeOutDuration/2))
            .ChainDelay(textTransitionDelay/2)
            .Chain(Tween.Alpha(musicNameText, endValue: 0f, duration: textFadeOutDuration/2))
            .ChainDelay(textTransitionDelay/2)
            .Chain(Tween.Alpha(musicAuthorText, endValue: 0f, duration: textFadeOutDuration/2));
    }
    
    
    private void OnValidate()
    {
        // Initial delay + time for all 4 fade ins with delays between them
        float totalTime = initialDelay + (textFadeInDuration * 4) + (textTransitionDelay * 3);
    
        // Delay before hide + time for all 4 fade outs with delays between them
        totalTime += displayDuration + (textFadeOutDuration * 4) + (textTransitionDelay * 3);
        
        animationTime = totalTime;
    }
    
}