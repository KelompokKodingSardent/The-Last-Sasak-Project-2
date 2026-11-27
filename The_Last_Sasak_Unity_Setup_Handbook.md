# The Last Sasak — Unity Setup & Script Usage Handbook

**Versi acuan:** Unity 6000.3.23f1  
**Sumber:** `UNITY PROJECT QUICK AUDIT`, dibuat 27 November 2026 19:54:56  
**Project root pada audit:** `E:\Test-Project\The-Last-Sasak-P3`  
**Tujuan dokumen:** panduan setup Inspector, ScriptableObject, GameObject, prefab, scene, fungsi script, dan validasi sistem.

---

## 0. Cara membaca panduan ini dan batas audit

Panduan ini dibuat dari source C# dan inventaris file yang muncul di audit. Artinya, panduan dapat menjelaskan:

- tipe ScriptableObject yang didefinisikan dan menu `Create`-nya;
- field yang harus diisi di Inspector;
- MonoBehaviour mana yang harus dipasang ke GameObject tertentu;
- method utama, tugas method, serta urutan pemanggilan runtime;
- dependensi antar-sistem dan masalah integrasi yang tampak dari source.

**Yang tidak bisa dibuktikan dari audit ini:** nilai field Inspector yang benar-benar tersimpan pada setiap scene/prefab, struktur child GameObject, object reference yang sudah di-drag, status prefab overrides, dan kondisi saat Play. Audit mencatat file `.unity`, `.prefab`, dan `.asset`, tetapi tidak memuat isi YAML/serialized object-nya. Jadi bagian setup di bawah adalah **wiring yang dituntut oleh source code**, bukan klaim bahwa semua wiring sudah benar-benar terpasang.

Sebelum mengubah project:

1. Buat commit Git atau salinan project.
2. Jangan hapus asset lama yang namanya generik seperti `New Weapon Definition SO.asset` sebelum membuka dan mengecek tipenya.
3. Jangan membuat ulang asset yang mungkin sudah dipakai scene/prefab. Periksa dulu object reference di Inspector.
4. Jangan mengubah `ItemId` setelah item pernah masuk save file. Save system menyimpan ID, bukan sekadar nama file asset.
5. Selesaikan setup dan uji satu domain dahulu (Player → Combat → Enemy → Loot/Inventory → World Generation → Save/UI).

### Ringkasan audit

- Unity Editor: **6000.3.23f1**.
- Package utama yang tercatat: Input System 1.20.0, Cinemachine 3.1.7, AI Navigation 2.0.14, URP 17.3.0, uGUI 2.0.0, Test Framework 1.6.0.
- Scene yang tercatat: `MainMenu`, `CombatTest`, `ForestRun`, `Hub`, `Level_01` sampai `Level_05`, `Level_Tes_Dialogue_Scene`, dan `Level_Tes_VFX`.
- Source project berisi sekitar 160 file/class section C# yang terdeteksi pada ringkasan audit.
- Ada **22 tipe ScriptableObject yang memiliki `[CreateAssetMenu]`**. Tidak semua tipe tersebut dipakai runtime saat ini.
- Inventory audit memuat asset enemy/attack, beberapa asset item/player, satu loot/reward group, dan cutscene. Saya tidak menemukan file asset bernama jelas untuk katalog room, definisi room, encounter pool, map config, atau pool catalog pada inventory yang ditampilkan. Buat/cek asset untuk sistem-sistem itu secara khusus.

---

# 1. Peta arsitektur: apa yang menjadi apa?

| Jenis | Contoh di project | Dibuat di mana? | Perannya |
|---|---|---|---|
| ScriptableObject | `EnemyDefinitionSO`, `WeaponDefinitionSO`, `MapGenerationConfigSO` | Project window → Create | Data asset yang dapat dipakai berulang oleh banyak object |
| MonoBehaviour | `PlayerMotor`, `EnemyBrain`, `Inventory` | Dipasang ke GameObject | Perilaku/runtime component |
| Prefab | Player, enemy, projectile, room | Project window → Prefab | Template GameObject beserta component dan child-nya |
| Scene object | `CombatSystem`, Player instance, Canvas | Hierarchy scene | Instance yang aktif dalam suatu scene |
| Plain C# class/struct | `InventoryModel`, `StatModifier`, `RoomNodeData` | Dibuat oleh script saat dibutuhkan | Data/helper internal; bukan komponen Inspector |
| Interface/enum/static class | `IDamageable`, `GameState`, `GameEvents` | Tidak dipasang | Kontrak, konstanta, event, atau definisi nilai |

**Aturan penting:** jangan mencoba menambahkan class biasa seperti `InventoryModel`, `StatModifier`, `DamageInfo`, `RoomNodeData`, `IDamageable`, atau `GameEvents` ke GameObject. Itu bukan MonoBehaviour. Sebaliknya, jangan membuat asset untuk `Inventory`, `PlayerMotor`, atau `EnemyBrain`; itu component.

### Alur data utama

**Player & Combat**  
`PlayerInputReader` → `PlayerActionController` → `AttackController` / `DodgeController` / `ParryController` → `CombatSystem` → `CombatResolver` / `ParryResolver` → `Health` / `StaggerComponent` → event feedback/UI.

**Equipment & Stats**  
`ItemCatalogSO` menyimpan referensi item → `Inventory` memegang item milik Player → `EquipmentTransactionHandler` mengatur transaksi equip/unequip → `EquipmentController` / `ArtifactController` menyimpan perlengkapan aktif → `PlayerStatController` menghitung stat akhir → `Health` menerima perubahan Max HP.

**Loot**  
Enemy mati → `RewardCollector` per-enemy → `LootDropController` → `LootPickup` → `LootCollector` Player → `Inventory` / `CurrencyWallet` / `ExperienceSystem`.

**Procedural World**  
`WorldGenerationCoordinator` → `MapGenerationConfigSO` → `RoomCatalogSO` + `RoomDefinitionSO` + `EncounterCompositionSO` → graph/placement/assembler → `RoomController` → `RoomEncounterController` → prefab enemy → `NavMesh`.

**Save**  
`SaveSystem` ⇄ `ItemPersistenceCoordinator` ⇄ `ItemCatalogSO` + Inventory/Equipment/Artifacts/Stats. Save data menggunakan ID item yang harus konsisten.

---

# 2. Persiapan Unity Editor sebelum setup gameplay

## 2.1 Package yang harus dicek

Buka **Window → Package Management → Package Manager** (atau Package Manager dari menu Window, bergantung layout Editor). Pastikan package berikut aktif/terpasang sesuai `Packages/manifest.json` audit:

| Package | Versi audit | Dipakai untuk |
|---|---:|---|
| Input System | 1.20.0 | Input keyboard, mouse, gamepad di `PlayerInputReader` |
| Cinemachine | 3.1.7 | Kamera follow/virtual camera dan impulse shake |
| AI Navigation | 2.0.14 | `NavMeshSurface`, NavMeshAgent, runtime bake |
| URP | 17.3.0 | Render pipeline |
| uGUI | 2.0.0 | Canvas, Button, Image, UI |
| Test Framework | 1.6.0 | Editor tests |
| Timeline | 1.8.13 | Tercatat sebagai dependency; source cutscene saat ini menggunakan sequence sendiri |

Jangan menambahkan package baru hanya karena namanya terlihat relevan. Source yang diaudit menggunakan namespace API package yang tercantum di atas.

## 2.2 Tags dan Layers

Buka **Edit → Project Settings → Tags and Layers**. Tambahkan tag dan layer dengan ejaan persis berikut.

**Tag:**

- `Player` — wajib. Dipakai oleh sistem yang mencari Player melalui tag: enemy AI, room entry/exit, cutscene trigger, loot pickup, dan diagnostics.
- `Collectible` — untuk jalur collectible lama pada `PlayerInteract`.

**Layers:**

- `Player`
- `Enemy`
- `Environment`
- `PlayerHitbox`
- `EnemyHitbox`
- `Projectile`
- `Collectible`
- `Interactable`

`GameplayLayers` mencari nama layer secara runtime. Bila nama layer tidak ada atau salah ketik, `LayerMask.NameToLayer` menghasilkan `-1` dan mask bisa menjadi kosong. Setelah dibuat, assign layer ke root/collider yang tepat. Layer berbeda dari Tag: sebuah object bisa memiliki tag `Player` sekaligus layer `Player`.

**LayerMask yang perlu dicek:**

- Player `AttackController.TargetMask` → layer `Enemy`.
- Enemy `EnemyAttackController.playerLayerMask` → layer `Player`.
- `EnemyDefinitionSO.ObstacleLayerMask` → layer tembok/obstacle yang harus menghalangi line-of-sight Ranger.
- `PlayerMotor` auto-jump/ground probe memakai `Environment` sesuai source; pastikan lantai/obstacle yang dimaksud berada pada layer tersebut.

## 2.3 Input System

`PlayerInputReader` membuat binding lewat kode. Audit menunjukkan kontrol default berikut:

| Aksi | Keyboard/Mouse | Gamepad |
|---|---|---|
| Gerak | WASD | Left stick |
| Attack | Mouse kiri / J | West button |
| Parry | Mouse kanan / K | North button |
| Dodge | Space / Left Shift | East button / Right shoulder |
| Interact | E | South button |
| Weapon slot 1/2 | 1 / 2 | D-pad kiri/kanan |
| Aim | Posisi cursor | Right stick |

Tidak perlu membuat `InputActionAsset` tambahan hanya untuk binding di atas kecuali desain input diubah. Pastikan **Active Input Handling** di Project Settings sesuai penggunaan Input System package (umumnya Input System Package atau Both). Jika input tidak merespons, cek Console dan PlayerInputReader sebelum membuat ulang binding.

## 2.4 Project window & prefab organization

Folder audit sudah memiliki pemisahan `Data`, `Prefabs`, `Scenes`, `ScriptableObjects`, `Scripts`, dan `Tests`. Pertahankan struktur ini. Saran pemisahan praktis:

- `Data/` — asset data yang sudah ada dari project lama/hasil iterasi.
- `ScriptableObjects/` — definisi `.cs` SO dan asset data terorganisasi berdasarkan domain.
- `Prefabs/` — prefab room, enemy, character, pickup, projectile, UI.
- `Scenes/` — scene runtime/test.
- `Scripts/` — MonoBehaviour/helper runtime.

Tidak wajib memindahkan `.asset` lama ke folder `ScriptableObjects` hanya supaya terlihat rapi. Yang penting referensi tetap utuh dan object tipe-nya benar.

---

# 3. Panduan 22 ScriptableObject

## Cara umum membuat dan mengisi SO

1. Tunggu Unity selesai compile. Tidak boleh ada compile error.
2. Di Project window, klik kanan folder tujuan.
3. Pilih **Create** lalu cari menu `TLS/...`, `The Last Sasak/...`, atau `Kasantara/...` sesuai tabel.
4. Beri nama asset yang konsisten, misalnya `SO_PlayerBaseStats_Default`, `SO_Enemy_Vanguard`, `SO_Attack_Vanguard_QuickSlash`.
5. Pilih asset. Isi field di Inspector.
6. Drag asset tersebut ke field component yang memakainya.
7. Jangan menganggap asset bekerja hanya karena berhasil dibuat. Ia harus direferensikan oleh component/runtime system.

## 3.1 Tabel menu Create

| No. | Tipe | Menu Create persis | Dipakai oleh |
|---:|---|---|---|
| 1 | `CutsceneSequence` | `Kasantara/Cutscene Sequence` | `CutsceneTrigger`, `SequenceManager` |
| 2 | `AttackDefinitionSO` | `TLS/Combat/Attack Definition` | `AttackController`, `WeaponDefinitionSO` |
| 3 | `HitstopConfigSO` | `TLS/Combat/Hitstop Config` | **Belum ada consumer runtime yang ditemukan** |
| 4 | `StaggerConfigSO` | `TLS/Combat/Stagger Config` | **Belum ada consumer runtime yang ditemukan** |
| 5 | `EnemyAttackPatternSO` | `TheLastSasak/Combat/Enemy Attack Pattern` | `EnemyDefinitionSO`, `EnemyBrain`, `EnemyAttackController` |
| 6 | `EnemyDefinitionSO` | `TLS/Enemy/Enemy Definition` | `EnemyBrain`, `RoomEncounterController` |
| 7 | `EnemyModifierSO` | `TLS/Enemy/Enemy Modifier` | `EnemyBrain`, room/run modifier injection |
| 8 | `ItemCatalogSO` | `The Last Sasak/Items/Item Catalog` | `ItemPersistenceCoordinator` |
| 9 | `ArmorDefinitionSO` | `The Last Sasak/Items/Armor Definition` | inventory/equipment/stat controller |
| 10 | `ArtifactDefinitionSO` | `The Last Sasak/Items/Artifact Definition` | inventory/`ArtifactController`/stats |
| 11 | `WeaponDefinitionSO` | `The Last Sasak/Items/Weapon Definition` | inventory/`EquipmentController`/attack chain |
| 12 | `GameplayArtifactSO` | `TLS/Roguelike/Gameplay Artifact` | **Belum ada consumer runtime aktif yang ditemukan** |
| 13 | `LootDefinitionSO` | `The Last Sasak/Loot/Loot Definition` | `LootDropController`, `LootPickup`, `LootCollector` |
| 14 | `RewardDefinitionSO` | `TLS/Items/Reward Definition` | **Belum ada consumer runtime yang ditemukan** |
| 15 | `PlayerMovementConfig` | `TLS/Player/Movement Config` | `PlayerMotor`, `PlayerFacingController`, `DodgeController` |
| 16 | `PlayerBaseStatsSO` | `The Last Sasak/Stats/Player Base Stats` | `PlayerStatController` |
| 17 | `EncounterCompositionSO` | `TLS/Roguelike/Encounter Composition` | `MapGenerationConfigSO`, `RoomDefinitionSO`, `RoomController`, encounter controller |
| 18 | `MapGenerationConfigSO` | `TLS/World/Map Generation Config` | `WorldGenerationCoordinator`, `RunMapGenerator` |
| 19 | `RoomCatalogSO` | `TLS/World/Room Catalog` | `MapGenerationConfigSO` |
| 20 | `RoomDefinitionSO` | `TLS/World/Room Definition` | `RoomCatalogSO` |
| 21 | `RoomModifierSO` | `TLS/Roguelike/Room Modifier` | `RoomController`, `RoomEncounterController` |
| 22 | `PoolCatalogSO` | `The Last Sasak/Systems/Pool Catalog` | `ObjectPoolManager` |

