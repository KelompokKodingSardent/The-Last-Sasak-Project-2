using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float turnSmoothTime = 0.1f;
    
    [Header("Gravity")]
    public float gravity = -9.81f;
    private float velocityY;

    private CharacterController controller;
    private Transform camTransform;
    private float turnSmoothVelocity;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Mengambil referensi rotasi kamera utama agar arah gerak sinkron dengan layar
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Main Camera tidak ditemukan! Pastikan kamera memiliki tag 'MainCamera'.");
        }
    }

    private void Update()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        // 1. Ambil input dari WASD atau Analog (Nilai -1 hingga 1)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // Vektor arah input murni
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // 2. Kalkulasi sudut putar karakter berdasarkan input + rotasi kamera
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            
            // 3. Efek rotasi yang mulus (smooth) pada model karakter
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // 4. Ubah sudut putar tadi menjadi vektor arah gerak (maju)
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            
            // Gerakkan karakter
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
        }

        // 5. Terapkan Gravitasi (agar karakter tidak melayang saat melewati turunan/tangga)
        if (controller.isGrounded && velocityY < 0)
        {
            velocityY = -2f; // Nilai kecil agar tetap menempel di tanah
        }

        velocityY += gravity * Time.deltaTime;
        controller.Move(new Vector3(0, velocityY, 0) * Time.deltaTime);
    }
}