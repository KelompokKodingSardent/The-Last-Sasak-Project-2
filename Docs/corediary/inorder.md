# **The Last Sasak — Core Systems Stabilization & Polish v1.0**

**Kasantara Studio · Engineering Design Proposal**



Proposed Baseline

Post-Gameplay Foundation



## **1. Kesimpulan analisis**

Menurutku, sekarang The Last Sasak tidak membutuhkan penambahan banyak fitur gameplay baru. Yang dibutuhkan adalah **memperkuat sistem yang menghubungkan seluruh gameplay** agar bisa bekerja konsisten dalam satu run, dari mulai permainan sampai Retry atau Game Over.

Aku merekomendasikan milestone:

**CORE SYSTEMS STABILIZATION & GAMEPLAY ORCHESTRATION v1.0**

Fokusnya ada pada enam domain:

1. **Pooling** — lifecycle objek, reset state, kapasitas, dan pengelolaan instance.
2. **Progression & Reward** — EXP, level, Souls, modifier, dan integrasi equipment.
3. **Event Architecture** — komunikasi antardomain, urutan event, dan pencegahan event ganda.
4. **Camera & Time Feedback** — Cinemachine, camera impulse, hitstop, dan transisi.
5. **Save & Run Lifecycle** — pemisahan data permanen dan sementara, load, reset, serta Retry.
6. **System Integration** — kontrak antarsistem, validasi, debugging, dan automated testing.

Ada satu prinsip yang harus dikunci sejak awal:

> **Kita tidak mengganti fondasi yang sudah bekerja. Kita memperbaiki lifecycle, kontrak komunikasi, pengelolaan state, dan integrasi antarsistem.**

Ini penting karena fondasi item/equipment v1.0 sudah memiliki pemilik state yang jelas: inventory mengelola kepemilikan, equipment mengelola perlengkapan aktif, artifact mengelola slot artefak, dan `PlayerStatController` menjadi satu-satunya kalkulator `FinalStats`. Arsitektur itu harus dipertahankan.

Pasted text (2)(7).txt

Demikian pula, jangan mengulang pembangunan sistem Health, Combat, Enemy, Room, atau World Generation yang sudah tersedia. Milestone ini bertugas menyempurnakan sistem inti dan hubungan di antaranya.

## **2. Prinsip arsitektur yang dikunci**

Aku tidak menyarankan membuat satu `GameCoreManager` yang mengatur semuanya. Kita justru perlu mempertahankan domain yang mandiri, lalu membuat koordinasi lifecycle secara eksplisit.

**Gambaran arsitektur target**

**RunLifecycleCoordinator**

Mengatur mulai, selesai, reset, dan Retry

**Pooling Domain**

Enemy · Projectile · VFX · Pickup

**Progression Domain**

EXP · Level · Souls · Modifiers

**Event Domain**

Local Events · Game Events

**Presentation Domain**

Camera · Hitstop · Feedback

**Persistence Domain**

Save · Load · Validation

**Existing Gameplay**

Player · Combat · Enemy · Room

**PlayerStatController + UI Presentation**

Menerima hasil perubahan state, bukan mengambil alih kepemilikannya

Diagram ini menunjukkan kelompok tanggung jawab, bukan berarti semua sistem harus saling memanggil secara langsung.

### **2.1 Aturan kepemilikan state**

| Data / tanggung jawab      | Pemilik tunggal           |
| -------------------------- | ------------------------- |
| Active pooled instances    | Pool system               |
| HP aktual                  | `Health`                  |
| EXP dan level run          | `ExperienceSystem`        |
| Souls permanen             | `CurrencyWallet`          |
| Kepemilikan item           | `InventoryController`     |
| Equipment aktif            | `EquipmentController`     |
| Artifact aktif             | `ArtifactController`      |
| Final player stats         | `PlayerStatController`    |
| Game state                 | `GameManager`             |
| Run lifecycle              | `RunLifecycleCoordinator` |
| Save file                  | `SaveSystem`              |
| Camera follow dan feedback | Camera domain             |
| Global gameplay events     | `GameEvents`              |

Tidak boleh ada dua sistem yang menjadi pemilik data yang sama. Sistem lain hanya meminta perubahan melalui API pemilik tersebut atau mendengarkan event-nya.

## **3. Rancangan domain yang disarankan**

### **3.1 Pooling — dari sistem pemakaian ulang menjadi lifecycle manager**

Pooling jangan hanya bertugas menyimpan GameObject yang sedang tidak digunakan. Sistem ini harus menjamin bahwa setiap objek yang dipinjam kembali dalam keadaan bersih dan bisa digunakan dengan benar.

Dokumentasi proyek sebelumnya memang sudah menetapkan `ObjectPoolManager`, `EnemyPool`, `ProjectilePool`, serta reset HP, invulnerability, event subscription, timer, dan Animator. Jadi fokus peningkatannya adalah **standarisasi lifecycle dan validasi peminjaman/pengembalian**.&#x20;

Pasted text(20261127-024716).txt



**Arsitektur target**

```
ObjectPoolManager
       |
       +── EnemyPool
       |      └── Enemy instances
       |
       +── ProjectilePool
       |      └── Projectile instances
       |
       +── VFXPool       (opsional)
       |      └── Impact / particles
       |
       └── LootPool      (opsional)
              └── Loot pickup
```

`ObjectPoolManager` menjadi pemilik registry pool. `EnemyPool` dan `ProjectilePool` bertindak sebagai adapter khusus agar sistem gameplay tidak perlu mengelola generic pool secara langsung.

**Keputusan penting:**

| Aspek                 | Keputusan                                         |
| --------------------- | ------------------------------------------------- |
| Pool implementation   | `UnityEngine.Pool.ObjectPool<T>`                  |
| Pool registry         | `ObjectPoolManager`                               |
| Pool configuration    | Satu `PoolCatalogSO` berisi konfigurasi tiap pool |
| Object lifecycle      | Kontrak `IPoolable`                               |
| Enemy spawn           | Melalui `EnemyPool`                               |
| Projectile spawn      | Melalui `ProjectilePool`                          |
| Release               | Melalui pool yang memiliki instance               |
| Scene ownership       | Service sudah tersedia di `SceneServices`         |
| Runtime instantiation | Hanya prefab yang terdaftar dalam catalog         |

Aku menyarankan satu `PoolCatalogSO` yang berisi entry seperti:

| Konfigurasi       | Arti                                     |
| ----------------- | ---------------------------------------- |
| `PoolId`          | Identitas pool yang stabil               |
| `Prefab`          | Prefab yang boleh digunakan              |
| `PrewarmCount`    | Jumlah awal yang disiapkan               |
| `MaxRetained`     | Batas instance tidak aktif yang disimpan |
| `MaxActive`       | Batas instance aktif                     |
| `ExpansionPolicy` | Kebijakan ketika kapasitas penuh         |
| `CollectionCheck` | Pemeriksaan double release               |

Perlu dibedakan antara `MaxRetained` dan `MaxActive`. Pada `ObjectPool<T>`, batas penyimpanan objek yang dikembalikan tidak otomatis membatasi jumlah objek yang bisa diminta. Karena itu, `MaxActive` harus menjadi validasi tambahan di sistem kita.

Aku merekomendasikan kebijakan **bounded expansion**: pool boleh berkembang sampai batas aktif yang ditentukan. Setelah mencapai hard limit, permintaan spawn ditolak dengan diagnostic yang jelas, bukan membuat objek baru tanpa batas.

