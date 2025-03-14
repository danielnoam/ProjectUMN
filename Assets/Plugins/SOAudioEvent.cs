using System;
using UnityEngine;
using UnityEditor;
using CustomAttribute;
using UnityEngine.Audio;
using VInspector;
using Random = UnityEngine.Random;


[CreateAssetMenu(fileName = "AudioEvent", menuName = "SO Audio/Audio Event")]
public class SOAudioEvent : ScriptableObject
{
    public string aoName = "Name";
    public string aoAuthor = "Author";
    public AudioClip[] clips;
    public AudioMixerGroup mixerGroup;
    [MinMaxRange(0f, 1f)] public RangedFloat volume = 1f;
    [MinMaxRange(-3f, 3f)] public RangedFloat pitch = 1f;
    [Range(-1f, 1f)] public float stereoPan = 0f;
    [Range(0f, 1f)] public float spatialBlend = 0f; 
    [Range(0f, 1.1f)] public float reverbZoneMix = 1f;
    public bool bypassEffects;
    public bool bypassListenerEffects;
    public bool bypassReverbZones;
    public bool loop;
    

    
    [Header("3D Sound Settings")]
    public bool set3DSettings = false;
    [EnableIf("set3DSettings")]
    [MinMaxRange(0f, 5f)] public float dopplerLevel = 1f; 
    [MinMaxRange(0f, 360f)] public float spread = 0f; 
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    [Min(0)] public float minDistance = 1f;
    [Min(0)] public float maxDistance = 500f;
    [EndIf]
    
    





    #region Play AE ----------------------------------------------------------------------------

    public void Play(AudioSource source)
    {
        if (clips.Length == 0) 
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
        if (clips.Length == 0) 
        {
            #if UNITY_EDITOR
            Debug.Log("No clips found");
            #endif
            return;
        }
        
        SetAudioSourceSettings(source);
        source.PlayDelayed(delay);
    }
    
    
    public void CrossFade(AudioSource source, float fadeDuration, float delay = 0)
    {
        if (clips.Length == 0) 
        {
#if UNITY_EDITOR
            Debug.Log("No clips found");
#endif
            return;
        }
    
        // Get or add AudioFadeController to the AudioSource's GameObject
        AudioFadeController controller = source.gameObject.GetComponent<AudioFadeController>();
        if (controller == null)
        {
            controller = source.gameObject.AddComponent<AudioFadeController>();
        }
    
        // Store the current volume to return to after fade completes
        float targetVolume = Random.Range(volume.minValue, volume.maxValue);
    
        // If audio is already playing, start full crossfade
        if (source.isPlaying)
        {
            // Start at current volume
            float startVolume = source.volume;
        
            // Choose the new clip
            AudioClip newClip = clips[Random.Range(0, clips.Length)];
            controller.CrossFade(source, newClip, this, fadeDuration, startVolume, targetVolume, delay);
        }
        else
        {
            // Just fade in the new clip
            SetAudioSourceSettings(source);
            source.volume = 0f;
        
            if (delay > 0f)
            {
                source.PlayDelayed(delay);
                controller.FadeInDelayed(source, fadeDuration, targetVolume, delay);
            }
            else
            {
                source.Play();
                controller.FadeIn(source, fadeDuration, targetVolume);
            }
        }
    }
    
    
    public void PlayAtPoint(Vector3 position = new Vector3())
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

        // Set settings to audio source and play
        SetAudioSourceSettings(source);
        source.Play();
        Destroy(source.gameObject, source.clip.length);
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
        source.Stop();
    }

    public void Pause(AudioSource source)
    {
        source.Pause();
    }

    public void Continue(AudioSource source)
    {
        
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

    
    

