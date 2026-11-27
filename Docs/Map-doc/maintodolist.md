## 1. Art Pass Lingkungan Hutan (Menjaga Kontrak Spasial)

Penggantian blok *greybox* menjadi aset lingkungan hutan (reruntuhan batu lumut, pohon beringin/tropis, pagar bambu, dan gapura khas Sasak) wajib mematuhi batas fisik agar tidak merusak penyambungan antar-ruangan:

* **Batas Perimeter Ketat:** Pastikan seluruh mesh dan collider prop berada di dalam area lokal `X [0 .. Width×12]` dan `Z [0 .. Depth×12]`. Hindari dahan pohon, akar, atau tebing yang menjorok keluar dari dinding perimeter karena akan menembus masuk ke ruangan tetangga saat dua ruangan ditempatkan bersebelahan.
* **Elevasi Lantai & Kontak Kaki:** Pertahankan permukaan lantai walkable pada `Y = 0.0f`. Gunakan variasi visual melalui *vertex color*, *terrain decal*, atau rumput tipis tanpa collider (height kurang dari 0.3 unit) agar `CharacterController` dan `NavMeshAgent` tidak tersangkut.
* **Keterbacaan Siluet & Kamera:** Pada sisi dinding yang menghadap kamera (umumnya sisi Selatan/Barat pada kamera isometrik/top-down), gunakan tebing rendah atau vegetasi transparan agar karakter pemain dan *telegraph* serangan musuh tidak tertutup objek depan.
* **Penutupan Celah Cahaya (`Light Leak`):** Pastikan mesh `ClosedWall` pada setiap `RoomConnector` memiliki ketebalan minimal `1.0` unit dan menutupi seluruh tinggi dinding (`≥ 4.5` unit) sehingga cahaya *Directional Light* tidak bocor dari sambungan pintu yang tertutup.
* **Standarisasi Material:** Gunakan maksimal 3–4 shared material berbasis *Texture Atlas* untuk seluruh prefab ruangan (misalnya `M_ForestGround`, `M_SasakStone`, `M_WoodBamboo`, `M_Foliage`) untuk memaksimalkan batching.

---

## 2. Pengaturan Static Batching & SRP Batcher (Dunia Prosedural)

Pada Unity 6.3 LTS, mencentang flag **Batching Static** di Inspector **tidak otomatis menggabungkan mesh** yang di-instantiate saat runtime lewat `Object.Instantiate`. Ikuti pemisahan hierarki dan pemanggilan API berikut:

1. **Pisahkan Hierarki Statis vs Dinamis di Setiap Prefab:**
* **`StaticGeometry` (Child):** Berisi lantai, dinding permanen, pilar batu, dan tebing yang tidak pernah bergerak atau dimatikan. Aktifkan *Batching Static* (atau *Contribute GI* jika memakai Light Probes).
* **`DynamicElements` (Child):** Berisi `RoomConnector` (`OpenFrame`, `ClosedWall`, `CombatGate`), `RoomExit`, obor/partikel, dan vegetasi bergoyang (*wind shader*). **Jangan** masukkan objek yang menggunakan `SetActive()` ke dalam batch statis.


2. **Aktifkan SRP Batcher & GPU Instancing:**
* Pastikan **SRP Batcher** aktif pada aset *Universal Render Pipeline (URP)*.
* Centang **Enable GPU Instancing** pada material prop yang berulang dalam jumlah banyak (batu kecil, semak, rumput, pagar bambu).


3. **Panggil `StaticBatchingUtility.Combine` Setelah Assembly:**
Tambahkan satu baris penggabungan batch statis pada `WorldGenerationCoordinator.cs` tepat setelah `prefabAssembler.TryAssemble(...)` berhasil dan sebelum `mapValidator.ValidateAndBake(...)`:

```csharp
// Di dalam WorldGenerationCoordinator.cs setelah TryAssemble() sukses:
StaticBatchingUtility.Combine(MapContainer.gameObject);

```

