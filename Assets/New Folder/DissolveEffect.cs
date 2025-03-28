using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Adds a dissolve effect to any GameObject while preserving its existing materials.
/// Works similarly to the Outline component by adding an additional overlay material.
/// </summary>
[DisallowMultipleComponent]
public class DissolveEffect : MonoBehaviour
{
    private static List<DissolveEffect> activeEffects = new List<DissolveEffect>();
    
    // Maximum number of interactors supported by the shader
    private const int MAX_INTERACTORS = 20;
    
    // Cached shader property IDs for better performance
    private static readonly int PositionID = Shader.PropertyToID("_Position");
    private static readonly int RadiusID = Shader.PropertyToID("_Radius");
    private static readonly int InteractorCountID = Shader.PropertyToID("_InteractorCount");
    private static readonly int InteractorPositionsID = Shader.PropertyToID("_ShaderInteractorsPositions");
    private static readonly int InteractorRadiusesID = Shader.PropertyToID("_ShaderInteractorsRadiuses");
    private static readonly int InteractorBoxBoundsID = Shader.PropertyToID("_ShaderInteractorsBoxBounds");
    private static readonly int InteractorRotationID = Shader.PropertyToID("_ShaderInteractorRotation");
    private static readonly int ShapeCutoffID = Shader.PropertyToID("_ShapeCutoff");
    private static readonly int ShapeSmoothnessID = Shader.PropertyToID("_ShapeSmoothness");
    private static readonly int DisAmountID = Shader.PropertyToID("_DisAmount");
    private static readonly int DisLineWidthID = Shader.PropertyToID("_DisLineWidth");
    private static readonly int DisLineColorID = Shader.PropertyToID("_DisLineColor");
    private static readonly int NoiseScaleID = Shader.PropertyToID("_NScale");
    private static readonly int NoiseStrengthID = Shader.PropertyToID("_NoiseStrength");
    private static readonly int SecondTexID = Shader.PropertyToID("_SecondTex");
    private static readonly int NoiseTexID = Shader.PropertyToID("_NoiseTex");
    private static readonly int Color2ID = Shader.PropertyToID("_Color2");
    
    [Header("Effect Settings")]
    [Tooltip("The style of dissolve effect to apply")]
    public EffectStyle effectStyle = EffectStyle.SwapTextures;
    
    [Tooltip("Dissolve cutoff threshold")]
    [Range(0.01f, 1f)]
    public float dissolveAmount = 0.01f;
    
    [Tooltip("Width of the dissolve effect line")]
    [Range(0f, 2f)]
    public float lineWidth = 0.05f;
    
    [Tooltip("Color of the dissolve effect line")]
    public Color lineColor = Color.white;
    
    [Tooltip("Use HDR for the line color")]
    public bool useHDRColor = true;
    
    [Tooltip("Intensity multiplier for HDR color")]
    [Range(1f, 10f)]
    public float colorIntensity = 1.5f;
    
    [Header("Textures")]
    [Tooltip("Secondary texture to show in the dissolved areas")]
    public Texture2D secondaryTexture;
    
    [Tooltip("Color tint for the secondary texture")]
    public Color secondaryColor = Color.white;
    
    [Tooltip("Noise texture for the dissolve effect")]
    public Texture2D noiseTexture;
    
    [Tooltip("Scale of the noise texture")]
    [Range(0.1f, 10f)]
    public float noiseScale = 1f;
    
    [Tooltip("Strength of the noise effect")]
    [Range(0f, 1f)]
    public float noiseStrength = 0.5f;
    
    [Header("Interactor Settings")]
    [Tooltip("Use multiple interactors")]
    public bool useMultipleInteractors = false;
    
    [Tooltip("Shape cutoff value for smoother transitions")]
    [Range(0f, 1f)]
    public float shapeCutoff = 0.5f;
    
    [Tooltip("Shape smoothness for softer transitions")]
    [Range(0f, 1f)]
    public float shapeSmoothness = 0.1f;
    
    [Tooltip("Connect to a DissolveController for positioning")]
    public bool useController = false;
    
    [Tooltip("DissolveController to use")]
    public DissolveController controller;
    
    private Renderer[] renderers;
    private Material dissolveMaterial;
    private bool needsUpdate = true;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    
    // Dissolve effect styles
    public enum EffectStyle
    {
        SwapTextures,
        Appear,
        Disappear
    }
    
