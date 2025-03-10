using System;
using UnityEngine;
using PrimeTween;
using TMPro;

public class InfoTextHandler : MonoBehaviour
{
    [Header("Animation")] 
    [SerializeField] private float animationTime = 1f;
    
    [Header("References")] 
    [SerializeField] private TestManager testManager;
    [SerializeField] private TextMeshProUGUI testNameText;
    [SerializeField] private TextMeshProUGUI testDescriptionText;
    [SerializeField] private TextMeshProUGUI musicNameText;
    [SerializeField] private TextMeshProUGUI musicAuthorText;

    private Sequence _textSequence;

    private void OnEnable()
    {
        if (!testManager) return;
        testManager.onTestLoaded.AddListener(OnTestLoaded);
    }
    
    private void OnDisable()
    {
        if (!testManager) return;
        testManager.onTestLoaded.RemoveListener(OnTestLoaded);
    }
    
    private void OnTestLoaded(SOTest test)
    {
        testNameText.text = $"{test.GetName()}";
        testDescriptionText.text = $"{test.GetDescription()}";
        musicNameText.text = $"";
        musicAuthorText.text = $"";
        
        _textSequence = ShowInfoText();
        _textSequence.ChainDelay(animationTime);
        _textSequence.Group(HideInfoText());
    }

    private Sequence ShowInfoText()
    {
        Sequence showSequence = Sequence.Create();
        showSequence.Group(Tween.Alpha(testNameText,startValue: 0f, endValue: 1f, duration: animationTime));
        showSequence.Group(Tween.Alpha(testDescriptionText, startValue: 0f, endValue: 1f, duration: animationTime));
        showSequence.Group(Tween.Alpha(musicNameText, startValue: 0f, endValue: 1f, duration: animationTime));
        showSequence.Group(Tween.Alpha(musicAuthorText, startValue: 0f, endValue: 1f, duration: animationTime));
        
        return  showSequence;
    }
    
    private Sequence HideInfoText()
    {
        Sequence hideSequence = Sequence.Create();
        hideSequence.Group(Tween.Alpha(testNameText, startValue: 1f, endValue: 0f, duration: animationTime));
        hideSequence.Group(Tween.Alpha(testDescriptionText, startValue: 1f, endValue: 0f, duration: animationTime));
        hideSequence.Group(Tween.Alpha(musicNameText, startValue: 1f, endValue: 0f, duration: animationTime));
        hideSequence.Group(Tween.Alpha(musicAuthorText, startValue: 1f, endValue: 0f, duration: animationTime));
        
        return hideSequence;
    }
}
