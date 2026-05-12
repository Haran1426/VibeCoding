using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Local-only micro hitstop for offline/editor feel testing.
/// Online matches intentionally skip global timescale changes to avoid desync.
/// </summary>
public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    [SerializeField] private float minForce = 16f;
    [SerializeField] private float maxDuration = 0.055f;
    [SerializeField] private float timeScaleDuringStop = 0.08f;

    private Coroutine _routine;
    private float _restoreScale = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Pulse(float force)
    {
        if (force < minForce) return;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) return;

        float t = Mathf.InverseLerp(minForce, minForce * 2.5f, force);
        float duration = Mathf.Lerp(0.018f, maxDuration, t);

        if (_routine != null)
        {
            StopCoroutine(_routine);
            Time.timeScale = _restoreScale;
        }

        _routine = StartCoroutine(PulseRoutine(duration));
    }

    private IEnumerator PulseRoutine(float duration)
    {
        _restoreScale = Time.timeScale;
        Time.timeScale = timeScaleDuringStop;

        yield return new WaitForSecondsRealtime(duration);

        if (Mathf.Approximately(Time.timeScale, timeScaleDuringStop))
            Time.timeScale = _restoreScale;

        _routine = null;
    }
}
