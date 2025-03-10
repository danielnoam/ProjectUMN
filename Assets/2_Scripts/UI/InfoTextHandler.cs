using UnityEngine;
using PrimeTween;
using TMPEffects.Components;
using TMPro;
using VInspector;

public class InfoTextHandler : MonoBehaviour
{

    [Header("Animation")] 
    [SerializeField] private float fadeInTime = 2f;
    [SerializeField] private float fadeOutTime = 1f;
    [SerializeField] private float startDelay = 2f;
    [SerializeField] private float delayBetweenFadesTime = 2f;
    

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
        testNameText.text = $"{test.GetName()}";
        testDescriptionText.text = $"{test.GetDescription()}";
        musicNameText.text = $"";
        musicAuthorText.text = $"";
    }

    [Button]
    private void PlayTextAnimation()
    {
        _textSequence.Stop();
        _textSequence = Sequence.Create();
        _textSequence.Group(ShowInfoText());
        _textSequence.ChainDelay(delayBetweenFadesTime);
        _textSequence.Group(HideInfoText());
    }

    [Button]
    private void ForceHideAnimation()
    {
        _textSequence.Stop();
        _textSequence = Sequence.Create();
        _textSequence.Group(HideInfoText());
    }
    
    
    private Sequence ShowInfoText()
    {
        Sequence showSequence = Sequence.Create()
                .ChainDelay(startDelay)
                .ChainCallback(() => { _testNameWriter.RestartWriter();})
                .Group(Tween.Alpha(testNameText, startValue: 0f, endValue: 1f, duration: fadeInTime/2))
                .ChainDelay(delayBetweenFadesTime/4)
                .ChainCallback(() => { _testDescriptionWriter.RestartWriter();})
                .Group(Tween.Alpha(testDescriptionText, startValue: 0f, endValue: 1f, duration: fadeInTime/2))
                .ChainDelay(delayBetweenFadesTime/4)
                .ChainCallback(() => { _musicNameWriter.RestartWriter();})
                .Group(Tween.Alpha(musicNameText, startValue: 0f, endValue: 1f, duration: fadeInTime/2))
                .ChainDelay(delayBetweenFadesTime/4)
                .ChainCallback(() => { _musicAuthorWriter.RestartWriter();})
                .Group(Tween.Alpha(musicAuthorText, startValue: 0f, endValue: 1f, duration: fadeInTime/2))
                

            ;
        return  showSequence;
    }
    
    private Sequence HideInfoText()
    {
        Sequence hideSequence = Sequence.Create()
                .Group(Tween.Alpha(testNameText, startValue: testNameText.alpha, endValue: 0f, duration: fadeOutTime/4))
                .ChainDelay(delayBetweenFadesTime/4)
                .Group(Tween.Alpha(testDescriptionText, startValue: testDescriptionText.alpha, endValue: 0f, duration: fadeOutTime/4))
                .ChainDelay(delayBetweenFadesTime/4)
                .Group(Tween.Alpha(musicNameText, startValue: musicNameText.alpha, endValue: 0f, duration: fadeOutTime/4))
                .ChainDelay(delayBetweenFadesTime/4)
                .Group(Tween.Alpha(musicAuthorText, startValue: musicAuthorText.alpha, endValue: 0f, duration: fadeOutTime/4))
            ;
        
        return hideSequence;
    }
}
