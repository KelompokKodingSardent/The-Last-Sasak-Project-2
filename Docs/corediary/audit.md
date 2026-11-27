Berdasarkan dokumen proposal **Core Systems Stabilization & Polish v1.0** dan file `AUDIT_FOR_AI.txt`, eksekusi dimulai dari **PHASE 00 — Existing Core Systems Audit** sebelum menyentuh atau merefaktor kode produksi.

**Cakupan Data Audit (`AUDIT_FOR_AI.txt`):**

* **Environment & Package:** Unity `6000.3.23f1`, `com.unity.cinemachine 3.1.7`, `com.unity.ai.navigation 2.0.14`, `com.unity.inputsystem 1.20.0`, `com.unity.render-pipelines.universal 17.3.0`, dan `com.unity.test-framework 1.6.0`.
* **Inventaris File:** Seluruh struktur folder, scene (`MainMenu`, `CombatTest`, `ForestRun`, `Hub`, `Level_01`–`05`), dan daftar script tercatat lengkap, namun isi source code C# pada file dump terpotong secara alfabetis hingga pertengahan `EnemyBrain.cs` (script setelah huruf `E` akhir seperti `ObjectPoolManager.cs`, `ExperienceSystem.cs`, `CurrencyWallet.cs`, `SaveSystem.cs`, dan `RoomEncounterController.cs` tercatat path serta ukurannya, tetapi isi baris kodenya perlu dump lanjutan).



---

## 1. Eksekusi 12 Task Audit Teknis (PHASE 00)

