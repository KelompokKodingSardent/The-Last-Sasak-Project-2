using UnityEngine;

public class PlayerFacingController : MonoBehaviour
{
    public PlayerMovementConfig Config;

    PlayerInputReader input;
    PlayerActionController actions;
    Camera mainCam;
    float turnVelocity;
    bool rotationLocked;
    Vector3 lastValidAimDir;

    public Vector3 LastValidAimDirection => lastValidAimDir.sqrMagnitude > 0.01f ? lastValidAimDir : transform.forward;

    void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        actions = GetComponent<PlayerActionController>();
        lastValidAimDir = transform.forward;
        CacheCamera();
    }

    void CacheCamera()
    {
        if (mainCam == null)
            mainCam = Camera.main;
    }

    public void SetRotationLocked(bool locked)
    {
        rotationLocked = locked;
    }

    public Vector3 GetCameraRelativeMove(Vector2 inputMove)
    {
        CacheCamera();
        Transform camTransform = mainCam != null ? mainCam.transform : null;
        Vector3 camForward = camTransform != null ? camTransform.forward : Vector3.forward;
        Vector3 camRight = camTransform != null ? camTransform.right : Vector3.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        Vector3 dir = camForward * inputMove.y + camRight * inputMove.x;
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }

    public Vector3 GetAimDirection()
    {
        if (input == null)
            return LastValidAimDirection;

        if (input.UsingGamepadAim)
        {
            if (input.AimStick.sqrMagnitude > 0.04f)
            {
                Vector3 stickDir = GetCameraRelativeMove(input.AimStick).normalized;
                if (stickDir.sqrMagnitude > 0.01f)
                {
                    lastValidAimDir = stickDir;
                    return stickDir;
                }
            }
        }
        else
        {
            CacheCamera();
            if (mainCam != null)
            {
                Ray ray = mainCam.ScreenPointToRay(input.AimScreenPosition);
                Plane groundPlane = new Plane(Vector3.up, transform.position + Vector3.up * 0.8f);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    Vector3 toCursor = hitPoint - transform.position;
                    toCursor.y = 0f;
                    if (toCursor.sqrMagnitude > 0.04f)
                    {
                        lastValidAimDir = toCursor.normalized;
                        return lastValidAimDir;
                    }
                }
            }
        }

        if (input.Move.sqrMagnitude > 0.01f)
        {
            Vector3 moveDir = GetCameraRelativeMove(input.Move).normalized;
            if (moveDir.sqrMagnitude > 0.01f)
            {
                lastValidAimDir = moveDir;
                return moveDir;
            }
        }

        return LastValidAimDirection;
    }

    public void SnapToAimDirection()
    {
        Vector3 aimDir = GetAimDirection();
        if (aimDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(aimDir, Vector3.up);
        }
    }

    public void FaceDirection(Vector3 worldDir, bool instant = false)
    {
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 0.01f)
            return;

        worldDir.Normalize();
        lastValidAimDir = worldDir;

        if (instant)
        {
            transform.rotation = Quaternion.LookRotation(worldDir, Vector3.up);
            return;
        }

        float target = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
        float smooth = Config != null ? Config.TurnSmoothTime : 0.08f;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, target, ref turnVelocity, smooth);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    void Update()
    {
        if (rotationLocked || input == null)
            return;

        if (actions != null && actions.State == PlayerActionState.Dead)
            return;

        if (input.Move.sqrMagnitude < 0.01f)
            return;

        Vector3 dir = GetCameraRelativeMove(input.Move);
        FaceDirection(dir, instant: false);
    }
}