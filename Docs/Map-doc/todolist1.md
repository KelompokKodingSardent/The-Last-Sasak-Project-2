## Phase 01 — World Design Specification v1.0 (Dikunci)

Seluruh prefab ruangan untuk **Level 01 — Hutan** dan solver penempatan spasial wajib mengikuti kontrak metrik berikut:

| Parameter | Standar Dikunci | Aturan Teknis |
| --- | --- | --- |
| **Base Grid Unit (`CellSize`)** | **12 × 12 Unity units** | Semua dimensi ruangan harus kelipatan bulat 12 unit (`12`, `24`, `36`, `48`). |
| **Room Root Pivot** | **Sudut Bottom-Left `(0, 0, 0)**` | Geometri ruangan berada di kuadran positif lokal: X `[0 .. Width*12]` dan Z `[0 .. Depth*12]`. |
| **Elevasi Lantai (`Floor Y`)** | **`Y = 0.0f`** | Permukaan lantai walkable dan seluruh pivot `RoomConnector` wajib berada di `Y = 0`. |
| **Posisi `RoomConnector**` | **Titik Tengah Sisi Luar Cell** | Berada tepat di tepi perimeter (`X = 0` / `W*12` atau `Z = 0` / `D*12`) pada koordinat tengah cell (`6`, `18`, `30`, dst.). |
| **Orientasi `RoomConnector**` | **Sumbu Z (`transform.forward`) Keluar** | Panah biru (`+Z`) wajib menghadap tegak lurus **keluar** ruangan agar dua ruangan menyatu saat `forwardA == -forwardB`. |
| **Dimensi Bukaan Pintu** | **Lebar ≥ 5.0u, Tinggi ≥ 4.0u** | Menjamin `NavMeshAgent` musuh bertubuh besar (`Bulwark` / `Elite`) tidak tersangkut di sudut doorway. |
| **Penutup Socket Kosong** | **Toggle `OpenFrame` vs `ClosedWall**` | Connector yang terhubung ke graph mengaktifkan `OpenFrame`; connector tanpa pasangan mengaktifkan `ClosedWall` beserta collider-nya. |

### Standar Ukuran Footprint Awal (Level 01 Hutan)

| Kategori Ruangan | Ukuran Cell | Dimensi Dunia | Peran Gameplay |
| --- | --- | --- | --- |
| **Corridor / Traversal** | `1 × 2` cell | `12 × 24` unit | Lorong transisi, penyergapan cepat, penghubung cabang. |
| **Starting / Reward / Exit** | `1 × 1` atau `2 × 1` cell | `12 × 12` / `24 × 12` unit | Titik aman awal run, ruang hadiah/shrine, atau gerbang akhir area. |
| **Open / Obstacle Combat** | `2 × 2` cell | `24 × 24` unit | Arena tempur utama dengan ruang manuver dodge/parry dan obstacle. |
| **Large Arena / Elite** | `3 × 3` cell | `36 × 36` unit | Pertarungan multi-wave atau `Elite` / `Boss` dengan jarak tembak luas. |

---

## Phase 02 — Room Module System (Scripts)

Ketiga script berikut menggunakan `Direction` yang sudah ada di proyek (`North`, `South`, `East`, `West`) dan dirancang agar kompatibel langsung dengan `RoomController`.

### 1. `RoomFootprint.cs`