1. **Audit `ObjectPoolManager`, `EnemyPool`, dan `ProjectilePool`:**
Ketiga script berada di `Assets\_Project\Scripts\Systems\Pooling\` (`ObjectPoolManager.cs` 3.990 bytes, `EnemyPool.cs` 594 bytes, `ProjectilePool.cs` 753 bytes), namun belum ditemukan `PoolCatalogSO`, `IPoolable`, `PooledObject`, maupun `PoolMetrics`. Selain itu, `Assets\_Project\ScriptableObjects\Items\Loot\LootDropController.cs` membangun sistem pooling terpisah menggunakan `Dictionary<LootPickup, Queue<LootPickup>>`.


2. **Pemeriksaan Konsistensi Lifecycle Reset Prefab:**
`LootPickup.cs` sudah memiliki `ResetPickupState()`, tetapi `EnemyBrain.cs` belum mengimplementasikan kontrak `IPoolable` dan masih mengandalkan `Start()` untuk `FindPlayer()` dan `ChangeState(EnemyBrainState.Idle)` yang tidak akan terpanggil ulang saat instance dirent dari pool.


3. **Audit Pengembalian Projectile dan Enemy ke Pool:**
`EnemyBrain.Bind(...)` menerima referensi `ProjectilePool` secara opsional melalui parameter overload `object arg2/arg3`, sedangkan `Projectile.cs` (4.352 bytes) dan `EnemyController.cs` (9.419 bytes) perlu diverifikasi isi kodenya pada dump batch kedua untuk memastikan jalur `Release` tidak memanggil `Destroy`.


4. **Audit `CurrencyWallet`, `ExperienceSystem`, dan `PlayerProgression`:**
Ketiganya berada di `Assets\_Project\Scripts\Systems\Progression\` (`CurrencyWallet.cs` 1.118 bytes, `ExperienceSystem.cs` 997 bytes, `PlayerProgression.cs` 1.022 bytes). Dari pemanggilan di `GameManager.cs`, `ExperienceSystem` diakses sebagai singleton (`ExperienceSystem.Instance?.ResetRun()`) dan memiliki adapter `SetRunLevelHealthBonus(float)` di `PlayerStatController.cs`.


5. **Hubungan `RewardCollector` dengan `Health` dan `RoomEncounterController`:**
Ditemukan **duplikasi file**: `Assets\_Project\ScriptableObjects\Items\Loot\RewardCollector.cs` (2.347 bytes) dan `Assets\_Project\Scripts\Systems\Progression\RewardCollector.cs` (746 bytes). Pada versi `Items\Loot\RewardCollector.cs`, distribusi reward bergantung pada metode manual `DistributeRewards(GameObject killerObject)` dengan guard `bool _rewardsDistributed` lokal, belum terhubung ke `GameEvents.OnEntityDied(Health)` maupun `LeaseId`.


6. **Audit `PlayerStatController` dan Semua Modifier Sources:**
`PlayerStatController.cs` sudah berjalan sesuai arsitektur target sebagai satu-satunya kalkulator `FinalStats` menggunakan `ModifierCalculator` dengan formula $(Base + \sum Additive) \times \prod (1 + Multiplicative)$ dan `SourceId` per item/upgrade. Namun, `Health.cs` belum mengimplementasikan `IHealthStatReceiver`, sehingga pembaruan `MaxHealth` dari `PlayerStatController` tidak tersampaikan ke `Health`.


7. **Implementasi `GameEvents` dan Seluruh Subscription:**
`GameEvents.cs` menggunakan static C# events (`OnGameStateChanged`, `OnPlayerDied`, `OnRoomCleared`, `OnRoomInstanceCleared`, `OnRoomEntered`, `OnEntityDied`, `OnCurrencyChanged`, `OnExperienceChanged`) dan sudah menyediakan `ClearAll()`, tetapi `ClearAll()` belum dipanggil saat inisialisasi runtime (riskan saat Domain Reload dimatikan di Unity 6).


8. **Audit `TimeEffectController`:**
`TimeEffectController.cs` menggunakan singleton berbasis single-boolean (`playing`) dan coroutine `Hitstop` yang menimpa `Time.timeScale = 0.04f`. Jika game di-pause saat hitstop aktif, selesainya coroutine hitstop akan memaksa `Time.timeScale` kembali ke `pausedScale` atau `1f`, berpotensi membatalkan status Pause secara tidak sengaja.


9. **Audit `CameraEventManager` dan Konfigurasi Cinemachine:**
Paket proyek menggunakan Cinemachine 3 (`Unity.Cinemachine`). `CinemachineRig.cs` hanya berisi helper statis `FindFirstObjectByType<CinemachineCamera>()`. `CameraEventManager` dipanggil secara statis dari `CombatResolver` (`TriggerCameraShake(1.2f)`) dan `SequenceManager` (`TriggerSwitchCamera`), sehingga keputusan gameplay/combat masih memanggil presentasi kamera secara langsung.


10. **Pemeriksaan `SaveSystem`, `ItemPersistenceCoordinator`, dan `PermanentUpgradeSystem`:**
Terdapat 7 script save di `Assets\_Project\Scripts\Systems\Save\` (`SaveSystem.cs`, `TLS_SaveData.cs`, `SaveValidator.cs`, `SaveVersionMigrator.cs`, `ItemPersistenceCoordinator.cs`, `InventorySaveData.cs`, `EquipmentSaveData.cs`) serta `PermanentUpgradeSystem.cs` di folder `Progression`. Isi internal ketujuh script save berada setelah titik potong `EnemyBrain.cs` sehingga wajib diaudit pada batch lanjutan.


11. **Audit Lifecycle pada `GameManager`, `MainMenu`, `CombatTest`, `ForestRun`, dan `Hub`:**
`GameManager.cs` menggunakan `DontDestroyOnLoad` dan transisi scene sinkron (`SceneManager.LoadScene`). Ditemukan bug kritis pada `GameManager.RestartRun()` yang selalu memanggil `StartGame()` (memuat `"CombatTest"`), sehingga pemain yang mati di `ForestRun` akan terlempar ke `CombatTest` saat menekan Retry.


12. **Pencatatan Penggunaan `Instantiate`, `Destroy`, `SetActive`, dan `Time.timeScale`:**
* **`Instantiate`:** `LootDropController.cs` (membuat `LootPickup` baru saat antrean kosong) dan `ArenaHazardZone.cs` (memanggil `Instantiate(customPrefab)` atau `GameObject.CreatePrimitive` setiap hazard muncul).
* **`Destroy`:** `ArenaHazardZone.cs` (`Destroy(gameObject)` saat durasi habis), `GameManager.cs`, `TimeEffectController.cs`, `CombatSystem.cs`, dan `SequenceManager.cs` (untuk pembersihan duplikat singleton).
* **`SetActive`:** `LootDropController.cs`, `LootPickup.cs`, `EquipmentVisualController.cs`, `GroundTelegraphView.cs`, dan `UIDialogueManager.cs`.
* **`Time.timeScale`:** Ditemukan pada `TimeEffectController.cs` (`0.04f`, `1f`, `pausedScale`).





---

## 2. Deliverables PHASE 00

### A. Core System Audit Report (Temuan Masalah Nyata)

| Severity | Domain | Temuan Spesifik di Codebase | Dampak |
| --- | --- | --- | --- |
| **P0 (Critical)** | Lifecycle / GameManager | `GameManager.RestartRun()` memanggil `StartGame()` yang hardcoded ke `CombatScene` (`"CombatTest"`), serta memanggil `SetState(GameState.InRun)` sebelum scene selesai dimuat | Retry di `ForestRun` salah memuat scene; state `InRun` aktif sebelum `Awake`/`Start` scene selesai

 |
| **P0 (Critical)** | Stats / Health | `Health.cs` tidak mengimplementasikan `IHealthStatReceiver`, dan `Health.SetMaxHealth` memakai default `healDifference = true` | Kenaikan Max HP dari level/equipment tidak memperbarui `Health`, atau melanggar kebijakan `KeepCurrentAndClamp`

 |
| **P0 (Critical)** | Progression / Loot | Duplikasi script dengan nama kelas identik: `RewardCollector.cs` (2 file), `LootDropController.cs` (2 file), `LootDefinitionSO.cs` (2 file), dan `ItemSwitching.cs` (2 file) | Risiko ambiguitas referensi inspector, salah pemanggilan sistem reward, dan konflik namespace

 |
| **P1 (High)** | Camera / Time | `TimeEffectController` memakai single timer tanpa antrean request dan mengembalikan `Time.timeScale` secara paksa setelah durasi selesai | Hitstop bertumpuk saling menimpa; pause bisa ter-unpause otomatis jika terjadi bersamaan dengan hitstop

 |
| **P1 (High)** | Enemy / Pooling | `EnemyBrain.OnEnable()` dan `EnemyBrain.Start()` keduanya memanggil `CombatDirector.Instance.RegisterEnemy(this)` | Registrasi ganda pada spawn pertama; state `Start()` tidak jalan ulang saat enemy dirent kembali dari pool

 |
| **P1 (High)** | Architecture | Tidak ada `SceneServices`, `RunLifecycleCoordinator`, `CombatFeedbackRouter`, `IPoolable`, maupun `PoolCatalogSO` di proyek | Service masih diakses via singleton statis (`Instance`) atau `FindFirstObjectByType`

 |
| **P2 (Medium)** | Items / Equipment | Di `EquipmentTransactionHandler.Awake()`, `artifactController.SetOwnershipQuery(inventory)` dipanggil sebelum null-check `GetComponent<Inventory>()` | Jika field `inventory` kosong di Inspector, validasi kepemilikan artefak terlewati (fallback ke `allowEquipWithoutInventoryInEditor`)

 |

### B. Existing Dependency Map

```
GameManager (Singleton, DontDestroyOnLoad)
 ├── memanggil -> ExperienceSystem.Instance?.ResetRun()
 ├── mendengarkan -> GameEvents.OnPlayerDied
 └── memuat scene -> CombatTest / ForestRun / Hub / MainMenu

