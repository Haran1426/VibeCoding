using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float height = 20f;
    public float smoothSpeed = 8f;
    public float lookAheadFactor = 2f; // 마우스 방향으로 살짝 앞을 봄

    private Camera cam;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, height, target.position.z);

        // 마우스 방향으로 카메라 살짝 오프셋
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, target.position);
        if (groundPlane.Raycast(ray, out float dist))
        {
            Vector3 mouseWorld = ray.GetPoint(dist);
            Vector3 offset = (mouseWorld - target.position);
            offset.y = 0f;
            offset = Vector3.ClampMagnitude(offset, 5f);
            desired.x += offset.x * 0.2f;
            desired.z += offset.z * 0.2f;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 1f / smoothSpeed);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
