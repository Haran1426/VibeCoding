using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 서버 권한 매치 타이머 + 상태 동기화.
/// MatchManager 의 네트워크 확장판입니다.
/// </summary>
public class MatchNetworkManager : NetworkBehaviour
{
    public static MatchNetworkManager Instance { get; private set; }

    [SerializeField] private float matchDuration    = 120f;
    [SerializeField] private float countdownSeconds = 3f;

    // ── NetworkVariables ────────────────────────────────────
    public NetworkVariable<float>      NetTimeRemaining = new NetworkVariable<float>(
        120f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<MatchState> NetMatchState = new NetworkVariable<MatchState>(
        MatchState.WaitingToStart,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        NetMatchState.OnValueChanged     += OnMatchStateSync;
        NetTimeRemaining.OnValueChanged  += OnTimeSync;

        if (IsServer)
            StartCoroutine(RunMatch());
    }

    public override void OnNetworkDespawn()
    {
        NetMatchState.OnValueChanged     -= OnMatchStateSync;
        NetTimeRemaining.OnValueChanged  -= OnTimeSync;
    }

    // ── 서버: 매치 진행 ──────────────────────────────────────
    void Update()
    {
        if (!IsServer) return;
        if (NetMatchState.Value != MatchState.Playing) return;

        NetTimeRemaining.Value -= Time.deltaTime;
        if (NetTimeRemaining.Value <= 0f)
        {
            NetTimeRemaining.Value = 0f;
            EndMatch();
        }
    }

    private IEnumerator RunMatch()
    {
        NetMatchState.Value     = MatchState.Countdown;
        NetTimeRemaining.Value  = matchDuration;
        yield return new WaitForSeconds(countdownSeconds);
        NetMatchState.Value = MatchState.Playing;
    }

    private void EndMatch()
    {
        NetMatchState.Value = MatchState.Ended;

        // 점수 수집 후 브로드캐스트
        var scores = new Dictionary<int, int>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var sync = client.PlayerObject?.GetComponent<PlayerNetworkSync>();
            if (sync != null)
                scores[(int)client.ClientId] = sync.NetScore.Value;
        }
        EndMatchClientRpc(SerializeScores(scores));
    }

    [ClientRpc]
    private void EndMatchClientRpc(int[] flatScores)
    {
        var scores = DeserializeScores(flatScores);
        EventBus.RaiseMatchEnded(scores);
        AudioManager.Instance?.PlayGameOver();
    }

    // ── 클라이언트: NetworkVariable 변경 반영 ────────────────
    private void OnMatchStateSync(MatchState prev, MatchState next)
    {
        EventBus.RaiseMatchStateChanged(next);
        if (next == MatchState.Playing) EventBus.RaiseMatchStarted();
    }

    private void OnTimeSync(float prev, float next) { /* HUDManager 는 MatchManager 참조 */ }

    // ── 직렬화 헬퍼 (int[] 평탄화: [id0, score0, id1, score1, ...]) ──
    private static int[] SerializeScores(Dictionary<int, int> d)
    {
        var arr = new int[d.Count * 2];
        int i = 0;
        foreach (var kv in d) { arr[i++] = kv.Key; arr[i++] = kv.Value; }
        return arr;
    }

    private static Dictionary<int, int> DeserializeScores(int[] arr)
    {
        var d = new Dictionary<int, int>();
        for (int i = 0; i + 1 < arr.Length; i += 2) d[arr[i]] = arr[i + 1];
        return d;
    }

    // ── 공개 유틸 (HUDManager 용) ────────────────────────────
    public float TimeRemaining => NetTimeRemaining.Value;

    public string GetFormattedTime()
    {
        int m = Mathf.FloorToInt(TimeRemaining / 60f);
        int s = Mathf.FloorToInt(TimeRemaining % 60f);
        return string.Format("{0:00}:{1:00}", m, s);
    }
}