Untuk enemy, kegagalan spawn harus terdeteksi sebelum encounter dikunci. Jangan sampai pemain masuk ruangan, gerbang tertutup, tetapi beberapa musuh gagal dibuat sehingga encounter tidak pernah selesai.

**Lifecycle objek**

```
INACTIVE IN POOL
       |
       v
TRY ACQUIRE
       |
       v
VALIDATE CAPACITY
       |
       v
ASSIGN SPAWN DATA
       |
       v
RESET RUNTIME STATE
       |
       v
ACTIVATE GAMEOBJECT
       |
       v
ACTIVE / IN USE
       |
       v
RELEASE REQUEST
       |
       v
VALIDATE LEASE
       |
       v
CLEANUP RUNTIME STATE
       |
       v
DEACTIVATE GAMEOBJECT
       |
       v
RETURN TO POOL
```

Aku menyarankan kontrak sederhana:

```
public interface IPoolable
{
    void OnPoolRent();
    void OnPoolReturn();
}
```

Kontrak ini adalah batas lifecycle, bukan tempat seluruh konfigurasi spawn dilakukan. Data spawn tetap diterima melalui API khusus, misalnya `EnemyPool.TrySpawn(...)` atau `ProjectilePool.TrySpawn(...)`.

**Reset yang wajib dilakukan**

| Enemy              | Projectile             |
| ------------------ | ---------------------- |
| HP dan Max HP      | Damage                 |
| Invulnerability    | Owner dan faction      |
| Stagger            | Direction dan velocity |
| AI State           | Lifetime               |
| Target Player      | Hit history            |
| Attack Cooldown    | Parry state            |
| Animator           | Collision state        |
| Status Effects     | Trail dan visual       |
| NavMeshAgent       | Timer dan callbacks    |
| Event subscription | Event subscription     |

Satu detail yang perlu dijaga: `OnPoolRent()` tidak boleh mengasumsikan bahwa semua objek baru saja dibuat. Sistem harus bekerja sama baiknya untuk instance pertama maupun instance yang sudah digunakan puluhan kali.

**Peningkatan tambahan yang penting: Pool Lease**

Setiap peminjaman mendapatkan `LeaseId` baru. Identitas ini digunakan untuk membedakan pemakaian pertama dan pemakaian berikutnya dari GameObject yang sama.

Contoh masalah tanpa lease:

1. Enemy A mati.
2. Enemy A dikembalikan ke pool.
3. Instance yang sama dipinjam lagi sebagai Enemy B.
4. Callback lama dari penggunaan Enemy A terlambat diproses.
5. Sistem encounter justru menganggap Enemy B sudah mati.

Dengan `LeaseId`, event dari penggunaan lama dapat ditolak.

Tambahkan pula `PoolMetrics` untuk mencatat:

- Total instance dibuat.
- Jumlah active dan inactive.
- Peak active.
- Total rent dan release.
- Jumlah spawn yang ditolak.
- Jumlah double release.
- Jumlah pool expansion.

**Apa yang tidak perlu di-pool?**

Jangan melakukan pooling terhadap semua GameObject hanya karena teknologinya tersedia.

Static room geometry, Player utama, HUD, kamera, gate permanen, dan NPC Hub tidak membutuhkan pool untuk baseline ini. Prioritasnya adalah Enemy dan Projectile; VFX serta loot pickup menyusul berdasarkan frekuensi penggunaan dan hasil profiling.

### **3.2 Progression — satu alur, dua masa hidup data**

Bagian ini harus sangat hati-hati karena proyek sudah mempunyai inventory, equipment, artifact, dan stat calculator yang terpisah.

Kesalahan yang harus dihindari adalah memiliki `ExperienceSystem`, `PlayerProgression`, `EquipmentController`, dan `ArtifactController` yang masing-masing mengubah HP atau Attack Damage secara langsung.

**Aku merekomendasikan dua lapisan progression:**

**A. Run Progression**

Temporary

- EXP selama run.
- Level selama run.
- Bonus statistik dari level.
- Modifier sementara yang memang disetujui GDD.

**B. Permanent Progression**

Persistent

- Souls Coin.
- Permanent Upgrade.
- Data perlengkapan yang dipertahankan.
- Progression permanen yang disimpan.

**Arsitektur target**

```
                  REWARD
                    |
          +---------+---------+
          |                   |
          v                   v
     EXPERIENCE           SOULS
          |                   |
          v                   v
 ExperienceSystem       CurrencyWallet
          |                   |
          v                   v
 PlayerProgression      PermanentUpgradeSystem
          |                   |
          +---------+---------+
                    |
                    v
           PlayerStatController
                    |
                    v
               FinalStats
                    |
          +---------+---------+
          |                   |
          v                   v
        Health             Combat
```

Alur ini menjaga agar setiap sistem tetap mempunyai satu tanggung jawab.

#### **Pembagian tanggung jawab**

| Komponen                 | Tugas                                                               |
| ------------------------ | ------------------------------------------------------------------- |
| `RewardCollector`        | Menentukan reward yang diberikan berdasarkan sumber dan konfigurasi |
| `ExperienceSystem`       | Menyimpan EXP run dan menghitung level                              |
| `PlayerProgression`      | Mengubah level menjadi modifier progression                         |
| `CurrencyWallet`         | Menyimpan dan mengubah jumlah Souls                                 |
| `PermanentUpgradeSystem` | Validasi pembelian, biaya, dan kepemilikan upgrade                  |
| `PlayerStatController`   | Menghitung seluruh FinalStats                                       |
| `SaveSystem`             | Menyimpan dan memuat data permanen                                  |

#### **Experience System**

Aku menyarankan menggunakan `ProgressionConfigSO` yang menyimpan tabel kebutuhan EXP per level.

Contoh struktur data:

```
ProgressionConfigSO
│
├── MaxLevel
├── ExperienceRequirements
│
├── LevelRewards
│   ├── Level 01
│   ├── Level 02
│   └── Level 03
│
└── OverflowPolicy
```

Gunakan tabel threshold atau kurva yang eksplisit agar bisa diubah melalui Inspector tanpa mengganti script.

Sistem harus mampu menangani:

- EXP negatif ditolak.
- EXP nol tidak mengubah state.
- Satu reward dapat menyebabkan lebih dari satu level-up.
- EXP yang tersisa setelah level-up tidak hilang.
- Level tidak melewati batas maksimum.
- Event level-up tidak terpanggil dua kali.
- XP tidak tertukar dengan Souls.

`PlayerProgression` tetap mempertahankan perilaku yang sudah ada, yaitu bonus HP berdasarkan level. Jangan langsung menambahkan pilihan skill, upgrade acak, atau skill tree karena itu akan memperluas scope desain.

#### **Integrasi statistik**

Ini bagian yang paling penting.

Semua sumber modifier harus masuk ke `PlayerStatController`.

```
BASE PLAYER STATS
        |
        +── Equipment Modifiers
        |
        +── Armor Modifiers
        |
        +── Artifact Modifiers
        |
        +── Run Progression Modifiers
        |
        +── Permanent Upgrade Modifiers
        |
        +── Temporary Status Modifiers
        |
        v
PLAYER STAT CONTROLLER
        |
        v
FINAL STATS
```

Gunakan formula yang telah dikunci di Item & Equipment Domain v1.0:

\\[ FinalStat=(Base+\sum Additive)\times\prod(1+Multiplicative) \\]

