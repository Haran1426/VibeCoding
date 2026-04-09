using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    private PlayerStats stats;
    private List<AbilityData> allAbilities = new List<AbilityData>();
    private List<AbilityData> acquiredAbilities = new List<AbilityData>();

    public System.Action<List<AbilityData>> OnAbilityChoiceReady;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        InitAbilities();
    }

    void Start()
    {
        stats = PlayerStats.Instance;
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp += PresentAbilityChoice;
    }

    void InitAbilities()
    {
        allAbilities.Add(new AbilityData("damage_up",    "파워 업",      "총알 데미지 +30%",         5));
        allAbilities.Add(new AbilityData("firerate_up",  "속사",         "발사 속도 +25%",           5));
        allAbilities.Add(new AbilityData("speed_up",     "질풍",         "이동 속도 +20%",           4));
        allAbilities.Add(new AbilityData("health_up",    "강인함",       "최대 체력 +30",            4));
        allAbilities.Add(new AbilityData("heal",         "회복",         "체력을 20 회복",           3));
        allAbilities.Add(new AbilityData("triple_shot",  "트리플 샷",    "3방향 동시 사격",          1));
        allAbilities.Add(new AbilityData("split_shot",   "십자 사격",    "추가로 좌우 사격",         1));
        allAbilities.Add(new AbilityData("pierce",       "관통",         "총알이 적을 관통",         3));
        allAbilities.Add(new AbilityData("magnet",       "자석",         "경험치 흡수 범위 +50%",    3));
        allAbilities.Add(new AbilityData("bullet_speed", "탄속 증가",    "총알 속도 +30%",           3));
        allAbilities.Add(new AbilityData("bullet_range", "사거리 증가",  "총알 사거리 +40%",         3));
    }

    void PresentAbilityChoice(int level)
    {
        List<AbilityData> available = allAbilities.Where(a => a.CanOffer).ToList();
        available = available.OrderBy(_ => Random.value).Take(3).ToList();

        if (available.Count == 0)
        {
            GameManager.Instance?.ResumeFromLevelUp();
            return;
        }
        OnAbilityChoiceReady?.Invoke(available);
    }

    public void ApplyAbility(AbilityData ability)
    {
        ability.currentLevel++;
        if (!acquiredAbilities.Contains(ability)) acquiredAbilities.Add(ability);

        if (stats == null) stats = PlayerStats.Instance;

        switch (ability.id)
        {
            case "damage_up":    stats.damageMultiplier    += 0.30f; break;
            case "firerate_up":  stats.fireRateMultiplier  += 0.25f; break;
            case "speed_up":     stats.speedMultiplier     += 0.20f; break;
            case "health_up":
                stats.maxHealth += 30f;
                stats.Heal(30f);
                break;
            case "heal":         stats.Heal(20f); break;
            case "triple_shot":  stats.hasTripleShot = true; break;
            case "split_shot":   stats.hasSplitShot  = true; break;
            case "pierce":       stats.pierceCount   += 1; break;
            case "magnet":       stats.magnetRadius  *= 1.5f; break;
            case "bullet_speed": stats.bulletSpeed   *= 1.30f; break;
            case "bullet_range": stats.bulletRange   *= 1.40f; break;
        }

        GameManager.Instance?.ResumeFromLevelUp();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp -= PresentAbilityChoice;
    }
}
