# Sistem Gameplay — The Last Sasak

Ringkasan implementasi saat ini. Roadmap lengkap ada di `README.md`. Dokumen ini hanya menjelaskan **apa yang sudah terpasang di scene** dan **fungsi setiap script**.

Prinsip: **script tidak membuat Player, lantai, kamera, HUD, atau musuh di scene.** Semua contoh sudah diletakkan di scene. Script hanya menggerakkan, meresolusi damage, dan (kalau perlu) memakai ulang prefab proyektil lewat pool.

---

## Cara main

Buka `Assets/_Project/Scenes/MainMenu.unity`, Play.

| Tombol menu | Scene |
| --- | --- |
| Start Combat Test | `CombatTest` — arena 1 ruangan, melee + ranged + loot |
| Forest Run | `ForestRun` — 2 ruangan, gerbang, NavMesh bake saat Play |
| Hub | `Hub` — NPC upgrade (tekan E, bayar Souls) |
| Quit | keluar aplikasi |

Kontrol:

| Aksi | Keyboard | Gamepad |
| --- | --- | --- |
| Gerak | WASD | Left stick |
| Serang | Mouse kiri / J | West |
| Parry | Mouse kanan / K | North |
| Dodge | Space / Left Shift | East / RB |
| Interaksi | E | South |
| Ganti senjata | 1 / 2 | — |

Setelah semua musuh di ruangan mati: HUD menulis `Room Cleared`, gerbang terbuka. Jalan ke trigger exit untuk pindah ruangan (`ForestRun`).

Kalau HP Player habis: layar Game Over → Retry / Main Menu.

---

## Apa yang ada di scene (contoh)

Tidak perlu Instantiate capsule dari kode.

**MainMenu:** `GameManager`, `CombatSystem` + resolver, pool, wallet, EXP, reward, director, hitstop, save, UI tombol.

**CombatTest:**
- `Player` (CharacterController + semua komponen player + `CameraTarget` + `AttackPoint`)
- `CM_Follow` (Cinemachine, Follow = CameraTarget)
- `ArenaFloor` + 3 obstacle + NavMeshSurface
- `Example_Melee`, `Example_Ranged`, `Example_Elite` (elite nonaktif, contoh saja)
- `Example_Loot` (tag Collectible, trigger)
- `Example_Projectile` (nonaktif, template pool)
- `Example_Room` + spawn point + `RoomManager` (`SpawnFromPool = 0` → pakai musuh yang sudah di scene)
- HUD + Game Over

**ForestRun:** sama seperti CombatTest, plus `Room_Start` / `Room_Combat`, gerbang, exit trigger, `RuntimeNavMeshBuilder`.

**Hub:** Player + kamera + `UpgradeNpc` (`Interactable` → `PermanentUpgradeSystem.BuyForPlayer`).

Layer yang dipakai: Player 8, Enemy 3, Environment 9, Projectile 12, Collectible 13, Interactable 14.

---

## Alur singkat

```
Input → PlayerActionController
         ├── DodgeController + i-frame
         ├── ParryController + ParryWindow
         └── AttackController → Hitbox → CombatSystem → Health

Musuh: EnemyBrain (Idle / Chase / Attack)
         → EnemyMovement (NavMesh)
         → EnemyAttackController (melee overlap / projectile)

Mati: Health.OnDied
         → RewardCollector (EXP + Souls)
         → RoomEncounterController (kalau kosong → Room Cleared)
         → PlayerDeathHandler → GameEvents.OnPlayerDied → GameManager.GameOver
```

---

## Perubahan yang dibuat

1. Object contoh dipasang **langsung di scene**, bukan dari factory.
2. `EntityFactory` dikosongkan. Tidak lagi `CreatePrimitive` / `AddComponent` massal.
3. `RunMapGenerator` tidak lagi membuat lantai/dinding. Hanya membaca `RoomController` yang sudah ada.
4. Script tidak lagi membuat GameObject cadangan (`TimeEffectController`, `SaveSystem`, `CombatResolver`, `ParryWindow`, `Health`, `Projectile`). Komponen harus sudah ada di object scene.
5. `RoomEncounterController` default **track musuh di scene**. Tidak spawn cube di udara kalau spawn point kosong.
6. `EnemySpawner.AutoSpawn` default off.
7. HUD CombatTest / ForestRun dilengkapi font + teks. Posisi Player diturunkan ke y=0 agar kaki di lantai.
8. Build settings memakai GUID scene yang sudah ada (`CombatTest`, `ForestRun`, `Hub`).
9. Kalau `PlayerMovementConfig` / `AttackDefinitionSO` belum di-assign, script memakai nilai runtime (bukan membuat object scene).

Pool `Instantiate(prefab)` tetap ada **hanya untuk proyektil/musuh yang sudah ada prefab-nya**, bukan untuk merakit arena.

---

## Fungsi per script

### Core

| Script | Fungsi |
| --- | --- |
| `GameManager` | State `MainMenu / Loading / InRun / GameOver`. Load scene. Game Over saat player mati. |
| `GameState` | Enum state. |
| `GameEvents` | Event global: HP mati, room cleared, souls, EXP, game state. |
| `GameplayLayers` | Nama layer → mask. |
| `SceneBootstrap` / `EntityFactory` | Kosong. Jangan pakai untuk spawn. |

### Player