## 3.2 Combat assets

### `AttackDefinitionSO`

**Tujuan:** menyimpan satu definisi serangan Player atau weapon combo. Satu gerakan serangan = satu asset. Jangan satukan seluruh combo dalam satu asset.

Isi Inspector:

- `Damage`: damage dasar sebelum resolver/stat multiplier.
- `Startup`: waktu ancang-ancang sebelum hit aktif.
- `Active`: durasi hit window.
- `Recovery`: waktu pemulihan setelah hit.
- `Range`, `Radius`: jangkauan dan radius pencarian target.
- `Stagger`: nilai stagger yang dikirim bersama damage.
- `NextAttack`: serangan berikutnya untuk chain sederhana bila dipakai.
- `ComboWindowStartNormalized`: posisi pembukaan combo window dalam rentang animasi serangan (0–1).
- `CanCancelToDodge`, `CanCancelToParry`: izin cancel action.
- `ForwardLungeSpeed`, `ForwardLungeDuration`: dorongan maju ketika serangan berlangsung.
- `HitstopDuration`, `CameraShakeIntensity`: feedback feel.

**Cara setup:**

1. Buat asset untuk setiap serangan, misalnya QuickSlash, HeavySwing, DoubleSlash.
2. Atur timing dan damage secara terpisah.
3. Drag serangan dasar ke `AttackController.Definition`.
4. Jika serangan dipakai sebagai combo weapon, isi `WeaponDefinitionSO.ComboChain` sesuai urutan. `AttackController` lebih dahulu mencoba chain dari weapon aktif, lalu fallback ke loadout/Definition.
5. Uji combo satu per satu. Hindari `NextAttack` saling menunjuk hingga membentuk loop.

`CreateRuntime(name, damage, range)` hanya membuat instance runtime melalui `ScriptableObject.CreateInstance`; ini bukan cara membuat asset project yang persisten.

### `HitstopConfigSO` dan `StaggerConfigSO`

- `HitstopConfigSO.Duration` berisi durasi hitstop.
- `StaggerConfigSO.MaxStagger` berisi batas stagger.

**Catatan audit:** tidak ditemukan referensi consumer terhadap kedua tipe ini selain definisi class/menu. Runtime hitstop saat ini menggunakan `TimeEffectController` dan `AttackDefinitionSO.HitstopDuration`; stagger enemy dikendalikan `StaggerComponent`/data enemy. Jadi membuat asset dari dua tipe ini belum mengubah gameplay sampai diintegrasikan ke component yang membaca asset tersebut. Jangan mengira field ini otomatis terhubung.

## 3.3 Enemy assets

### `EnemyAttackPatternSO`

**Tujuan:** pola serangan milik satu archetype enemy. Asset ini dapat berisi satu atau beberapa tahap (`swings`).

Field tingkat pattern:

- `patternName`: nama untuk debugging/data.
- `selectionWeight`: peluang relatif dipilih dibanding pattern lain yang memenuhi syarat.
- `cooldown`: jeda sebelum pattern bisa dipakai lagi.
- `minTriggerRange`, `maxTriggerRange`: rentang jarak pemicu.
- `canBeParried`, `parryStaggerBonus`, `baseDamage`: aturan parry dan damage dasar.
- `swings`: daftar tahap serangan.
- `repositionAfterAttack`, `repositionDistance`: apakah enemy reposisi setelah pattern selesai.
- `minBossPhase`, `maxBossPhase`: rentang fase boss yang mengizinkan pattern.
- `chainFollowUpPattern`, `chainFollowUpChance`, `chainRequiredPhase`: kemungkinan melanjutkan ke pattern lain.

Field setiap `AttackSwingStep`:

- `deliveryType`: `Melee`, `Projectile`, `TargetedZone`, atau `SupportEmpower`.
- `telegraphDuration`, `startupDuration`, `executionDuration`, `recoveryDuration`: waktu tiap tahap dalam detik.
- `telegraphShape`: `Cone`, `Circle`, `Line`, `TargetedZone`.
- `range`, `angle`, `width`: ukuran area serangan/indikator.
- `damageMultiplier`, `staggerDamage`, `lungeDistance`, `lungeDuration`, `lockRotationOnStartup`.
- `projectileCount`, `projectileSpreadAngle`: parameter projectile.
- `zoneRadius`, `zonePersistDuration`, `zoneTickInterval`, `trackPlayerDuringTelegraph`, `customHazardPrefab`: zona/hazard.
- `empowerRadius`, `empowerDuration`, `empowerSpeedMultiplier`, `empowerDamageMultiplier`: support buff.

**Cara setup:**

1. Buat `EnemyAttackPatternSO`.
2. Tambahkan minimal satu elemen pada `Swings`.
3. Pilih Delivery Type dan isi field tahap yang relevan. Untuk Melee, jangan mengandalkan nilai projectile/hazard; untuk Projectile, siapkan projectile pool/prefab; untuk TargetedZone, pastikan jalur hazard tersedia; untuk SupportEmpower, isi radius/durasi/multiplier.
4. Masukkan asset pattern ke `EnemyDefinitionSO.AttackPatterns`.
5. Bila boss, isi `BossStages` dengan threshold HP atau jumlah stagger break, kemudian assign pattern transisi dan pattern unlock pada setiap stage.
6. Pastikan prefab enemy memiliki `EnemyAttackController` dan telegraph view. Pattern dengan telegraph tetapi tanpa tampilan visual masih dapat membingungkan player.

### `EnemyDefinitionSO`

**Tujuan:** satu profil archetype enemy (statistik, jarak ideal, taktik, pola serang, dan boss phase).

Field identitas/stat:

- `DisplayName`, `Archetype`, `MaxHealth`, `MoveSpeed`, `DetectRange`, `AttackRange`, `AttackDamage`, `AttackCooldown`, `ExpReward`, `SoulsReward`.
- `Attack`: field legacy `AttackDefinitionSO`; pola AI baru terutama memakai `AttackPatterns`.

Movement/position:

- `IdealCombatDistance`, `SpacingTolerance`, `StrafeSpeed`, `RepositionSpeed`, `RotationSpeed`.
- `StaggerPunishDuration`, `AttackDecisionDelay`.
- `PreferredZone`, `FlankAngle`, `AllowRearFlank`, `PlayerFacingAvoidAngle`, `FlankRepositionChance`.
- `CanSideDash`, `SideDashDistance`, `SideDashDuration`, `SideDashCooldown`, `SeparationRadius`, `SeparationStrength`.
- `MinSafeDistance`, `RequireLineOfSight`, `ObstacleLayerMask` (khusus perilaku ranged).
- `BaseStaggerResistance`, `HasFrontalGuard`, `FrontalGuardAngle` (khusus tank/bulwark).
- `IsEliteOrBoss`, `StaggerGrowthPerBreak`, `BossStages`, `AttackPatterns`.

**Cara setup:**

1. Buat satu asset per varian enemy.
2. Tentukan archetype sesuai role. Contoh: Vanguard untuk melee dasar, Skirmisher untuk flank/dash, Ranger untuk ranged, Bulwark untuk defense, Controller untuk kontrol/support, Boss untuk multi-stage.
3. Atur nilai dasar; nilai default bukan jaminan balance final.
4. Isi `AttackPatterns` dengan asset yang sudah dibuat.
5. Untuk Ranger, centang LOS hanya bila obstacle layer mask benar; atur jarak aman.
6. Untuk Bulwark, atur stagger resistance dan frontal guard.
7. Untuk Boss, centang `IsEliteOrBoss`, atur `BossStages`, dan pattern per fase.
8. Pada prefab enemy, drag asset ini ke `EnemyBrain.Definition`.

### `EnemyModifierSO`

**Tujuan:** modifier runtime/varian untuk mengubah ritme dan perilaku enemy.

- `MoveSpeedMultiplier`, `RepositionSpeedMultiplier`, `AttackCooldownMultiplier`, `DecisionDelayMultiplier`, `DamageMultiplier` mengalikan parameter utama.
- `BonusStaggerResistance`, `GrantFrontalGuard`, `GrantSideDash`, `BonusFlankRepositionChance`, `ForceRepositionAfterAttack`, `BonusChainChance` mengubah perilaku.
- `ExtraAttackPattern` menambahkan pattern opsional.

**Cara pakai:** buat asset Swift/Armored/Aggressive/Tactical/Empowered sesuai kebutuhan, lalu referensikan pada daftar modifier awal di `EnemyBrain` atau injeksikan lewat `RoomModifierSO`/`RoomEncounterController`. Jangan memakai modifier hanya dengan membuat asset; harus ada component yang memasukkannya ke enemy.

## 3.4 Item, equipment, dan catalog

### `ItemDefinitionSO` dan `EquipmentDefinitionSO` (base class)

Keduanya adalah class induk abstract, bukan tipe asset yang harus dibuat langsung. Child asset-nya adalah Weapon, Armor, Artifact. Field umum item:

- `ItemId`: ID persisten yang harus unik, contoh `weapon.klewang`, `armor.leather`, `artifact.ambet`. Huruf kecil dan stabil.
- `DisplayName`, `Description`, `Icon`, `Category`, `MaxStackSize`.
- `EquipmentDefinitionSO` juga menyediakan stat modifier umum.

**Jangan mengganti `ItemId` setelah item pernah tersimpan.** Filename boleh berubah, ID jangan.

### `WeaponDefinitionSO`

- `WeaponType`: kategori senjata.
- `ComboChain`: array serangan `AttackDefinitionSO` berurutan.
- `AnimatorOverride`: override controller animasi untuk weapon.
- `WeaponVisualId`: ID visual child yang dicari oleh visual controller.
- `WeaponDescription` dan `BaseModifiers`.

**Cara setup:**

1. Buat Weapon Definition.
2. Isi `ItemId` unik dan metadata umum (DisplayName/Description/Icon/MaxStackSize).
3. Isi `ComboChain` secara urut (contoh QuickSlash → HeavySwing → DoubleSlash).
4. Isi `AnimatorOverride` jika senjata memakai set animasi khusus.
5. Isi `WeaponVisualId` persis sama dengan ID visual pada `EquipmentVisualController` (case-sensitive/harus konsisten).
6. Isi statistik senjata di **`BaseModifiers`**. Catatan penting: method `GetScopedModifiers()` di weapon membaca list `baseModifiers`, bukan list `statModifiers` milik base class. Jadi jangan menaruh bonus stat hanya di inherited `StatModifiers` lalu berharap diterapkan.
7. Tambahkan asset ke `ItemCatalogSO` dan beri ke Inventory/Equipment sesuai skema awal.

### `ArmorDefinitionSO`

- `ArmorMaterial`: material yang diterapkan ke armor renderer bila menggunakan pipeline material.
- `ArmorVisualId`: ID child visual armor.
- `ArmorDescription`.
- `StatModifiers`: bonus/penalti statistik.

Isi `ArmorMaterial` atau `ArmorVisualId` minimal salah satu. Jika memakai visual ID, pastikan ID yang sama ada pada daftar visual armor di `EquipmentVisualController`. Tambahkan asset ke Item Catalog.

### `ArtifactDefinitionSO`

- `PassiveDescription` untuk penjelasan efek.
- `ArtifactVisual` sprite untuk UI/ikon visual artifact.
- `StatModifiers` untuk efek stat pasif.

Masukkan asset ke Item Catalog dan `ArtifactController`/inventory. Isi modifier agar artifact memiliki efek stat yang nyata. Artifact pasif di runtime saat ini menggunakan `ArtifactDefinitionSO`.