Modifier mempunyai `SourceId` agar setiap sumber dapat ditambahkan, diganti, maupun dihapus tanpa menggandakan modifier yang sama.

Contoh:

```
progression.level.03
equipment.weapon.klewang
equipment.armor.besi_ambat
artifact.batu_ambang
upgrade.hp_permanent_01
```

Nama di atas contoh pola ID, bukan ID asset baru yang wajib digunakan.

Pertahankan juga kebijakan:

`MaxHealthRecalculationPolicy.KeepCurrentAndClamp`

Jadi kenaikan Max HP tidak otomatis mengisi HP, sementara penurunan Max HP tetap melakukan clamp terhadap Current HP.

**Permanent Upgrade**

`PermanentUpgradeSystem` tidak boleh melakukan:

```
health.MaxHealth += 20;
```

Sistem ini harus menjalankan transaksi:

```
Request Purchase
       |
       v
Validate Upgrade
       |
       v
Validate Currency
       |
       v
TrySpend Souls
       |
       v
Commit Upgrade Ownership
       |
       v
Publish Upgrade Changed
       |
       v
Recalculate Final Stats
       |
       v
Mark Save Dirty
```

Apabila transaksi gagal, currency dan ownership tidak boleh berubah sebagian.

#### **Satu kebijakan desain yang perlu dipertahankan sebagai keputusan terbuka**

GDD sebelumnya belum menetapkan secara tegas apakah Souls dari musuh langsung menjadi permanen, atau baru dikonfirmasi setelah ruangan/run selesai. GDD juga belum menetapkan apakah seluruh reward tetap dipertahankan saat Player mati.&#x20;

Pasted text(20261127-024716).txt



Secara engineering, sistem bisa mendukung kedua kebijakan tanpa ditulis ulang. Namun, aturan gameplay-nya harus disahkan Project Lead.

Untuk implementasi pertama, jangan mengubah perilaku reward yang sudah berjalan sebelum kebijakan tersebut dikonfirmasi.

### **3.3 Event Architecture — komunikasi yang dapat dilacak**

Aku tidak merekomendasikan satu EventBus yang menerima semua hal. Hal ini akan membuat hubungan sistem terlalu abstrak, sehingga sulit mencari sumber event ketika terjadi bug.

Pertahankan pendekatan **Local C# Events + Global GameEvents**. Pendekatan pemisahan ini juga sesuai dengan rancangan sebelumnya.&#x20;

Pasted text(20261127-024716).txt



**A. Local Events**

Digunakan untuk komunikasi dalam domain yang sudah mempunyai pemilik jelas.

| Pengirim               | Event                 | Penerima                    |
| ---------------------- | --------------------- | --------------------------- |
| `Health`               | `OnHealthChanged`     | HUD                         |
| `Health`               | `OnDied`              | Death Handler               |
| `InventoryController`  | `OnInventoryChanged`  | Inventory UI                |
| `EquipmentController`  | `OnEquipmentChanged`  | Equipment UI                |
| `ExperienceSystem`     | `OnExperienceChanged` | HUD                         |
| `ExperienceSystem`     | `OnLevelUp`           | PlayerProgression           |
| `CurrencyWallet`       | `OnCurrencyChanged`   | HUD / Hub UI                |
| `PlayerStatController` | `OnStatsRecalculated` | Health / gameplay receivers |

**B. Global GameEvents**

Hanya untuk peristiwa yang memang melintasi beberapa domain.

| Global Event         | Tujuan                    |
| -------------------- | ------------------------- |
| `OnGameStateChanged` | Perubahan state permainan |
| `OnRunStarted`       | Run baru dimulai          |
| `OnRunEnded`         | Run selesai               |
| `OnPlayerDied`       | Player mati               |
| `OnRoomCleared`      | Encounter selesai         |
| `OnSceneReady`       | Scene siap digunakan      |

Jangan membuat `OnEveryFrame`, `OnEveryStatChanged`, atau event global untuk perubahan yang cukup ditangani secara lokal.

**C. Event Payload**

Event penting sebaiknya tidak hanya mengirim `Action` tanpa informasi.

Gunakan data yang bisa membantu penerima mengidentifikasi kejadian.

Contoh konsep:

```
public readonly struct EntityDiedEvent
{
    public readonly string EntityId;
    public readonly int PoolLeaseId;
    public readonly Vector3 Position;
}
```

Untuk run, event dapat membawa `RunId` dan `Seed`. Untuk reward, gunakan `RewardTransactionId`.

Dengan demikian, event dapat dilacak dan tidak mudah tercampur antara run lama dan run baru.

**D. Event Lifecycle**

Aturan wajib:

- Subscribe ketika komponen aktif.
- Unsubscribe ketika komponen dinonaktifkan.
- Pooled object harus dapat subscribe kembali ketika dipinjam.
- Event kematian tidak boleh dipanggil berulang.
- Event room-cleared hanya boleh terjadi sekali.
- Event pembelian hanya dipublish setelah transaksi berhasil.
- Perubahan GameState yang tidak berubah tidak perlu memancarkan event baru.

Karena proyek menggunakan static/global `GameEvents`, tambahkan mekanisme reset static event ketika runtime dimulai, termasuk untuk skenario Unity dengan Domain Reload dinonaktifkan. Jangan mengandalkan asumsi bahwa seluruh static state selalu bersih setelah menekan Play.

**E. Debugging Event**

Tambahkan `EventDiagnostics` untuk Development Build:

```
[10:21:01] RunStarted
[10:21:04] EntityDied: Enemy.Vanguard / Lease 18
[10:21:04] RewardGranted: EXP 20
[10:21:04] CurrencyChanged: Souls +5
[10:21:05] RoomCleared: Room_03
```

Ini bukan sistem gameplay baru. Ini alat untuk memeriksa urutan kejadian, mendeteksi event ganda, dan mencari sumber bug.

### **3.4 Camera & Time Feedback — pisahkan keputusan gameplay dari presentasi**

Kamera jangan dijadikan bagian dari `PlayerMotor`, `CombatSystem`, ataupun `EnemyBrain`.

Kita sudah mempunyai:

- Cinemachine follow camera.
- `CameraEventManager`.
- `TimeEffectController`.
- Hitstop yang dapat dikonfigurasi dari serangan.
- Feedback camera shake per attack.

Fondasi presentasi combat sudah ada; laporan Player Domain sebelumnya juga mencatat hitstop dan camera shake sebagai bagian dari feedback tiap serangan.&#x20;

Pasted text (2)(7).txt



Yang perlu dilakukan adalah menyatukan pengelolaannya.

**Arsitektur target**

```
Combat Resolution
       |
       v
Combat Feedback Request
       |
       v
CombatFeedbackRouter
       |
       +-----------------+
       |                 |
       v                 v
TimeEffectController  CameraEventManager
       |                 |
       v                 v
Hitstop Request       Impulse Request
                         |
                         v
                 Cinemachine Adapter
                         |
                         v
                    Main Camera
```

#### **Camera Follow Domain**

Struktur:

```
CameraRig
│
├── Main Camera
│   └── Cinemachine Brain
│
├── CM_Follow
│   ├── Follow = CameraTarget
│   └── Follow / composition settings
│
└── CameraEventManager
    └── Cinemachine Impulse integration
```

`CameraTarget` tetap berada pada Player. `CameraEventManager` hanya mengelola feedback dan tidak mengambil alih transform follow target.

Untuk project isometrik, yang harus diuji:

