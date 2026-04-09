using UnityEngine;

/// <summary>
/// 효과음 재생. AudioSource 기반.
/// Inspector에서 AudioClip을 할당하거나, 없으면 절차적으로 생성된 비프음 사용.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Clips (비워두면 절차적 생성)")]
    [SerializeField] private AudioClip popSmallClip;
    [SerializeField] private AudioClip popMediumClip;
    [SerializeField] private AudioClip popLargeClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.75f;

    private AudioSource _source;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    public void PlayPop(int groupSize)
    {
        AudioClip clip;
        if (groupSize >= 10 && popLargeClip)       clip = popLargeClip;
        else if (groupSize >= 5 && popMediumClip)  clip = popMediumClip;
        else if (popSmallClip)                     clip = popSmallClip;
        else { PlayProceduralPop(groupSize); return; }

        _source.PlayOneShot(clip, sfxVolume);
    }

    public void PlayResult(bool win)
    {
        AudioClip clip = win ? winClip : loseClip;
        if (clip) _source.PlayOneShot(clip, sfxVolume);
        else PlayProceduralResult(win);
    }

    // ── 절차적 사운드 (클립 없을 때 폴백) ────────────────────
    private void PlayProceduralPop(int groupSize)
    {
        float freq = 440f + groupSize * 30f;
        float dur  = 0.08f + groupSize * 0.01f;
        StartCoroutine(PlayTone(freq, dur, 0.3f));
    }

    private void PlayProceduralResult(bool win)
    {
        if (win)
            StartCoroutine(PlayChord(new[]{ 523f, 659f, 784f }, 0.25f, 0.5f));
        else
            StartCoroutine(PlayChord(new[]{ 220f, 196f }, 0.3f, 0.6f));
    }

    private System.Collections.IEnumerator PlayTone(float freq, float duration, float volume)
    {
        int sampleRate = AudioSettings.outputSampleRate;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Clamp01(1f - t / duration);
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create("pop", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        _source.PlayOneShot(clip, sfxVolume);
        yield return null;
    }

    private System.Collections.IEnumerator PlayChord(float[] freqs, float dur, float vol)
    {
        float delay = 0.12f;
        foreach (float f in freqs)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(PlayTone(f, dur, vol));
        }
    }
}