### `ItemCatalogSO`

**Tujuan:** registry master seluruh `ItemDefinitionSO` yang dapat disimpan/dipulihkan melalui ID.

Cara setup:

1. Buat satu asset `Item Catalog` utama.
2. Tambahkan seluruh Weapon, Armor, Artifact (dan item lain turunan `ItemDefinitionSO`) ke list `Items`.
3. Pastikan tidak ada `null`, `ItemId` kosong, atau duplikasi.
4. Buka menu konteks asset → **Validate Catalog**.
5. Assign catalog ke `ItemPersistenceCoordinator.ItemCatalog`.

Method:

- `OnEnable()` / `RebuildCache()` membangun dictionary lookup ID → item.
- `TryGetItem(id, out item)` mencari item berdasarkan ID.
- generic `TryGetItem<T>` melakukan lookup sekaligus memastikan subtype.
- `Contains(id)` memeriksa apakah ID tersedia.
- `ValidateCatalog()` memeriksa null, ID kosong, dan duplikasi.
- `OnValidate()` menjalankan validasi/cache rebuild saat asset diedit.

Jika item tidak ada dalam catalog, restore save dapat gagal menemukan definisi item yang disimpan.

## 3.5 Inventory, loot, dan reward

### `LootDefinitionSO`

Satu asset = satu jenis loot/reward.

- `lootId`: ID loot.
- `rewardType`: Item, Currency, atau Experience.
- `itemDefinition`: wajib untuk reward type Item.
- `amount`: jumlah default.
- `pickupPrefab`: prefab `LootPickup` yang ditampilkan di world.
- `pickupCondition`: `AutoOnTrigger` atau `ManualInteract`.

Cara setup:

1. Buat Loot Definition.
2. Isi `lootId` unik.
3. Pilih Reward Type.
4. Jika Item, assign Weapon/Armor/Artifact/ItemDefinition.
5. Set amount.
6. Assign prefab pickup yang memiliki `LootPickup` dan Collider trigger.
7. Pilih kondisi pickup.
8. Masukkan asset ke entry drop table `LootDropController` pada prefab enemy.

### `RewardDefinitionSO`

Hanya memuat `Exp` dan `SoulsCoin`. **Consumer runtime tidak ditemukan pada source audit.** Sistem reward aktif yang tampak di source memakai `LootDefinitionSO`, `RewardCollector` per enemy, `CurrencyWallet`, dan `ExperienceSystem`. Jadi jangan gunakan RewardDefinitionSO sebagai sumber drop utama sebelum ada script yang membacanya.

### `GameplayArtifactSO`

Menyimpan `ArtifactName`, `Playstyle`, dan beberapa parameter gaya gameplay (MaxHealthBonus, ParryBonusStagger, CounterWindowDuration, ComboResetTime, HealOnKill, HealOnRoomClear). **Consumer runtime aktif tidak ditemukan.** Sistem equip artifact yang berjalan menggunakan `ArtifactDefinitionSO` dan `ArtifactController`. Jangan tertukar dua tipe ini.

## 3.6 Player configuration dan stats

### `PlayerMovementConfig`

Field utama:

- `MoveSpeed`, `TurnSmoothTime`, `Gravity`.
- `StepOffset`, `SlopeLimit`, `AutoJumpHeight`.
- `AttackMoveSpeedMultiplier`, `ParryMoveSpeedMultiplier`.
- `DodgeDistance`, `DodgeDuration`, `DodgeIFrame`, `DodgeCooldown`.
- `AimSnapTurnTime` tercatat tetapi tidak ditemukan pemakaian runtime pada source yang diaudit.

Cara pakai:

1. Buat satu config standar.
2. Drag config yang sama ke `PlayerMotor.Config` dan `PlayerFacingController.Config`.
3. `DodgeController` mengambil config dari motor/di-bind lewat `BindConfig`.
4. Jika field null, `PlayerMotor` membuat fallback runtime config. Namun sebaiknya asset eksplisit dipasang agar tuning konsisten.

### `PlayerBaseStatsSO`

Field:

- `maxHealth`
- `attackDamage`
- `attackSpeed`
- `damageReduction`
- `poiseResistance`
- `knockbackResistance`
- `moveSpeed`
- `staminaRegen`

Buat asset default Player, lalu assign ke `PlayerStatController.BaseStatsAsset`. Controller menghitung stat final dari base stats + permanent upgrades + run progression + equipment + artifacts + temporary modifiers.

### `MaxHealthRecalculationPolicy`

Bukan SO. Ini enum/policy di `PlayerStatController`:

- `KeepCurrentAndClamp`: pertahankan current HP, lalu clamp bila melampaui Max HP baru.
- `PreservePercentage`: jaga rasio HP saat Max HP berubah.

Pilih berdasarkan desain healing/progression game.

## 3.7 Procedural room/world assets

Kelima asset ini saling bergantung. Urutan setup yang benar adalah **Room Prefab → RoomDefinitionSO → RoomCatalogSO → EncounterCompositionSO → MapGenerationConfigSO → WorldGenerationCoordinator**.

### `RoomDefinitionSO`

Satu asset mendeskripsikan satu prefab ruangan.

- `RoomID`: ID unik, stabil.
- `DisplayName`, `Biome`, `Role`, `LayoutArchetype`.
- `Prefab`: prefab ruangan yang sesuai.
- `RoomTags`: label seperti `open`, `ranged-friendly`, `reward`, dll.
- `OverrideEncounters`: opsional, jika room hanya boleh memakai encounter khusus.
- Rotation flags 90/180/270.
- `MinRunDepth`, `MaxRunDepth`, `GenerationWeight`, `MaxInstancesPerRun`.
- `footprintCells` dan `localConnectorDirections` adalah metadata cache yang disinkronkan dari prefab oleh `SyncFromPrefab`/`OnValidate`.

Setelah assign Prefab, cek cache footprint dan connector. Jika tidak berubah, pilih asset lalu jalankan `SyncFromPrefab` melalui Inspector/validasi ulang script. Jangan edit cache manual kecuali memang tahu sebabnya.

### `RoomCatalogSO`

- `TargetBiome`: biome catalog.
- `Rooms`: semua `RoomDefinitionSO` yang boleh dipilih generator.

Masukkan room definitions yang cocok dengan biome. Context menu **Validate All Rooms In Catalog** memeriksa RoomID dan prefab melalui `RoomPrefabValidator`. `GetValidCandidates` menyaring berdasarkan Role, biome, run depth, jumlah connector, dan batas instance. `PickWeightedRoom` memilih kandidat secara berbobot.

### `EncounterCompositionSO`

Asset ini mendefinisikan daftar archetype enemy untuk satu room encounter.

- `CompositionName`, `PresetType`, `Enemies`.
- `MinRunDepth`, `MaxRunDepth`, `SelectionWeight`.
- `AllowedLayouts`, `RequiredRoomTags`.
- `SpawnTagPreferences`: preferensi tag spawn point per archetype.

Preset yang tersedia: Basic (3 Vanguard), Pressure (2 Vanguard + Ranger), Flanking (2 Skirmisher + Vanguard), Heavy (Bulwark + 2 Vanguard), Control (Controller + 2 Skirmisher), Elite (Elite + Skirmisher + Ranger). Preset adalah titik awal; sesuaikan depth/compatibility/layout. Untuk Ranger, gunakan tag spawn point seperti `RangedPosition` dan tambahkan tag itu ke `EnemySpawnPoint` yang sesuai.

Method `IsCompatibleWith` memeriksa enemy list, depth, allowed layout, dan required room tags. `GetPreferredTagFor` mengembalikan spawn tag untuk archetype tertentu. `CreatePreset` menghasilkan runtime object; untuk asset permanen, buat lewat menu Create dan isi datanya di Inspector.

### `MapGenerationConfigSO`

- `ConfigName`, `Biome`, `GenerationVersion`.
- `RoomCatalog`: assign RoomCatalog yang benar.
- `EncounterPool`: isi encounter yang bisa muncul.
- `UseFixedSeed`, `FixedSeed`: gunakan fixed seed untuk reproduksi debugging; matikan untuk variasi random.
- `MainPathLength`, `TraversalRoomChance`.
- `MaxBranchCount`, `BranchSpawnProbability`, `MaxBranchLength`.
- `PrioritizeRewardOnDeadEnd`, `AllowEliteInRun`.
- `MaxPlacementAttempts`, `MaxBacktrackSteps`, `MaxGridRadius`.

`ResolveSeed()` memilih fixed seed atau seed runtime. `PickCompatibleEncounter()` memilih encounter yang cocok dengan room/depth berdasarkan weight. Assign config ke `WorldGenerationCoordinator.Config`.

### `RoomModifierSO`

Bagian yang saat ini didukung:

- `ApplyEnemyModifierToAll`
- `InjectedEnemyModifier`

Kedua field tersebut mengatur modifier enemy yang dimasukkan ke semua musuh di room. Field hazard (`SpawnPeriodicHazards`, `HazardInterval`, `HazardRadius`, `HazardDuration`, `HazardDamagePerTick`) dan enum `VolatileGround` **belum terbukti dipakai runtime** oleh source audit. Jangan jadikan hazard berkala sebagai fitur aktif sampai ada implementasi consumer.

## 3.8 Pooling asset

### `PoolCatalogSO`

Asset daftar prefab yang ingin dipool. Setiap entry memiliki:

- `Prefab`: prefab asli yang ingin dibuatkan pool.
- `PrewarmCount`: instance awal yang dibuat saat prewarm.
- `DefaultCapacity`: kapasitas awal container pool.
- `MaxSize`: batas object yang disimpan.

Cara setup:

1. Buat Pool Catalog.
2. Tambahkan prefab projectile/enemy/pickup yang benar-benar dipakai lewat pooling.
3. Set PrewarmCount secara wajar; jangan prewarm ratusan object tanpa kebutuhan.
4. Assign catalog ke `ObjectPoolManager.PoolCatalog`.
5. Tes `Get` dan `Release`; pastikan component `IPoolable` reset state dengan benar.

`OnValidate` memeriksa entry. Catalog bukan prefab dan tidak dipasang di Hierarchy.

---

# 4. Setup GameObject dan Prefab

## 4.1 Player prefab — hierarki yang disarankan

Contoh hierarki (nama object bebas, penempatan component mengikuti dependency):

```text
Player (Tag: Player, Layer: Player)
├── Model
│   ├── Animator
│   ├── Mesh / SkinnedMeshRenderer
│   └── WeaponSockets
│       ├── WeaponSlot_01
│       └── WeaponSlot_02
├── AttackPoint (optional Transform)
├── ArmorSocket (optional Renderer / visual children)
└── CameraTarget (optional Transform)
```

### Component pada root `Player`

Pasang component berikut sesuai fitur yang dipakai:

**Movement & input — wajib untuk controller baru**

- `CharacterController` — diwajibkan oleh `PlayerMotor`.
- `PlayerInputReader` — membaca input dan action buffer.
- `PlayerActionController` — status/izin action.
- `PlayerMotor` — gerakan fisik memakai CharacterController; assign `PlayerMovementConfig`.
- `PlayerFacingController` — camera-relative direction/aim; assign config yang sama.

**Combat**

- `AttackController` — assign `Definition`, `AttackPoint`, `TargetMask = Enemy`.
- `DodgeController` — mengambil movement config dari PlayerMotor.
- `ParryController` — durasi active/recovery/stagger; pastikan `ParryWindow` tersedia.
- `ParryWindow` — mengatur jendela parry pada Player.
- `HitboxController` — helper overlap hit untuk attack yang memakainya.
- `Health` — nyawa dan damage receiver.
- `InvulnerabilityHandler` — i-frame/invulnerability.
- `PlayerDeathHandler` — merespons kematian.

**Stats & equipment**

- `PlayerStatController` — assign `PlayerBaseStatsSO`.
- `Inventory` — capacity, duplicate equipment rule, starting items.
- `EquipmentController` — initial weapon slot 1/2, initial armor, visual config.
- `ArtifactController` — max artifact slots dan starting artifacts.
- `EquipmentTransactionHandler` — assign Inventory, EquipmentController, ArtifactController.
- `ItemSwitching` — input pergantian weapon slot.
- `LootCollector` — assign Inventory; currency/experience receiver component jika tidak auto-discover.
- Optional `EquipmentVisualController` untuk visual-ID pipeline.

**Animation/interact/progression**

- `PlayerAnimationController` — menghubungkan trigger/action ke Animator child.
- `PlayerInteract` — interaksi world; saat ini mencari component `Interactable`.
- Optional `PlayerProgression` jika level memberikan bonus HP.
- Optional `PlayerActionGateAdapter` hanya setelah gate artifact benar-benar diintegrasikan.

**Catatan:** sebagian component menggunakan `GetComponent` pada root untuk menemukan dependency. Karena itu jangan sembarang memindahkan `Health`, `PlayerActionController`, `PlayerMotor`, `AttackController`, `Inventory`, atau `EquipmentController` ke child object. Animator/Renderer boleh berada di child.

