using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomExit : MonoBehaviour
{
    [Header("Activation Rules")]
    [Tooltip("Jika true, Player harus menekan tombol interaksi di dalam zona Exit.")]
    public bool RequireInteractInput = false;
    public KeyCode InteractKey = KeyCode.E;

    [Tooltip("Jika true, semua ruangan Combat di jalur utama harus Cleared sebelum Exit terbuka.")]
    public bool RequireAllMainPathCleared = true;

    [Header("Visual State Feedback (Optional)")]
    public GameObject LockedVisual;
    public GameObject UnlockedVisual;

    public bool IsPlayerInRange { get; private set; }
    public bool IsExitUnlocked { get; private set; }

    private RoomController ownerRoom;
    private bool triggered;

    private void Awake()
    {
        ownerRoom = GetComponentInParent<RoomController>();

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnEnable()
    {
        triggered = false;
        IsPlayerInRange = false;
        GameEvents.OnRoomInstanceCleared += HandleAnyRoomCleared;
        GameEvents.OnRoomEntered += HandleAnyRoomEntered;
        RefreshExitAvailability();
    }

    private void OnDisable()
    {
        GameEvents.OnRoomInstanceCleared -= HandleAnyRoomCleared;
        GameEvents.OnRoomEntered -= HandleAnyRoomEntered;
    }

    private void Update()
    {
        if (triggered || !IsPlayerInRange || !RequireInteractInput)
            return;

        if (!IsExitUnlocked)
        {
            RefreshExitAvailability();
            if (!IsExitUnlocked)
                return;
        }

        if (Input.GetKeyDown(InteractKey))
            ActivateExit();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || other == null || !other.CompareTag("Player"))
            return;

        IsPlayerInRange = true;
        RefreshExitAvailability();

        if (!RequireInteractInput && IsExitUnlocked)
            ActivateExit();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null || !other.CompareTag("Player"))
            return;

        IsPlayerInRange = false;
    }

    private void HandleAnyRoomCleared(RoomController _)
    {
        RefreshExitAvailability();
    }

    private void HandleAnyRoomEntered(RoomController _)
    {
        RefreshExitAvailability();
    }

    public void RefreshExitAvailability()
    {
        bool roomReady = ownerRoom == null || ownerRoom.Cleared;
        bool mainPathReady = !RequireAllMainPathCleared ||
                             RoomManager.Instance == null ||
                             RoomManager.Instance.AreAllMainPathCombatRoomsCleared();

        IsExitUnlocked = roomReady && mainPathReady;

        if (LockedVisual != null)
            LockedVisual.SetActive(!IsExitUnlocked);

        if (UnlockedVisual != null)
            UnlockedVisual.SetActive(IsExitUnlocked);
    }

    public void ActivateExit()
    {
        if (triggered || !IsExitUnlocked)
            return;

        triggered = true;

        if (RoomManager.Instance != null)
            RoomManager.Instance.AdvanceToNextDepth();
    }
}