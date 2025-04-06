using System;
using UnityEngine;
using VInspector;

[SelectionBase]
public class LaserGround : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private bool hideOnAwake;
    [SerializeField] private bool affectPlayer = true;
    [SerializeField] private bool affectRobot = false;
    [SerializeField, ShowIf("affectRobot")] private bool destroyRobot = false;

    private Renderer _renderer;
    public bool AffectsPlayer => affectPlayer;
    public bool AffectsRobot => affectRobot;
    public bool DestroyRobot => destroyRobot;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (hideOnAwake)
        {
            _renderer.enabled = false;
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
}
