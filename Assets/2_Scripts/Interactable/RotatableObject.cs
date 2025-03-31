using UnityEngine;
using PrimeTween;
using VInspector;

[RequireComponent(typeof(AudioSource))]
public class RotatableObject : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField, Tooltip("How long is the rotation")] private float rotationTime = 0.5f;
    [SerializeField, Tooltip("Rotation ease")] private  Ease rotationEase = Ease.Linear;
    [SerializeField, Tooltip("Rotation SFX")] private SOAudioEvent rotationSfx;
    
    private  AudioSource _audioSource;
    private Vector3 _defaultRotation;
    private Tween _rotateTween;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _defaultRotation = transform.rotation.eulerAngles;
    }
    
    
    
    [Button]
    public void ResetRotation()
    {
        if (_rotateTween.isAlive || !Application.isPlaying) return;

        _rotateTween = Tween.Rotation(transform, Quaternion.Euler(_defaultRotation), rotationTime, rotationEase);
        if (_audioSource != null) rotationSfx?.Play(_audioSource);
    }
    
    public void SetRotationOnX(float degrees)
    {
        if (_rotateTween.isAlive || !Application.isPlaying) return;
        
        
        Quaternion targetRotation = Quaternion.Euler(degrees, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
        _rotateTween = Tween.Rotation(transform, targetRotation, rotationTime, rotationEase);
        if (_audioSource != null) rotationSfx?.Play(_audioSource);
    }
    
    public void SetRotationOnY(float degrees)
    {
        if (_rotateTween.isAlive || !Application.isPlaying) return;


        Quaternion targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, degrees, transform.rotation.eulerAngles.z);
        _rotateTween = Tween.Rotation(transform, targetRotation, rotationTime, rotationEase);
        if (_audioSource != null) rotationSfx?.Play(_audioSource);
    }
    
    public void SetRotationOnZ(float degrees)
    {
        if (_rotateTween.isAlive || !Application.isPlaying) return;


        Quaternion targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, degrees);
        _rotateTween = Tween.Rotation(transform, targetRotation, rotationTime, rotationEase);
        if (_audioSource != null) rotationSfx?.Play(_audioSource);
    }

    
    public void RotateOnXBy(float degrees)
    {
        if (_rotateTween.isAlive || !Application.isPlaying) return;
        

        Quaternion targetRotation = transform.rotation * Quaternion.AngleAxis(degrees, Vector3.right);
        _rotateTween = Tween.Rotation(transform, targetRotation, rotationTime, rotationEase);
        if (_audioSource != null) rotationSfx?.Play(_audioSource);
    }
    
    public void RotateOnYBy(float degrees)
    {
        if (_rotateTween.isAlive || !Application.isPlaying) return;


        Quaternion targetRotation = transform.rotation * Quaternion.AngleAxis(degrees, Vector3.up);
        _rotateTween = Tween.Rotation(transform, targetRotation, rotationTime, rotationEase);
        if (_audioSource != null) rotationSfx?.Play(_audioSource);
    }
    
    public void RotateOnZBy(float degrees)
    {
        if (_rotateTween.isAlive || !Application.isPlaying) return;


        Quaternion targetRotation = transform.rotation * Quaternion.AngleAxis(degrees, Vector3.forward);
        _rotateTween = Tween.Rotation(transform, targetRotation, rotationTime, rotationEase);
        if (_audioSource != null) rotationSfx?.Play(_audioSource);
    }

}
