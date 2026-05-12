using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArenaJumpPad : MonoBehaviour
{
    [SerializeField] private float launchVelocity = 16f;
    [SerializeField] private float cooldown = 0.45f;

    private readonly Dictionary<Rigidbody, float> _lastLaunchTime = new Dictionary<Rigidbody, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (!ShouldAffect(other)) return;

        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        if (_lastLaunchTime.TryGetValue(rb, out float lastTime) && Time.time - lastTime < cooldown)
            return;

        _lastLaunchTime[rb] = Time.time;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = Mathf.Max(velocity.y, launchVelocity);
        rb.linearVelocity = velocity;

        other.GetComponentInParent<PlayerController>()?.NotifyKnockedBack();
        VFXManager.Instance?.PlayLevelUp(transform.position + Vector3.up * 0.35f);
        AudioManager.Instance?.PlayRespawn();
    }

    private static bool ShouldAffect(Collider other)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return true;

        var netSync = other.GetComponentInParent<PlayerNetworkSync>();
        return netSync == null || netSync.IsOwner;
    }
}
