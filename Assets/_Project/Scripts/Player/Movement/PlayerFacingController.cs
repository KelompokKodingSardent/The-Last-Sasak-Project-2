using UnityEngine;

public class PlayerFacingController : MonoBehaviour
{
    public PlayerMovementConfig Config;
    PlayerInputReader input;
    Transform cam;
    float turnVelocity;

    void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        CacheCamera();
    }

    void CacheCamera()
    {
        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;
    }

    public Vector3 GetCameraRelativeMove(Vector2 inputMove)
    {
        CacheCamera();
        Vector3 camForward = cam != null ? cam.forward : Vector3.forward;
        Vector3 camRight = cam != null ? cam.right : Vector3.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        Vector3 dir = camForward * inputMove.y + camRight * inputMove.x;
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }

    void Update()
    {
        if (input == null || input.Move.sqrMagnitude < 0.01f)
            return;

        Vector3 dir = GetCameraRelativeMove(input.Move);
        if (dir.sqrMagnitude < 0.01f)
            return;

        float target = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float smooth = Config != null ? Config.TurnSmoothTime : 0.1f;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, target, ref turnVelocity, smooth);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }
}