> **Catatan:** Pastikan seluruh mesh prop statis tidak mengaktifkan *Read/Write Enabled* kecuali diperlukan, serta tidak memiliki skala negatif (`-1` pada X/Y/Z) karena skala negatif memecah batching dan membalik normal collider.

---

## 3. Audit NavMesh & NavMeshObstacle Carving

Karena `RuntimeNavMeshBuilder` melakukan baking `NavMeshSurface` **setelah** seluruh ruangan dirakit, sebagian besar rintangan lingkungan tidak memerlukan *runtime carving*:

* **Prop Statis = Tanpa `NavMeshObstacle`:** Hapus komponen `NavMeshObstacle` dari pohon, pilar, dan batu permanen. Selama objek tersebut memiliki `Collider` (atau `NavMeshModifier`) dan berada di layer yang dibaca `NavMeshSurface`, sistem otomatis memotong area NavMesh saat `BakeNavMeshRuntime()` dijalankan dengan **0 ms beban CPU** selama gameplay.
* **Audit `CombatGate` (Gerbang Pertempuran):**
* Karena musuh sudah di-spawn di dalam ruangan dan selalu mengejar pemain di dalam ruangan yang sama, `CombatGate` cukup menggunakan `BoxCollider` fisik untuk menahan pemain agar tidak keluar arena.
* Jika tetap memakai `NavMeshObstacle` pada `CombatGate`, set **`Carving = false`** (atau jika harus `Carving = true`, aktifkan **`Carve Only Stationary = true`** dengan `Time To Stationary = 0.25f`) agar tidak memicu *re-carve* berulang kali saat gerbang muncul/hilang.


* **Sinkronisasi `Agent Radius`:** Pastikan nilai *Agent Radius* pada `NavMeshSurface` sesuai dengan radius `NavMeshAgent` musuh terbesar (`Bulwark` / `Elite`, biasanya `0.6`–`0.75` unit) dan lebar bersih pintu `RoomConnector` minimal `5.0` unit agar musuh besar tidak macet di sudut gapura.
* **Bersihkan Data Lama Saat Regenerasi:** Pastikan `NavMeshBuilder.ClearNavMesh()` selalu dipanggil sebelum `ClearWorld()` menghancurkan ruangan lama (sudah ditangani otomatis di `WorldGenerationCoordinator.ClearWorld()`).

---

## 4. Checklist Performa & Profiling Akhir (Unity 6.3 LTS)

Gunakan daftar verifikasi berikut melalui **Unity Profiler (`Ctrl+7`)** dan **Frame Debugger** pada scene `WorldGenerationTest` sebelum rilis build:

| Kategori | Target Teknis | Cara Verifikasi & Tindakan Koreksi |
| --- | --- | --- |
| **Generasi & Baking** | Total waktu `GenerateWorld()` di bawah **150 ms** | Ukur durasi generasi dengan `WorldDebugHUD`. Jika baking NavMesh lambat, set *Collect Objects* pada `NavMeshSurface` ke **Children** (hanya memindai anak dari `MapContainer`) dan gunakan *Voxel Size* `0.15`–`0.20`. |
| **Rendering & Draw Calls** | **SetPass Calls di bawah 80**, Batches di bawah 250 | Buka *Frame Debugger*; pastikan lantai dan dinding dari ruangan berbeda tergabung dalam *SRP Batch* atau *Static Batch* yang sama. |
| **Lighting & Shadows** | Maksimal **1 Shadow-Casting Light** (Directional) | Matikan *Cast Shadows* pada prop rumput kecil dan gunakan satu *Directional Light* dengan jarak bayangan (*Shadow Distance*) `35`–`45` unit sesuai sudut kamera. |
| **Memory & GC Alloc** | **0 B GC Alloc per frame** saat combat berlangsung | Pastikan seluruh musuh dan proyektil di-recycle lewat `ObjectPoolManager` dan `EnemyPool` tanpa `Instantiate`/`Destroy` di tengah pertempuran. |
| **Stress Test Deterministik** | **100 dari 100 seed berturut-turut** lolos `MapValidator` | Jalankan pengujian acak 100 seed melalui `WorldDebugHUD` atau editor test untuk memastikan tidak ada kombinasi layout yang mentok atau gagal menyambungkan `Start` ke `Exit`. |

