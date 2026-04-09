using UnityEngine;

public class ExpOrb : MonoBehaviour
{
    private int expValue;
    private float moveSpeed = 8f;
    private float pickupRadius = 1.5f;
    private Transform player;
    private bool attracted = false;
    private float lifetime = 10f;

    public static void SpawnAt(Vector3 pos, int exp)
    {
        // 오브 프리팹이 없으면 동적으로 생성
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "ExpOrb";
        orb.transform.position = pos + Vector3.up * 0.3f;
        orb.transform.localScale = Vector3.one * 0.3f;

        // 초록색 네온 느낌
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0f, 1f, 0.4f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0f, 2f, 0.8f));
        orb.GetComponent<Renderer>().material = mat;

        // 콜라이더 트리거로
        SphereCollider sc = orb.GetComponent<SphereCollider>();
        sc.isTrigger = true;

        ExpOrb component = orb.AddComponent<ExpOrb>();
        component.expValue = exp;

        Rigidbody rb = orb.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) { Destroy(gameObject); return; }

        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        PlayerStats ps = player.GetComponent<PlayerStats>();
        float magnetR = ps != null ? ps.magnetRadius : 3f;

        if (dist <= magnetR) attracted = true;

        if (attracted)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
        }

        // 약간 떠다니는 효과
        transform.position += Vector3.up * Mathf.Sin(Time.time * 3f + transform.position.x) * 0.002f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance?.AddExp(expValue);
            Destroy(gameObject);
        }
    }
}