- Follow tetap stabil saat Player menyerang.
- Kamera tidak tertinggal ketika Dodge.
- Damping tidak membuat input terasa lambat.
- Kamera tidak bergetar berlebihan saat beberapa serangan terjadi.
- Pergantian ruangan fisik tidak membuat kamera kehilangan target.
- Scene transition tidak menimbulkan dua kamera aktif yang bertentangan.

#### **Camera Feedback Profile**

Aku merekomendasikan satu `CameraFeedbackConfigSO`, bukan banyak konfigurasi asset kecil.

Isi profil misalnya:

| Event           | Parameter yang dikonfigurasi        |
| --------------- | ----------------------------------- |
| Light Hit       | Amplitude dan duration              |
| Heavy Hit       | Amplitude dan duration              |
| Perfect Parry   | Amplitude dan frequency             |
| Player Damaged  | Amplitude dan duration              |
| Elite Impact    | Amplitude dan frequency             |
| Player Death    | Feedback khusus                     |
| Room Transition | Blend atau impulse sesuai kebutuhan |

Nilai final harus ditentukan melalui gameplay testing, bukan angka asumsi engineering.

Setiap `AttackDefinitionSO` dapat memberikan multiplier feedback. Dengan begitu, dua serangan tidak harus menghasilkan camera shake yang sama.

#### **TimeEffectController**

Bagian ini wajib diperkuat karena hitstop yang sederhana sering bermasalah ketika dua serangan terjadi berdekatan.

Contoh:

- Player mengenai musuh.
- Hitstop aktif.
- Dalam waktu yang hampir bersamaan musuh lain terkena serangan.
- Hitstop kedua aktif sebelum hitstop pertama selesai.

Sistem tidak boleh mengembalikan `Time.timeScale` ke normal hanya karena salah satu permintaan selesai.

Aku merekomendasikan sistem **request-based hitstop**.

```
Hitstop Request A ──┐
                    |
Hitstop Request B ──┼── TimeEffectController
                    |          |
Hitstop Request C ──┘          v
                         Effective Time
```

Setiap hitstop memiliki ID, durasi, dan time scale. Seluruh request aktif dipertimbangkan untuk menentukan time scale efektif.

Kebijakan yang disarankan:

- Permintaan yang lebih kuat dapat menghasilkan time scale lebih rendah.
- Setiap request berakhir berdasarkan durasinya sendiri.
- Satu request tidak boleh menghapus request yang lain.
- Timer hitstop menggunakan unscaled time.
- Pause mempunyai prioritas di atas hitstop.
- Setelah pause dilepas, hitstop yang masih berlaku tetap diproses.
- Hanya satu sistem yang boleh menulis `Time.timeScale`.

Jadi, `GameManager`, `CombatResolver`, dan UI tidak boleh mengubah `Time.timeScale` masing-masing.

Untuk baseline, tidak perlu membangun framework time dilation yang kompleks. Fokus pada pause dan hitstop yang stabil.

### **3.5 Save & Run Lifecycle — mencegah data run tercampur dengan progres permanen**

Ini bagian yang menghubungkan hampir seluruh sistem.

Aku menyarankan kita membedakan dua hal:

**Run State** adalah kondisi permainan yang sedang berlangsung.

**Persistent State** adalah data yang harus tetap tersedia setelah scene berpindah, permainan ditutup, atau run selesai.

```
PERSISTENT STATE
│
├── Souls
├── Permanent Upgrades
├── Inventory / Equipment (sesuai policy)
└── Save Version

RUN STATE
│
├── Run ID
├── Seed
├── Experience
├── Run Level
├── Temporary Modifiers
├── Visited Rooms
└── Encounter Progress
```

`FinalStats` tidak perlu disimpan sebagai nilai permanen. Saat load, hitung kembali dari base stats dan modifier yang valid.

#### **SaveSystem sebagai satu-satunya file writer**

Aku merekomendasikan perubahan tanggung jawab berikut:

| Sistem                       | Tanggung jawab                              |
| ---------------------------- | ------------------------------------------- |
| `CurrencyWallet`             | Menyimpan nilai Souls dalam runtime         |
| `ItemPersistenceCoordinator` | Capture dan apply data item                 |
| `PermanentUpgradeSystem`     | Menangani pembelian dan kepemilikan upgrade |
| `SaveSystem`                 | Menulis dan membaca `tls_save.json`         |

Jangan biarkan `CurrencyWallet`, `SaveSystem`, dan `ItemPersistenceCoordinator` masing-masing menulis file yang sama.

Struktur data:

```
SaveData
│
├── SaveVersion
├── CurrencyData
│   └── Souls
│
├── PermanentUpgradeData
│
├── InventoryData
│   └── ItemId + Quantity
│
├── EquipmentData
│
├── ArtifactData
│
└── SettingsData
```

Tetap gunakan format ID-based persistence yang sudah ditentukan dalam Item & Equipment Domain. Jangan menyimpan referensi ScriptableObject atau GameObject secara langsung.

Tambahkan:

- `SaveVersion`.
- Data validation.
- Invalid/missing item handling.
- Backup file.
- Atomic save menggunakan temporary file.
- Error handling ketika file rusak.
- Migration ketika struktur save berubah.

#### **RunLifecycleCoordinator**

`GameManager` tetap mengelola GameState. Jangan membuatnya menjadi tempat semua reset dan cleanup.

Tambahkan `RunLifecycleCoordinator` sebagai pengatur urutan lifecycle run.

Tanggung jawabnya mengoordinasikan:

- Start Run.
- Finalize Run.
- Game Over.
- Retry.
- Cleanup.
- Reset temporary progression.
- Reset modifier run.
- Pool cleanup.
- World cleanup.
- Save checkpoint bila diperlukan.

Contoh lifecycle:

```
NEW RUN
   |
   v
CREATE RUN CONTEXT
   |
   v
INITIALIZE PROGRESSION
   |
   v
GENERATE WORLD
   |
   v
ASSEMBLE MAP
   |
   v
BUILD NAVMESH
   |
   v
POSITION PLAYER
   |
   v
START GAMEPLAY
```

Saat Player mati:

```
PLAYER DIED
     |
     v
FINALIZE REWARD
     |
     v
FINALIZE ROOM / RUN STATE
     |
     v
DISPLAY GAME OVER
     |
     v
PLAYER SELECTS RETRY
     |
     v
STOP NEW SPAWNING
     |
     v
RELEASE ACTIVE POOLS
     |
     v
RESET RUN PROGRESSION
     |
     v
RESET TEMPORARY MODIFIERS
     |
     v
REBUILD / REGENERATE WORLD
     |
     v
BUILD NAVMESH
     |
     v
INITIALIZE PLAYER
     |
     v
START NEW RUN
```

**Satu keputusan penting:** jangan menghapus EXP dan data run tepat pada saat Player mati sebelum Game Over atau sistem hasil run selesai membacanya. Pisahkan proses `FinalizeRun` dengan `ResetRun`.

Untuk Retry, kebijakan seed perlu dapat dipilih: menggunakan seed yang sama untuk reproduksi/debugging atau seed baru untuk run baru. Jangan biarkan keputusan ini tersembunyi di dalam generator.

Untuk inventory dan equipment, tetap hormati:

`ItemPersistenceCoordinator.PersistInventoryAndEquipment`

Flag ini sudah menjadi kontrol kebijakan persistence pada baseline item. Jangan menggantinya dengan aturan baru tanpa keputusan desain.

## **4. System Contracts — hubungan antarsistem**