Merakit keenam prefab greybox akan jauh lebih cepat jika kamu membuat **1 sub-prefab `Connector_Standard.prefab**` terlebih dahulu, lalu menempelkannya ke masing-masing ruangan sesuai koordinat tabel di bawah.

## 1. Struktur Dasar Prefab & `Connector_Standard`

### A. Buat `Connector_Standard.prefab` (Dipakai di Semua Ruangan)

1. Buat Empty GameObject bernama `Connector_Standard` di `(0, 0, 0)` dan pasang komponen **`RoomConnector`** (`OpeningWidth = 5`, `OpeningHeight = 4`).
2. Tambahkan tiga child di bawahnya:
* **`OpenFrame`**: Dua pilar kusen kiri-kanan di lokal `X = -2.75` dan `X = +2.75` (skala `0.5, 4.5, 1.0`) ditambah balok atas di `Y = 4.25` (skala `6.0, 0.5, 1.0`).
* **`ClosedWall`**: Cube penutup lubang di lokal `(0, 2.25, 0)` dengan skala `(6.0, 4.5, 1.0)` beserta `BoxCollider` aktif.
* **`CombatGate`**: Cube gerbang berjeruji di lokal `(0, 2.0, 0)` dengan skala `(5.2, 4.0, 0.4)` beserta `BoxCollider` aktif (set **nonaktif** secara default di prefab).


3. Hubungkan `OpenFrame`, `ClosedWall`, dan `CombatGate` ke field yang sesuai di Inspector `RoomConnector`.

### B. Rumus Lantai & Dinding Perimeter Ruangan

Untuk setiap ruangan berukuran `W × D` cell (`1 cell = 12 unit`):

* **Root GameObject:** Posisi `(0, 0, 0)`, rotasi `(0, 0, 0)`. Pasang **`RoomController`** dan **`RoomFootprint`** (`WidthCells = W`, `DepthCells = D`).
* **Child `StaticGeometry`:**
* **Floor Cube:** Posisi `(W * 6, -0.25, D * 6)`, Skala `(W * 12, 0.5, D * 12)` agar permukaan atas lantai tepat di `Y = 0`.
* **Perimeter Walls:** Bangun dinding keliling setinggi `4.5` unit, lalu sisakan celah selebar **`6.0` unit** tepat di koordinat titik tengah cell (`6` atau `18`) tempat `Connector_Standard` dipasang.


* **Child `DynamicElements`:** Tempatkan seluruh `Connector_Standard` dan `ExitPortal` di sini.

---

## 2. Spesifikasi Detail 6 Prefab Greybox Level 01 Hutan

> **Aturan Validator:** Pada tepi berukuran `1 cell` (`12u`), titik tengah yang sah hanya **`6`**. Pada tepi berukuran `2 cell` (`24u`), titik tengah yang sah hanya **`6`** dan **`18`**.

### 1. `Room_Forest_Start_01` (Ruang Awal Run)

* **Footprint & Role:** `1 × 1` cell (`12 × 12` unit) | `Role = Start` | `LayoutArchetype = Open`
* **Anchors:** `PlayerSpawn` di `(6, 0, 6)`, rotasi `(0, 0, 0)`.
* **Connectors (2 unit — memberi opsi arah keluar ke solver):**

| Nama Object | Local Position | Local Rotation (Y) | Arah Keluar |
| --- | --- | --- | --- |
| `Connector_North` | `(6, 0, 12)` | `0°` | North (`+Z`) |
| `Connector_East` | `(12, 0, 6)` | `90°` | East (`+X`) |