| Script | Fungsi |
| --- | --- |
| `PlayerInputReader` | Baca Input System (bukan `Input.GetKey` tersebar). |
| `PlayerMotor` | `CharacterController.Move`, gravity, step, auto-jump undakan kecil. |
| `PlayerFacingController` | Hadap sesuai kamera. |
| `PlayerActionController` | State Idle / Attack / Parry / Dodge / Dead + aturan cancel. |
| `AttackController` | Startup → overlap hitbox → damage. |
| `DodgeController` | Dash + minta i-frame. |
| `ParryController` | Buka `ParryWindow` sebentar. |
| `HitboxController` | 1 serangan tidak hit target yang sama dua kali. |
| `InvulnerabilityHandler` | Timer i-frame. |
| `Health` | HP, invuln, `OnDied` sekali. Dipakai Player dan Enemy. |
| `PlayerDeathHandler` | Raise `OnPlayerDied`. |
| `PlayerInteract` | E + overlap `Interactable`; trigger loot. |
| `PlayerProgression` | Bonus HP per level. |
| `Inventory` | Daftar item sederhana. |
| `PlayerAnimationController` | Parameter Animator jika ada. |
| `PlayerHealth` | Wrapper lama ke `Health`. |

### Combat

| Script | Fungsi |
| --- | --- |
| `CombatSystem` | Satu pintu masuk damage. |
| `CombatResolver` | Cek parry dulu, lalu `TakeDamage`, stagger, hitstop. |
| `ParryResolver` | Parry valid jika window aktif + serangan `CanBeParried`. |
| `ParryWindow` | Timer jendela parry. |
| `DamageInfo` / `DamageResult` | Data damage / hasil. |
| `IDamageable` / `IParryable` | Kontrak HP dan deflect. |
| `Projectile` | Terbang, hit player, kalau di-parry berbalik. |
| `AttackDefinitionSO` | Damage, timing, radius, cancel. |

### Enemy

| Script | Fungsi |
| --- | --- |
| `EnemyBrain` | FSM Idle → Chase → Attack. |
| `EnemyMovement` | NavMeshAgent. |
| `EnemyAttackController` | Melee jarak dekat / tembak projectile. |
| `EnemyDefinitionSO` | Statistik per jenis. |
| `StaggerComponent` | Bar stagger elite. |
| `CombatDirector` | Batasi jumlah musuh yang agresif bersamaan. |
| `EnemySpawner` | Opsional. `AutoSpawn` harus dicentang manual. |

### World / room

| Script | Fungsi |
| --- | --- |
| `RoomController` | Spawn point, gerbang, cleared. |
| `RoomManager` | Aktifkan ruangan example, `Advance` ke ruangan berikutnya. |
| `RoomEncounterController` | Hitung musuh hidup. `SpawnFromPool = false` = pakai object scene. |
| `RoomExit` | Trigger pindah ruangan setelah cleared. |
| `EnemySpawnPoint` | Penanda posisi + archetype. |
| `RunMapGenerator` | Baca ruangan yang sudah ada. Tidak membangun mesh. |
| `RuntimeNavMeshBuilder` | Bake NavMesh saat Play (ForestRun). |
| `Interactable` | UnityEvent saat E. |

### Sistem lain

| Script | Fungsi |
| --- | --- |
| `ObjectPoolManager` | Pool prefab yang **sudah ada**. |
| `ProjectilePool` / `EnemyPool` | Ambil/kembalikan instance. |
| `CurrencyWallet` | Souls Coin + save. |
| `ExperienceSystem` | EXP run + level. |
| `RewardCollector` | Hadiah saat musuh mati. |
| `LootPickup` | Trigger loot. |
| `SaveSystem` | JSON lokal (`tls_save.json`). |
| `PermanentUpgradeSystem` | Beli HP permanen di Hub. |
| `TimeEffectController` | Hitstop. Harus ada object-nya di scene. |
| `CameraEventManager` | Event shake. Kamera sendiri di scene (Cinemachine). |
| `ItemSwitching` / `EquipmentController` | Ganti definisi serangan 1/2. |
| `ArtifactController` | Modifier damage/HP dari data artefak. |

### UI

| Script | Fungsi |
| --- | --- |
| `MainMenuUI` | Tombol load scene. |
| `HUDController` | Bind HP, souls, EXP, room. |
| `HealthBarUI` | Teks/slider HP. |
| `InventoryUI` | `Bag N`. |
| `GameOverUI` | Muncul saat `GameOver`. Retry / menu. |
| `StaggerBarUI` | Tampil stagger elite (kalau slider diisi). |

---

## Yang sengaja belum dijalankan dari script scene-builder

`MapGenerator` / `SetDressingGenerator` masih bisa Instantiate prefab ruangan (sistem map lama). **Scene playable tidak memakai itu.** Vertical slice dulu, generator hutan belakangan — sesuai urutan di `README.md`.

---

## Cek cepat di Editor

1. Play `CombatTest`: gerak, tabrak obstacle, serang melee, parry peluru ranged, ambil loot.
2. Play `ForestRun`: kejar musuh ruangan 1, gerbang buka, masuk ruangan 2.
3. Mati → Game Over.
4. Hub: kumpulkan souls dulu di CombatTest, lalu E di NPC.

Kalau Missing Script: komponen harus sudah menempel di object scene, jangan di-AddComponent dari kode.