### Animator

`PlayerAnimationController` mencari parameter yang dikenal jika tersedia: `Speed`, `MoveX`, `MoveZ`, `Attack`, `AttackIndex`, `AttackSpeed`, `Dodge`, `Parry`, `ParrySuccess`, `Hit`, `Dead`. Parameter yang tidak ada akan dilewati, sehingga tidak selalu menghasilkan error tetapi animasi bisa tidak berjalan. Samakan parameter Animator Controller dengan nama yang dibaca script.

### PlayerMovementConfig dan PlayerBaseStatsSO

Jangan menaruh semua angka movement/stat di banyak component dengan nilai berbeda. Gunakan satu config movement yang di-share PlayerMotor + PlayerFacingController. Gunakan satu base stats asset untuk PlayerStatController. Weapon/armor/artifact menambahkan modifier dari asset masing-masing.

## 4.2 Enemy prefab — hierarki yang disarankan

```text
EnemyRoot (Layer: Enemy)
├── Model
│   └── Animator / Renderers
├── AttackOrigin (Transform)
├── TelegraphView (visual ground indicator)
└── Optional muzzle / projectile spawn points
```

Pada `EnemyRoot` pasang:

- `NavMeshAgent` (dibutuhkan oleh `EnemyMovement`).
- `EnemyBrain` (AI/state machine baru; isi Definition).
- `EnemyMovement`.
- `EnemyAttackController`.
- `Health`.
- `StaggerComponent`.
- Per-enemy `TheLastSasak.Items.Loot.RewardCollector` untuk Souls/XP/drop table.
- Optional `EnemyVisualFeedback` dan `PooledObject`.

`EnemyBrain` mensyaratkan `EnemyMovement`, `EnemyAttackController`, dan `Health`. Pastikan komponen ini pada root yang sama. Assign `EnemyBrain.Definition` ke asset `EnemyDefinitionSO` yang benar. Isi initial modifiers hanya bila dipakai.

Pada `EnemyAttackController` assign `AttackOrigin`, `GroundTelegraphView`/prefab view yang dipakai, dan Player LayerMask. Untuk ranged, siapkan projectile pool/prefab yang di-inject oleh encounter controller atau referensi yang diwajibkan source.

**Jangan memasang `EnemyController` legacy bersama `EnemyBrain`.** `EnemyController` mematikan dirinya jika `EnemyBrain` ditemukan. Memasang dua AI pada satu prefab hanya menambah kebingungan Inspector.

### Spawn points

Pada room prefab, buat child kosong dengan `EnemySpawnPoint`. Set archetype (jika dipakai oleh curated spawn) dan SpawnTags, contohnya `RangedPosition`. Letakkan titik Ranger di area yang punya line of sight, bukan tepat di depan pintu. Room encounter system memeriksa jarak aman dari player.

## 4.3 Pickup prefab

```text
LootPickupRoot (Layer: Collectible)
├── Visual
└── Collider (Is Trigger = true)
```

Tambahkan `LootPickup` pada root, assign `defaultLootDefinition` jika pickup diletakkan manual di scene. Untuk pickup hasil drop table, `LootDropController` akan menginisialisasi definition dan amount saat spawn.

Physics trigger memerlukan Collider yang benar dan setup Rigidbody sesuai konfigurasi physics Unity (setidaknya salah satu pihak yang berinteraksi harus memenuhi persyaratan trigger physics). Pastikan Player collider dan layer collision matrix mengizinkan interaksi.

- `AutoOnTrigger`: `LootPickup.OnTriggerEnter` mengecek Player tag lalu mencari `LootCollector`.
- `ManualInteract`: implementasi `ILootInteractable` tersedia, tetapi PlayerInteract saat ini mencari `Interactable`, bukan `ILootInteractable`. Jalur manual **belum terhubung langsung**. Perlu adapter atau perubahan PlayerInteract untuk mendukung interface tersebut.

## 4.4 Room prefab — format yang digunakan generator baru

Generator baru menggunakan `RoomFootprint` dan `RoomConnector` dengan cell size **12 world units**. Jangan mengandalkan catatan lama RoomModule yang menyebut 10x10; itu cabang generator legacy.

Pada root room prefab:

- `RoomController`
- `RoomFootprint`
- Collider(s) ruangan/entry trigger bila relevan
- child connector, spawn points, player spawn

Child penting:

- `PlayerSpawn`: titik dan arah spawn Player.
- `EnemySpawnPoint` (satu atau lebih): titik spawn musuh.
- `RoomConnector`: pada perimeter room, mengarah keluar sesuai arah connector.
- Pada setiap `RoomConnector`, isi `OpenFrame`, `ClosedWall`, optional `CombatGate` dengan object child yang sesuai.

Ketentuan yang perlu dijaga dari validator/source:

- Connector berada pada perimeter grid dan menghadap arah yang benar.
- Grid room baru menggunakan cell size 12; connector harus align dengan midpoint cell.
- Connector jangan ditaruh pada posisi Y berbeda dari level dasar (ground plane).
- Opening doorway dianjurkan minimal sekitar 5 unit (validator memberi warning untuk bukaan yang kecil; batas minimal source validator sekitar 4).
- Player spawn harus berada di dalam footprint.
- Spawn point musuh sebaiknya tidak terlalu dekat pintu (validator memeriksa clearance; rekomendasi aman minimal sekitar 2.5 unit).
- `RoomDefinitionSO.SyncFromPrefab()` membaca `RoomFootprint` dan `RoomConnector` dari prefab.

Sebelum memasukkan room ke catalog, pilih prefab di Project window dan jalankan **Tools/TLS menu → Validate Selected Room Prefabs** (menu source: `TLS/World/Validate Selected Room Prefabs`). Setelah itu jalankan `RoomCatalogSO` → **Validate All Rooms In Catalog**.

## 4.5 Projectile prefab

Tambahkan `Projectile` dan Collider trigger pada prefab projectile. Pastikan projectile layer `Projectile`, collider collision matrix benar, visual/trail terpasang. `ProjectilePool` menyediakan `Spawn(position, direction, owner, fromPlayer, damage)`. `Projectile` mendukung `IPoolable`, `Launch`, `OnTriggerEnter`, `OnParried`, dan `Despawn`; jadi state harus di-reset saat rent/return pool.

---

# 5. System GameObjects dan wiring dependency

## 5.1 CombatSystem root

Buat satu GameObject `CombatSystem` di gameplay scene. Pasang:

- `CombatSystem`
- `CombatResolver`
- `ParryResolver`

**Ketiganya pada GameObject yang sama**, karena source memakai `GetComponent` untuk menemukan resolver. Jangan membuat dua CombatSystem di satu scene.

Fungsi:

- `NextAttackId()` menghasilkan ID unik untuk satu kejadian serangan.
- `ApplyDamage(...)` menerima target berbentuk `IDamageable`, `Collider`, atau `GameObject` lalu meneruskan ke resolver.
- `CombatResolver.Resolve(...)` mengurutkan parry/damage, memeriksa stat, lalu menghasilkan `DamageResult`.
- `ParryResolver.TryResolve(...)` memeriksa apakah hit dapat di-parry dan apakah target sedang membuka parry window.
- `Health.TakeDamage(...)` menerima damage, memperbarui HP, dan memunculkan event kematian/HP.
- `Health.Heal(...)` menambah HP dengan batas Max HP.

## 5.2 TimeEffectController

Satu GameObject `TimeEffects` dengan `TimeEffectController`. Singleton ini menerima permintaan hitstop dan mengelola request aktif berdasarkan prioritas/time scale. Method yang terlihat: `Play`, `PlayRequest`, `Run`, `EnqueueHitstop`, `CancelAllHitstops`. `Update` memilih request terkuat lalu mengembalikan time scale setelah selesai. Ia juga mendengarkan state pause.

Jangan mengubah `Time.timeScale` dari beberapa script lain secara bersamaan tanpa koordinasi; bisa menyebabkan hitstop/pause saling menimpa.

## 5.3 Camera setup (Cinemachine 3)

Pada scene gameplay:

- Pasang `CameraManager` pada satu object system.
- Pastikan GameObject itu juga memiliki `CinemachineImpulseSource` (dipenuhi oleh `RequireComponent`).
- Isi list `Virtual Cameras` dengan ID unik dan object `CinemachineCamera` yang sesuai.
- Pastikan Main Camera memiliki Cinemachine Brain.
- Kamera yang ingin aktif diberi priority lebih tinggi oleh `CameraManager.SwitchCamera(id)`.

`CameraEventManager` adalah static event channel, bukan component. `CinemachineRig.FindFollow()` adalah helper static yang mencari CinemachineCamera; jangan menambahkannya sebagai component.

## 5.4 CombatDirector

Pasang satu `CombatDirector` pada scene combat. Ia mengatur attack budget/slot dan posisi taktis agar semua enemy tidak menyerang pada frame yang sama. `RegisterEnemy`/`UnregisterEnemy` mendaftarkan enemy; `TryRequestAttackSlot` meminta izin menyerang; `ReleaseAttackSlot` mengembalikan slot. Method aliases lama seperti `RequestAggro`/`ReleaseAggro` ada untuk kompatibilitas. Singleton satu saja.

## 5.5 ObjectPoolManager

Buat satu GameObject `ObjectPoolManager`, pasang component `ObjectPoolManager`, assign `PoolCatalogSO`. Isi catalog dengan prefab yang ingin dipool. `PrewarmFromCatalog` menyiapkan object awal; `Get(prefab, position, rotation)` mengambil instance; `Release(instance)` mengembalikan. `GetPoolMetrics()` untuk diagnostics.

- `PooledObject` biasanya ditambahkan/diikat oleh pool manager saat perlu; jangan menambahkan manual ke semua prefab kecuali memang dibutuhkan konfigurasi khusus.
- Component yang implement `IPoolable` harus reset state ketika `OnRentFromPool`/`OnReturnToPool` dipanggil.
- `EnemyPool` dan `ProjectilePool` adalah wrapper khusus pada beberapa sistem; jangan menganggap mereka asset SO.

## 5.6 GameManager dan singleton ekonomi/save

`GameManager` mengatur perpindahan scene dan state game. Default name field di source mengharapkan scene `MainMenu`, `Hub`, `CombatTest`, `ForestRun`; cek ejaan scene harus sama. Ia menggunakan singleton/persistensi lintas scene. Jangan menaruh duplikat permanen GameManager di setiap scene jika object lama sudah `DontDestroyOnLoad`.

Manager yang umumnya diperlukan untuk gameplay/progression:

- `CurrencyWallet` — saldo Souls; `Set`, `AddCurrency`/`Add`, `Spend`, `ResetRun`, save/load hooks.
- `ExperienceSystem` — EXP/level; `AddExperience`/`Add`, `ResetRun`.
- `PermanentUpgradeSystem` — pembelian upgrade permanen dan penerapan bonus health ke Player.
- `PlayerProgression` — mendengar event EXP berubah dan menerapkan bonus per-level.
- `SaveSystem` — `SaveGame`, `LoadGame`, `DeleteSaveFile`, `SaveNow`, `HasSaveFile`, event saved/loaded.
- `ItemPersistenceCoordinator` — snapshot/restore inventory, equipment, artifacts melalui catalog item.

Assign `ItemPersistenceCoordinator` field secara eksplisit: `ItemCatalog`, `Inventory`, `EquipmentController`, `ArtifactController`, `PlayerStatController`. `SaveSystem` mereferensikan coordinator. Pastikan hanya satu instance `SaveSystem`, `CurrencyWallet`, `ExperienceSystem`, dan `PermanentUpgradeSystem` aktif sesuai lifetime desain.

### Urutan save/load yang perlu dipahami

1. Saat save, `SaveSystem` meminta data dari saveable systems dan `ItemPersistenceCoordinator`.
2. Coordinator menyimpan item sebagai ItemId/quantity, weapon slot ID, armor ID, artifact ID.
3. Saat load, `ItemCatalogSO` mencari ID tersebut.
4. Inventory dan equipment dipulihkan, lalu stats dihitung kembali.

Jika ItemId diubah atau item tidak didaftarkan ke catalog, file save lama mungkin tidak dapat me-resolve item itu.

---

# 6. Scene setup per tujuan

## 6.1 `MainMenu`

Isi minimum:

- Canvas + EventSystem.
- Menu buttons.
- `MainMenuUI` pada controller/menu object.
- `GameManager` singleton harus sudah tersedia (di scene ini atau dipersistensikan dari bootstrap yang benar).

`MainMenuUI` menyediakan aksi `StartCombatTest`, `StartForestRun`, `OpenHub`, dan `QuitGame`. Hubungkan Button OnClick ke method yang sesuai. Pastikan nama scene terdaftar di **File → Build Profiles** (atau Build Settings sesuai layout editor) dan namanya cocok.

## 6.2 `CombatTest` — scene utama uji sistem statis