    void Awake()
    {
        // Cache renderers
        renderers = GetComponentsInChildren<Renderer>();
        
        // Create dissolve material
        dissolveMaterial = new Material(Shader.Find("Custom/DissolveOverlay"));
        dissolveMaterial.name = "DissolveOverlay (Instance)";
        
        // Set default textures if none are assigned
        if (noiseTexture == null)
        {
            // Use a simple perlin noise texture
            noiseTexture = CreateDefaultNoiseTexture();
        }
        
        if (secondaryTexture == null && effectStyle == EffectStyle.SwapTextures)
        {
            // Create a simple red color texture as default
            secondaryTexture = CreateDefaultSecondTexture();
        }
        
        // Store original materials for each renderer
        foreach (var renderer in renderers)
        {
            originalMaterials[renderer] = renderer.sharedMaterials;
        }
        
        // Apply initial properties
        UpdateMaterialProperties();
    }
    
    void OnEnable()
    {
        // Register this effect
        if (!activeEffects.Contains(this))
        {
            activeEffects.Add(this);
        }
        
        // Add material to renderers
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            // Get original materials
            Material[] originals = originalMaterials.ContainsKey(renderer) ? 
                originalMaterials[renderer] : renderer.sharedMaterials;
            
            // Create new array with one extra slot
            var newMaterials = new Material[originals.Length + 1];
            
            // Copy original materials
            for (int i = 0; i < originals.Length; i++)
            {
                newMaterials[i] = originals[i];
            }
            
            // Add dissolve material at the end
            newMaterials[originals.Length] = dissolveMaterial;
            
            // Apply to renderer
            renderer.materials = newMaterials;
        }
        
