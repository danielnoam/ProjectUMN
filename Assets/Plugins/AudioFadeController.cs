using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class AudioFadeController : MonoBehaviour
{
    private Coroutine currentFade = null;
    
    /// <summary>
    /// Crossfades between the current playing audio and a new clip
    /// </summary>
    /// <param name="source">The AudioSource to control</param>
    /// <param name="newClip">The new AudioClip to fade to</param>
    /// <param name="audioEvent">The SOAudioEvent that contains audio settings</param>
    /// <param name="duration">The total duration of the fade</param>
    /// <param name="startVolume">The starting volume</param>
    /// <param name="targetVolume">The target volume after fade completes</param>
    /// <param name="delay">Optional delay before starting the crossfade</param>
    public void CrossFade(AudioSource source, AudioClip newClip, SOAudioEvent audioEvent, float duration, float startVolume, float targetVolume, float delay = 0f)
    {
        // Stop any current fade operations
        if (currentFade != null)
        {
            StopCoroutine(currentFade);
        }
        
        if (delay > 0f)
        {
            currentFade = StartCoroutine(DelayedCrossFadeCoroutine(source, newClip, audioEvent, duration, startVolume, targetVolume, delay));
        }
        else
        {
            currentFade = StartCoroutine(CrossFadeCoroutine(source, newClip, audioEvent, duration, startVolume, targetVolume));
        }
    }
    
    /// <summary>
    /// Fades in the audio from silent to the target volume
    /// </summary>
    /// <param name="source">The AudioSource to control</param>
    /// <param name="duration">The duration of the fade</param>
    /// <param name="targetVolume">The target volume after fade completes</param>
    public void FadeIn(AudioSource source, float duration, float targetVolume)
    {
        // Stop any current fade operations
        if (currentFade != null)
        {
            StopCoroutine(currentFade);
        }
        
        currentFade = StartCoroutine(FadeInCoroutine(source, duration, targetVolume));
    }
    
    /// <summary>
    /// Fades in the audio after a delay
    /// </summary>
    /// <param name="source">The AudioSource to control</param>
    /// <param name="duration">The duration of the fade</param>
    /// <param name="targetVolume">The target volume after fade completes</param>
    /// <param name="delay">The delay before starting the fade</param>
    public void FadeInDelayed(AudioSource source, float duration, float targetVolume, float delay)
    {
        // Stop any current fade operations
        if (currentFade != null)
        {
            StopCoroutine(currentFade);
        }
        
        currentFade = StartCoroutine(DelayedFadeInCoroutine(source, duration, targetVolume, delay));
    }
    
    /// <summary>
    /// Fades out the audio to silence
    /// </summary>
    /// <param name="source">The AudioSource to control</param>
    /// <param name="duration">The duration of the fade</param>
    /// <param name="stopAfterFade">Whether to stop the audio after fade completes</param>
    public void FadeOut(AudioSource source, float duration, bool stopAfterFade = true)
    {
        // Stop any current fade operations
        if (currentFade != null)
        {
            StopCoroutine(currentFade);
        }
        
        currentFade = StartCoroutine(FadeOutCoroutine(source, duration, stopAfterFade));
    }
    
    private IEnumerator CrossFadeCoroutine(AudioSource source, AudioClip newClip, SOAudioEvent audioEvent, float duration, float startVolume, float targetVolume)
    {
        // Fade out
        float timer = 0f;
        
        while (timer < duration / 2)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / (duration / 2));
            yield return null;
        }
        
        // Switch clips
        audioEvent.SetAudioSourceSettings(source);
        source.volume = 0f;
        
        // Fade in
        timer = 0f;
        
        while (timer < duration / 2)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, timer / (duration / 2));
            yield return null;
        }
        
        // Ensure volume is set to target at the end
        source.volume = targetVolume;
        currentFade = null;
    }
    
    private IEnumerator DelayedCrossFadeCoroutine(AudioSource source, AudioClip newClip, SOAudioEvent audioEvent, float duration, float startVolume, float targetVolume, float delay)
    {
        // Wait for delay
        yield return new WaitForSeconds(delay);
        
        // Then perform normal crossfade
        yield return StartCoroutine(CrossFadeCoroutine(source, newClip, audioEvent, duration, startVolume, targetVolume));
    }
    
    private IEnumerator FadeInCoroutine(AudioSource source, float duration, float targetVolume)
    {
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, timer / duration);
            yield return null;
        }
        
        // Ensure volume is set to target at the end
        source.volume = targetVolume;
        currentFade = null;
    }
    
    private IEnumerator DelayedFadeInCoroutine(AudioSource source, float duration, float targetVolume, float delay)
    {
        // Since PlayDelayed was already called, we need to wait until it starts playing
        yield return new WaitForSeconds(delay);
        
        // Now perform the fade in
        yield return StartCoroutine(FadeInCoroutine(source, duration, targetVolume));
    }
    
    private IEnumerator FadeOutCoroutine(AudioSource source, float duration, bool stopAfterFade)
    {
        float startVolume = source.volume;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }
        
        // Ensure volume is set to 0 at the end
        source.volume = 0f;
        
        if (stopAfterFade)
        {
            source.Stop();
        }
        
        currentFade = null;
    }
}