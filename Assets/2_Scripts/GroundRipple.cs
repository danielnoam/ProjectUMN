using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class GroundRipple : MonoBehaviour
{
    [Header("Ripple Settings")]
    [SerializeField] private float rippleCooldown = 0.4f;
    [SerializeField] private VisualEffect sparks;
    
    
    private static readonly int RippleOrigin = Shader.PropertyToID("_RippleOrigin");
    private static readonly int RippleThickness = Shader.PropertyToID("_RippleThickness");
    private static readonly int RippleTime = Shader.PropertyToID("_RippleTime");
    private Material _material;
    private float _rippleTime = 100.0f;

    private void Start()
    {
        _material = GetComponent<Renderer>().material;
        sparks.enabled = false;
    }

    public void GetHit(RaycastHit hit)
    {
        if(_rippleTime < rippleCooldown)
        {
            return;
        }

        _material.SetVector(RippleOrigin, hit.textureCoord);
        _rippleTime = _material.GetFloat(RippleThickness) * -2.0f;
    }

    private void Update()
    {
        _rippleTime += Time.deltaTime;
        _material.SetFloat(RippleTime, _rippleTime);
    }
}