Gunakan untuk menguji Player, combat, enemy prefab, parry, loot, inventory, dan UI sebelum procedural generation.

Daftar object yang disarankan:

```text
CombatTest
├── GameManager (atau singleton persisten yang sudah ada)
├── CombatSystem (CombatSystem + CombatResolver + ParryResolver)
├── TimeEffects (TimeEffectController)
├── CameraSystem (CameraManager + CinemachineImpulseSource)
├── CombatDirector
├── ObjectPoolManager (PoolCatalogSO)
├── CurrencyWallet
├── ExperienceSystem
├── SaveSystem (+ ItemPersistenceCoordinator bila save item diuji)
├── Player
├── EnemyTestGroup
│   └── Enemy prefab instance(s)
├── LootTestGroup
├── Canvas / HUD / Inventory UI
└── Environment / Floor / Walls
```

Setup testing:

1. Pastikan Player root tag/layer benar dan semua required components terpasang.
2. Assign Player stats/config/attack assets.
3. Spawn enemy secara manual pada NavMesh yang valid.
4. Test attack, dodge, parry, damage, death.
5. Test kill reward, pickup, inventory, equip, stat recalculation.
6. Test save/load setelah ItemCatalog lengkap.
7. Jangan aktifkan `WorldGenerationCoordinator.GenerateOnStart` jika tujuan scene ini hanya tes combat statis.

## 6.3 `ForestRun` — procedural world / roguelike

Isi system scene:

```text
ForestRun
├── GameManager / singleton dependencies
├── CombatSystem
├── TimeEffects
├── CameraSystem
├── CombatDirector
├── ObjectPoolManager
├── CurrencyWallet / ExperienceSystem / SaveSystem
├── Player
├── WorldSystems
│   ├── WorldGenerationCoordinator
│   ├── RuntimeNavMeshBuilder + NavMeshSurface
│   ├── RoomEncounterController
│   └── RoomManager
├── MapContainer (Transform kosong)
└── Canvas / HUD
```

Pada `WorldGenerationCoordinator`:

- `Config` → assign `MapGenerationConfigSO`.
- `MapContainer` → assign transform kosong untuk hasil assembly; boleh dibuat otomatis oleh coordinator bila null.
- `NavMeshBuilder` → assign `RuntimeNavMeshBuilder`.
- `EncounterController` → assign `RoomEncounterController`.
- `PlayerTransform` → drag transform root Player.
- `GenerateOnStart` → true untuk generate otomatis saat scene dibuka; false bila ingin klik `GenerateWorld` dari debug workflow.

Pada `RuntimeNavMeshBuilder`:

- Pastikan `NavMeshSurface` ada di object yang sama (RequireComponent).
- Untuk world prosedural, set `BakeOnStart = false` agar tidak bake scene kosong sebelum room dirakit. Coordinator kemudian melakukan bake setelah assembly.
- Untuk scene statis, `BakeOnStart = true` boleh dipakai.

Pada `RoomEncounterController` assign prefab untuk archetype: Melee/Vanguard, Skirmisher, Ranged/Ranger, Elite, Controller, Boss, dan Player transform. Assign pool wrapper jika memakai pooling.

**Penting:** config ini hanya akan bekerja jika Room Catalog memiliki room prefab yang valid; setiap RoomDefinitionSO punya prefab; prefab punya connector, footprint, spawn point, dan player spawn yang cocok.

## 6.4 `Hub`

Isi Hub disesuaikan apakah Hub menampilkan Player/merchant/equipment UI. Minimal:

- GameManager/singleton dependency tanpa duplikasi.
- CurrencyWallet/ExperienceSystem/PermanentUpgradeSystem/SaveSystem dengan lifetime yang benar.
- Canvas UI, equipment/inventory UI jika dipakai.
- Interactable NPC/object untuk upgrade/merchant bila sistem interaksi sudah dihubungkan.

Jika SaveSystem atau wallet telah hidup pada object `DontDestroyOnLoad`, jangan membuat object kedua hanya karena pindah ke Hub. Sebaliknya, kalau mereka bukan persistent, pastikan ada satu instance aktif pada scene Hub.

## 6.5 `Level_01`–`Level_05` dan test scenes

Nama file saja tidak cukup untuk mengetahui apakah level-level ini memakai pipeline lama atau baru. Buka scene dan lihat component aktual. Untuk level statis, gunakan `RuntimeNavMeshBuilder` dengan bake-on-start bila perlu. Untuk pipeline prosedural baru, jangan menaruh generator grid lama dan `WorldGenerationCoordinator` aktif bersamaan tanpa tujuan migrasi yang jelas.

---

# 7. Referensi semua script: komponen, fungsi, dan cara pakai

Bagian ini membedakan **component yang ditempel ke GameObject** dari helper/data script. Method yang ditulis merupakan fungsi yang ditemukan pada source audit. Method bertanda “internal” biasanya dipanggil oleh Unity lifecycle/event/coroutine atau komponen lain; tidak perlu dipanggil manual dari Inspector.

## 7.1 Cutscene & dialogue

| Script | Pasang di | Fungsi dan pemakaian |
|---|---|---|
| `CutsceneAction.cs` | Tidak dipasang | Serializable action data. `CutsceneActionType`, `VNSlot`, `VNActorCommand` mendefinisikan jenis aksi/slot/aktor. Isi data ini di list `CutsceneSequence`. |
| `CutsceneSequence.cs` | Tidak dipasang | SO berisi `sequenceSteps`; buat asset lalu isi urutan dialogue, actor, camera, dan wait actions. |
| `CutsceneTrigger.cs` | Trigger volume | `OnTriggerEnter` memulai sequence saat Player memasuki BoxCollider trigger. Assign `cutsceneToPlay`; `playOnlyOnce` membatasi trigger sekali. |
| `SequenceManager.cs` | Satu object manager di scene | `StartCutscene` menerima sequence; `RunSequence` menjalankan tiap step dan mengirim dialogue/camera/action; `ResumeFromDialogue` melanjutkan setelah dialogue selesai. `Awake` singleton; `OnEnable/OnDisable` subscribe event. |
| `UIDialogueManager.cs` | Dialogue Canvas root | `PlayDialogue` mulai dialogue, `HideDialogue` menutup UI, `UpdateVNSprites` mengatur actor slots, `TypeTextCinematic`/`CompleteDialogueText` mengetik/menyelesaikan teks, fade dan sprite animation helper menjalankan transisi. Assign CanvasGroup, speaker/text, left/center/right Image, AudioSource, typing clip. |

**Bug integrasi cutscene:** `SequenceManager` memakai `PlayerMovement.SetMovementState(false)` untuk mengunci gerak. Tetapi `PlayerMovement.Update()` berhenti ketika `PlayerMotor` ada; controller aktif saat ini adalah PlayerMotor + PlayerInputReader. Jadi lock cutscene tidak otomatis mengunci controller baru. Integrasikan ke `PlayerMotor.CanMove` dan/atau input/action gate sebelum mengandalkan cutscene saat gameplay.

## 7.2 ScriptableObjects / items / inventory / loot

| Script | Pasang di | Fungsi dan pemakaian |
|---|---|---|
| `ArtifactController.cs` | Player root | `TryEquipArtifact`, `TryEquipArtifactAuto`, `TryUnequipArtifact`, `ForceUnequipArtifact` mengatur slot. `GetActiveArtifacts`/`GetArtifactModifiers` dibaca stats. `RestoreArtifactState` dipakai save load. `NotifyDodgeSuccess`/`NotifyParrySuccess`/`EvaluateContextualDamage` menjadi hook efek kontekstual bila ada logic yang memanggilnya. Assign startingArtifacts dan max slots. |
| `ItemCatalogSO.cs` | Tidak dipasang | Master registry item. `RebuildCache`, `TryGetItem`, `Contains`, `ValidateCatalog`. Assign ke ItemPersistenceCoordinator. |
| `ItemDefinitionSO.cs` | Base class | Base item abstract: ID, name, description, icon, category, stack. `OnValidate` menormalisasi/menjaga data. Buat subtype. |
| `EquipmentDefinitionSO.cs` | Base class | Base untuk equipment yang memiliki stat modifiers; tidak dibuat sendiri karena abstract. |
| `WeaponDefinitionSO.cs` | SO asset | Isi combo chain, animator override, visual ID, base modifiers. `GetScopedModifiers` mengikat modifier ke sumber ItemId; `OnValidate` menjaga kategori dan warning setup. |
| `ArmorDefinitionSO.cs` | SO asset | Isi armor visual/material dan stat modifiers. `GetScopedModifiers` mengeluarkan stat modifier bersumber pada ID armor; `OnValidate` menetapkan category dan memeriksa visual. |
| `ArtifactDefinitionSO.cs` | SO asset | Isi deskripsi, visual, stat modifiers. `GetScopedModifiers` menyediakan modifier ke PlayerStatController; `OnValidate` menetapkan category. |
| `ItemCategory.cs` | Tidak dipasang | Enum kategori item untuk Weapon/Armor/Artifact dan filter UI; bukan component. |
| `EquipmentContracts.cs` | Tidak dipasang | Interface contract untuk item/equipment. Dipakai oleh controller/adapter agar tidak tergantung concrete class. |
| `EquipmentTransactionHandler.cs` | Player root | `TryEquipFromInventory` transaksi equip; `TryUnequipWeaponSlot`/`TryUnequipArmor` unequip; `IsItemEquipped` memeriksa status; `HandleInventoryItemRemoved` menjaga equipment tidak merujuk item yang sudah hilang. Assign Inventory + EquipmentController + ArtifactController. |
| `EquipmentController.cs` | Player root | Runtime loadout utama. Isi `initialWeaponSlot1/2`, `initialArmor`, simple `weaponSlot1Visual/2Visual`, dan `armorRenderer` bila memakai jalur visual sederhana. `TryEquipWeapon`, `TryUnequipWeapon`, `TrySwitchWeaponSlot`, `TryToggleWeaponSlot`, `TryEquipArmor`, `TryUnequipArmor` mengubah equipment; `ForceUnequip*` melewati gate untuk cleanup; `RestoreEquipmentState` dipanggil oleh save restore. `RefreshPresentationAndStats` memperbarui visual/Animator dan meminta PlayerStatController menghitung ulang. Menemukan PlayerStatController, PlayerActionController, Animator melalui `GetComponent`/`GetComponentInChildren`, jadi tempatkan pada Player root. Field `loadout` adalah fallback lama; senjata aktif mengisi loadout dari ComboChain. Pilih simple visuals **atau** EquipmentVisualController sebagai pipeline visual utama agar tidak menampilkan dua mesh ganda. |
| `EquipmentVisualController.cs` | Player root atau visual rig | Subscribe event equipment lalu `RefreshAllVisuals`, `RefreshWeaponVisuals`, `RefreshArmorVisuals`, `ActivateMatchingVisual` mengaktifkan mesh/renderer berdasarkan visual ID. Isi weaponSlot roots, daftar visual, armor visuals. |
| `ItemSwitching.cs` | Player root | Input callback `OnSwitchSlot1/2/Toggle` memanggil `SwitchToSlot1`, `SwitchToSlot2`, `ToggleWeaponSlot`. Pastikan PlayerInputReader/action binding ada dan EquipmentController ditemukan. |
| `IWeaponCombatReceiver.cs` | Tidak dipasang | Interface receiver untuk data weapon ke combat. Bukan component. |
| `PlayerActionGateAdapter.cs` | Player root jika digunakan | Adapter izin switch/equipment. Audit menunjukkan method gate pada adapter saat ini mengembalikan true secara default dan koneksi langsung PlayerActionController belum aktif; jangan andalkan sebagai larangan equip selama attack/dodge. |
| `PlayerEquipmentState.cs` | Tidak dipasang | Serializable runtime state (weapon slots, active slot, armor); bukan MonoBehaviour/SO. |
| `WeaponCombatAdapter.cs` | Opsional Player root | Subscribe weapon switched dan sinkronkan animator/combat receiver. **Integrasi saat ini tidak lengkap:** `AttackController` tidak mengimplementasikan `IWeaponCombatReceiver`, jadi adapter tidak akan mengirim data serangan ke AttackController melalui interface tersebut. EquipmentController tetap membaca ActiveWeapon.ComboChain secara langsung. |
| `GameplayArtifactSO.cs` | SO asset (belum aktif) | Menyimpan data gaya roguelike, tapi tidak ditemukan consumer runtime. Jangan tertukar dengan ArtifactDefinitionSO. |
| `Inventory.cs` | Player root | Komponen facade inventory. `TryAddItem`, `TryRemoveItem`, `HasItem`, `FindItem`, `GetItemCount`, `GetSlots`; event `OnItemAdded/Removed/Changed`. Set capacity dan startingItems. |
| `InventoryController.cs` | Tidak dipasang | Pure C# controller transaksi inventory. `CanAddItem`, `TryAddItem`, `TryRemoveItem`, `FindItem`, `HasItem`, `GetSlots`, `ClearInventory`; dibuat internal oleh Inventory. |
| `InventoryItem.cs` | Tidak dipasang | Struct definisi + quantity; snapshot satu stack item. |
| `InventoryModel.cs` | Tidak dipasang | Model data inventory. Menghitung used/empty slots, item count, lookup slot, max addable amount, clear. |
| `InventorySlot.cs` | Tidak dipasang | Slot data. `Set`, `AddQuantity`, `RemoveQuantity`, `Clear`, `ToItemSnapshot`. |
| `LootCollector.cs` | Player root | `TryCollectLoot` mengarahkan item/currency/XP; `CollectItemReward`, `CollectCurrencyReward`, `CollectExperienceReward` memproses tiap tipe. Assign Inventory dan receiver currency/experience jika diperlukan. |
| `LootDefinitionSO.cs` | SO asset | Data loot. `GetDisplayLabel` menghasilkan label UI; `OnValidate` memvalidasi field/ID. |
| `LootDropController.cs` | Enemy prefab atau loot system owner | `RollAndSpawnDrops` melakukan roll tabel; `SpawnLoot` membuat pickup; `ReturnPickupToPool` mengembalikan pickup; `GetOrCreateFromPool` menyediakan instance; `ComputeScatteredPosition` menyebar posisi drop. Isi drop table, prefab, min/max amount, scatter. |
| `LootPickup.cs` | Pickup prefab | `Initialize` set data/amount/pool owner; `ResetPickupState` membersihkan state; `OnTriggerEnter` menangani AutoOnTrigger; `GetInteractPrompt`, `CanInteract`, `Interact` implement interface interaksi manual; `TryCollectBy` meminta LootCollector memproses loot; `DespawnOrReturnToPool` menutup pickup. Manual path tidak tersambung ke PlayerInteract saat ini. |
| `LootRewardContracts.cs` | Tidak dipasang | Enum reward/pickup condition dan interface currency/XP/manual loot. |
| `ScriptableObjects/Items/Loot/RewardCollector.cs` | Enemy prefab | Per-enemy reward. `DistributeRewards` sekali saat enemy mati memberi direct Souls/XP dan drop list melalui LootDropController; `ResetRewardState` untuk pooling. |
| `IStatModifier.cs` | Tidak dipasang | Interface stat modifier untuk calculator. |
| `ModifierCalculator.cs` | Tidak dipasang | `CalculateStat` dan `CalculateAllStats` menggabungkan base value dan modifier. Operasi additive, multiplicative, override; `Clamp` menjaga bound stat. |
| `StatModifier.cs` | Tidak dipasang | Struct berisi StatType, Operation, Value, SourceId; `WithSource` menambahkan ID sumber. Isi di list modifier pada item/upgrades. |
| `StatType.cs` | Tidak dipasang | Enum stat dan enum operasi; bukan component. |
| `RewardDefinitionSO.cs` | SO asset (belum aktif) | Data Exp/SoulsCoin; tidak ditemukan pemakai runtime. Gunakan LootDefinitionSO untuk drop aktif sekarang. |