Sebelum implementasi, kontrak berikut harus dibakukan agar tidak terjadi ketergantungan yang saling melingkar.

| Dari                      | Ke                        | Kontrak                             |
| ------------------------- | ------------------------- | ----------------------------------- |
| `Health`                  | `RewardCollector`         | Entity death + spawn lease identity |
| `Health`                  | `RoomEncounterController` | Kematian enemy                      |
| `RewardCollector`         | `ExperienceSystem`        | EXP reward                          |
| `RewardCollector`         | `CurrencyWallet`          | Souls reward                        |
| `ExperienceSystem`        | `PlayerProgression`       | Level changed                       |
| `PlayerProgression`       | `PlayerStatController`    | Progression modifier                |
| `PermanentUpgradeSystem`  | `CurrencyWallet`          | TrySpend                            |
| `PermanentUpgradeSystem`  | `PlayerStatController`    | Upgrade modifier source             |
| `PlayerStatController`    | `Health`                  | MaxHealth update                    |
| `PlayerStatController`    | Gameplay receivers        | FinalStats changed                  |
| `EnemySpawner`            | `EnemyPool`               | Acquire enemy                       |
| `Projectile`              | `ProjectilePool`          | Release projectile                  |
| `CombatResolver`          | `CombatFeedbackRouter`    | Combat resolution feedback          |
| `CombatFeedbackRouter`    | `CameraEventManager`      | Camera feedback request             |
| `CombatFeedbackRouter`    | `TimeEffectController`    | Hitstop request                     |
| `RunLifecycleCoordinator` | Core systems              | Start, finalize, reset              |
| `SaveSystem`              | Persistence participants  | Capture / Apply                     |

**Aturan dependency:**

1. Domain bawah tidak boleh mengetahui UI.
2. UI tidak boleh mengubah state gameplay secara langsung.
3. Pool manager tidak boleh mengetahui reward.
4. Enemy AI tidak boleh mengetahui Souls atau EXP.
5. CombatResolver tidak boleh mengatur Cinemachine secara langsung.
6. GameManager tidak boleh melakukan reset internal semua komponen secara manual.
7. PlayerStatController tetap menjadi satu-satunya penghasil FinalStats.

# **5. PHASE IMPLEMENTATION ROADMAP**

Aku menyarankan delapan fase. Urutannya dibuat berdasarkan dependency, bukan berdasarkan nama sistem atau banyaknya script.

**Target milestone**

# **Core Systems v1.0**

8 implementation phases · 1 integrated baseline

Core foundation

**P0**

Pooling, progression, events

**P0**

Persistence, camera, time

**P0**

Diagnostics and optimization

**P1**

## **PHASE 00 — Existing Core Systems Audit**

P0 · Wajib

**Objective:** Mengetahui kondisi implementasi aktual sebelum memodifikasi sistem yang sudah ada.

### **Tasks**

1. Audit `ObjectPoolManager`, `EnemyPool`, dan `ProjectilePool`.
2. Periksa apakah semua prefab memiliki lifecycle reset yang konsisten.
3. Audit bagaimana projectile dan enemy dikembalikan ke pool.
4. Audit `CurrencyWallet`, `ExperienceSystem`, dan `PlayerProgression`.
5. Periksa hubungan `RewardCollector` dengan Health dan Room Encounter.
6. Audit `PlayerStatController` dan semua modifier sources.
7. Periksa implementasi `GameEvents` dan seluruh subscription.
8. Audit `TimeEffectController`.
9. Audit `CameraEventManager` dan konfigurasi Cinemachine.
10. Periksa `SaveSystem`, `ItemPersistenceCoordinator`, dan `PermanentUpgradeSystem`.
11. Audit lifecycle pada `GameManager`, `MainMenu`, `CombatTest`, `ForestRun`, dan `Hub`.
12. Catat setiap penggunaan `Instantiate`, `Destroy`, `SetActive`, dan `Time.timeScale` yang berhubungan dengan sistem tersebut.

### **Deliverables**

- Core System Audit Report.
- Existing Dependency Map.
- Pool Lifecycle Report.
- Progression Ownership Map.
- Event Subscription Map.
- Camera / Time Feedback Map.
- Save Ownership Map.
- List of Refactoring Candidates.

### **Acceptance Criteria**

Tidak ada perubahan arsitektur sebelum diketahui sistem mana yang sudah bekerja, tumpang tindih, atau mempunyai lifecycle bermasalah.

## **PHASE 01 — Core Event Architecture & Service Lifecycle**

P0 · Fondasi

**Objective:** Membuat hubungan antarsistem dapat diprediksi dan tidak menghasilkan event ganda atau subscription tertinggal.

### **Scripts**

```
Core/
├── Events/
│   ├── GameEvents.cs
│   ├── GameEventPayloads.cs
│   └── EventDiagnostics.cs
│
├── Lifecycle/
│   └── RunLifecycleCoordinator.cs
│
└── Services/
    └── SceneServicesValidator.cs
```

`GameEvents` tetap menjadi implementasi existing jika memang sudah digunakan. Jangan membuat EventBus baru yang berjalan paralel.

### **Tasks**

1. Tetapkan event lokal dan global.
2. Buat payload untuk event yang memerlukan informasi tambahan.
3. Tetapkan `RunId` dan identitas spawn instance.
4. Buat aturan subscribe/unsubscribe.
5. Pastikan pooled component dapat subscribe ulang dengan aman.
6. Tambahkan event duplicate protection untuk kematian dan room-cleared.
7. Tambahkan reset static event state ketika runtime dimulai.
8. Tambahkan event log untuk Development Build.
9. Validasi seluruh service yang wajib ada pada setiap scene.
10. Hilangkan ketergantungan tersembunyi pada service MainMenu ketika CombatTest atau ForestRun dijalankan langsung.

### **Tests**

- Tidak ada event listener ganda setelah Retry.
- Tidak ada subscription lama setelah scene berpindah.
- `OnDied` hanya diterbitkan sekali.
- Room-cleared hanya diterbitkan sekali.
- Event lama dari run sebelumnya tidak mengubah state run baru.
- Scene dapat dijalankan langsung tanpa service yang hilang.

### **Acceptance Criteria**

Event dapat dilacak, hubungan antardomain jelas, dan service tidak bergantung pada urutan masuk scene yang tersembunyi.

## **PHASE 02 — Pooling Lifecycle & Reliability**

P0 · Sistem inti

**Objective:** Menjadikan pooling dapat diandalkan dalam combat, encounter, dan procedural room.

### **Scripts**

```
Systems/
└── Pooling/
    ├── ObjectPoolManager.cs
    ├── PoolCatalogSO.cs
    ├── PooledObject.cs
    ├── IPoolable.cs
    ├── PoolHandle.cs
    ├── PoolMetrics.cs
    ├── EnemyPool.cs
    └── ProjectilePool.cs
```

Nama dapat disesuaikan dengan file existing. Jangan membuat file baru apabila tanggung jawab yang sama sudah ditangani script yang ada.

### **Tasks**

1. Buat atau rapikan pool registry.
2. Implementasikan pool configuration.
3. Standarkan rent dan release API.
4. Implementasikan pool lifecycle contract.
5. Tambahkan spawn reset untuk Enemy.
6. Tambahkan spawn reset untuk Projectile.
7. Implementasikan lease identity.
8. Tambahkan double-release protection.
9. Terapkan batas active instance.
10. Terapkan prewarm.
11. Tambahkan kapasitas dan overflow policy.
12. Integrasikan dengan EnemySpawner.
13. Integrasikan dengan projectile dari EnemyAttackController.
14. Uji pool saat musuh mati, parry, dan terkena projectile.
15. Uji release semua objek saat Retry.