* **EnemySpawnPoints:** Kosong (zona aman awal).

---

### 2. `Room_Forest_OpenCombat_01` (Arena Tempur Terbuka)

* **Footprint & Role:** `2 × 2` cell (`24 × 24` unit) | `Role = Combat` | `LayoutArchetype = Open`
* **Anchors:** `PlayerSpawn` di `(12, 0, 12)`.
* **Connectors (4 unit — mendukung jalur utama sekaligus percabangan):**

| Nama Object | Local Position | Local Rotation (Y) | Arah Keluar |
| --- | --- | --- | --- |
| `Connector_North` | `(6, 0, 24)` | `0°` | North (`+Z`) |
| `Connector_South` | `(18, 0, 0)` | `180°` | South (`-Z`) |
| `Connector_East` | `(24, 0, 18)` | `90°` | East (`+X`) |
| `Connector_West` | `(0, 0, 6)` | `270°` | West (`-X`) |

* **EnemySpawnPoints (5 titik — seluruhnya berjarak ≥ 5u dari pintu):**

| Nama Object | Local Position | `SpawnTags` |
| --- | --- | --- |
| `SP_Center_01` | `(12, 0, 12)` | `Center` |
| `SP_Outer_NW` | `(6, 0, 18)` | `OuterPerimeter`, `AmbushPosition` |
| `SP_Outer_SE` | `(18, 0, 6)` | `OuterPerimeter` |
| `SP_Ranged_NE` | `(19, 0, 19)` | `RangedPosition` |
| `SP_Ranged_SW` | `(5, 0, 5)` | `RangedPosition` |

---

### 3. `Room_Forest_PillarCombat_01` (Arena Pilar & Cover)

* **Footprint & Role:** `2 × 2` cell (`24 × 24` unit) | `Role = Combat` | `LayoutArchetype = Pillar`
* **Anchors:** `PlayerSpawn` di `(12, 0, 12)`.
* **Internal Obstacles (di dalam `StaticGeometry` untuk memutus line-of-sight):**
* `Pillar_SW`: Pos `(7.5, 2.0, 7.5)`, Skala `(2, 4, 2)`
* `Pillar_SE`: Pos `(16.5, 2.0, 7.5)`, Skala `(2, 4, 2)`
* `Pillar_NW`: Pos `(7.5, 2.0, 16.5)`, Skala `(2, 4, 2)`
* `Pillar_NE`: Pos `(16.5, 2.0, 16.5)`, Skala `(2, 4, 2)`


* **Connectors (4 unit — posisi bersilang dari `OpenCombat_01` untuk variasi layout):**

| Nama Object | Local Position | Local Rotation (Y) | Arah Keluar |
| --- | --- | --- | --- |
| `Connector_North` | `(18, 0, 24)` | `0°` | North (`+Z`) |
| `Connector_South` | `(6, 0, 0)` | `180°` | South (`-Z`) |
| `Connector_East` | `(24, 0, 6)` | `90°` | East (`+X`) |
| `Connector_West` | `(0, 0, 18)` | `270°` | West (`-X`) |

* **EnemySpawnPoints (5 titik — bebas dari pilar dan doorway):**

| Nama Object | Local Position | `SpawnTags` |
| --- | --- | --- |
| `SP_Center_01` | `(12, 0, 12)` | `Center`, `ElitePosition` |
| `SP_Flank_N` | `(12, 0, 18.5)` | `OuterPerimeter` |
| `SP_Flank_S` | `(12, 0, 5.5)` | `OuterPerimeter` |
| `SP_Ranged_E` | `(19.5, 0, 12)` | `RangedPosition` |
| `SP_Ranged_W` | `(4.5, 0, 12)` | `RangedPosition` |

---

### 4. `Room_Forest_Corridor_01` (Lorong Transisi / Penyergapan)

