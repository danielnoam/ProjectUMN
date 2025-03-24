using UnityEngine;
using PrimeTween;
using TMPEffects.Components;
using TMPro;
using UnityEngine.Serialization;
using VInspector;

public class InfoTextHandler : MonoBehaviour
{

    [Header("Animation")] 
    [SerializeField] private float textFadeInDuration = 2f;
    [SerializeField] private float textFadeOutDuration = 1f;
    [SerializeField] private float textTransitionDelay = 2f;
    [SerializeField] private float initialDelay = 2f;
    [SerializeField] private float displayDuration = 1f;
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

    private void Start()
    {
        if (TestManager.Instance)
        {
            TestManager.Instance.onTestLoaded.AddListener(OnTestLoaded);
        }

        if (player)
        {
            player.onPlayerSpawned.AddListener(PlayTextAnimation);
            player.onPlayerOpenedMenu.AddListener(ForceHideAnimation);
        }
    }
    
    private void OnDisable()
    {
        if (TestManager.Instance)
        {
            TestManager.Instance.onTestLoaded.RemoveListener(OnTestLoaded);
        }
        
        if (player)
        {
            player.onPlayerSpawned.RemoveListener(PlayTextAnimation);
            player.onPlayerOpenedMenu.RemoveListener(ForceHideAnimation);
        }
    }
    
    private void OnTestLoaded(SOTest test)
    {
        string prfix = "";
        string suffix = "";
        
        
        testNameText.text = $"{prfix}{test.GetName()}{suffix}";
        testDescriptionText.text = $"{prfix}{test.GetDescription()}{suffix}";
        musicNameText.text = $"{prfix}'{test.GetTheme().aoName}'{suffix}";
        musicAuthorText.text = $"{prfix}By {test.GetTheme().aoAuthor}{suffix}";
    }

    private float GetAnimationTime()
    {
        // Initial delay + time for all 4 fade ins with delays between them
        float totalTime = initialDelay + (textFadeInDuration * 4) + (textTransitionDelay * 3);
    
        // Delay before hide + time for all 4 fade outs with delays between them
        totalTime += displayDuration + (textFadeOutDuration * 4) + (textTransitionDelay * 3);
    
        return totalTime;
    }

    [Button]
    private void PlayTextAnimation()
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
        if (_textSequence.isAlive)
        {
            _textSequence.Stop();
        }
        
        _textSequence = Sequence.Create()
            .Chain(Tween.Alpha(testNameText, endValue: 0f, duration: textFadeOutDuration/2))
            .ChainDelay(textTransitionDelay/2)
            .Chain(Tween.Alpha(testDescriptionText, endValue: 0f, duration: textFadeOutDuration/2))
            .ChainDelay(textTransitionDelay/2)
            .Chain(Tween.Alpha(musicNameText, endValue: 0f, duration: textFadeOutDuration/2))
            .ChainDelay(textTransitionDelay/2)
            .Chain(Tween.Alpha(musicAuthorText, endValue: 0f, duration: textFadeOutDuration/2));
    }
    
    [Button]
    private void ForceShowAnimation()
    {
        if (_textSequence.isAlive)
        {
            _textSequence.Stop();
        }
        
        _textSequence = Sequence.Create()
            .ChainCallback(() => { _testNameWriter.RestartWriter(); })
            .Chain(Tween.Alpha(testNameText, startValue: 0f, endValue: 1f, duration: textFadeInDuration/2))
            .ChainDelay(textTransitionDelay/2)
            .ChainCallback(() => { _testDescriptionWriter.RestartWriter(); })
            .Chain(Tween.Alpha(testDescriptionText, startValue: 0f, endValue: 1f, duration: textFadeInDuration/2))
            .ChainDelay(textTransitionDelay/2)
            .ChainCallback(() => { _musicNameWriter.RestartWriter(); })
            .Chain(Tween.Alpha(musicNameText, startValue: 0f, endValue: 1f, duration: textFadeInDuration/2))
            .ChainDelay(textTransitionDelay/2)
            .ChainCallback(() => { _musicAuthorWriter.RestartWriter(); })
            .Chain(Tween.Alpha(musicAuthorText, startValue: 0f, endValue: 1f, duration: textFadeInDuration/2));
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        animationTime = GetAnimationTime();
    }
#endif
}