Menyimpan ukuran cell ruangan (`12×12` unit per cell), menghitung koordinat cell yang terisi setelah rotasi `0°/90°/180°/270°`, dan menggambar grid visual di Scene View.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class RoomFootprint : MonoBehaviour
{
    public const float CellSize = 12f;

    [Header("Grid Dimensions (1 Cell = 12x12 Units)")]
    [Min(1)] public int WidthCells = 2;
    [Min(1)] public int DepthCells = 2;

    [Header("Gizmo Visualization")]
    public bool ShowGridGizmos = true;
    public Color FootprintColor = new Color(0.15f, 0.85f, 0.45f, 0.85f);

    public Vector2Int SizeInCells => new Vector2Int(Mathf.Max(1, WidthCells), Mathf.Max(1, DepthCells));
    public Vector2 WorldDimensions => new Vector2(WidthCells * CellSize, DepthCells * CellSize);

    /// <summary>
    /// Mengembalikan daftar koordinat grid (X, Z) dunia yang ditempati ruangan ini
    /// berdasarkan posisi root (kelipatan 12) dan rotasi kelipatan 90 derajat.
    /// </summary>
    public List<Vector2Int> GetOccupiedCells(Vector3 rootWorldPosition, Quaternion rootWorldRotation)
    {
        List<Vector2Int> cells = new List<Vector2Int>(WidthCells * DepthCells);

        for (int x = 0; x < WidthCells; x++)
        {
            for (int z = 0; z < DepthCells; z++)
            {
                Vector3 localCellCenter = new Vector3((x + 0.5f) * CellSize, 0f, (z + 0.5f) * CellSize);
                Vector3 worldCellCenter = rootWorldPosition + (rootWorldRotation * localCellCenter);
                cells.Add(WorldToGrid(worldCellCenter));
            }
        }

        return cells;
    }

    public List<Vector2Int> GetCurrentOccupiedCells()
    {
        return GetOccupiedCells(transform.position, transform.rotation);
    }

    public Bounds GetLocalBounds()
    {
        Vector3 size = new Vector3(WidthCells * CellSize, 6f, DepthCells * CellSize);
        Vector3 center = new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
        return new Bounds(center, size);
    }

    public bool IsLocalPointInside(Vector3 localPoint, float margin = 0.05f)
    {
        float maxX = WidthCells * CellSize;
        float maxZ = DepthCells * CellSize;
        return localPoint.x >= -margin && localPoint.x <= maxX + margin &&
               localPoint.z >= -margin && localPoint.z <= maxZ + margin;
    }

    public static Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / CellSize),
            Mathf.FloorToInt(worldPosition.z / CellSize)
        );
    }

    public static Vector3 GridToWorldOrigin(Vector2Int gridCell)
    {
        return new Vector3(gridCell.x * CellSize, 0f, gridCell.y * CellSize);
    }

    private void OnDrawGizmos()
    {
        if (!ShowGridGizmos)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        float totalW = WidthCells * CellSize;
        float totalD = DepthCells * CellSize;

        // Gambar garis pembatas setiap cell 12x12 pada Y = 0
        Gizmos.color = FootprintColor;
        for (int x = 0; x <= WidthCells; x++)
        {
            float posX = x * CellSize;
            Gizmos.DrawLine(new Vector3(posX, 0.02f, 0f), new Vector3(posX, 0.02f, totalD));
        }

        for (int z = 0; z <= DepthCells; z++)
        {
            float posZ = z * CellSize;
            Gizmos.DrawLine(new Vector3(0f, 0.02f, posZ), new Vector3(totalW, 0.02f, posZ));
        }

        // Gambar volume bounding box tipis
        Gizmos.color = new Color(FootprintColor.r, FootprintColor.g, FootprintColor.b, 0.25f);
        Bounds localBounds = GetLocalBounds();
        Gizmos.DrawWireCube(localBounds.center, localBounds.size);

        // Penanda pivot (0,0,0)
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(Vector3.zero, 0.4f);

        Gizmos.matrix = previousMatrix;
    }
}

```

---

### 2. `RoomConnector.cs`

Menggantikan sistem 8 field hardcoded di `RoomModule.cs`. Mengelola orientasi sambungan, toggling `OpenFrame` vs `ClosedWall`, penguncian `CombatGate` selama encounter, dan validasi visual posisi tepi grid di Scene View.

```csharp
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

