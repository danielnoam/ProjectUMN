
using System.Collections;
using UnityEngine;
using UnityEditor;
using CustomAttribute;
using UnityEngine.Audio;
using VInspector;
using Random = UnityEngine.Random;


[CreateAssetMenu(fileName = "AudioEvent", menuName = "SO Audio/Audio Event")]
public class SOAudioEvent : ScriptableObject
{

    [Header("Settings")]
    public AudioClip[] clips;
    public AudioMixerGroup mixerGroup;
    [MinMaxRange(0f, 1f)] public RangedFloat volume = 1f;
    [MinMaxRange(-3f, 3f)] public RangedFloat pitch = 1f;
    [Range(-1f, 1f), Tooltip("Left,Right")] public float stereoPan = 0f;
    [Range(0f, 1f), Tooltip("2D,3D")] public float spatialBlend = 0f; 
    [Range(0f, 1.1f)] public float reverbZoneMix = 1f;
    public bool bypassEffects;
    public bool bypassListenerEffects;
    public bool bypassReverbZones;
    public bool loop;

    
    [Header("3D Sound")]
    public bool set3DSettings = false;
    [EnableIf("set3DSettings")]
    [MinMaxRange(0f, 5f)] public float dopplerLevel = 1f; 
    [MinMaxRange(0f, 360f)] public float spread = 0f; 
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    [Min(0)] public float minDistance = 1f;
    [Min(0)] public float maxDistance = 500f;
    [EndIf]
    

    [Header("Information")]
    public string aoName = "Name";
    public string aoAuthor = "Author";




    #region Play AE ----------------------------------------------------------------------------

    public void Play(AudioSource source)
    {
        if (clips.Length == 0 || !source) 
        {
            #if UNITY_EDITOR
            Debug.Log("No clips found");
            #endif
            return;
        }
        

        SetAudioSourceSettings(source);
        source.Play();
    }

    public void Play(AudioSource source, float delay)
    {
        if (clips.Length == 0 || !source) 
        {
            #if UNITY_EDITOR
            Debug.Log("No clips found");
            #endif
            return;
        }
        
        SetAudioSourceSettings(source);
        source.PlayDelayed(delay);
    }
    
    
    public void PlayAtPoint(Vector3 position = new())
    {
        if (clips.Length == 0)
        {
            #if UNITY_EDITOR
            Debug.Log("No clips found");
            #endif
            return;
        }
        
        AudioSource source = new GameObject("OneShotAudioEvent").AddComponent<AudioSource>();
        source.transform.position = position;
        
        SetAudioSourceSettings(source);
        source.Play();
        Destroy(source.gameObject, source.clip.length);
    }
    
    
    public IEnumerator FadeOutRoutine(AudioSource source, float fadeTime)
    {
        float startVolume = source.volume;

        while (source.volume > 0)
        {
            source.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        source.Stop();
        source.volume = startVolume;
    }


    #endregion Play AE ----------------------------------------------------------------------------
    

    
    #region Autdio source controll ------------------------------------------------------------------------------------------------
    
    public void SetAudioSourceSettings(AudioSource source)
    {
        if (!source) return;
        
        source.clip = clips[Random.Range(0, clips.Length)];
        source.outputAudioMixerGroup = mixerGroup;
        source.volume = Random.Range(volume.minValue, volume.maxValue);
        source.pitch = Random.Range(pitch.minValue, pitch.maxValue);
        source.panStereo = stereoPan;
        source.spatialBlend = spatialBlend;
        source.reverbZoneMix = reverbZoneMix;
        source.bypassEffects = bypassEffects;
        source.bypassListenerEffects = bypassListenerEffects;
        source.bypassReverbZones = bypassReverbZones;
        source.loop = loop;

        if (set3DSettings)
        {
            source.dopplerLevel = dopplerLevel;
            source.spread = spread;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = rolloffMode;
        }
    }
    
    public void Stop(AudioSource source)
    {
        if (!source) return;
        
        source.Stop();
    }

    public void Pause(AudioSource source)
    {
        if (!source) return;
        
        source.Pause();
    }

    public void Continue(AudioSource source)
    {
        if (!source) return;
        source.UnPause();
    }

    #endregion Autdio source controll ------------------------------------------------------------------------------------------------

    
    
}


#region Editor ------------------------------------------------------------------------------------------------
#if UNITY_EDITOR


// Preview button
[CustomEditor(typeof(SOAudioEvent), true)]
public class AudioEventEditor : Editor
{

    [SerializeField] private AudioSource previewer;

    public void OnEnable()
    {
        previewer = EditorUtility
            .CreateGameObjectWithHideFlags("Audio preview", HideFlags.HideAndDontSave, typeof(AudioSource))
            .GetComponent<AudioSource>();
    }

    public void OnDisable()
    {
        DestroyImmediate(previewer.gameObject);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
        if (GUILayout.Button("Preview Sound"))
        {
            ((SOAudioEvent)target).Play(previewer);
            Debug.Log("Playing " + previewer.clip.name);
        }
        
        if (GUILayout.Button("Stop Sound"))
        {
            ((SOAudioEvent)target).Stop(previewer);
        }

        EditorGUI.EndDisabledGroup();
    }
}
#endif
#endregion Editor ------------------------------------------------------------------------------------------------

    
    

