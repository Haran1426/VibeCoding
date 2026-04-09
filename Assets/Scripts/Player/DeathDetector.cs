using UnityEngine;

/// <summary>
/// SRP: 낙사 감지만 담당합니다.
///
/// 멀티: NetworkObject가 있으면 PlayerNetworkSync.DiedServerRpc 로 전송.
///       없으면(싱글/테스트) EventBus 로 직접 발행.
/// </summary>
public class DeathDetector : MonoBehaviour
{
    [SerializeField] private float deathY = -8f;

    private PlayerStats      _stats;
    private InputRecorder    _recorder;
    private bool             _dead;

    void Awake()
    {
        _stats    = GetComponent<PlayerStats>();
        _recorder = GetComponent<InputRecorder>();
    }

    void Update()
    {
        if (_dead) return;
        if (transform.position.y < deathY) TriggerDeath();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_dead) return;
        if (other.CompareTag("DeathZone")) TriggerDeath();
    }

    private void TriggerDeath()
    {
        _dead = true;
        gameObject.SetActive(false);

        var netSync = GetComponent<PlayerNetworkSync>();

        if (netSync != null && netSync.IsOwner)
        {
            // 멀티: 기록을 배열로 변환 후 ServerRpc 전송
            var frames = _recorder != null
                ? _recorder.GetRecording().ToArray()
                : new InputFrame[0];

            // 최대 2000 프레임으로 제한 (네트워크 페이로드 한계 방지)
            if (frames.Length > 2000)
            {
                var trimmed = new InputFrame[2000];
                System.Array.Copy(frames, frames.Length - 2000, trimmed, 0, 2000);
                frames = trimmed;
            }

            netSync.DiedServerRpc(frames);
            _recorder?.ClearRecording();
        }
        else if (netSync == null)
        {
            // 싱글 / 로컬 테스트용 fallback
            int hitBy = _stats != null ? _stats.lastHitBy  : -1;
            int id    = _stats != null ? _stats.playerId   :  0;
            EventBus.RaiseEntityDied(id, transform.position, hitBy);
        }
    }

    public void ResetDead() => _dead = false;
}