* **Footprint & Role:** `1 × 2` cell (`12 × 24` unit) | `Role = Traversal` | `LayoutArchetype = Narrow`
* **Anchors:** `PlayerSpawn` di `(6, 0, 12)`.
* **Connectors (3 unit — bisa menjadi lorong lurus atau pertigaan T):**

| Nama Object | Local Position | Local Rotation (Y) | Arah Keluar |
| --- | --- | --- | --- |
| `Connector_North` | `(6, 0, 24)` | `0°` | North (`+Z`) |
| `Connector_South` | `(6, 0, 0)` | `180°` | South (`-Z`) |
| `Connector_East` | `(12, 0, 18)` | `90°` | East (`+X`) |

* **EnemySpawnPoints (2 titik cadangan jika dipakai untuk ambush):**

| Nama Object | Local Position | `SpawnTags` |
| --- | --- | --- |
| `SP_Mid_01` | `(6, 0, 12)` | `Center`, `AmbushPosition` |
| `SP_North_02` | `(4, 0, 17)` | `OuterPerimeter` |

---

### 5. `Room_Forest_Reward_01` (Ruang Hadiah / Shrine Cabang)

* **Footprint & Role:** `1 × 1` cell (`12 × 12` unit) | `Role = Reward` | `LayoutArchetype = Open`
* **Anchors & Props:**
* `PlayerSpawn`: `(6, 0, 4)`
* `RewardPedestal`: `(6, 0, 8)` menghadap Selatan (`0, 180, 0`).


* **Connectors (1 unit — ruangan ujung cabang / dead-end):**

| Nama Object | Local Position | Local Rotation (Y) | Arah Keluar |
| --- | --- | --- | --- |
| `Connector_South` | `(6, 0, 0)` | `180°` | South (`-Z`) |

* **EnemySpawnPoints:** Kosong.

---

### 6. `Room_Forest_Exit_01` (Gerbang Akhir Area Hutan)

* **Footprint & Role:** `2 × 2` cell (`24 × 24` unit) | `Role = Exit` | `LayoutArchetype = Arena`
* **Anchors & Exit Trigger:**
* `PlayerSpawn`: `(12, 0, 12)`
* `ExitPortal`: Posisi `(16, 0, 16)`, pasang `BoxCollider` (`Is Trigger = true`, Size `(4, 3, 4)`) dan komponen **`RoomExit`**.


* **Connectors (2 unit — memberi 2 sudut masuk bagi solver):**

| Nama Object | Local Position | Local Rotation (Y) | Arah Keluar |
| --- | --- | --- | --- |
| `Connector_South` | `(6, 0, 0)` | `180°` | South (`-Z`) |
| `Connector_West` | `(0, 0, 6)` | `270°` | West (`-X`) |

* **EnemySpawnPoints (3 titik opsional jika ruangan Exit diberi encounter penjaga):**

| Nama Object | Local Position | `SpawnTags` |
| --- | --- | --- |
| `SP_Boss_01` | `(15, 0, 15)` | `ElitePosition`, `Center` |
| `SP_Guard_01` | `(10, 0, 17)` | `OuterPerimeter` |
| `SP_Guard_02` | `(17, 0, 10)` | `RangedPosition` |

---

## 3. Verifikasi Akhir Sebelum Play Test

1. Blok keenam prefab di folder `Assets/_Project/Prefabs/World/Rooms/Forest/`, lalu klik menu **`TLS > World > Validate Selected Room Prefabs`**. Pastikan seluruhnya menampilkan **`[PASS]`** di Console.
2. Buat `RoomDefinitionSO` untuk masing-masing prefab, masukkan ke `RoomCatalog_Forest`, lalu jalankan **`Validate All Rooms In Catalog`**.
3. Buka scene **`WorldGenerationTest`** dan tekan **Play** untuk memverifikasi generasi map lengkap dari ruang `Start` sampai `Exit`.