        // Update material properties and shader arrays
        needsUpdate = true;
        UpdateShaderArrays();
    }
    
    void OnDisable()
    {
        // Unregister this effect
        if (activeEffects.Contains(this))
        {
            activeEffects.Remove(this);
        }
        
        // Restore original materials
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            if (originalMaterials.ContainsKey(renderer))
            {
                renderer.materials = originalMaterials[renderer];
            }
            else
            {
                // Remove our dissolve material
                var materials = renderer.sharedMaterials.ToList();
                materials.Remove(dissolveMaterial);
                renderer.materials = materials.ToArray();
            }
        }
        
        // Update shader arrays without this effect
        UpdateShaderArrays();
    }
    
    void OnDestroy()
    {
        // Destroy material instance
        if (dissolveMaterial != null)
        {
            Destroy(dissolveMaterial);
        }
    }
    
    void OnValidate()
    {
        // Update material when properties change in inspector
        needsUpdate = true;
    }
    
    void Update()
    {
        if (needsUpdate && dissolveMaterial != null)
        {
            UpdateMaterialProperties();
            needsUpdate = false;
        }
        
        if (useController && controller != null && dissolveMaterial != null)
        {
            // Update position based on controller
            dissolveMaterial.SetVector(PositionID, controller.transform.position);
            dissolveMaterial.SetFloat(RadiusID, controller.radius);
        }
        else if (dissolveMaterial != null)
        {
            // Default position at this transform
            dissolveMaterial.SetVector(PositionID, transform.position);
            dissolveMaterial.SetFloat(RadiusID, 5.0f); // Default radius
        }
        
        // Update shader arrays for multiple interactors support
        if (useMultipleInteractors)
        {
            UpdateShaderArrays();
        }
    }
    
    void UpdateMaterialProperties()
    {
        if (dissolveMaterial == null)
            return;
        
        // Set dissolve properties
        dissolveMaterial.SetFloat(DisAmountID, dissolveAmount);
        dissolveMaterial.SetFloat(DisLineWidthID, lineWidth);
        
        // Set HDR color if enabled
        if (useHDRColor)
        {
            Color hdrColor = lineColor * colorIntensity;
            hdrColor.a = lineColor.a;
            dissolveMaterial.SetColor(DisLineColorID, hdrColor);
        }
        else
        {
            dissolveMaterial.SetColor(DisLineColorID, lineColor);
        }
        
        // Set textures
        if (secondaryTexture != null)
        {
            dissolveMaterial.SetTexture(SecondTexID, secondaryTexture);
        }
        
        if (noiseTexture != null)
        {
            dissolveMaterial.SetTexture(NoiseTexID, noiseTexture);
        }
        
        // Set colors and values
        dissolveMaterial.SetColor(Color2ID, secondaryColor);
        dissolveMaterial.SetFloat(NoiseScaleID, noiseScale);
        dissolveMaterial.SetFloat(NoiseStrengthID, noiseStrength);
        dissolveMaterial.SetFloat(ShapeCutoffID, shapeCutoff);
        dissolveMaterial.SetFloat(ShapeSmoothnessID, shapeSmoothness);
        
        // Set style keywords
        SetStyleKeyword();
        
        // Set multiple interactors keyword
        if (useMultipleInteractors)
        {
            dissolveMaterial.EnableKeyword("_USE_MULTIPLE_INTERACTORS");
        }
        else
        {
            dissolveMaterial.DisableKeyword("_USE_MULTIPLE_INTERACTORS");
        }
    }
    
    void SetStyleKeyword()
    {
        // First, disable all style keywords
        dissolveMaterial.DisableKeyword("_STYLE_SWAPTEXTURES");
        dissolveMaterial.DisableKeyword("_STYLE_APPEAR");
        dissolveMaterial.DisableKeyword("_STYLE_DISAPPEAR");
        
        // Then enable the one we want
        switch (effectStyle)
        {
            case EffectStyle.SwapTextures:
                dissolveMaterial.EnableKeyword("_STYLE_SWAPTEXTURES");
                break;
            case EffectStyle.Appear:
                dissolveMaterial.EnableKeyword("_STYLE_APPEAR");
                break;
            case EffectStyle.Disappear:
                dissolveMaterial.EnableKeyword("_STYLE_DISAPPEAR");
                break;
        }
    }
    
    // Update all materials with interactor data
    public static void UpdateShaderArrays()
    {
        if (activeEffects.Count == 0)
            return;
        
        // Get controllers
        DissolveController[] controllers = GameObject.FindObjectsOfType<DissolveController>();
        int count = Mathf.Min(controllers.Length, MAX_INTERACTORS);
        
        if (count == 0)
            return;
        
        // Create arrays to hold all interactor data
        Vector4[] positions = new Vector4[MAX_INTERACTORS];
        float[] radiuses = new float[MAX_INTERACTORS];
        Vector4[] boxBounds = new Vector4[MAX_INTERACTORS];
        Vector4[] rotations = new Vector4[MAX_INTERACTORS];
        
        // Fill arrays with data from controllers
        for (int i = 0; i < count; i++)
        {
            DissolveController controller = controllers[i];
            
            positions[i] = controller.transform.position;
            radiuses[i] = controller.radius;
            boxBounds[i] = new Vector4(
                controller.boxSize.x,
                controller.boxSize.y,
                controller.boxSize.z,
                0.0f
            );
            rotations[i] = new Vector4(
                controller.boxRotation.x,
                controller.boxRotation.y,
                controller.boxRotation.z,
                0.0f
            );
        }
        
        // Update all dissolve materials
        foreach (DissolveEffect effect in activeEffects)
        {
            if (effect.dissolveMaterial != null)
            {
                effect.dissolveMaterial.SetVectorArray(InteractorPositionsID, positions);
                effect.dissolveMaterial.SetFloatArray(InteractorRadiusesID, radiuses);
                effect.dissolveMaterial.SetVectorArray(InteractorBoxBoundsID, boxBounds);
                effect.dissolveMaterial.SetVectorArray(InteractorRotationID, rotations);
                effect.dissolveMaterial.SetInt(InteractorCountID, count);
            }
        }
    }
    
    // Creates a default noise texture for the dissolve effect
    private Texture2D CreateDefaultNoiseTexture()
    {
        int resolution = 256;
        Texture2D texture = new Texture2D(resolution, resolution);
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Simple Perlin-like noise
                float xCoord = (float)x / resolution * 8;
                float yCoord = (float)y / resolution * 8;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                
                texture.SetPixel(x, y, new Color(sample, sample, sample, 1));
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    // Creates a default secondary texture
    private Texture2D CreateDefaultSecondTexture()
    {
        Texture2D texture = new Texture2D(2, 2);
        Color redColor = new Color(1f, 0.3f, 0.3f, 1f);
        
        texture.SetPixel(0, 0, redColor);
        texture.SetPixel(1, 0, redColor);
        texture.SetPixel(0, 1, redColor);
        texture.SetPixel(1, 1, redColor);
        
        texture.Apply();
        return texture;
    }
    
    // For debugging - draws gizmos to show the effect radius
    private void OnDrawGizmosSelected()
    {
        if (useController && controller != null)
        {
            // Draw from controller position
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.DrawWireSphere(controller.transform.position, controller.radius);
        }
        else
        {
            // Draw from this object position
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, 5.0f);
        }
    }
}