```

---

### 3. `RoomPrefabValidator.cs`

Memvalidasi apakah sebuah room prefab mematuhi standar grid `12×12`, elevasi `Y = 0`, posisi midpoint connector, orientasi keluar, kelengkapan `OpenFrame`/`ClosedWall`, dan posisi `PlayerSpawn` maupun `EnemySpawnPoint`. Tersedia baik untuk **runtime (`MapValidator`)** maupun **menu Editor Unity**.

```csharp
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoomValidationReport
{
    public string RoomName;
    public readonly List<string> Errors = new List<string>();
    public readonly List<string> Warnings = new List<string>();

    public bool IsValid => Errors.Count == 0;
}

public static class RoomPrefabValidator
{
    private const float PositionTolerance = 0.05f;
    private const float MinDoorwayWidth = 4.0f;
    private const float MinSpawnClearanceFromDoor = 2.5f;

    public static RoomValidationReport Validate(GameObject roomObject)
    {
        RoomValidationReport report = new RoomValidationReport
        {
            RoomName = roomObject != null ? roomObject.name : "NullRoom"
        };

        if (roomObject == null)
        {
            report.Errors.Add("GameObject ruangan bernilai null.");
            return report;
        }

        // 1. Cek Komponen Wajib pada Root
        RoomController controller = roomObject.GetComponent<RoomController>();
        if (controller == null)
            report.Errors.Add("Root prefab tidak memiliki komponen RoomController.");

        RoomFootprint footprint = roomObject.GetComponent<RoomFootprint>();
        if (footprint == null)
        {
            report.Errors.Add("Root prefab tidak memiliki komponen RoomFootprint.");
            return report;
        }

        if (footprint.WidthCells < 1 || footprint.DepthCells < 1)
            report.Errors.Add($"Ukuran cell tidak valid: {footprint.WidthCells}x{footprint.DepthCells}. Minimal 1x1.");

        float maxX = footprint.WidthCells * RoomFootprint.CellSize;
        float maxZ = footprint.DepthCells * RoomFootprint.CellSize;

        // 2. Cek Floor Collider
        Collider[] colliders = roomObject.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
            report.Errors.Add("Tidak ditemukan Collider sama sekali (lantai/dinding belum memiliki collision).");

        // 3. Cek PlayerSpawn Anchor
        if (controller != null)
        {
            if (controller.PlayerSpawn == null)
            {
                report.Warnings.Add("PlayerSpawn belum di-assign pada RoomController.");
            }
            else
            {
                Vector3 localSpawn = roomObject.transform.InverseTransformPoint(controller.PlayerSpawn.position);
                if (!footprint.IsLocalPointInside(localSpawn))
                    report.Errors.Add($"PlayerSpawn berada di luar area footprint ({localSpawn}).");
                if (Mathf.Abs(localSpawn.y) > 0.5f)
                    report.Warnings.Add($"Elevasi PlayerSpawn (Y={localSpawn.y:F2}) menyimpang dari standar lantai Y=0.");
            }
        }

        // 4. Cek RoomConnectors
        RoomConnector[] connectors = roomObject.GetComponentsInChildren<RoomConnector>(true);
        if (connectors.Length == 0)
        {
            report.Errors.Add("Ruangan tidak memiliki RoomConnector sama sekali.");
        }
        else
        {
            HashSet<Vector2> uniqueConnectorCoords = new HashSet<Vector2>();

            for (int i = 0; i < connectors.Length; i++)
            {
                RoomConnector conn = connectors[i];
                Vector3 localPos = roomObject.transform.InverseTransformPoint(conn.transform.position);
                Vector3 localFwd = roomObject.transform.InverseTransformDirection(conn.transform.forward).normalized;

                // Cek Y = 0
                if (Mathf.Abs(localPos.y) > PositionTolerance)
                    report.Errors.Add($"Connector '{conn.name}' memiliki Y={localPos.y:F2} (wajib 0.0).");

                // Cek duplikasi posisi connector
                Vector2 roundedPos = new Vector2(Mathf.Round(localPos.x * 10f) / 10f, Mathf.Round(localPos.z * 10f) / 10f);
                if (!uniqueConnectorCoords.Add(roundedPos))
                    report.Errors.Add($"Connector '{conn.name}' bertumpuk pada koordinat yang sama {roundedPos}.");

                // Cek posisi berada tepat di tepi perimeter & midpoint cell (6, 18, 30...)
                bool onWestEdge = Mathf.Abs(localPos.x - 0f) <= PositionTolerance;
                bool onEastEdge = Mathf.Abs(localPos.x - maxX) <= PositionTolerance;
                bool onSouthEdge = Mathf.Abs(localPos.z - 0f) <= PositionTolerance;
                bool onNorthEdge = Mathf.Abs(localPos.z - maxZ) <= PositionTolerance;

                if (!onWestEdge && !onEastEdge && !onSouthEdge && !onNorthEdge)
                {
                    report.Errors.Add($"Connector '{conn.name}' ({localPos.x:F2}, {localPos.z:F2}) tidak berada di tepi luar footprint ({maxX}x{maxZ}).");
                }
                else
                {
                    if (onWestEdge || onEastEdge)
                    {
                        if (!IsCellMidpoint(localPos.z, footprint.DepthCells))
                            report.Errors.Add($"Connector '{conn.name}' Z={localPos.z:F2} tidak berada di titik tengah cell (6, 18, 30...).");

                        Vector3 expectedFwd = onEastEdge ? Vector3.right : Vector3.left;
                        if (Vector3.Dot(localFwd, expectedFwd) < 0.98f)
                            report.Errors.Add($"Connector '{conn.name}' arah forward ({localFwd}) harus menghadap keluar ({expectedFwd}).");
                    }

                    if (onSouthEdge || onNorthEdge)
                    {
                        if (!IsCellMidpoint(localPos.x, footprint.WidthCells))
                            report.Errors.Add($"Connector '{conn.name}' X={localPos.x:F2} tidak berada di titik tengah cell (6, 18, 30...).");

                        Vector3 expectedFwd = onNorthEdge ? Vector3.forward : Vector3.back;
                        if (Vector3.Dot(localFwd, expectedFwd) < 0.98f)
                            report.Errors.Add($"Connector '{conn.name}' arah forward ({localFwd}) harus menghadap keluar ({expectedFwd}).");
                    }
                }

                // Cek dimensi bukaan & kelengkapan OpenFrame/ClosedWall
                if (conn.OpeningWidth < MinDoorwayWidth)
                    report.Warnings.Add($"Connector '{conn.name}' memiliki OpeningWidth {conn.OpeningWidth}u (disarankan >= 5u).");

                if (conn.OpenFrame == null)
                    report.Errors.Add($"Connector '{conn.name}' belum mengisi referensi OpenFrame.");

                if (conn.ClosedWall == null)
                    report.Errors.Add($"Connector '{conn.name}' belum mengisi referensi ClosedWall.");

                if (conn.OpenFrame != null && conn.OpenFrame == conn.ClosedWall)
                    report.Errors.Add($"Connector '{conn.name}' menggunakan GameObject yang sama untuk OpenFrame dan ClosedWall.");
            }
        }

        // 5. Cek EnemySpawnPoints
        EnemySpawnPoint[] spawnPoints = roomObject.GetComponentsInChildren<EnemySpawnPoint>(true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            EnemySpawnPoint sp = spawnPoints[i];
            Vector3 localSp = roomObject.transform.InverseTransformPoint(sp.transform.position);

            if (!footprint.IsLocalPointInside(localSp, -0.5f))
                report.Errors.Add($"EnemySpawnPoint '{sp.name}' berada di luar batas ruangan.");

            for (int c = 0; c < connectors.Length; c++)
            {
                float distToDoor = Vector3.Distance(sp.transform.position, connectors[c].transform.position);
                if (distToDoor < MinSpawnClearanceFromDoor)
                    report.Warnings.Add($"EnemySpawnPoint '{sp.name}' terlalu dekat ({distToDoor:F1}u) dengan connector '{connectors[c].name}'.");
            }
        }

        return report;
    }

