using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float turnSmoothTime = 0.1f;

    [Header("Gravity")]
    public float gravity = -9.81f;

    // TAMBAHAN: State untuk mengunci pergerakan saat cutscene berjalan
    [Header("State")]
    public bool canMove = true;

    private float velocityY;
    private CharacterController controller;
    private Transform camTransform;
    private float turnSmoothVelocity;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        // Ambil referensi kamera utama
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError(
                "Main Camera tidak ditemukan! Pastikan kamera memiliki tag 'MainCamera'."
            );
        }
    }

    // TAMBAHAN: Method ini akan dipanggil oleh SequenceManager nanti
    public void SetMovementState(bool state)
    {
        canMove = state;
    }

    private void Update()
    {
        // TAMBAHAN: Cek canMove sebelum mengeksekusi pergerakan WASD
        if (canMove)
        {
            MovePlayer();
        }

        // Gravitasi selalu dipanggil setiap frame, meskipun canMove = false
        ApplyGravity();
    }

    private void MovePlayer()
    {
        // =========================================
        // 1. INPUT WASD - INPUT SYSTEM BARU
        // =========================================

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
                horizontal = -1f;

            if (Keyboard.current.dKey.isPressed)
                horizontal = 1f;

            if (Keyboard.current.sKey.isPressed)
                vertical = -1f;

            if (Keyboard.current.wKey.isPressed)
                vertical = 1f;
        }

        // =========================================
        // 2. ARAH GERAK
        // =========================================

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f && camTransform != null)
        {
            // =========================================
            // 3. HITUNG ROTASI BERDASARKAN KAMERA
            // =========================================

            float targetAngle =
                Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg
                + camTransform.eulerAngles.y;

            // =========================================
            // 4. ROTASI PLAYER SMOOTH
            // =========================================

            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref turnSmoothVelocity,
                turnSmoothTime
            );

            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // =========================================
            // 5. GERAK PLAYER
            // =========================================

            Vector3 moveDir =
                Quaternion.Euler(0f, targetAngle, 0f)
                * Vector3.forward;

            controller.Move(
                moveDir.normalized
                * moveSpeed
                * Time.deltaTime
            );
        }
    }

    // TAMBAHAN: Pisahkan blok gravitasi ke method tersendiri agar lebih rapi
    private void ApplyGravity()
    {
        // =========================================
        // 6. GRAVITASI
        // =========================================

        if (controller.isGrounded && velocityY < 0)
        {
            velocityY = -2f;
        }

        velocityY += gravity * Time.deltaTime;

        controller.Move(
            new Vector3(0f, velocityY, 0f)
            * Time.deltaTime
        );
    }
}