CombatResolver (pada CombatSystem)
 ├── memanggil langsung -> CameraEventManager.TriggerCameraShake(1.2f)
 ├── memanggil langsung -> TimeEffectController.Play(0.03f / 0.07f)
 ├── memanggil -> PlayerStatController (ComputeOutgoingDamage / ComputeIncomingDamage)
 └── memanggil -> Health.TakeDamage() -> GameEvents.RaiseEntityDied(this)

PlayerStatController
 ├── mendengarkan -> EquipmentController.OnEquipmentChanged
 ├── mendengarkan -> ArtifactController.OnArtifactsChanged
 └── mencari -> IHealthStatReceiver (saat ini NULL karena Health belum implement)

EnemyBrain
 ├── memanggil -> CombatDirector.Instance.RegisterEnemy(this)
 ├── mencari -> GameObject.FindGameObjectWithTag("Player") di Start()
 └── mendengarkan -> Health.OnDied, StaggerComponent.OnStaggered, EnemyAttackController.OnAttackParried

```



### C. Pool Lifecycle Report

* **Existing Pool Scripts:** `ObjectPoolManager.cs`, `EnemyPool.cs`, `ProjectilePool.cs` sudah ada di `Scripts\Systems\Pooling\`, ditambah pool lokal internal di `Items\Loot\LootDropController.cs`.


* **Celah Lifecycle:**
* Belum ada `PoolLeaseId` untuk membatalkan callback/event lama dari instance enemy atau projectile yang sudah dikembalikan ke pool.
* `ArenaHazardZone` masih membuat objek runtime dengan `Instantiate` / `CreatePrimitive` dan menghancurkannya dengan `Destroy`.
* `EnemyBrain.BuildRuntimeFallbackPatterns()` membuat instance `ScriptableObject.CreateInstance<EnemyAttackPatternSO>()` setiap kali `Bind()` dipanggil jika list pattern kosong.





### D. Progression & Stats Ownership Map

| State / Data | Pemilik Aktual di Codebase | Status Kesesuaian Target |
| --- | --- | --- |
| Final player stats | `PlayerStatController` | **Sesuai** — terpusat melalui `ModifierCalculator` dengan `SourceId`

 |
| HP aktual & Max HP | `Health` | **Perlu Perbaikan** — belum terhubung ke `PlayerStatController` via `IHealthStatReceiver`

 |
| Kepemilikan item | `Inventory` / `InventoryController` | **Sesuai** — aturan non-duplikat equipment v1.0 sudah aktif

 |
| Equipment aktif | `EquipmentController` | **Sesuai** — terhubung ke `WeaponCombatAdapter` & `EquipmentVisualController`

 |
| Artifact aktif | `ArtifactController` | **Sebagian** — stat modifier aktif, tetapi hook pasif (`NotifyDodgeSuccess`, `NotifyParrySuccess`, `EvaluateContextualDamage`) masih kosong

 |
| EXP & Level run | `ExperienceSystem` & `PlayerProgression` | **Perlu Audit Lanjutan** — isi script terpotong; perlu verifikasi overflow & multi-level-up

 |
| Souls & Upgrade permanen | `CurrencyWallet` & `PermanentUpgradeSystem` | **Perlu Audit Lanjutan** — isi script terpotong; perlu verifikasi atomicity transaksi

 |

### E. Event Subscription Map

* **Global (`GameEvents`):**
* `OnPlayerDied` -> disubscribe oleh `GameManager` (`OnEnable`/`OnDisable`), dipanggil oleh `PlayerDeathHandler` (perlu verifikasi pada dump lanjutan).
* `OnEntityDied(Health)` -> dipanggil oleh `Health.TakeDamage` saat `currentHealth <= 0f`.
* `OnRoomCleared` / `OnRoomInstanceCleared` / `OnRoomEntered` -> digunakan oleh sistem ruangan (`RoomEncounterController` / `WorldDebugHUD`).
* `OnCurrencyChanged` & `OnExperienceChanged` -> digunakan oleh HUD/UI.


* **Local C# Events:**
* `Health.OnHealthChanged`, `Health.OnDied`
* `Inventory.OnItemAdded`, `Inventory.OnItemRemoved`, `Inventory.OnInventoryChanged`
* `EquipmentController.OnEquipmentChanged`, `OnWeaponSwitched`, `OnWeaponEquipped`, `OnWeaponUnequipped`, `OnArmorEquipped`, `OnArmorUnequipped`
* `ArtifactController.OnArtifactsChanged`, `OnArtifactEquipped`, `OnArtifactUnequipped`
* `PlayerStatController.OnStatsRecalculated`, `OnMaxHealthChanged`
* `UIDialogueManager.OnDialogueFinished` (static `Action`, riskan tertinggal jika scene berganti saat dialog aktif).





### F. Camera & Time Feedback Map

* **Komponen Terpasang:** `CinemachineRig.cs`, `CameraEventManager.cs`, `CameraManager.cs`, `TimeEffectController.cs`, `GroundTelegraphView.cs`.


* **Temuan Coupling:** `CombatResolver` memanggil `CameraEventManager.TriggerCameraShake(1.2f)` dan `TimeEffectController.Play(0.03f)` secara hardcoded, mengabaikan properti `HitstopDuration` dan `CameraShakeIntensity` yang sebenarnya sudah didefinisikan di dalam `AttackDefinitionSO.cs`.



### G. Save Ownership Map

* **File Terinventarisasi:** `SaveSystem.cs` (6.991 bytes), `TLS_SaveData.cs` (1.986 bytes), `SaveValidator.cs` (389 bytes), `SaveVersionMigrator.cs` (2.928 bytes), `ItemPersistenceCoordinator.cs` (9.078 bytes), `InventorySaveData.cs` (835 bytes), dan `EquipmentSaveData.cs` (1.078 bytes).


* **Status:** Struktur pemisahan `SaveValidator`, `SaveVersionMigrator`, dan `ItemPersistenceCoordinator` sudah ada di folder `Scripts\Systems\Save\`, namun isi kode pemanggilan `File.WriteAllText` dan integrasi dengan `CurrencyWallet` serta `PermanentUpgradeSystem` belum terlihat karena batas pemotongan dump.



### H. List of Refactoring Candidates (Prioritas Eksekusi)

1. **Resolusi Duplikasi File (Sebelum Phase 01):** Hapus atau gabungkan file duplikat (`Scripts\Systems\Progression\RewardCollector.cs` vs `Items\Loot\RewardCollector.cs`, `LootDropController.cs`, `LootDefinitionSO.cs`, `ItemSwitching.cs`, serta file kosong 0-byte `ModifierOperation.cs`).


2. **Perbaikan `GameManager` & Pembuatan `RunLifecycleCoordinator` (Phase 01 & 04):** Pisahkan manajemen `GameState` dari eksekusi `StartRun`, `FinalizeRun`, dan `ResetRun` (Retry harus mengingat scene run aktif, bukan hardcoded ke `CombatTest`).


3. **Kontrak `IHealthStatReceiver` pada `Health.cs` (Phase 03):** Hubungkan `Health` ke `PlayerStatController.OnMaxHealthChanged` dengan default `MaxHealthRecalculationPolicy.KeepCurrentAndClamp` agar kenaikan Max HP tidak melakukan auto-heal.


4. **Standardisasi `IPoolable` & `LeaseId` (Phase 02):** Terapkan `IPoolable`, `PoolCatalogSO`, dan `LeaseId` pada `ObjectPoolManager`, `EnemyPool`, `ProjectilePool`, serta migrasikan pool lokal `LootDropController` dan `ArenaHazardZone` ke registry pool terpusat.


5. **Pemisahan `CombatFeedbackRouter` & Request-Based Hitstop (Phase 05):** Arahkan feedback dari `CombatResolver` ke `CombatFeedbackRouter` dengan membaca nilai dari `AttackDefinitionSO`, serta ubah `TimeEffectController` menjadi antrean request berbasis `unscaledDeltaTime` yang menghormati state Pause.



---

## 3. Langkah Kerja Awal Konkret (Sesuai Section 9 Proposal)

Sebelum mengubah satu baris kode pun, jalankan urutan persiapan berikut:

1. **Buat Git Branch Isolasi:**
```bash
git checkout -b feature/core-systems-stabilization