    private static bool IsCellMidpoint(float coord, int cellCount)
    {
        for (int i = 0; i < cellCount; i++)
        {
            float expectedMidpoint = (i + 0.5f) * RoomFootprint.CellSize;
            if (Mathf.Abs(coord - expectedMidpoint) <= PositionTolerance)
                return true;
        }
        return false;
    }

#if UNITY_EDITOR
    [MenuItem("TLS/World/Validate Selected Room Prefabs")]
    private static void ValidateSelectedPrefabs()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[RoomPrefabValidator] Pilih minimal satu Room Prefab atau GameObject di Project/Hierarchy.");
            return;
        }

        for (int i = 0; i < selected.Length; i++)
        {
            RoomValidationReport report = Validate(selected[i]);
            if (report.IsValid)
            {
                Debug.Log($"<color=#4CAF50>[PASS]</color> <b>{report.RoomName}</b> lolos validasi modular ({report.Warnings.Count} warning).");
            }
            else
            {
                Debug.LogError($"<color=#F44336>[FAIL]</color> <b>{report.RoomName}</b> gagal validasi dengan {report.Errors.Count} error:");
                foreach (string err in report.Errors)
                    Debug.LogError($"   • {err}", selected[i]);
            }

            foreach (string warn in report.Warnings)
                Debug.LogWarning($"   ⚠ [{report.RoomName}] {warn}", selected[i]);
        }
    }