### Cara kerja stat modifier

- `Additive`: ditambahkan ke nilai dasar.
- `Multiplicative`: multiplier relatif; nilai `0.15` berarti faktor `1.15` (naik 15%) bila rumus source memakai `(1 + value)`.
- `Override`: mengganti nilai hasil sebelumnya.
- `SourceId`: asal modifier; gunakan ItemId untuk item agar temporary removal/save/debug tetap jelas.

`PlayerStatController` menggabungkan base stats, permanent upgrades, progression run, weapon, armor, artifact, dan temporary effects. Gunakan `GetStat` untuk hasil akhir dan `GetBaseStat` untuk nilai awal.

## 7.3 Player, combat, health, movement

| Script | Pasang di | Fungsi dan pemakaian |
|---|---|---|
| `AttackController.cs` | Player root | `TryAttack` mulai attack bila state mengizinkan; `RefreshComboChainSource` mencari combo dari weapon/loadout/Definition; `GetComboStage`/`GetNextComboAttack` memilih stage; `AttackRoutine` menjalankan startup-active-recovery; `UpdateComboWindow` mengatur buffer; `TryExecuteBufferedCombo` lanjut combo; `CancelAttack` membersihkan attack; `ResolveHits` melakukan overlap dan damage. Assign Definition, AttackPoint, TargetMask. |
| `DodgeController.cs` | Player root | `TryDodge` memulai dodge bila action gate/cooldown mengizinkan; `DodgeRoutine` menjalankan perpindahan/durasi/i-frame; `BindConfig` menerima PlayerMovementConfig; `Update` menjaga timing. |
| `ParryController.cs` | Player root | `TryParry` mulai parry; `ParryRoutine` membuka active window lalu recovery; `OnParrySuccess` memberi respons setelah parry sukses. Assign nilai ActiveDuration, Recovery, BaseParryStagger. |
| `PlayerActionController.cs` | Player root | Mengatur `PlayerActionState`; `CanEnter` memeriksa transisi action; `CanSwitchWeapon` memeriksa apakah pergantian weapon boleh; `SetState` mengubah state; `HandleDeath` memblokir action. |
| `InvulnerabilityHandler.cs` | Player root | `Activate(duration)` mengaktifkan i-frame; `Clear` mematikan; `Update` menghitung expiry. Dipanggil Dodge/Health flow. |
| `PlayerDeathHandler.cs` | Player root | Subscribe event Health; `Handle` merespons death state/game over. |
| `PlayerInputReader.cs` | Player root | Baca gerak/aim/attack/parry/dodge/interact/switch input. `BufferAction`, `HasBufferedAction`, `TryConsumeBufferedAction`, `ClearBuffer` mengatur action buffer. Tidak perlu diisi InputActionAsset berdasarkan implementasi audit. |
| `PlayerFacingController.cs` | Player root | `GetCameraRelativeMove` mengubah input jadi arah relatif kamera; `GetAimDirection` mengambil aim; `SnapToAimDirection`/`FaceDirection` mengatur arah karakter; `SetRotationLocked` mengunci orientasi saat action tertentu. Assign movement config dan pastikan camera reference ditemukan. |
| `PlayerMotor.cs` | Player root + CharacterController | `ApplyControllerSettings` mengatur CharacterController; `AddDash`/`ApplyImpulse` memberi perpindahan; `Update` menjalankan movement/gravity; `GetActionSpeedMultiplier` menyesuaikan speed untuk state; `TryAutoJump` bantu naik step. Set Config. |
| `PlayerAnimationController.cs` | Player root (Animator child) | `RefreshAnimatorReference` mencari Animator; `CacheAnimatorParameters` memetakan parameter; `PlayAttack/Dodge/Parry/ParrySuccess/Hit/Dead` memicu animasi; `HandleHealthChanged` menangani health. Samakan Animator parameter. |
| `PlayerAttack.cs` | Legacy Player component | Komponen compatibility/logging saat AttackController tidak tersedia. Tidak menggantikan AttackController. Jangan pasang sebagai sistem attack kedua. |
| `PlayerInteract.cs` | Player root | `Update` mencari `Interactable` dalam range; `OnTriggerEnter` menangani collectible legacy tag. Untuk interaksi world pasang `Interactable` pada object dan isi Prompt/UnityEvent. LootPickup manual butuh integrasi tambahan. |
| `PlayerMovement.cs` | Legacy Player component | Sistem movement lama; `SetMovementState`, `MovePlayer`, `ApplyGravity`. Source menghentikan Update bila PlayerMotor ada. Jangan jadikan movement utama bersamaan dengan PlayerMotor. |
| `PlayerStatController.cs` | Player root | `InitializeBaseStats`, `RecalculateStats`, `CollectAllActiveModifiers`, `GetBaseStat`, `GetStat`; setter permanent/run modifiers; `ComputeOutgoingDamage`, `ComputeIncomingDamage`; add/remove temporary modifier; preview stat untuk UI. Assign PlayerBaseStatsSO. |
| `MaxHealthPolicy.cs` | Tidak dipasang | Enum `MaxHealthRecalculationPolicy`, interface `IHealthStatReceiver`, helper `HealthPolicyUtility.ResolveNewCurrentHealth` untuk menentukan HP saat Max HP berubah. Pilih KeepCurrentAndClamp atau PreservePercentage pada PlayerStatController. |
| `Health.cs` | Player dan enemy root | `Configure`, `SyncInitialMaxHealth`, `ApplyMaxHealthChange`, `SetMaxHealth`, `ResetHealth`, `TakeDamage`, `Heal`; implementasi health/damage/death. Pool hooks reset state. |
| `HitboxController.cs` | Player/weapon hitbox sesuai desain | `BeginAttack`, overlap search, `TryHit`; digunakan untuk kontrol target yang sudah terkena agar satu attack tidak memberikan hit berulang tanpa aturan. |
| `DamageInfo.cs` | Tidak dipasang | Struct payload damage: amount, instigator, hit point, parry flag, stagger, attack ID. `Create` factory sederhana. |
| `DamageResult.cs` | Tidak dipasang | Struct hasil resolusi damage: Applied/Blocked/Parried/Ignored/Killed dan metadata. |
| `IDamageable.cs`, `IParryable.cs` | Tidak dipasang | Interface contract target damage/parry. |
| `ParryResolver.cs` | Object CombatSystem | `TryResolve` memutuskan parry berhasil atau damage diteruskan. |
| `ParryWindow.cs` | Player root | `Open`, `Close`, `Update` mengatur interval ketika serangan incoming dapat diparry. |
| `CombatFeedbackRouter.cs` | Tidak dipasang sebagai component | Static/utility routing feedback. `EmitHitFeedback`, `EmitParryFeedback`, `ResolveAttackDefinition` mencari sumber attack config untuk hitstop/shake. |
| `CombatSystem.cs` | CombatSystem root | Singleton pusat `ApplyDamage` dan `NextAttackId`. `CombatResolver` juga terdapat pada source file yang sama dan harus berada di GameObject yang sama. |

## 7.4 Enemy AI dan combat

| Script | Pasang di | Fungsi dan pemakaian |
|---|---|---|
| `EnemyBrain.cs` | Enemy root | AI FSM baru. `Bind` menghubungkan room/player/pool context; `AddModifier`/`ClearModifiers`; `ApplyEmpower`; `ResetBrain`; `InitializePatterns`; `Update` menjalankan state; helper internal mengatur approach, positioning, telegraph, startup, execution, recovery, stagger, reposition, death, boss phase/pattern selection. Assign Definition wajib. |
| `EnemyMovement.cs` | Enemy root | Wrapper NavMeshAgent. `MoveTo`, `Stop`, `FacePosition`, `FacePositionImmediate`, `PerformLunge`, `CancelLunge`, separation/flank/sightline helpers. Perlu NavMeshAgent dan NavMesh bake valid. |
| `EnemyAttackController.cs` | Enemy root | `BeginTelegraph`, `UpdateTelegraphProgress`, `BeginStartup`, `BeginExecution`, `EndExecution`, `AbortAttack`, `OnParried`; mengeksekusi melee/projectile/zone/empower; `DeliverDamage` mengirim damage ke CombatSystem. Assign origin, Player mask, telegraph visual. |
| `StaggerComponent.cs` | Enemy root | Menampung nilai stagger; `Add` menambahkan stagger dari hit dan `ResetStagger` reset. MaxStagger/resistance dibaca dari component/definition, bukan `StaggerConfigSO`. |
| `CombatDirector.cs` | Satu object pada combat scene | Mengelola attack slot, sektor, role, tactical spacing. `RegisterEnemy`, `UnregisterEnemy`, `TryRequestAttackSlot`, `ReleaseAttackSlot`; alias RequestAggro/ReleaseAggro untuk kompatibilitas. |
| `CombatSpatialTypes.cs` | Tidak dipasang | Data/enum koordinasi spasial/sector/range yang digunakan director dan AI. |
| `EnemyController.cs` | Legacy enemy root | AI versi lama. Auto-disable bila EnemyBrain tersedia. Jangan aktifkan bersama AI baru. |
| `EnemySpawner.cs` | Test/spawner object lama | `SpawnRoutine` dan `SpawnEnemy` membuat enemy berkala; tracking death event. Terpisah dari RoomEncounterController. Gunakan hanya untuk test/simple spawn lama. |
| `EnemyVisualFeedback.cs` | Enemy root atau model visual | Subscribe health/brain state; `PlayHitFlash`, coroutine flash, `ApplyStateColor`, `SetColor`; assign target renderers. |
| `EnemyState.cs` | Tidak dipasang | Enum state enemy legacy; bukan component. |
| `GroundTelegraphView.cs` | Telegraph child/prefab | `Show` menampilkan bentuk/warna attack, `UpdateWorldPosition`, `SetProgress`, `TriggerStartupFlash`, `Hide`, `ApplyShapeScale`, `ApplyColor`. Dipakai EnemyAttackController. |

## 7.5 World generation, rooms, navmesh

