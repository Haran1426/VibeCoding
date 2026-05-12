using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArenaSpinnerHazard : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 72f;
    [SerializeField] private float knockbackPower = 12f;
    [SerializeField] private float hitCooldown = 0.7f;

    private readonly Dictionary<KnockbackReceiver, float> _lastHitTime = new Dictionary<KnockbackReceiver, float>();

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider other)
    {
        if (!ShouldAffect(other)) return;

        var receiver = other.GetComponentInParent<KnockbackReceiver>();
        if (receiver == null) return;

        if (_lastHitTime.TryGetValue(receiver, out float lastTime) && Time.time - lastTime < hitCooldown)
            return;

        _lastHitTime[receiver] = Time.time;

        Vector3 dir = receiver.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f)
            dir = transform.right;

        receiver.ApplyKnockback(dir.normalized, knockbackPower, -1);
        VFXManager.Instance?.PlayAttack(receiver.transform.position + Vector3.up * 0.8f);
    }

    private static bool ShouldAffect(Collider other)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return true;

        var netSync = other.GetComponentInParent<PlayerNetworkSync>();
        return netSync == null || netSync.IsOwner;
    }
}