#endif
}

```

---

## Panduan Membuat `Room_Template.prefab` (2 × 2 Cell = 24 × 24 Unit)

1. Buat Empty GameObject bernama `Room_Template` di posisi `(0, 0, 0)`, lalu tempelkan **`RoomController`** dan **`RoomFootprint`** (`WidthCells = 2`, `DepthCells = 2`).
2. Buat hierarki child standar di bawah `Room_Template`:
* **`Geometry`**: Lantai ukuran `24 × 24` dengan pusat mesh digeser ke `(12, 0, 12)` agar sudut kirinya tepat di `(0, 0, 0)` (titik biru Gizmo).
* **`Connectors`**:
* `Connector_South` di posisi `(6, 0, 0)` atau `(18, 0, 0)`, rotasi Y = `180°` (panah biru menghadap `-Z`).
* `Connector_North` di posisi `(6, 0, 24)` atau `(18, 0, 24)`, rotasi Y = `0°` (panah biru menghadap `+Z`).
* `Connector_West` di posisi `(0, 0, 6)` atau `(0, 0, 18)`, rotasi Y = `270°` (panah biru menghadap `-X`).
* `Connector_East` di posisi `(24, 0, 6)` atau `(24, 0, 18)`, rotasi Y = `90°` (panah biru menghadap `+X`).


* Di dalam setiap `Connector_*`, tambahkan komponen **`RoomConnector`**, lalu buat dua child: `OpenFrame` (bingkai pintu terbuka) dan `ClosedWall` (tembok + BoxCollider penutup), kemudian hubungkan keduanya di Inspector.
* **`Anchors`**: Buat `PlayerSpawn` di `(12, 0, 12)` dan hubungkan ke field `PlayerSpawn` pada `RoomController`.
* **`SpawnPoints`**: Tambahkan `EnemySpawnPoint` dengan jarak minimal `3` unit dari setiap connector, lalu masukkan ke array `SpawnPoints` pada `RoomController`.


3. Simpan sebagai prefab dan klik menu atas Unity **`TLS > World > Validate Selected Room Prefabs`** untuk memastikan status **`[PASS]`** sebelum digandakan menjadi greybox room di Phase 04.