```


2. **Lengkapi Sisa Dump Source Code PHASE 00 (Batch 2):**
Karena `AUDIT_FOR_AI.txt` terpotong di `EnemyBrain.cs`, jalankan ekstraksi khusus untuk sisa script inti yang belum terbaca isinya agar audit 100% terverifikasi sebelum Phase 01:
* `Assets\_Project\Scripts\Systems\Pooling\*` (`ObjectPoolManager.cs`, `EnemyPool.cs`, `ProjectilePool.cs`)
* `Assets\_Project\Scripts\Systems\Progression\*` (`CurrencyWallet.cs`, `ExperienceSystem.cs`, `PermanentUpgradeSystem.cs`, `PlayerProgression.cs`, `RewardCollector.cs`, `LootDropController.cs`)
* `Assets\_Project\Scripts\Systems\Save\*` (`SaveSystem.cs`, `TLS_SaveData.cs`, `SaveValidator.cs`, `SaveVersionMigrator.cs`, `ItemPersistenceCoordinator.cs`)
* `Assets\_Project\Scripts\Systems\CameraEventManager.cs` & `CameraManager.cs`
* `Assets\_Project\Scripts\Enemy\EnemySpawner.cs`, `EnemyController.cs`, `Weapon\Projectile.cs`, dan `World\Rooms\RoomEncounterController.cs`




3. **Susun 7 Dokumen Kontrak Engineering (di folder `Docs/`):**
* `Docs/CoreSystemsArchitecture.md`
* `Docs/CoreSystemsAudit.md` (berisi hasil temuan di atas + hasil Batch 2)
* `Docs/EventContracts.md`
* `Docs/PoolingLifecycleSpecification.md`
* `Docs/ProgressionAndPersistenceSpecification.md`
* `Docs/CameraAndTimeFeedbackSpecification.md`
* `Docs/CoreSystemsTestPlan.md`


4. **Masuk ke PHASE 01 (Fondasi Event & Service Lifecycle):**
Implementasikan `GameEvents.ClearAll()` pada `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`, buat `GameEventPayloads.cs` (`RunId`, `PoolLeaseId`), dan validasi service per scene (`MainMenu`, `CombatTest`, `ForestRun`, `Hub`).