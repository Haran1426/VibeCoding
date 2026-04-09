using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// SRP: BGM 루프 재생 + 절차적 SFX 생성만 담당합니다.
/// PlayerPrefs MasterVolume / SFXVolume 을 반영합니다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    // ── 캐시된 SFX 클립 ─────────────────────────────────────
    private AudioClip clipShoot;
    private AudioClip clipBulletHit;
    private AudioClip clipEnemyDeath;
    private AudioClip clipLevelUp;
    private AudioClip clipGameOver;
    private AudioClip clipExpPickup;
    private AudioClip clipPlayerHurt;
    private AudioClip _clipDash;        // [버그6 픽스]
    private AudioClip _clipCloneSpawn;  // [버그6 픽스]

    private const int SampleRate = 22050;

    // ── 볼륨 ────────────────────────────────────────────────
    private float MasterVol => PlayerPrefs.GetFloat("MasterVolume", 0.8f);
    private float SFXVol    => PlayerPrefs.GetFloat("SFXVolume",    0.8f);

    // ── MonoBehaviour ────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        BakeAllSFX();
        StartBGM();
    }

    void OnEnable()  => EventBus.OnMatchStateChanged += OnMatchStateChanged;
    void OnDisable() => EventBus.OnMatchStateChanged -= OnMatchStateChanged;

    // ── BGM ─────────────────────────────────────────────────
    private void StartBGM()
    {
        AudioClip bgm = GenerateBGM(duration: 4f);
        bgmSource.clip   = bgm;
        bgmSource.loop   = true;
        bgmSource.volume = MasterVol * 0.45f;
        bgmSource.Play();
    }

    private void OnMatchStateChanged(MatchState state)
    {
        if (state == MatchState.Ended)
        {
            // PlayGameOver() 는 MatchManager 가 직접 호출
            bgmSource.Stop();
        }
        else if (state == MatchState.Playing && !bgmSource.isPlaying)
        {
            bgmSource.volume = MasterVol * 0.45f;
            bgmSource.Play();
        }
    }

    // ── Public SFX ──────────────────────────────────────────
    public void PlayShoot()       => PlaySFX(clipShoot,       0.35f);
    public void PlayBulletHit()   => PlaySFX(clipBulletHit,   0.4f);
    public void PlayEnemyDeath()  => PlaySFX(clipEnemyDeath,  0.55f);
    public void PlayLevelUp()     => PlaySFX(clipLevelUp,     0.9f);
    public void PlayGameOver()    => PlaySFX(clipGameOver,    1.0f);
    public void PlayExpPickup()   => PlaySFX(clipExpPickup,   0.3f);
    public void PlayPlayerHurt()  => PlaySFX(clipPlayerHurt,  0.6f);

    // ── Neon Rewind Arena SFX ────────────────────────────────
    public void PlayAttack()      => PlaySFX(clipShoot,       0.5f);
    public void PlayAttackHit()   => PlaySFX(clipBulletHit,   0.7f);
    public void PlayDash()        => PlaySFX(_clipDash,       0.5f);    // [버그6 픽스] 캐싱
    public void PlayCloneSpawn()  => PlaySFX(_clipCloneSpawn, 0.7f);    // [버그6 픽스] 캐싱
    public void PlayRespawn()     => PlaySFX(clipLevelUp,     0.6f);

    private void PlaySFX(AudioClip clip, float relativeVol)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, MasterVol * SFXVol * relativeVol);
    }

    // ── 볼륨 갱신 (MenuManager에서 호출) ────────────────────
    public void RefreshVolume()
    {
        if (bgmSource != null) bgmSource.volume = MasterVol * 0.45f;
    }

    // ── AudioSource 자동 생성 ────────────────────────────────
    private void EnsureAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource          = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
        }
        if (sfxSource == null)
        {
            sfxSource          = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }
    }

    // ════════════════════════════════════════════════════════
    //  SFX 클립 베이킹
    // ════════════════════════════════════════════════════════
    private void BakeAllSFX()
    {
        // 발사: 날카로운 레이저 (2200→800 Hz, 0.07s)
        clipShoot = GenerateSweep(2200f, 800f, 0.07f, 0.7f);

        // 총알 충돌: 짧은 화이트 노이즈 임팩트 (0.05s)
        clipBulletHit = GenerateNoise(0.05f, 0.6f, fadeOut: true);

        // 적 사망: 피치 다운 + 노이즈 (500→60 Hz, 0.18s)
        clipEnemyDeath = GenerateMix(
            GenerateSweep(500f, 60f, 0.18f, 0.8f),
            GenerateNoise(0.18f, 0.4f, fadeOut: true)
        );

        // 레벨업: 상승 아르페지오 (Do-Mi-Sol-Do', 0.35s)
        clipLevelUp = GenerateArpeggio(new float[]{ 523f, 659f, 784f, 1047f }, 0.09f, 0.8f);

        // 게임오버: 하강 3음 (400→280→180 Hz, 0.55s)
        clipGameOver = GenerateArpeggio(new float[]{ 400f, 280f, 180f, 120f }, 0.14f, 0.9f, ascending: false);

        // EXP 픽업: 짧은 핑 (2000 Hz, 0.06s)
        clipExpPickup = GenerateTone(2000f, 0.06f, 0.5f, fadeOut: true);

        // 플레이어 피격: 저음 쿵 + 노이즈 (180 Hz, 0.1s)
        clipPlayerHurt = GenerateMix(
            GenerateTone(180f, 0.1f, 0.9f, fadeOut: true),
            GenerateNoise(0.05f, 0.5f, fadeOut: true)
        );

        // [버그6 픽스] 대시/분신 생성 SFX 미리 베이킹
        _clipDash       = GenerateSweep(600f, 1800f, 0.06f, 0.5f);
        _clipCloneSpawn = GenerateSweep(200f, 800f,  0.12f, 0.6f);
    }

    // ════════════════════════════════════════════════════════
    //  BGM 절차적 생성 (사이버펑크 앰비언트)
    // ════════════════════════════════════════════════════════
    private AudioClip GenerateBGM(float duration)
    {
        int samples = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[samples];

        // 레이어: 베이스 드론 + 서브베이스 + 하이 글리치 펄스
        float[] freqs   = { 55f, 110f, 165f, 220f, 440f };
        float[] volumes = { 0.35f, 0.18f, 0.10f, 0.06f, 0.04f };

        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / SampleRate;
            float val = 0f;

            for (int f = 0; f < freqs.Length; f++)
                val += Mathf.Sin(2f * Mathf.PI * freqs[f] * t) * volumes[f];

            // 글리치 게이트: 8분의 1박자마다 미세 노이즈 버스트
            float beatPhase = (t * 4f) % 1f;
            if (beatPhase < 0.05f)
                val += (UnityEngine.Random.value * 2f - 1f) * 0.07f * (1f - beatPhase / 0.05f);

            // 느린 LFO amplitude modulation (0.4 Hz)
            float lfo = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.4f * t);
            data[i] = Mathf.Clamp(val * lfo, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("BGM", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ════════════════════════════════════════════════════════
    //  SFX 생성 헬퍼
    // ════════════════════════════════════════════════════════

    /// 단순 사인파 톤
    private AudioClip GenerateTone(float freq, float duration, float vol, bool fadeOut = false)
    {
        int samples = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / SampleRate;
            float fade = fadeOut ? (1f - (float)i / samples) : 1f;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * vol * fade;
        }
        return MakeClip("Tone", data);
    }

    /// 주파수 스윕 (선형)
    private AudioClip GenerateSweep(float startHz, float endHz, float duration, float vol)
    {
        int samples = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[samples];
        double phase = 0.0;
        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / samples;
            float freq = Mathf.Lerp(startHz, endHz, t);
            float fade = 1f - t;
            phase += freq / SampleRate;
            data[i] = (float)Math.Sin(2.0 * Math.PI * phase) * vol * fade;
        }
        return MakeClip("Sweep", data);
    }

    /// 화이트 노이즈
    private AudioClip GenerateNoise(float duration, float vol, bool fadeOut = false)
    {
        int samples = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[samples];
        System.Random rng = new System.Random();
        for (int i = 0; i < samples; i++)
        {
            float fade = fadeOut ? (1f - (float)i / samples) : 1f;
            data[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * vol * fade;
        }
        return MakeClip("Noise", data);
    }

    /// 아르페지오 (음표 배열, 각 음 duration초)
    private AudioClip GenerateArpeggio(float[] freqs, float noteDur, float vol, bool ascending = true)
    {
        int noteLen  = Mathf.RoundToInt(SampleRate * noteDur);
        int total    = noteLen * freqs.Length;
        float[] data = new float[total];

        for (int n = 0; n < freqs.Length; n++)
        {
            int noteIdx = ascending ? n : (freqs.Length - 1 - n);
            float freq = freqs[noteIdx];
            for (int i = 0; i < noteLen; i++)
            {
                float t    = (float)i / SampleRate;
                float fade = 1f - (float)i / noteLen;
                data[n * noteLen + i] = Mathf.Sin(2f * Mathf.PI * freq * t) * vol * fade;
            }
        }
        return MakeClip("Arpeggio", data);
    }

    /// 두 클립 샘플 합산
    private AudioClip GenerateMix(AudioClip a, AudioClip b)
    {
        int len  = Mathf.Max(a.samples, b.samples);
        float[] da = new float[a.samples]; a.GetData(da, 0);
        float[] db = new float[b.samples]; b.GetData(db, 0);
        float[] out_ = new float[len];
        for (int i = 0; i < len; i++)
        {
            float va = i < da.Length ? da[i] : 0f;
            float vb = i < db.Length ? db[i] : 0f;
            out_[i] = Mathf.Clamp(va + vb, -1f, 1f);
        }
        return MakeClip("Mix", out_);
    }

    private AudioClip MakeClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