### **Tests**

| ID   | Test             | Expected Result                            |
| ---- | ---------------- | ------------------------------------------ |
| PL01 | Initial prewarm  | Jumlah instance sesuai konfigurasi         |
| PL02 | Rent object      | Instance aktif dan terinisialisasi         |
| PL03 | Release object   | Instance dinonaktifkan dan kembali         |
| PL04 | Double release   | Ditolak dan tercatat                       |
| PL05 | State reset      | Tidak ada state lama                       |
| PL06 | Pool expansion   | Tidak melewati batas                       |
| PL07 | Capacity full    | Spawn gagal secara terkendali              |
| PL08 | Enemy reuse      | HP, AI, target, dan Animator reset         |
| PL09 | Projectile reuse | Owner, direction, dan lifetime reset       |
| PL10 | Retry cleanup    | Tidak ada instance aktif dari run lama     |
| PL11 | Stale callback   | Lease lama tidak memengaruhi instance baru |
| PL12 | Scene transition | Tidak terjadi orphan object                |

### **Acceptance Criteria**

Enemy dan Projectile dapat digunakan ulang berkali-kali tanpa state contamination, double release, orphan object, atau spawn yang tidak terkendali.

## **PHASE 03 — Progression, Reward & Modifier Integration**

P0 · Gameplay core

**Objective:** Menyatukan progression sementara, mata uang permanen, reward, dan perhitungan FinalStats.

### **Scripts**

```
Systems/
└── Progression/
    ├── ProgressionConfigSO.cs
    ├── ExperienceSystem.cs
    ├── PlayerProgression.cs
    ├── RewardDefinitionSO.cs
    ├── RewardCollector.cs
    ├── CurrencyWallet.cs
    ├── PermanentUpgradeSystem.cs
    └── RewardTransactionId.cs
```

Pertahankan nama, namespace, dan API existing yang sudah dipakai oleh gameplay. Ini daftar tanggung jawab target, bukan perintah mengganti semua implementasi.

### **Tasks**

1. Audit EXP threshold dan level calculation.
2. Tetapkan sumber data progression.
3. Perbaiki EXP overflow dan multi-level-up.
4. Pastikan max level memiliki batas yang jelas.
5. Hubungkan level-up dengan `PlayerProgression`.
6. Hubungkan progression modifiers ke `PlayerStatController`.
7. Pastikan modifier memiliki SourceId yang konsisten.
8. Periksa duplicate modifier saat level-up berulang.
9. Audit reward yang diterima ketika Enemy mati.
10. Pastikan satu enemy spawn lease hanya memberi reward sekali.
11. Integrasikan `CurrencyWallet` dengan permanent reward.
12. Implementasikan transaksi pembelian upgrade yang atomic.
13. Hubungkan perubahan upgrade dengan recalculation stats.
14. Periksa MaxHealth policy.
15. Pastikan Souls tidak pernah masuk InventorySlot.
16. Uji reset EXP tanpa menghapus Souls.

### **Tests**

| ID   | Test               | Expected Result                             |
| ---- | ------------------ | ------------------------------------------- |
| PR01 | Add EXP            | Nilai EXP benar                             |
| PR02 | Level threshold    | Level naik tepat waktu                      |
| PR03 | EXP overflow       | Sisa EXP tidak hilang                       |
| PR04 | Multi-level-up     | Semua level yang dicapai terhitung          |
| PR05 | Max level          | Level tidak melewati batas                  |
| PR06 | Modifier source    | Tidak ada modifier ganda                    |
| PR07 | Stat recalculation | FinalStats konsisten                        |
| PR08 | MaxHealth policy   | HP tidak auto-heal                          |
| PR09 | Duplicate reward   | Reward kedua ditolak                        |
| PR10 | Souls transaction  | Pembelian mengurangi Souls sesuai biaya     |
| PR11 | Failed transaction | Tidak ada data yang berubah sebagian        |
| PR12 | Run reset          | EXP run di-reset, Souls tetap sesuai policy |

### **Acceptance Criteria**

Progression berjalan tanpa modifikasi langsung terhadap FinalStats dari berbagai sistem. Reward tidak bisa diklaim ganda, dan perubahan progression maupun permanent upgrade selalu melalui pemilik state yang tepat.

## **PHASE 04 — Save System & Run Lifecycle**

P0 · Konsistensi data

**Objective:** Menjamin Save/Load, Game Over, Retry, dan run baru tidak meninggalkan state lama.

### **Scripts**

```
Systems/
└── Save/
    ├── SaveSystem.cs
    ├── SaveData.cs
    ├── SaveValidator.cs
    ├── SaveMigration.cs
    └── ItemPersistenceCoordinator.cs

Systems/
└── Lifecycle/
    ├── RunLifecycleCoordinator.cs
    ├── RunContext.cs
    └── RunResetPolicy.cs
```

### **Tasks**

1. Tetapkan data permanen dan run data.
2. Pastikan hanya SaveSystem menulis file.
3. Integrasikan CurrencyWallet dengan SaveData.
4. Integrasikan PermanentUpgradeSystem.
5. Integrasikan Inventory dan Equipment persistence.
6. Pertahankan `PersistInventoryAndEquipment`.
7. Tambahkan SaveVersion.
8. Tambahkan validasi dan migration.
9. Tambahkan temporary file dan backup.
10. Implementasikan capture data sebelum Game Over selesai.
11. Implementasikan cleanup saat Retry.
12. Hubungkan pool cleanup dengan RunLifecycleCoordinator.
13. Reset EXP dan modifier sementara.
14. Reset encounter dan world state.
15. Tentukan kebijakan seed Retry.
16. Jalankan kembali generation dan NavMesh sebelum gameplay aktif.

### **Tests**

- Save dan Load.
- Save file tidak tersedia.
- Save file rusak.
- Item ID tidak dikenal.
- Permanent upgrade tetap terbaca.
- Inventory persistence sesuai flag.
- Run EXP tidak masuk data permanen.
- Retry tidak meninggalkan projectile/enemy aktif.
- Player muncul pada start anchor.
- Tidak ada event dari run lama.
- Save tidak dilakukan sebelum transaksi permanen berhasil.

### **Acceptance Criteria**

Pemain dapat menyelesaikan run, mengalami Game Over, melakukan Retry, serta memulai run baru tanpa kehilangan data permanen yang seharusnya disimpan atau membawa data sementara dari run sebelumnya.

## **PHASE 05 — Camera Feedback & Time Control Polish**

P0 · Combat presentation

**Objective:** Menyatukan feedback combat dan memastikan camera/hitstop tidak mengganggu gameplay.

### **Scripts**

```
Camera/
├── CameraEventManager.cs
├── CameraFeedbackConfigSO.cs
└── CinemachineImpulseAdapter.cs

Systems/
└── Feedback/
    └── CombatFeedbackRouter.cs

Systems/
└── Time/
    └── TimeEffectController.cs
```

Nama yang sudah tersedia harus dipertahankan jika implementasinya sudah mencakup kontrak tersebut.

### **Tasks**

