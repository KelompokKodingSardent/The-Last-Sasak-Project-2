using UnityEngine;

public enum ConnectorSocketType
{
    Standard,
    BossGate,
    RewardDoor
}

public class RoomConnector : MonoBehaviour
{
    [Header("Connector Metadata")]
    public ConnectorSocketType SocketType = ConnectorSocketType.Standard;
    public float OpeningWidth = 5f;
    public float OpeningHeight = 4f;

    [Header("Socket Visual & Collision Toggle")]
    [Tooltip("Aktif saat connector tersambung ke ruangan lain di graph.")]
    public GameObject OpenFrame;

    [Tooltip("Aktif saat connector tidak dipakai (menutup lubang ke void).")]
    public GameObject ClosedWall;

    [Tooltip("Opsional: Gerbang yang terkunci saat encounter ruangan aktif.")]
    public GameObject CombatGate;

    public bool IsConnected { get; private set; }
    public RoomConnector ConnectedPartner { get; private set; }
    public RoomController OwnerRoom { get; private set; }

    private void Awake()
    {
        OwnerRoom = GetComponentInParent<RoomController>();
    }

    /// <summary>
    /// Menghitung arah mata angin dunia berdasarkan arah transform.forward saat ini
    /// (otomatis memperhitungkan rotasi ruangan 0/90/180/270 derajat).
    /// </summary>
    public Direction GetWorldDirection()
    {
        Vector3 fwd = transform.forward;
        if (Mathf.Abs(fwd.x) > Mathf.Abs(fwd.z))
            return fwd.x >= 0f ? Direction.East : Direction.West;

        return fwd.z >= 0f ? Direction.North : Direction.South;
    }

    public bool IsCompatibleWith(RoomConnector other)
    {
        if (other == null || other == this)
            return false;

        if (SocketType != other.SocketType)
            return false;

        // Arah forward harus saling berhadapan (dot product mendekati -1)
        float facingDot = Vector3.Dot(transform.forward.normalized, other.transform.forward.normalized);
        return facingDot <= -0.95f;
    }

    public void SetConnected(RoomConnector partner)
    {
        IsConnected = partner != null;
        ConnectedPartner = partner;
        ApplySocketVisuals();
    }

    public void ResetConnection()
    {
        IsConnected = false;
        ConnectedPartner = null;
        ApplySocketVisuals();
        SetCombatGateLocked(false);
    }

    public void ApplySocketVisuals()
    {
        if (OpenFrame != null)
            OpenFrame.SetActive(IsConnected);

        if (ClosedWall != null)
            ClosedWall.SetActive(!IsConnected);
    }

    public void SetCombatGateLocked(bool locked)
    {
        if (CombatGate != null)
            CombatGate.SetActive(IsConnected && locked);
    }

    private void OnDrawGizmos()
    {
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward.normalized;
        Vector3 right = transform.right.normalized;

        // Warna hijau jika aktif/valid, kuning jika belum tersambung di scene
        Gizmos.color = IsConnected ? Color.green : new Color(1f, 0.75f, 0.1f, 0.95f);

        // Gambar bingkai bukaan doorway (Width x Height)
        float halfW = OpeningWidth * 0.5f;
        Vector3 bl = pos - right * halfW;
        Vector3 br = pos + right * halfW;
        Vector3 tl = bl + Vector3.up * OpeningHeight;
        Vector3 tr = br + Vector3.up * OpeningHeight;

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);

        // Gambar panah arah keluar (+Z)
        Vector3 arrowStart = pos + Vector3.up * 0.5f;
        Vector3 arrowEnd = arrowStart + fwd * 2.5f;
        Gizmos.DrawLine(arrowStart, arrowEnd);
        Gizmos.DrawLine(arrowEnd, arrowEnd - fwd * 0.6f + right * 0.4f);
        Gizmos.DrawLine(arrowEnd, arrowEnd - fwd * 0.6f - right * 0.4f);
    }
}