| Script | Pasang di | Fungsi dan pemakaian |
|---|---|---|
| `WorldGenerationCoordinator.cs` | WorldSystems root | Orkestrator generation. `GenerateWorld`/overload seed membangun graph, placement, prefab, validasi, bake; `ClearWorld` membersihkan hasil; `PositionPlayerAtStart`; assign Config, NavMeshBuilder, EncounterController, PlayerTransform. |
| `RunMapGenerator.cs` | Generator object bila memakai API generator level | `Generate(parent)`, `AllReachable`, `BakeNavMesh`; gunakan config yang sama. Hindari menjalankan generator lain secara bersamaan. |
| `RoomGraphGenerator.cs` | Helper plain C# | Membentuk graph room/topology; `RequiresEncounter` menentukan role yang membutuhkan encounter. Dibuat oleh coordinator. |
| `RoomGraph.cs` | Plain C# | Graph data: `Add`, `AddNode`, `ConnectNodes`, `GetNode`, `ValidateConnectivity`. |
| `RoomPlacementSolver.cs` | Plain C# | Memilih posisi/rotasi room agar footprint tidak overlap dan connector bisa dipasangkan; helper register/unregister/snap/connector key. |
| `RoomPrefabAssembler.cs` | Plain C# | Merakit prefab room dan memasangkan connector; `DestroyAssembledRooms` membersihkan hasil. |
| `GeneratedMapData.cs` | Plain C# | Container hasil generation (room list, graph, seed, player start, lookup). `TryGetRoom`, `FindByController`, `GetWorldCenter`. |
| `MapValidator.cs` | Plain C# | Menghasilkan errors/warnings map; `BuildSummary` merangkum status validasi. |
| `RoomPrefabValidator.cs` | Editor utility | `Validate(prefab)` memeriksa prefab; menu `TLS/World/Validate Selected Room Prefabs` menjalankan validasi prefab pilihan. Tidak ditempel ke scene. |
| `RoomFootprint.cs` | Root setiap room prefab | Menentukan ukuran room/grid. `GetOccupiedCells`, `GetCurrentOccupiedCells`, `GetLocalBounds`, `IsLocalPointInside`, `WorldToGrid`, `GridToWorldOrigin`; `OnDrawGizmos` memperlihatkan footprint. Cell size 12. |
| `RoomConnector.cs` | Child room prefab | Menentukan socket pintu. `GetWorldDirection`, `IsCompatibleWith`, `SetConnected`, `ResetConnection`, `ApplySocketVisuals`, `SetCombatGateLocked`; assign OpenFrame/ClosedWall/CombatGate. |
| `RoomController.cs` | Root setiap room prefab | Room lifecycle. `InitializeFromGraph`, `EnsureEntryTrigger`, `OnTriggerEnter`, `HandlePlayerEntered`, `BeginRoomEncounter`, `CompleteRoomEncounter`, `SetAllCombatGatesLocked`; isi Role, layout, PlayerSpawn, SpawnPoints, encounter/modifier. |
| `EnemySpawnPoint.cs` | Child room prefab | Penanda spawn. `HasTag`, `HasAnyTag`, Gizmo methods. Assign Archetype/SpawnTags. |
| `RoomEncounterController.cs` | WorldSystems root | `StartEncounter` memilih composition, `PrefabFor` memetakan archetype → prefab, `Spawn` membuat enemy, `ApplyRoomAndProgressionModifiers`, track Health/death, `CompleteEncounter`. Assign prefab per archetype + player + projectile/enemy pool bila dipakai. |
| `RoomExit.cs` | Exit portal/door object | Trigger exit. `OnTriggerEnter/Exit`, `RefreshExitAvailability`, `ActivateExit`, responds room cleared/entered. Assign LockedVisual/UnlockedVisual, `RequireInteractInput`, `RequireAllMainPathCleared`. Collider wajib. |
| `RoomManager.cs` | Satu object manager scene | Menghubungkan WorldGenerationCoordinator dan encounter controller; melacak depth/current room/active map; `AdvanceToNextDepth`, `ResetRun`, handlers for world/room events. |
| `ProceduralData.cs` | Tidak dipasang | Enum `RoomType`, `BiomeType`, `Direction` dan `MapNode` untuk generator grid legacy. |
| `RoomEdgeData.cs` | Tidak dipasang | Data edge graph: from/to node, main path, connector index; `GetOtherNodeId`. |
| `RoomNodeData.cs` | Tidak dipasang | Data node graph new pipeline (role, depth, path flag, room/encounter refs, edges). `IsConnectedTo`. |
| `MapGenerator.cs` | Legacy generator scene only | Membuat golden path/side paths dan instantiate grid physical rooms; memakai RoomModule & SetDressingGenerator. Jangan jalankan bersama WorldGenerationCoordinator untuk level yang sama. |
| `RoomModule.cs` | Legacy room prefab | Mengatur DoorNorth/South/East/West dan Wall via `SetupSockets(MapNode)`. Ini cabang grid lama 10×10, bukan RoomFootprint new pipeline. |
| `SetDressingGenerator.cs` | Legacy generator scene | Membuat background/fringe props berdasarkan biome dan mapGraph. Gunakan hanya dengan MapGenerator lama. |
| `RuntimeNavMeshBuilder.cs` | NavMesh system root | Require `NavMeshSurface`; `BakeNavMeshRuntime`, `ClearNavMesh`, `TrySamplePosition`; BakeOnStart untuk scene statis, off untuk procedural sebelum coordinator selesai merakit room. |

## 7.6 Camera, pooling, economy, save

| Script | Pasang di | Fungsi dan pemakaian |
|---|---|---|
| `CinemachineRig.cs` | Tidak dipasang | `FindFollow` helper statis pencari camera; bukan component. |
| `TimeEffectController.cs` | TimeEffects root | Singleton hitstop. `Play`, `PlayRequest`, `Run`, `EnqueueHitstop`, `CancelAllHitstops`; update chooses highest active request. |
| `CameraEventManager.cs` | Tidak dipasang | Static events `TriggerSwitchCamera`, `TriggerCameraShake`, `ClearAll`. |
| `CameraManager.cs` | Camera system root | Switch camera by ID and send impulse shake. Require CinemachineImpulseSource. Isi daftar ID → CinemachineCamera. |
| `ObjectPoolManager.cs` | Pool system root | `PrewarmFromCatalog`, `Prewarm`, `Get`, `Release`, `GetPoolMetrics`; creates/maintains prefab pools and resets physics/navigation. Assign PoolCatalogSO. |
| `PoolCatalogSO.cs` | SO asset | Entries prefab/prewarm/capacity/max size. Assign ke ObjectPoolManager. |
| `PooledObject.cs` | Pool manager handles / optional prefab | `BindPrefab`, `MarkRented`, `MarkReturned`, `ReleaseSelf`, `CachePoolables`; controls lease identity and IPoolable lifecycle. |
| `IPoolable.cs` | Tidak dipasang | Interface callbacks for pool rent/return. |
| `EnemyPool.cs` | Pool helper/wrapper | `Get(prefab,pos,rotation)`, `Release(instance)`. Used by enemy encounter/spawning integration. |
| `ProjectilePool.cs` | Projectile pool helper | `Spawn(position,direction,owner,fromPlayer,damage)`. Used by enemy/player projectile attacks. |
| `Projectile.cs` | Projectile prefab | `Launch`, `Update`, `OnTriggerEnter`, `OnParried`, `Despawn`; supports pooled lifecycle. |
| `CurrencyWallet.cs` | Persistent/system root | Singleton Souls balance. `Set`, `AddCurrency`/`Add`, `Spend`, `ResetRun`, save read/write methods. |
| `ExperienceSystem.cs` | Persistent/system root | Singleton experience/level. `AddExperience`/`Add`, `ResetRun`; fires XP/level changed event. |
| `PermanentUpgradeSystem.cs` | Persistent/system root | Buy health upgrades via `BuyForPlayer`/`TryBuyHealth`; applies accumulated bonus to active Player; save hooks. |
| `PlayerProgression.cs` | Player or persistent progression root (ikuti lifetime scene) | Subscribe XP event; `HandleExperienceChanged` adds health per level through stats/Health; prevent duplicate subscriptions. |
| `Systems/Progression/RewardCollector.cs` | System root | Static event listener for `GameEvents.OnEntityDied`/run started. Prevents duplicated rewards per run/pooled lease. **Berbeda dari RewardCollector di Items/Loot yang dipasang pada enemy.** |
| `ItemPersistenceCoordinator.cs` | Same system root as save or dedicated save root | `CaptureToSaveData`, `ApplyFromSaveData`, `CaptureInventory`, `CaptureEquipmentAndArtifacts`, `RestoreInventory`, `RestoreEquipmentAndArtifacts`; assign all item/player references and catalog. |
| `SaveSystem.cs` | Save root | `SaveGame`, `LoadGame`, `DeleteSaveFile`, `SaveNow`, `HasSaveFile`, `RegisterSaveable`, `UnregisterSaveable`; serializes TLS_SaveData and coordinates registered systems. |
| `TLS_SaveData.cs` | Tidak dipasang | Root serializable save schema: version, timestamp, player/currency/inventory/equipment/settings data. |
| `InventorySaveData.cs` | Tidak dipasang | Serializable inventory capacity + entries (slot index, item ID, quantity). |
| `EquipmentSaveData.cs` | Tidak dipasang | Serializable weapon IDs, active slot, armor ID, artifact IDs/slots. |
| `SaveValidator.cs` | Tidak dipasang | `IsValid` checks save data/version/required structures. |
| `SaveVersionMigrator.cs` | Tidak dipasang | `TryMigrateToLatest`, `MigrateV0ToV1` migrates old save versions. |
| `ISaveableSystem` | Tidak dipasang | Contract for systems that write/read their data to TLS_SaveData. |

## 7.7 UI, diagnostics, core, misc

| Script | Pasang di | Fungsi dan pemakaian |
|---|---|---|
| `EquipmentSlotUI.cs` | UI slot prefab | `Bind` fills slot title/item/icon/active state and callbacks; button handlers select/unequip. Assign TMP_Text, Image, Buttons. |
| `EquipmentUI.cs` | Equipment panel root | Refreshes slots and responds selection/switch actions; requires EquipmentController and slot UI prefab/references. |
| `StatComparisonUI.cs` | Equipment detail panel | `ShowComparison`, `RefreshDisplay`, formats before/after stats for candidate item. |
| `HealthBarUI.cs` | HUD health bar | subscribes Health change and `Refresh` updates fill/text. Assign Health and UI references. |
| `HUDController.cs` | HUD root | Coordinates HUD displays and refreshes health/economy/experience references. Assign component references in Inspector. |
| `StaggerBarUI.cs` | Enemy/player stagger UI | Updates bar from stagger component/event. |
| `WorldDebugHUD.cs` | Debug Canvas (optional) | Displays runtime world/graph diagnostics; not required in release UI. |
| `WorldGraphGizmoDrawer.cs` | Debug object (optional) | Draws graph/room gizmos for editor visualization. |
| `GameOverUI.cs` | GameOver Canvas/panel | Shows game-over state and buttons/retry/return actions; assign panel/text/buttons. |
| `InventoryFeedbackUI.cs` | Inventory Canvas | `ShowInfo`/`ShowWarning` displays inventory transaction feedback. |
| `InventorySlotUI.cs` | Inventory slot prefab | `Bind` displays item/quantity and emits selection callback. |
| `InventoryUI.cs` | Inventory panel root | Subscribes inventory events, refreshes/populates slots, handles selection, connects details/filter. |
| `ItemCategoryFilter.cs` | Inventory filter UI | Filters item list by ItemCategory; connect UI toggles/buttons. |
| `ItemDetailUI.cs` | Inventory item detail panel | Shows selected item and configures primary/secondary/unequip buttons. Transaction callbacks update result state. |
| `MainMenuUI.cs` | Main menu Canvas/controller | `StartCombatTest`, `StartForestRun`, `OpenHub`, `QuitGame`; bind Button OnClick. `StartForestRun` memanggil alias `GameManager.StartForest()`. |
| `GameManager.cs` | Persistent root | Controls game state and scene transitions. `StartGame` membuka CombatScene; `StartForestRun` membuka ForestRunScene (alias `StartForest` untuk kompatibilitas UI); `BeginRunInScene` memulai run; `RestartRun`, `LoadHub`/`OpenHub`, `ReturnToMainMenu`, `CompleteRunVictory`; scene names must match Build Profile. |
| `GameEvents.cs` | Tidak dipasang | Static event bus: state, death, room, currency, XP, run started/ended; Raise methods publish events; `ClearAll` resets. |
| `GameEventPayloads.cs` | Tidak dipasang | Immutable RunId, PoolLeaseId, RunEndReason data types. |
| `GameState.cs` | Tidak dipasang | Enum MainMenu/Loading/Hub/InRun/Paused/Victory/GameOver. |
| `GameplayLayers.cs` | Tidak dipasang | Resolves layer IDs, bit masks and `TrySet` layer. Names must match Tag Manager. |
| `ArenaHazardZone.cs` | Hazard prefab/object runtime | `Initialize` sets radius/damage/duration/owner/mask; `Update` times lifespan/ticks; `ApplyZoneTick` applies damage to overlapping player. It has to be spawned/initialized by a caller; RoomModifier periodic hazard fields are not automatically connected. |
| `Interactable.cs` | World object needing E interaction | `Interact()` invokes configured UnityEvent; PlayerInteract searches this concrete component. Fill Prompt and OnInteract. |
| `SystemDiagnosticsRunner.cs` | Optional diagnostics object | `RunFullSmokeCheck` verifies singleton/player/economy/save/feedback; `RunPoolStressTest` exercises pooling. Use in development scene, not required in every final scene. |
| `CoreSystemsValidatorWindow.cs` | Editor-only script, never scene component | Menu **TLS → Diagnostics → Core Systems Validator** then **Audit Active Scene**; checks missing scripts, singleton counts, Player tag, Health and PlayerStatController. |
| `EntityFactory.cs` | Tidak dipakai | Empty stub/comment indicates no runtime creation contract; do not call/attach. |
| `SceneBootstrap.cs` | Tidak dipakai | Empty stub/comment. Scenes are authored manually; it does not create objects. |

