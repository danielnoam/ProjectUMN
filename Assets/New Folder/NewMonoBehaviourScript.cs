using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for the DissolveEffect component to improve the user experience in the Inspector.
/// </summary>
[CustomEditor(typeof(DissolveEffect))]
public class DissolveEffectEditor : Editor
{
    // SerializedProperties
    private SerializedProperty effectStyleProp;
    private SerializedProperty dissolveAmountProp;
    private SerializedProperty lineWidthProp;
    private SerializedProperty lineColorProp;
    private SerializedProperty useHDRColorProp;
    private SerializedProperty colorIntensityProp;
    
    private SerializedProperty secondaryTextureProp;
    private SerializedProperty secondaryColorProp;
    private SerializedProperty noiseTextureProp;
    private SerializedProperty noiseScaleProp;
    private SerializedProperty noiseStrengthProp;
    
    private SerializedProperty useMultipleInteractorsProp;
    private SerializedProperty shapeCutoffProp;
    private SerializedProperty shapeSmoothnessProps;
    private SerializedProperty useControllerProp;
    private SerializedProperty controllerProp;

    // Foldouts
    private bool showEffectSettings = true;
    private bool showTextureSettings = true;
    private bool showInteractorSettings = true;
    private bool showPreview = true;

    private void OnEnable()
    {
        // Find serialized properties
        effectStyleProp = serializedObject.FindProperty("effectStyle");
        dissolveAmountProp = serializedObject.FindProperty("dissolveAmount");
        lineWidthProp = serializedObject.FindProperty("lineWidth");
        lineColorProp = serializedObject.FindProperty("lineColor");
        useHDRColorProp = serializedObject.FindProperty("useHDRColor");
        colorIntensityProp = serializedObject.FindProperty("colorIntensity");
        
        secondaryTextureProp = serializedObject.FindProperty("secondaryTexture");
        secondaryColorProp = serializedObject.FindProperty("secondaryColor");
        noiseTextureProp = serializedObject.FindProperty("noiseTexture");
        noiseScaleProp = serializedObject.FindProperty("noiseScale");
        noiseStrengthProp = serializedObject.FindProperty("noiseStrength");
        
        useMultipleInteractorsProp = serializedObject.FindProperty("useMultipleInteractors");
        shapeCutoffProp = serializedObject.FindProperty("shapeCutoff");
        shapeSmoothnessProps = serializedObject.FindProperty("shapeSmoothness");
        useControllerProp = serializedObject.FindProperty("useController");
        controllerProp = serializedObject.FindProperty("controller");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DissolveEffect dissolveEffect = (DissolveEffect)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dissolve Effect", EditorStyles.boldLabel);
        
        // Effect settings
        showEffectSettings = EditorGUILayout.Foldout(showEffectSettings, "Effect Settings", true);
        if (showEffectSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(effectStyleProp, new GUIContent("Effect Style"));
            EditorGUILayout.PropertyField(dissolveAmountProp, new GUIContent("Dissolve Amount"));
            EditorGUILayout.PropertyField(lineWidthProp, new GUIContent("Line Width"));
            
            // Handle HDR color
            EditorGUILayout.PropertyField(useHDRColorProp, new GUIContent("Use HDR Color"));
            
            if (useHDRColorProp.boolValue)
            {
                EditorGUILayout.PropertyField(lineColorProp, new GUIContent("Line Color"));
                EditorGUILayout.PropertyField(colorIntensityProp, new GUIContent("Color Intensity"));
                
                Rect lastRect = GUILayoutUtility.GetLastRect();
                Rect previewRect = new Rect(lastRect.x + 200, lastRect.y, 100, EditorGUIUtility.singleLineHeight);
                
                // Create a preview of the HDR color
                Color previewColor = dissolveEffect.lineColor * dissolveEffect.colorIntensity;
                previewColor.a = 1f;
                EditorGUI.DrawRect(previewRect, previewColor);
            }
            else
            {
                EditorGUILayout.PropertyField(lineColorProp, new GUIContent("Line Color"));
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Texture settings
        showTextureSettings = EditorGUILayout.Foldout(showTextureSettings, "Texture Settings", true);
        if (showTextureSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(secondaryTextureProp, new GUIContent("Secondary Texture"));
            EditorGUILayout.PropertyField(secondaryColorProp, new GUIContent("Secondary Color"));
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(noiseTextureProp, new GUIContent("Noise Texture"));
            
            if (noiseTextureProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("A noise texture is required for the dissolve effect.", MessageType.Warning);
            }
            
            EditorGUILayout.PropertyField(noiseScaleProp, new GUIContent("Noise Scale"));
            EditorGUILayout.PropertyField(noiseStrengthProp, new GUIContent("Noise Strength"));
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Interactor settings
        showInteractorSettings = EditorGUILayout.Foldout(showInteractorSettings, "Interactor Settings", true);
        if (showInteractorSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(useMultipleInteractorsProp, new GUIContent("Use Multiple Interactors"));
            
            if (useMultipleInteractorsProp.boolValue)
            {
                int interactorCount = FindObjectsOfType<DissolveController>().Length;
                EditorGUILayout.HelpBox("Using " + interactorCount + " interactors in the scene.", MessageType.Info);
            }
            
            EditorGUILayout.PropertyField(shapeCutoffProp, new GUIContent("Shape Cutoff"));
            EditorGUILayout.PropertyField(shapeSmoothnessProps, new GUIContent("Shape Smoothness"));
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(useControllerProp, new GUIContent("Use Controller"));
            
            if (useControllerProp.boolValue)
            {
                EditorGUILayout.PropertyField(controllerProp, new GUIContent("Controller"));
                
                if (controllerProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Please assign a DissolveController.", MessageType.Warning);
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Preview section
        showPreview = EditorGUILayout.Foldout(showPreview, "Help & Tips", true);
        if (showPreview)
        {
            EditorGUILayout.HelpBox(
                "This component adds a dissolve effect to objects while preserving their original materials.\n\n" +
                "Tips:\n" +
                "• The effect is applied as an overlay on top of existing materials\n" +
                "• For best results, use a noise texture with good contrast\n" +
                "• HDR colors can create a glowing effect at the dissolve edge\n" +
                "• Use a DissolveController for positioning the effect", 
                MessageType.Info);
        }
        
        // Apply changes
        serializedObject.ApplyModifiedProperties();
        
        // Show a button to refresh if needed
        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh Effect"))
        {
            dissolveEffect.enabled = false;
            dissolveEffect.enabled = true;
        }
    }
}