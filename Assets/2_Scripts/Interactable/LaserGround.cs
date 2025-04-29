using System;
using UnityEngine;
using UnityEngine.Serialization;
using VInspector;
using Random = UnityEngine.Random;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
public class LaserGround : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private bool hideOnAwake;
    [SerializeField] private bool affectPlayer = true;
    [SerializeField] private bool affectRobot = false;
    [SerializeField] private bool playSfx = true;
    [SerializeField] private SOAudioEvent humSfx;
    [SerializeField] private SOAudioEvent hitSfx;
    [SerializeField, ShowIf("affectRobot")] private bool destroyRobot = false;

    private Renderer _renderer;
    private AudioSource _audioSource;
    public bool AffectsPlayer => affectPlayer;
    public bool AffectsRobot => affectRobot;
    public bool DestroyRobot => destroyRobot;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _renderer = GetComponent<Renderer>();
        if (hideOnAwake)
        {
            _renderer.enabled = false;
        }
        
        if (playSfx)
        {
            humSfx?.Play(_audioSource, Random.Range(0,1f));
        }
    }
    
    public void SetVisibility(bool isVisible)
    {
        _renderer.enabled = isVisible;
    }
    
    public void SetAffectPlayer(bool value)
    {
        affectPlayer = value;
    }
    
    public void SetAffectRobot(bool value)
    {
        affectRobot = value;
    }
    
    public void SetDestroyRobot(bool value)
    {
        destroyRobot = value;
    }
    
    public void PlayHitSfx(Vector3 position)
    {
        if (!hitSfx || !playSfx) return;
        hitSfx?.PlayAtPoint(position);
    }
}
