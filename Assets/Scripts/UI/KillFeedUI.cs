using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 처치/낙사 알림을 화면에 순서대로 표시합니다.
///
/// Inspector 연결:
///   feedSlots — TextMeshProUGUI 3~4개 (세로로 배치, 위쪽이 최신)
///   displayTime — 한 항목이 표시되는 시간 (기본 3초)
///
/// 메시지 형식:
///   처치: "<color=#FF6060>Killer</color>  ▶  Victim"
///   낙사: "Victim  낙사"
/// </summary>
public class KillFeedUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] feedSlots;
    [SerializeField] private float            displayTime = 3f;

    private Coroutine[] _coroutines;  // 슬롯별 페이드 코루틴 추적
    private int         _nextSlot;    // 라운드로빈 인덱스

    private int _localPlayerId;

    // ════════════════════════════════════════════════════════
    void Awake()
    {
        if (feedSlots != null)
        {
            _coroutines = new Coroutine[feedSlots.Length];
            foreach (var slot in feedSlots)
                if (slot != null) slot.text = "";
        }
    }

    void Start()
    {
        // 싱글=0, 멀티=OwnerClientId (지연 초기화)
        var netSync = FindFirstObjectByType<PlayerNetworkSync>();
        if (netSync != null && netSync.IsOwner)
            _localPlayerId = (int)netSync.OwnerClientId;
    }

    void OnEnable()  => EventBus.OnEntityDied += OnEntityDied;
    void OnDisable() => EventBus.OnEntityDied -= OnEntityDied;

    // ════════════════════════════════════════════════════════
    private void OnEntityDied(int victimId, UnityEngine.Vector3 pos, int killerId)
    {
        string victim = EntityLabel(victimId);
        string msg;

        if (killerId >= 0 && killerId != victimId)
        {
            string killer     = EntityLabel(killerId);
            string killerColor = killerId == _localPlayerId ? "#60FF90" : "#FF6060";
            msg = $"<color={killerColor}>{killer}</color>  ▶  {victim}";
        }
        else
        {
            msg = $"{victim}  낙사";
        }

        ShowMessage(msg);
    }

    private void ShowMessage(string msg)
    {
        if (feedSlots == null || feedSlots.Length == 0) return;

        int idx  = _nextSlot % feedSlots.Length;
        _nextSlot++;

        var slot = feedSlots[idx];
        if (slot == null) return;

        // 기존 코루틴 중단 후 새 코루틴 시작
        if (_coroutines[idx] != null)
            StopCoroutine(_coroutines[idx]);

        _coroutines[idx] = StartCoroutine(ShowAndFade(slot, msg));
    }

    private IEnumerator ShowAndFade(TextMeshProUGUI slot, string msg)
    {
        slot.text  = msg;
        slot.color = new Color(slot.color.r, slot.color.g, slot.color.b, 1f);

        // 표시 유지
        yield return new WaitForSeconds(displayTime - 0.5f);

        // 0.5초 페이드 아웃
        float elapsed = 0f;
        Color c = slot.color;
        while (elapsed < 0.5f)
        {
            elapsed     += Time.deltaTime;
            slot.color   = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, elapsed / 0.5f));
            yield return null;
        }

        slot.text = "";
    }

    // ════════════════════════════════════════════════════════
    private string EntityLabel(int id)
    {
        if (id == _localPlayerId) return "You";
        if (id >= 100)            return "Clone";
        return $"P{id + 1}";
    }
}
