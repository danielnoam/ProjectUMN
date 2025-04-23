using System;
using UnityEngine;
using PrimeTween;
using VInspector;

public class CollectableItem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject itemObject;
    
    [Header("Pick Up Effect")]
    [SerializeField] private float animationDuration = 2f;
    [SerializeField] private Vector3 animationScale;
    [SerializeField] private Vector3 animationMovePosition = Vector3.up;
    
    [Header("Post Pick Up Effect")]
    [SerializeField] private float hoverSpeed = 1f;
    [SerializeField] private float rotationSpeed;
    
    private bool _pickedUp;
    private Vector3 _itemStartPosition;
    private Quaternion _itemStartRotation;
    private Vector3 _itemStartScale;
    private Sequence _effectSequence;

    private void Awake()
    {
        _itemStartPosition = itemObject.transform.localPosition;
        _itemStartRotation = itemObject.transform.rotation;
        _itemStartScale = itemObject.transform.localScale;
    }

    private void Update()
    {
        if (!_pickedUp) return;
        
        // Hover in place
        itemObject.transform.localPosition = new Vector3(itemObject.transform.localPosition.x, itemObject.transform.localPosition.y + Mathf.Sin(Time.time * hoverSpeed) * Time.deltaTime, itemObject.transform.localPosition.z);
        
        // Rotate
        
        itemObject.transform.localRotation = Quaternion.Slerp(itemObject.transform.localRotation, _itemStartRotation, Time.deltaTime * rotationSpeed);
        
    }


    [Button]
    public void PickUp()
    {
        
        _pickedUp = true;
        if (_effectSequence.isAlive) 
        {
            _effectSequence.Stop();
        }
        

        _effectSequence = Sequence.Create();
        _effectSequence = _effectSequence
                .Group(Tween.LocalPosition(itemObject.transform, _itemStartPosition, animationMovePosition, duration: animationDuration, ease: Ease.InOutSine))
                .Group(Tween.Scale(itemObject.transform, _itemStartScale, animationScale, duration: animationDuration, ease: Ease.InOutSine))

            ;
    }
}