1. Audit Cinemachine Brain dan Virtual Camera.
2. Pastikan Follow memakai CameraTarget.
3. Periksa damping dan update mode.
4. Pisahkan camera follow dan camera feedback.
5. Hubungkan CombatResolver ke CombatFeedbackRouter.
6. Hubungkan camera shake per AttackDefinition.
7. Buat preset feedback untuk hit, heavy hit, parry, damage, dan death.
8. Rapikan impulse source/receiver.
9. Implementasikan request-based hitstop.
10. Pastikan hitstop berjalan dengan unscaled timer.
11. Atur prioritas pause atas hitstop.
12. Pastikan satu sistem menjadi penulis Time.timeScale.
13. Reset efek saat Game Over, Retry, dan Scene Transition.
14. Uji beberapa feedback yang terjadi berdekatan.

### **Tests**

| ID   | Test             | Expected Result                        |
| ---- | ---------------- | -------------------------------------- |
| CF01 | Camera follow    | Mengikuti target dengan stabil         |
| CF02 | Dodge            | Tidak tertinggal secara berlebihan     |
| CF03 | Light hit        | Feedback ringan                        |
| CF04 | Heavy hit        | Feedback lebih kuat sesuai konfigurasi |
| CF05 | Perfect parry    | Feedback berbeda dan terbaca           |
| CF06 | Multiple hitstop | Tidak saling menghapus                 |
| CF07 | Hitstop + pause  | Pause tetap menjadi prioritas          |
| CF08 | Resume           | Time scale kembali benar               |
| CF09 | Game Over        | Tidak ada time scale tersangkut        |
| CF10 | Retry            | Camera feedback dan hitstop bersih     |
| CF11 | Room transition  | Target dan framing tetap valid         |

### **Acceptance Criteria**

Camera follow tetap stabil, feedback peristiwa terasa konsisten, dan tidak ada timeScale, impulse, atau camera state yang tertinggal setelah transisi.

## **PHASE 06 — System Diagnostics & Automated Testing**

P1 · Quality Assurance

**Objective:** Memastikan developer bisa melihat kondisi sistem tanpa harus menebak-nebak melalui gameplay.

### **Development Tools**

```
Diagnostics/
├── CoreSystemsDebugPanel.cs
├── PoolDebugPanel.cs
├── ProgressionDebugPanel.cs
├── EventDebugPanel.cs
├── CameraDebugPanel.cs
└── RunDebugPanel.cs
```

Panel ini hanya diperlukan di development/test environment. Jangan sampai menjadi dependency gameplay.

### **Tasks**

1. Tampilkan jumlah pool aktif dan inactive.
2. Tampilkan jumlah object rejected.
3. Tampilkan current EXP dan level.
4. Tampilkan jumlah Souls.
5. Tampilkan modifier SourceId yang aktif.
6. Tampilkan event terakhir.
7. Tampilkan current GameState dan RunId.
8. Tampilkan active hitstop request.
9. Tampilkan camera feedback request terakhir.
10. Tambahkan stress test pool.
11. Tambahkan progression test.
12. Tambahkan event lifecycle test.
13. Tambahkan Retry regression test.

### **Automated Test Groups**

```
Tests/
├── EditMode/
│   ├── PoolingDomainTests.cs
│   ├── ProgressionDomainTests.cs
│   ├── RewardTransactionTests.cs
│   ├── EventContractTests.cs
│   └── SaveDomainTests.cs
│
└── PlayMode/
    ├── PoolLifecycleTests.cs
    ├── RunLifecycleTests.cs
    ├── CombatFeedbackTests.cs
    ├── CameraIntegrationTests.cs
    └── SceneTransitionTests.cs
```

### **Acceptance Criteria**

Setiap core system memiliki cara untuk diuji secara terisolasi maupun saat terintegrasi.

## **PHASE 07 — Performance, Stress Testing & Profiling**

P1 · Optimization

**Objective:** Mengetahui apakah sistem benar-benar mengurangi biaya runtime dan mampu menghadapi penggunaan berulang.

### **Pool Stress Testing**

Uji kombinasi:

- Spawn enemy berulang.
- Projectile dalam jumlah tinggi.
- Banyak objek aktif bersamaan.
- Semua objek kembali ke pool.
- Retry berulang.
- Scene transition.
- Pool mencapai kapasitas maksimum.

Catat:

- Created count.
- Active count.
- Peak active.
- Rejected requests.
- GC Allocations.
- Spawn/Release time.

### **Progression Stress Testing**

Uji:

- Banyak transaksi reward.
- Multi-level-up.
- Pembelian upgrade berulang.
- Recalculate stats dengan banyak modifier.
- Save dan Load berulang.
- Item ID invalid.

### **Event Stress Testing**

Uji:

- Banyak event dalam waktu berdekatan.
- Subscription dan unsubscription berulang.
- Scene reload.
- Pool rent/release.
- Retry berkali-kali.

### **Camera Stress Testing**

Uji:

- Banyak hit dalam waktu singkat.
- Hitstop bertumpuk.
- Pause saat hitstop.
- Scene transition saat feedback berlangsung.
- Death feedback dan Retry.

**Performance tidak boleh dinilai hanya dari jumlah Instantiate yang berkurang.** Gunakan Unity Profiler untuk CPU, memory, physics, GC allocation, dan rendering. Pooling harus dibuktikan memberikan manfaat pada beban yang memang ditemukan.

### **Acceptance Criteria**

Tidak ada unbounded pool growth, event leak, peningkatan object count yang tidak terkendali, save corruption, atau kondisi time scale yang tidak pulih.

## **PHASE 08 — Core Systems Freeze & Release Gate**

Final Milestone

**Objective:** Membekukan arsitektur inti sebelum dipakai sebagai fondasi penambahan fitur berikutnya.

### **Final Release Gate**

- Pooling memakai lifecycle contract yang sama.
- Enemy dan Projectile dapat digunakan ulang tanpa state lama.
- Pool memiliki batas kapasitas.
- Double release terdeteksi.
- Reward tidak diberikan dua kali.
- EXP dan Souls terpisah.
- Progression terintegrasi dengan PlayerStatController.
- Permanent Upgrade menggunakan transaksi yang aman.
- FinalStats hanya dihitung oleh PlayerStatController.
- MaxHealth policy konsisten.
- Event tidak mengalami subscription leak.
- Event run lama tidak mengubah run baru.
- SaveSystem menjadi satu-satunya file writer.
- Retry membersihkan runtime state.
- Inventory persistence mengikuti flag.
- Camera follow stabil.
- Camera feedback mengikuti profile.
- Hitstop dan pause tidak saling bertabrakan.
- Tidak ada timeScale yang tersangkut.
- CombatTest berjalan.
- ForestRun berjalan.
- Hub berjalan.
- Windows build lolos pengujian.

### **Deliverables**

1. Core Systems Architecture v1.0.
2. Pooling System v1.0.
3. Progression & Reward System v1.0.
4. Event Architecture v1.0.
5. Camera & Time Feedback v1.0.
6. Save & Run Lifecycle v1.0.
7. Diagnostics & Test Suite.
8. Profiling Report.
9. Core Systems Release Candidate.

# **6. Urutan dependency dan strategi pengerjaan**

Jangan mengerjakan delapan fase sebagai delapan sistem terpisah yang baru digabung di akhir.

Aku merekomendasikan urutan berikut:

1. **Audit dan kontrak**

   PHASE 00–01

   Petakan kepemilikan state, service, lifecycle, dan event.
2. **Pooling reliability**

   PHASE 02

   Pastikan lifecycle objek dan reset state benar.