### Editor tests

`Assets/_Project/Tests/Editor/GameplayTests.cs` bukan component scene. File ini memuat NUnit tests untuk: Health death event sekali, invulnerability block damage, struktur save data, perhitungan stat modifier, graph room sederhana, dan XP level-up. Jalankan lewat **Window → General → Test Runner → EditMode** (label menu dapat sedikit berbeda antar layout). Test ini memberi smoke coverage dasar, bukan bukti seluruh game loop/Inspector references sudah benar.

---

# 8. Konflik, orphan code, dan integrasi yang harus diperbaiki/dikonfirmasi

Berikut temuan yang penting sebelum menganggap seluruh sistem siap dipakai.

| Temuan | Dampak | Rekomendasi |
|---|---|---|
| `HitstopConfigSO` tidak punya consumer | Mengedit asset tidak mengubah hitstop | Sambungkan ke TimeEffectController atau tandai sebagai future/unused. |
| `StaggerConfigSO` tidak punya consumer | Mengedit MaxStagger asset tidak mengubah stagger | Sambungkan ke StaggerComponent/EnemyDefinition atau jangan dipakai dulu. |
| `GameplayArtifactSO` tidak dipakai oleh sistem equip saat ini | Artifact dari tipe ini tidak otomatis berfungsi | Gunakan `ArtifactDefinitionSO` untuk artifact aktif. |
| `RewardDefinitionSO` tidak punya consumer | Tidak masuk reward pipeline | Gunakan LootDefinitionSO + RewardCollectors. |
| `RoomModifierSO` hazard fields belum dibaca consumer | Volatile Ground berkala tidak aktif hanya dari SO | Implementasikan spawner hazard yang membaca field, atau sembunyikan/nonaktifkan field sampai siap. |
| `WeaponCombatAdapter` mengharapkan `IWeaponCombatReceiver`, tetapi `AttackController` tidak mengimplementasikannya | Adapter tidak bisa mengirim loadout melalui interface ke attack controller | Implementasikan interface pada AttackController atau hapus hubungan adapter yang tidak dipakai. ComboChain sekarang tetap dibaca langsung oleh AttackController dari EquipmentController. |
| `PlayerActionGateAdapter` default mengizinkan action | Artifact/equipment dapat lolos gate ketika Player sedang attack/dodge/parry | Sambungkan ke PlayerActionController dan implementasikan gating yang sesungguhnya. |
| `PlayerInteract` mencari `Interactable`, LootPickup manual mengekspos `ILootInteractable` | `ManualInteract` pada LootPickup belum dipanggil oleh PlayerInteract | Ubah PlayerInteract agar mencari interface ILootInteractable, atau buat adapter Interactable yang memanggil LootPickup. |
| `SequenceManager` mengunci `PlayerMovement`, tetapi PlayerMotor adalah controller aktif | Cutscene mungkin tidak menghentikan player | Tambahkan lock API ke PlayerMotor/PlayerInputReader/PlayerActionController lalu gunakan dari SequenceManager. |
| `PlayerMovement` legacy auto return jika PlayerMotor ada | Dua movement components membingungkan dan satu tidak bekerja | Pilih PlayerMotor untuk sistem baru; lepaskan PlayerMovement legacy kecuali kompatibilitas khusus. |
| `EnemyController` legacy auto-disable jika EnemyBrain ada | AI lama tidak berjalan di prefab baru | Gunakan EnemyBrain + EnemyMovement + EnemyAttackController. |
| `MapGenerator` + `RoomModule` legacy vs WorldGenerationCoordinator baru | Dua pipeline grid/room berbeda dan cell sizes berbeda | Pilih satu pipeline per scene. Generator baru: RoomFootprint (12-unit grid), RoomConnector, catalogs. Legacy: 10x10 RoomModule. |
| `PlayerStatController` memiliki beberapa output stat yang saat ini konstan | Beberapa efek progression yang terlihat seperti stat belum berubah | Jangan klaim stagger/dodge/parry bonus sudah berjalan bila hanya property konstan; implementasikan modifier path. |
| Asset room/pool config tidak tampak pada inventory audit | Procedural generation / pooling mungkin belum dikonfigurasi sebagai data assets | Buat dan assign asset room/catalog/config/pool setelah prefab valid. |

---

# 9. Checklist setup berurutan

Centang satu per satu. Jangan melompat ke procedural generation sebelum combat statis stabil.

## A. Project baseline

- [ ] Unity 6000.3.23f1 terbuka tanpa compile error.
- [ ] Package Input System, Cinemachine, AI Navigation, URP terpasang.
- [ ] Tag `Player` dan `Collectible` tersedia.
- [ ] Layer Player, Enemy, Environment, PlayerHitbox, EnemyHitbox, Projectile, Collectible, Interactable tersedia.
- [ ] Project backup/commit dibuat sebelum mengubah serialized assets.

## B. Data Player

- [ ] `PlayerMovementConfig` dibuat dan di-assign ke PlayerMotor + PlayerFacingController.
- [ ] `PlayerBaseStatsSO` dibuat dan di-assign ke PlayerStatController.
- [ ] Minimal satu AttackDefinitionSO dibuat.
- [ ] Animator parameter telah disamakan dengan PlayerAnimationController.

## C. Player prefab

- [ ] Root tag Player, layer Player.
- [ ] CharacterController + PlayerMotor.
- [ ] PlayerInputReader + PlayerActionController + PlayerFacingController.
- [ ] Health + InvulnerabilityHandler + PlayerDeathHandler.
- [ ] AttackController + DodgeController + ParryController + ParryWindow.
- [ ] PlayerStatController dengan BaseStats asset.
- [ ] Inventory + EquipmentController + ArtifactController bila fitur item dipakai.
- [ ] EquipmentTransactionHandler diisi semua reference.
- [ ] LootCollector diisi Inventory/receivers.
- [ ] PlayerAnimationController menemukan Animator.

## D. Combat & enemy

- [ ] CombatSystem, CombatResolver, ParryResolver pada satu GameObject.
- [ ] TimeEffectController satu instance.
- [ ] CameraManager + CinemachineImpulseSource terpasang.
- [ ] CombatDirector satu instance.
- [ ] EnemyDefinition asset untuk tiap archetype.
- [ ] EnemyAttackPattern asset dengan minimal satu swing.
- [ ] Enemy prefab punya NavMeshAgent, EnemyBrain, EnemyMovement, EnemyAttackController, Health, StaggerComponent.
- [ ] EnemyBrain.Definition tidak null.
- [ ] Attack telegraph references dan layer masks benar.
- [ ] EnemyController legacy tidak dipasang bersama EnemyBrain.

## E. Item, loot, save

- [ ] Weapon/Armor/Artifact assets memiliki ItemId unik dan stabil.
- [ ] Weapon ComboChain terisi dan BaseModifiers dipakai untuk weapon stat modifiers.
- [ ] Armor memiliki material atau visual ID valid.
- [ ] ArtifactDefinitionSO punya modifier jika memberi passive stat.
- [ ] ItemCatalogSO berisi semua item yang perlu disimpan; Validate Catalog bersih.
- [ ] LootDefinitionSO item reward mereferensikan item yang valid.
- [ ] LootPickup prefab memiliki collider trigger + LootPickup component.
- [ ] LootDropController drop table diisi pada enemy yang perlu drop.
- [ ] LootCollector terpasang pada Player.
- [ ] ItemPersistenceCoordinator assign catalog/inventory/equipment/artifact/stats.
- [ ] SaveSystem dapat Save → Restart/Reload → Load tanpa item hilang.

## F. Rooms / procedural generation

- [ ] Room prefab root memiliki RoomController + RoomFootprint.
- [ ] Room prefab memiliki PlayerSpawn, EnemySpawnPoint, dan RoomConnector yang sesuai.
- [ ] Connector memiliki OpenFrame/ClosedWall/CombatGate reference.
- [ ] Semua room prefab lolos Validate Selected Room Prefabs.
- [ ] Setiap RoomDefinitionSO memiliki RoomID unik, Prefab benar, role/biome/depth/weight benar.
- [ ] RoomCatalogSO berisi room definitions yang cocok dan lulus Validate All.
- [ ] EncounterCompositionSO berisi archetype yang tersedia dan sesuai depth/layout/tags.
- [ ] MapGenerationConfigSO assign RoomCatalog dan EncounterPool.
- [ ] WorldGenerationCoordinator assign Config, NavMeshBuilder, EncounterController, PlayerTransform.
- [ ] RuntimeNavMeshBuilder memiliki NavMeshSurface dan BakeOnStart false untuk procedural.
- [ ] RoomEncounterController semua prefab archetype sudah di-assign.
- [ ] Test dengan fixed seed agar bug layout bisa diulang.

## G. UI & scenes

- [ ] MainMenuUI buttons terhubung.
- [ ] Build Profiles mencakup scene tujuan dengan nama persis.
- [ ] HUD health/stagger/experience/wallet references benar.
- [ ] InventoryUI, ItemDetailUI, EquipmentUI, slot prefabs terhubung.
- [ ] GameOverUI panel/button action terhubung.
- [ ] Tidak ada duplicate singleton saat pindah scene.

## H. Diagnostics

- [ ] Buka scene `CombatTest`.
- [ ] Menu **TLS → Diagnostics → Core Systems Validator**.
- [ ] Tekan **Audit Active Scene**.
- [ ] Perbaiki semua `[ERROR]`.
- [ ] Evaluasi `[WARN]` sesuai tujuan scene (misalnya manager tertentu memang tidak dibutuhkan di MainMenu).
- [ ] Jalankan `SystemDiagnosticsRunner`/pool stress hanya di development scene.
- [ ] Play test attack, dodge, parry, enemy death, loot, inventory, equip, save/load.
- [ ] Baru setelah semua stabil, uji `ForestRun` dengan fixed seed.

---

# 10. Urutan implementasi yang direkomendasikan

1. **Compile + project validation** — pastikan source tidak error dan package sesuai.
2. **Tags/layers/input** — selesaikan dasar interaksi fisika dan input.
3. **Player prefab** — movement, facing, action state, health, input.
4. **CombatTest statis** — serangan, dodge, parry, damage, enemy death.
5. **Enemy prefabs + definitions + patterns** — baru uji archetype satu per satu.
6. **Equipment & stats** — weapon chain, armor, artifact, modifier calculator.
7. **Inventory + loot** — item collection, currency/XP, item catalog.
8. **Save/load** — baru setelah ItemId dan ItemCatalog stabil.
9. **Room prefab validation** — selesaikan connector/footprint/spawn setup.
10. **Room/encounter/map SOs** — catalog, definition, composition, generation config.
11. **ForestRun procedural generation** — fixed seed, navmesh, room encounter, exit.
12. **UI/cutscene/polish** — integrasikan UI dan perbaiki gap cutscene/manual loot/gating.

**Jangan menjadikan semua 22 tipe SO sebagai checklist “harus aktif”.** Sebagian adalah data yang sudah memiliki consumer, sebagian adalah fitur masa depan/legacy yang belum dihubungkan. Gunakan tabel status dan temuan integrasi di atas untuk menentukan prioritas.

---

## 11. Catatan untuk audit lanjutan di Unity

Untuk memastikan kondisi aktual, audit berikutnya perlu mengekspor juga serialized content dari:

- `Assets/_Project/Scenes/CombatTest.unity`
- `Assets/_Project/Scenes/ForestRun.unity`
- `Assets/_Project/Scenes/Hub.unity`
- prefab Player, semua prefab Enemy, pickup/projectile prefab, dan semua room prefab
- seluruh `.asset` ScriptableObject

Dari serialized YAML tersebut, audit dapat memverifikasi object hierarchy, component list, reference GUID, array element, dan nilai Inspector. Sampai data tersebut tersedia, panduan ini adalah **manual setup berdasarkan source**, bukan laporan pass/fail wiring scene.
