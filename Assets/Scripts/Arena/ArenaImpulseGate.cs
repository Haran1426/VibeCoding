using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArenaImpulseGate : MonoBehaviour
{
    [SerializeField] private Vector3 launchDirection = Vector3.forward;
    [SerializeField] private float horizontalVelocity = 18f;
    [SerializeField] private float liftVelocity = 4f;
    [SerializeField] private float cooldown = 0.55f;

    private readonly Dictionary<Rigidbody, float> _lastLaunchTime = new Dictionary<Rigidbody, float>();

    public void Configure(Vector3 direction, float horizontal, float lift)
    {
        launchDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        horizontalVelocity = horizontal;
        liftVelocity = lift;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryLaunch(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryLaunch(other);
    }

    private void TryLaunch(Collider other)
    {
        if (!ShouldAffect(other)) return;

        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        if (_lastLaunchTime.TryGetValue(rb, out float lastTime) && Time.time - lastTime < cooldown)
            return;

        _lastLaunchTime[rb] = Time.time;

        Vector3 horizontal = launchDirection;
        horizontal.y = 0f;
        if (horizontal.sqrMagnitude < 0.001f)
            horizontal = transform.forward;

        Vector3 velocity = horizontal.normalized * horizontalVelocity;
        velocity.y = Mathf.Max(rb.linearVelocity.y, liftVelocity);
        rb.linearVelocity = velocity;

        other.GetComponentInParent<PlayerController>()?.NotifyKnockedBack();
        VFXManager.Instance?.PlayLevelUp(transform.position + Vector3.up * 0.5f);
        AudioManager.Instance?.PlayDash();
    }

    private static bool ShouldAffect(Collider other)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return true;

        var netSync = other.GetComponentInParent<PlayerNetworkSync>();
        return netSync == null || netSync.IsOwner;
    }
}