3. **Progression integration**

   PHASE 03

   Pastikan reward, EXP, Souls, dan modifier konsisten.
4. **Persistence dan lifecycle**

   PHASE 04

   Pastikan Save, Game Over, dan Retry aman.
5. **Camera dan time feedback**

   PHASE 05

   Pastikan presentasi combat tidak merusak state permainan.
6. **Diagnostics dan testing**

   PHASE 06–07

   Temukan masalah melalui pengujian dan profiling.
7. **Freeze**

   PHASE 08

   Bekukan kontrak dan dokumentasikan baseline.

Beberapa pekerjaan dapat dilakukan paralel setelah kontraknya disepakati. Contohnya, pooling dan camera feedback bisa dikerjakan oleh dua programmer berbeda. Namun, progression dan Save sebaiknya dikerjakan secara terkoordinasi karena keduanya menggunakan data permanen dan sementara yang sama.

## **7. Keputusan yang harus dikunci sebelum implementasi**

Ada enam keputusan yang menurutku perlu masuk ke dokumen *Engineering Decision Record*.

| No. | Keputusan          | Rekomendasi                                                              |
| --- | ------------------ | ------------------------------------------------------------------------ |
| 1   | Pool capacity      | Prewarm dan bounded expansion, tidak unlimited                           |
| 2   | Reward timing      | Pertahankan perilaku existing sampai aturan reward kematian/run disahkan |
| 3   | EXP progression    | Pertahankan level dan bonus HP existing dahulu                           |
| 4   | Souls persistence  | Simpan sebagai currency permanen sesuai aturan reward yang disahkan      |
| 5   | Retry seed         | Sediakan pilihan Same Seed / New Seed                                    |
| 6   | Event architecture | Local C# Events + satu global `GameEvents`                               |

Ada juga keputusan teknis yang sudah cukup kuat untuk dikunci:

- `PlayerStatController` tetap satu-satunya kalkulator FinalStats.
- `SaveSystem` menjadi satu-satunya penulis file save.
- `ObjectPoolManager` menjadi registry pool.
- `TimeEffectController` menjadi satu-satunya pengelola time scale.
- `CameraEventManager` tidak mengendalikan movement atau combat state.
- `RunLifecycleCoordinator` mengatur urutan lifecycle, bukan mengambil alih logika setiap domain.
- Scene tetap menyediakan seluruh service yang dibutuhkan melalui `SceneServices`.

## **8. Hal yang sengaja tidak perlu dibangun sekarang**

Agar milestone ini tidak berkembang menjadi framework terlalu besar, aku menyarankan menunda:

| Fitur                              | Alasan                                                   |
| ---------------------------------- | -------------------------------------------------------- |
| ECS / DOTS                         | Belum terbukti menjadi kebutuhan performa                |
| Addressables migration             | Belum perlu mengubah sistem asset loading                |
| Universal Event Bus Framework      | `GameEvents` dan C# Events sudah cukup                   |
| Generic Modifier Framework baru    | Sudah ada modifier architecture                          |
| Full Gameplay State Machine baru   | `GameManager` dan domain state sudah tersedia            |
| Advanced Camera Director           | Cinemachine dan CameraEventManager sudah menjadi fondasi |
| Network-ready pooling              | Tidak ada kebutuhan multiplayer                          |
| Roguelike random upgrade selection | Belum perlu memperluas desain progression                |
| Pooling seluruh GameObject         | Tidak semua objek membutuhkan reuse                      |

Kita sedang meningkatkan kualitas sistem, bukan menambah abstraksi demi abstraksi.

# **9. Langkah implementasi pertama**

Aku merekomendasikan mulai dengan membuat branch:

**Git branch**

`feature/core-systems-stabilization`

&#x20;Copy

**Dokumen engineering yang perlu dibuat**

1. `CoreSystemsArchitecture.md`
2. `CoreSystemsAudit.md`
3. `EventContracts.md`
4. `PoolingLifecycleSpecification.md`
5. `ProgressionAndPersistenceSpecification.md`
6. `CameraAndTimeFeedbackSpecification.md`
7. `CoreSystemsTestPlan.md`

**Urutan kerja awal yang konkret:**

1. Inventarisasi seluruh script dan dependency dari sistem inti.
2. Catat setiap pemilik state dan seluruh jalur perubahan datanya.
3. Petakan event yang dikirim, penerima, serta lifecycle subscription.
4. Telusuri semua jalur peminjaman dan pengembalian objek.
5. Petakan aliran EXP, Souls, modifier, dan save.
6. Dokumentasikan kontrak final sebelum mengubah kode.

Setelah audit, implementasikan Phase 01 lalu Phase 02. Jangan langsung mengubah `ObjectPoolManager` sebelum kita mengetahui seluruh pemanggil `Get`, `Release`, `Instantiate`, dan callback lifecycle yang digunakan.

**Catatan:** Dokumen yang tersedia memberikan gambaran arsitektur dan daftar implementasi, bukan seluruh source code C#. Karena itu, roadmap ini merupakan rancangan engineering target; belum merupakan klaim bahwa implementasi aktual sudah memenuhi seluruh kontrak di atas.

## **10. Definition of Done — Core Systems Stabilization v1.0**

Milestone ini dinyatakan selesai ketika:

1. Pooling dapat menangani lifecycle objek secara konsisten.
2. Tidak terdapat state lama ketika Enemy atau Projectile digunakan kembali.
3. Seluruh kapasitas pool mempunyai batas dan kebijakan overflow.
4. Reward tidak dapat diproses dua kali.
5. Run Progression terpisah dari Permanent Progression.
6. Semua modifier dihitung melalui `PlayerStatController`.
7. Event tidak mengalami kebocoran subscription atau memengaruhi run berikutnya.
8. Save/Load aman dan mempunyai versioning.
9. Retry membersihkan seluruh state sementara.
10. Camera feedback terpisah dari gameplay logic.
11. Hitstop dan Pause dapat berjalan tanpa konflik.
12. Seluruh sistem memiliki automated test dan regression test.
13. CombatTest, ForestRun, dan Hub tetap berfungsi.
14. Windows standalone build lulus release gate.

# **Rekomendasi akhir**

Menurutku, milestone ini merupakan salah satu pekerjaan engineering paling penting setelah seluruh gameplay foundation selesai.

Ada tiga hal yang paling menentukan kualitasnya:

**Pertama, Pooling harus memperlakukan GameObject sebagai instance yang mempunyai lifecycle**, bukan sekadar objek yang dimatikan dan dinyalakan.

**Kedua, Progression harus memiliki batas kepemilikan data yang tegas.** EXP, Souls, Equipment, Artifact, dan Permanent Upgrade tidak boleh saling mengambil alih perubahan statistik.

**Ketiga, Event, Camera, dan Time harus menjadi infrastruktur pendukung gameplay.** Combat menentukan hasil interaksi; sistem feedback menerjemahkan hasil tersebut menjadi respons visual dan waktu.

Targetnya bukan membuat `The Last Sasak` memiliki lebih banyak script. Targetnya adalah menjadikan semua script yang sudah kamu bangun bekerja sebagai satu sistem yang konsisten, dapat diuji, dapat di-debug, dan aman untuk terus dikembangkan.

**Milestone berikutnya yang harus benar-benar dikerjakan adalah PHASE 00 — Existing Core Systems Audit.** Dari sana, kita bisa melakukan refactoring berdasarkan masalah nyata tanpa merusak hasil pengerjaan sebelumnya.