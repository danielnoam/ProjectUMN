using System;
using UnityEngine;
using UnityEngine.VFX;

public class GroundRipple : MonoBehaviour
{
    [Header("Ripple Settings")]
    [SerializeField] private float rippleCooldown = 0.4f;
    [SerializeField] private SOAudioEvent rippleSfx;
    
    private static readonly int RippleOrigin = Shader.PropertyToID("_RippleOrigin");
    private static readonly int RippleThickness = Shader.PropertyToID("_RippleThickness");
    private static readonly int RippleTime = Shader.PropertyToID("_RippleTime");
    private Material _material;
    private float _rippleTime = 100.0f;

    private void Awake()
    {
        _material = GetComponent<Renderer>().material;
    }
    
    
    private void Update()
    {
        _rippleTime += Time.deltaTime;
        _material.SetFloat(RippleTime, _rippleTime);
        
    }

    public void GetHit(RaycastHit hit)
    {
        if(_rippleTime < rippleCooldown)
        {
            return;
        }
        rippleSfx?.PlayAtPoint(hit.point);
        _material.SetVector(RippleOrigin, hit.textureCoord);
        _rippleTime = _material.GetFloat(RippleThickness) * -2.0f;
    }
}