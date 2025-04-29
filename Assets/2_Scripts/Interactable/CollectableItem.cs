using System;
using UnityEngine;
using PrimeTween;
using VInspector;

[RequireComponent(typeof(AudioSource))]
public class CollectableItem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject itemObject;
    
    [Header("Pick Up Effect")]
    [SerializeField] private float animationDuration = 2f;
    [SerializeField] private Vector3 animationScale;
    [SerializeField] private Vector3 animationMovePosition = Vector3.up;
    [SerializeField] private SOAudioEvent pickedUpSfx;
    
    [Header("Post Pick Up Effect")]
    [SerializeField] private float hoverSpeed = 0.3f;
    [SerializeField] private float hoverHeight = 0.5f;
    [SerializeField] private AnimationCurve hoverCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private Vector3 rotationDirection = Vector3.up;
    
    
    private bool _animationComplete;
    private Vector3 _itemStartPosition;
    private Quaternion _itemStartRotation;
    private Vector3 _itemStartScale;
    private Sequence _effectSequence;
    private float _timeOffset;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _itemStartPosition = itemObject.transform.localPosition;
        _itemStartRotation = itemObject.transform.rotation;
        _itemStartScale = itemObject.transform.localScale;
        _timeOffset = UnityEngine.Random.Range(0f, 1f);
    }

    private void Update()
    {
        // Only apply hover effect after animation is complete
        if (!_animationComplete) return;
        
        // Hover in place using animation curve
        float time = Mathf.PingPong(Time.time * hoverSpeed + _timeOffset, 1f);
        Vector3 newPosition = _itemStartPosition + animationMovePosition;
        newPosition.y += hoverCurve.Evaluate(time) * hoverHeight;
        
        itemObject.transform.localPosition = newPosition;
        
        // Rotate around the specified axis
        itemObject.transform.Rotate(rotationDirection * (rotationSpeed * Time.deltaTime));
    }

    [Button]
    public void PickUp()
    {
        
        if (_effectSequence.isAlive) 
        {
            _effectSequence.Stop();
        }
        
        itemObject.transform.localScale = _itemStartScale;
        itemObject.transform.localPosition = _itemStartPosition;
        itemObject.transform.rotation = _itemStartRotation;
        _animationComplete = false;

        _effectSequence = Sequence.Create();
        _effectSequence = _effectSequence
                .Group(Tween.LocalPosition(itemObject.transform, _itemStartPosition, animationMovePosition, duration: animationDuration, ease: Ease.InOutSine))
                .Group(Tween.Scale(itemObject.transform, _itemStartScale, animationScale, duration: animationDuration, ease: Ease.InOutSine))
                .OnComplete(() => {
                    _animationComplete = true;
                    pickedUpSfx?.Play(_audioSource);
                });
    }
}