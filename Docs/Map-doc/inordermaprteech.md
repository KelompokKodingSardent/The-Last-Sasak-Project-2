Menurutku, pengembangan map The Last Sasak sudah bisa dijadikan satu milestone besar tersendiri. Aku akan menyusunnya sebagai **dokumen keputusan engineering dan roadmap implementasi dari awal sampai siap menjadi bagian dari playable demo**.

Ada beberapa keputusan yang perlu dikunci sebelum mulai.

# **Analisis keputusan arsitektur dunia**

## **1. Dunia yang sebenarnya sedang kita bangun**

Aku tidak menyarankan The Last Sasak menggunakan pendekatan *open-world procedural generation* atau generator yang membuat seluruh geometri lingkungan secara bebas.

Bentuk permainan yang lebih sesuai dengan fondasi sekarang adalah:



Rekomendasi arsitektur

**Hybrid Procedural Modular World**



Artinya, dunia mempunyai tiga lapisan berbeda.

## **Layer 1 — World Structure**

[Example 2 | Edgar - Unity](https://images.openai.com/static-rsc-4/f8zGEHU1_N9QtCz1VFSTeoOhMcpk9Gn_7wsvsPaTIlzpLWAjAB8xbBLJymNUf7e2VYf1KAP_Fyh-4mlzJppbRPnyRZVDqg8pSzdriywyAxDuQ4m278Kk0DXwmZDFLMFV9EHNpuhMHFoQTobIXI46qYJKytmdTm7o5wCyoU-RIJw?purpose=inline)

[ondrejnepozitek.github.io](https://ondrejnepozitek.github.io/Edgar-Unity/docs/examples/example-2/?utm_source=chatgpt.com)

LOGICAL WORLD

Menentukan struktur perjalanan: ruangan awal, cabang, encounter, reward, elite, dan jalan keluar.

## **Layer 2 — Spatial Layout**

[Here's a New Medieval Modular Asset Pack](https://images.openai.com/static-rsc-4/tkC0sAVPlvybDz4LSs-DjQfpFUnKgjUymtGXVKuGQ2NFTAtBRsbZRYSF7E9xDMHszoHaxgq802y9a1QHQQIvFFVhHBeuaw5Eg__KipaB3j-RIIv-yChx4qW0cNLO1nAxRn82vtR0eHigJl7PGP9BghsFAO98-qMBZsRAa_JNHDA?purpose=inline)

[80.lv](https://80.lv/articles/here-s-a-new-medieval-modular-asset-pack?utm_source=chatgpt.com)

PHYSICAL WORLD

Menentukan prefab ruangan, posisi, rotasi, connector, lantai, dinding, obstacle, serta jalur navigasi.

## **Layer 3 — Gameplay Content**

[Wizard of Legend 2 game revenue and stats on Steam – Steam Marketing Tool](https://images.openai.com/static-rsc-4/_GHh-BmBkja1y-Nep_3VBCWQS8vOawe3R7R9uOzmzVuJQmgPKQr_hJSo3i-0h43iu37g__V1kH5VTCBDnQwbZhYJDdiaZi1OnQM5EGp-a99sSSCBvQWx8MMIxU6ERdxavi9BVT6e3Tdt46uUG_E0IRw4lYPi8VhkNVLHutacoKI?purpose=inline)

[games-stats.com](https://games-stats.com/steam/game/wizard-of-legend-2/?utm_source=chatgpt.com)

GAMEPLAY WORLD

Menentukan komposisi encounter, spawn musuh, loot, interaksi, reward, dan progresi pemain.

Ketiga lapisan tersebut harus bisa berkembang secara terpisah.

Contohnya, kamu menambahkan sepuluh ruangan baru tanpa harus mengubah `EnemyBrain`. Kamu menambahkan lima komposisi encounter tanpa harus membuat ulang ruangan. Kamu juga mengubah algoritma graph tanpa mengubah konfigurasi dasar serangan.

Inilah tujuan utama arsitektur yang akan kita bangun.

## **2. Keputusan teknis yang perlu dikunci**

| Aspek                   | Keputusan                                 |
| ----------------------- | ----------------------------------------- |
| World generation        | Modular Room-Based Generation             |
| Map topology            | Graph-Based Generation                    |
| Room geometry           | Authored Room Prefab                      |
| Room placement          | Connector Alignment + Grid/Bulk Bounds    |
| Room configuration      | ScriptableObject                          |
| Encounter configuration | Terpisah dari Room Definition             |
| Enemy activation        | Room Encounter Controller + Pool          |
| Navigation              | Satu NavMesh untuk satu susunan run       |
| Randomization           | Seed-Based Generation                     |
| Room progression        | Physical Connected Rooms                  |
| Environment             | Forest Biome dahulu                       |
| Set dressing            | Authored prefab + deterministic variation |
| Runtime geometry        | Tidak membuat mesh arena secara bebas     |

### **Keputusan penting pertama: ruangan tersambung secara fisik**

Aku merekomendasikan sistem ruangan yang benar-benar terhubung di dunia permainan, bukan mengubah scene setiap kali pemain selesai mengalahkan musuh.

[AI Game Map Generator for RPG, Roguelike & Strategy Games](https://images.openai.com/static-rsc-4/s09cdmg-WRyqlVD872BSrye5AdUPzgV6N96_EljlUeJYvSX3wwP4zrQxe5y-6jOkGHwbYTsq-ac5ryQL0hpacRcrPe0iTIKskZ8_poHeYbLsWtRMqF-0SWm_CAPpwGKGfQBh1QB04HQCokcu8dF8N-Z2JTkdhYfda9hHAG1DIDc?purpose=inline)

[spriteflow.io](https://spriteflow.io/game-map-generator?utm_source=chatgpt.com)

Pemain berjalan dari satu ruangan menuju ruangan lain.

Ketika memasuki ruangan encounter, gerbang dapat terkunci. Setelah encounter selesai, gerbang terbuka dan pemain bisa melanjutkan perjalanan.

Keuntungannya:

- Tidak perlu load scene untuk setiap ruangan.
- Posisi dan orientasi dunia tetap konsisten.
- Cabang map bisa memberikan pilihan perjalanan.
- NavMesh dapat dibangun untuk seluruh susunan map.
- RoomManager dapat mengontrol perjalanan melalui graph.

### **Keputusan kedua: generator hanya merakit prefab yang sudah dibuat**

Prinsip implementasi kamu tetap dipertahankan.

Generator boleh menggunakan:

```
Instantiate(roomDefinition.Prefab);
```

Namun tidak boleh merakit arena menggunakan `CreatePrimitive`, membuat lantai dengan `AddComponent`, atau membuat seluruh objek lingkungan dari kode.

Room prefab sudah mempunyai geometri, collider, connector, spawn point, dan komponen yang diperlukan.

Jadi yang dibuat procedural adalah **komposisi dunia**, bukan setiap bagian geometri dunia.

### **Keputusan ketiga: Room Pattern dan Encounter Pattern dipisahkan**

Ini keputusan yang paling penting untuk skalabilitas konten.

```
ROOM DEFINITION
       |
       ├── Open Arena
       ├── Corridor
       ├── Forest Ruins
       └── Reward Chamber

ENCOUNTER DEFINITION
       |
       ├── Melee Group
       ├── Mixed Group
       ├── Ranged Support
       └── Elite Encounter
```

Generator memilih ruangan dan encounter berdasarkan aturan kompatibilitas.

Satu ruangan bisa mempunyai beberapa variasi encounter. Satu encounter juga dapat digunakan oleh beberapa ruangan yang memiliki kondisi spasial sesuai.

### **Keputusan keempat: jangan membuat ScriptableObject terlalu banyak**

Aku tidak merekomendasikan membuat `RoomPatternSO`, `RoomLayoutSO`, `RoomTypeSO`, dan `RoomCategorySO` sekaligus apabila semuanya hanya menyimpan informasi kategori ruangan.

Baseline cukup menggunakan:

- `RoomDefinitionSO`
- `RoomCatalogSO`
- `EncounterDefinitionSO`
- `MapGenerationConfigSO`

Gunakan tags, role, dan metadata di dalam definisi tersebut. Tambahkan asset konfigurasi baru hanya ketika memang memiliki tanggung jawab berbeda.

### **Keputusan kelima: random bukan berarti tidak terkontrol**

Generator harus mempunyai:

- Random seed.
- Batas percobaan.
- Validasi konektivitas.
- Pemeriksaan overlap.
- Pemeriksaan connector.
- Fallback layout.

Gunakan `System.Random` khusus untuk generation, bukan mengandalkan random global gameplay. Dengan begitu, random loot, efek visual, atau perilaku AI tidak mengubah hasil map.

## **3. Hubungannya dengan sistem yang sudah kamu miliki**

Kita tidak membangun ulang seluruh World Domain.

| Sistem sekarang           | Keputusan pengembangan                                                    |
| ------------------------- | ------------------------------------------------------------------------- |
| `RoomController`          | Dipertahankan, diperluas sebagai lifecycle satu room instance             |
| `RoomManager`             | Diperluas menjadi pengelola graph dan perjalanan antar-room               |
| `RoomEncounterController` | Diperluas untuk membaca encounter definition                              |
| `RoomExit`                | Dihubungkan dengan edge pada graph                                        |
| `EnemySpawnPoint`         | Diperluas dengan spawn tags dan validasi posisi                           |
| `EnemySpawner`            | Tetap menjadi sistem aktivasi/spawn musuh                                 |
| `EnemyPool`               | Digunakan kembali                                                         |
| `RuntimeNavMeshBuilder`   | Diperluas agar menunggu generated map selesai                             |
| `RunMapGenerator`         | Dipecah tanggung jawabnya menjadi graph, placement, dan assembly          |
| `MapGenerator` lama       | Audit dan migrasikan logika yang masih berguna                            |
| `SetDressingGenerator`    | Dipertahankan sebagai sistem visual tambahan, bukan generator core layout |

Aku juga mempertahankan dua scene berbeda:

- `CombatTest`: laboratorium combat statis untuk regression test.
- `ForestRun`: contoh integrasi room dan gameplay yang sudah ada.

Tambahkan scene pengujian baru bernama `WorldGenerationTest` agar eksperimen map tidak merusak kedua scene tersebut.

Dengan demikian, implementasi tetap sejalan dengan roadmap sebelumnya yang menggunakan modular room prefab, graph, seed, NavMesh, dan core loop eksplorasi sampai reward.&#x20;

Pasted text (2)(2).txt



# **Engineering Roadmap — Modular World Generation v1.0**

# THE LAST SASAK: THE UNKNOWN LAND

# MODULAR WORLD GENERATION & PROCEDURAL ENCOUNTER ENGINE v1.0

**Kasantara Studio — Engineering Development Document**

Status: Proposed Engineering Baseline\
Scope: World Architecture, Room Generation, Encounter Generation, Environment Integration\
Target Biome: Level 01 — Hutan\
Engine: Unity 6\
Architecture: Hybrid Procedural Modular World\
Prerequisite: Existing Player, Combat, Enemy, Pooling, and Room Foundation

---

# 1. PROJECT OBJECTIVE

Membangun sistem dunia procedural yang mampu menghasilkan susunan ruangan berbeda pada setiap run, tanpa mengorbankan kualitas desain level, keterbacaan gameplay, navigasi musuh, dan stabilitas sistem.

Sistem harus memungkinkan developer menambahkan room prefab, encounter, environment props, dan variasi gameplay melalui konfigurasi data tanpa menulis ulang algoritma generator.

## 1.1 Final Vision

Pemain memulai perjalanan dari satu titik awal di bioma Hutan.

Setiap run membentuk jaringan ruangan dengan jalur utama dan cabang opsional. Setiap ruangan memiliki layout, karakteristik spatial, dan kemungkinan encounter yang berbeda.

Pemain menjelajahi ruangan, menghadapi encounter, memperoleh reward, memilih jalur perjalanan, dan akhirnya mencapai titik penyelesaian area.

## 1.2 Final Gameplay Loop

```
MAIN MENU
    |
    v
INITIALIZE RUN
    |
    v
GENERATE WORLD GRAPH
    |
    v
SELECT ROOM PREFABS
    |
    v
SOLVE ROOM PLACEMENT
    |
    v
VALIDATE GENERATED MAP
    |
    v
ASSEMBLE WORLD
    |
    v
BUILD NAVMESH
    |
    v
INITIALIZE PLAYER AT START ROOM
    |
    v
EXPLORE
    |
    v
ENTER ROOM
    |
    v
START ENCOUNTER
    |
    v
COMBAT
    |
    v
ROOM CLEARED
    |
    v
REWARD / EXIT UNLOCKED
    |
    v
SELECT NEXT PATH
    |
    v
REPEAT
    |
    v
REACH FINAL ROOM
```

## 1.3 Non-Goals

Fitur berikut tidak termasuk target awal milestone:

- Open-world terrain generation.
- Infinite terrain streaming.
- Voxel world generation.
- Runtime mesh construction untuk seluruh geometri.
- Wave Function Collapse.
- Multiplayer world synchronization.
- Destruction system untuk seluruh environment.
- Dynamic NavMesh rebuilding setiap ruangan dimasuki.
- Banyak biome sekaligus.

Fokus tetap satu biome Hutan dengan kualitas procedural generation yang dapat diuji.

---

# 2. WORLD ARCHITECTURE

## 2.1 Tiga Lapisan Dunia

### Layer A — World Topology

Mengatur hubungan antar-ruangan.

Komponen:

- Room Graph.
- Room Node.
- Room Connection.
- Start Node.
- Exit Node.
- Branching Path.
- Room Role.

Tugasnya menentukan bagaimana pemain melakukan perjalanan.

### Layer B — Spatial World

Mengatur bentuk fisik dunia.

Komponen:

- Room Prefab.
- Room Connector.
- Room Footprint.
- Room Placement.
- Environment Geometry.
- Collision.
- NavMesh.

Tugasnya menghasilkan dunia yang benar-benar dapat dimainkan.

### Layer C — Gameplay Content

Mengatur isi setiap ruangan.

Komponen:

- Encounter Definition.
- Enemy Spawn Point.
- Enemy Composition.
- Loot Point.
- Reward Definition.
- Encounter State.
- Room State.

Tugasnya mengubah dunia fisik menjadi pengalaman gameplay.

Ketiganya tidak boleh memiliki tanggung jawab yang saling mengambil alih.

## 2.2 Runtime Architecture

```
WorldGenerationCoordinator
        |
        v
RunMapGenerator
        |
        +--------------------+
        |                    |
        v                    v
RoomGraphGenerator     MapGenerationConfig
        |
        v
GeneratedMapData
        |
        v
RoomPlacementSolver
        |
        v
RoomPlacementData
        |
        v
RoomPrefabAssembler
        |
        v
Generated Room Instances
        |
        v
MapValidator
        |
        v
RuntimeNavMeshBuilder
        |
        v
RoomManager
        |
        +--------------------------+
        |                          |
        v                          v
RoomController             RoomEncounterController
        |                          |
        v                          v
RoomExit                   EnemySpawner
                                   |
                                   v
                              EnemyPool
                                   |
                                   v
                              EnemyBrain
```

## 2.3 Batas Tanggung Jawab

| Sistem                     | Tanggung jawab                            |
| -------------------------- | ----------------------------------------- |
| WorldGenerationCoordinator | Mengatur urutan seluruh proses generation |
| RunMapGenerator            | Mengkoordinasikan pembentukan data map    |
| RoomGraphGenerator         | Menghasilkan struktur graph               |
| RoomPlacementSolver        | Menghitung posisi dan rotasi room         |
| RoomPrefabAssembler        | Memasang prefab room                      |
| MapValidator               | Memvalidasi hasil generation              |
| RoomManager                | Mengelola perjalanan dan room lifecycle   |
| RoomController             | Mengelola satu room instance              |
| RoomEncounterController    | Mengelola encounter dalam satu room       |
| EnemySpawner               | Mengaktifkan enemy                        |
| RuntimeNavMeshBuilder      | Menyiapkan navigasi                       |
| SetDressingGenerator       | Variasi visual environment                |

Generator tidak boleh mengatur secara langsung:

- Player action state.
- Enemy FSM.
- Combat resolution.
- Health.
- Reward calculation.
- HUD presentation.

---

# 3. WORLD DESIGN RULES

Sebelum implementasi generator, seluruh room harus mengikuti aturan desain yang sama.

## 3.1 Coordinate Standard

Gunakan satu sistem koordinat dunia yang konsisten.

Keputusan awal:

- X dan Z merupakan bidang horizontal permainan.
- Y merupakan ketinggian.
- Seluruh room menggunakan orientasi dasar yang konsisten.
- Room menggunakan grid atau satuan footprint.
- Semua connector mempunyai transform lokal.
- Rotasi room dibatasi pada orientasi yang sudah didukung.

Untuk prototipe pertama, usahakan seluruh lantai gameplay berada pada satu ketinggian utama.

Perbedaan elevasi kecil dapat ditambahkan kemudian setelah navigasi dan collision terbukti stabil.

## 3.2 Room Footprint

Setiap room memiliki informasi ukuran fisik.

Contoh:

```
Room_Arena_01
Width  = 24 units
Depth  = 24 units

Room_Corridor_01
Width  = 12 units
Depth  = 24 units

Room_LargeArena_01
Width  = 48 units
Depth  = 48 units
```

Angka ini merupakan contoh awal untuk pengujian, bukan ukuran final desain level.

Semua ukuran harus menggunakan standar grid yang disepakati.

Room yang lebih besar boleh menggunakan footprint beberapa cell.

## 3.3 Connector Standard

Untuk baseline, gunakan empat arah:

```
NORTH
EAST
SOUTH
WEST
```

Setiap connector harus memiliki:

- Local Position.
- Local Rotation.
- Direction.
- Opening Width.
- Opening Height.
- Connector Type.
- Connection Compatibility.

Connector dibuat sebagai object di dalam prefab, bukan dibuat saat runtime.

## 3.4 Gameplay Spatial Rules

Setiap room harus memenuhi kebutuhan gameplay.

**Open Combat Room**

- Area pertarungan cukup luas.
- Jalur pergerakan tidak tertutup.
- Spawn point tidak berkumpul pada satu posisi.
- Player tetap dapat melakukan repositioning.

**Obstacle Combat Room**

- Obstacle tidak menutup seluruh jalur navigasi.
- Enemy tidak terjebak.
- Kamera tetap dapat membaca posisi karakter.
- Tidak ada collision yang menutupi doorway.

**Corridor**

- Lebar jalur sesuai ukuran karakter dan musuh.
- Tidak menghasilkan NavMesh terputus.
- Spawn point tidak menghalangi jalur utama.

**Reward Room**

- Lokasi reward mudah dikenali.
- Tidak memerlukan combat encounter.
- Akses masuk dan keluar valid.

**Elite Room**

- Memiliki area navigasi yang cukup.
- Memiliki ruang untuk variasi pergerakan.
- Mempunyai spawn point yang sesuai dengan encounter.

---

# 4. ROOM ARCHITECTURE

## 4.1 Standard Room Prefab Hierarchy

Seluruh room prefab mengikuti struktur dasar berikut.

```
Room_Prefab
│
├── RoomRoot
│
├── Geometry
│   ├── Floor
│   ├── Walls
│   ├── Architecture
│   └── Decorations
│
├── Collision
│   ├── FloorCollider
│   ├── WallCollider
│   └── ObstacleColliders
│
├── Connectors
│   ├── Connector_North
│   ├── Connector_South
│   ├── Connector_East
│   └── Connector_West
│
├── RoomAnchors
│   ├── PlayerEntry
│   ├── RoomCenter
│   └── CameraReference
│
├── EnemySpawnPoints
│   ├── Spawn_01
│   ├── Spawn_02
│   └── Spawn_03
│
├── LootPoints
│   ├── Loot_01
│   └── Loot_02
│
├── Gates
│   ├── Gate_01
│   └── Gate_02
│
└── RoomController
```

Tidak setiap room wajib memiliki semua object opsional.

Namun, struktur root dan kontrak komponen harus konsisten.

## 4.2 Room Connector

`RoomConnector.cs`

Fungsi:

- Menyediakan transform sambungan.
- Memberikan informasi orientasi.
- Menentukan jenis konektor.
- Menyediakan informasi kompatibilitas.
- Menjadi referensi untuk RoomPlacementSolver.

Connector tidak menentukan room berikutnya.

Room Graph yang menentukan hubungan logis, sedangkan connector membantu mewujudkan hubungan tersebut secara fisik.

## 4.3 Room Controller

`RoomController.cs`

Tetap menjadi pengelola satu room instance.

Tanggung jawab:

- Menyimpan identitas room instance.
- Menyimpan referensi room definition.
- Mengatur room state.
- Menyediakan informasi connector.
- Mengelola gate.
- Berkomunikasi dengan encounter controller.
- Mengirim pemberitahuan room cleared.

Jangan menaruh algoritma procedural generation di dalam RoomController.

## 4.4 Room State

Gunakan state yang jelas.

```
Inactive
    |
    v
Ready
    |
    v
Entered
    |
    v
EncounterActive
    |
    v
Cleared
    |
    v
Completed
```

Reward Room dapat melewati EncounterActive dan menjadi Cleared setelah kondisinya terpenuhi.

State harus dapat dibedakan dari status GameManager.

Room state adalah kondisi satu ruangan, bukan kondisi keseluruhan game.

---

# 5. SCRIPTABLE OBJECT DATA ARCHITECTURE

## 5.1 RoomDefinitionSO

Menyimpan definisi dasar satu ruangan.

Informasi yang disimpan:

- Room ID.
- Display Name.
- Room Prefab.
- Room Role.
- Room Tags.
- Footprint.
- Allowed Rotation.
- Connector Requirements.
- Minimum dan Maximum Run Depth.
- Generation Weight.
- Encounter Compatibility Tags.
- Room Biome.

Contoh:

```
RoomDefinitionSO
│
├── Identity
├── Prefab Reference
├── Room Role
├── Tags
├── Footprint
├── Allowed Rotations
├── Connector Metadata
├── Run Depth Constraints
└── Encounter Constraints
```

Asset ini bersifat konfigurasi.

Jangan menyimpan current HP enemy, status encounter aktif, atau kondisi room runtime di dalamnya.

## 5.2 RoomCatalogSO

Menyimpan seluruh room definition yang boleh digunakan generator.

Tanggung jawab:

- Menyimpan koleksi RoomDefinitionSO.
- Memfilter room berdasarkan role.
- Memfilter room berdasarkan tag.
- Memeriksa ketersediaan prefab.
- Memeriksa kemungkinan connector yang kompatibel.

## 5.3 EncounterDefinitionSO

Menyimpan komposisi encounter.

Informasi:

- Encounter ID.
- Display Name.
- Difficulty Category.
- Enemy Entries.
- Spawn Requirements.
- Wave Configuration jika digunakan.
- Room Compatibility Tags.
- Reward Reference.

Satu entry encounter dapat berisi:

```
Encounter Entry
    Enemy Definition
    Quantity
    Required Spawn Tags
    Wave Index
```

Encounter tidak menentukan ukuran lantai atau geometri room.

## 5.4 MapGenerationConfigSO

Mengatur parameter satu jenis run.

Informasi:

- Biome ID.
- Minimum Room Count.
- Maximum Room Count.
- Main Path Length.
- Branch Count.
- Branch Probability.
- Allowed Room Roles.
- Room Generation Weights.
- Allowed Rotations.
- Maximum Placement Attempts.
- Random Seed Override.
- Generation Version.

Gunakan konfigurasi yang berbeda untuk pengujian dan produksi.

## 5.5 Runtime Data

Pisahkan asset dari data hasil generation.

Contoh:

```
RoomDefinitionSO
       |
       v
RoomGraphGenerator
       |
       v
RoomNodeData
       |
       v
RoomPlacementSolver
       |
       v
RoomPlacementData
       |
       v
RoomPrefabAssembler
       |
       v
Room Runtime Instance
```

Data runtime disimpan dalam class atau struct C# yang sesuai.

ScriptableObject tetap menjadi definisi dasar.

---

# 6. COMPLETE DEVELOPMENT PHASES

# PHASE 00 — WORLD FOUNDATION AUDIT

**Prioritas: P0**

## Objective

Memeriksa seluruh implementasi World Domain yang sudah tersedia sebelum memperluasnya.

## Tasks

1. Audit `RoomController`.
2. Audit `RoomManager`.
3. Audit `RoomEncounterController`.
4. Audit `RoomExit`.
5. Audit `EnemySpawnPoint`.
6. Audit `EnemySpawner`.
7. Audit `RunMapGenerator`.
8. Audit `MapGenerator`.
9. Audit `SetDressingGenerator`.
10. Audit `RuntimeNavMeshBuilder`.
11. Audit `ObjectPoolManager`, `EnemyPool`, dan lifecycle reset.
12. Periksa dependency pada setiap scene.

## Important Decisions

- Jangan mengganti seluruh script yang sudah bekerja.
- Identifikasi fungsi yang masih diperlukan.
- Tandai fungsi yang tumpang tindih.
- Pisahkan logic world generation dari room runtime.
- Pertahankan prinsip scene-authored services.
- Jangan membuat backup GameObject melalui script.

## Tests

- CombatTest tetap bekerja.
- ForestRun tetap bekerja.
- Musuh mati dan room cleared tetap terdeteksi.
- Gate dapat dibuka dan ditutup.
- NavMesh runtime bekerja.
- EnemyPool dapat melakukan reset.

## Deliverables

- World Domain Audit.
- Existing Script Responsibility Map.
- Dependency Diagram.
- Daftar script yang dipertahankan, diperluas, dan diganti.
- Baseline test report.

## Acceptance Criteria

Tidak ada sistem World Domain penting yang belum diketahui pemilik tanggung jawabnya.

---

# PHASE 01 — WORLD DESIGN SPECIFICATION

**Prioritas: P0**

## Objective

Mengunci aturan desain ruang dan pembentukan Level 01 Hutan.

## Tasks

1. Tentukan standar koordinat dunia.
2. Tentukan ukuran grid.
3. Tentukan standar footprint room.
4. Tentukan ukuran connector.
5. Tentukan orientasi connector.
6. Tentukan standar pintu dan gate.
7. Tentukan standar entry dan exit anchor.
8. Tentukan batas area combat.
9. Tentukan standar obstacle.
10. Tentukan kebutuhan spawn point berdasarkan room role.
11. Tentukan batas kedalaman dan variasi elevasi.
12. Tentukan aturan kompatibilitas room.

## Initial Room Categories

| Category        | Fungsi                     |
| --------------- | -------------------------- |
| Starting        | Titik awal perjalanan      |
| Traversal       | Eksplorasi dan perpindahan |
| Open Combat     | Arena pertarungan luas     |
| Obstacle Combat | Arena dengan penghalang    |
| Corridor        | Area transisi sempit       |
| Reward          | Hadiah dan resource        |
| Elite           | Tantangan elite            |
| Exit            | Titik akhir area           |

## Gameplay Design Rules

Setiap room harus mempunyai alasan gameplay untuk digunakan.

Jangan hanya membuat 10 prefab yang memiliki layout sama dan mengganti warna.

Variasi harus memengaruhi:

- Ruang gerak.
- Arah pendekatan enemy.
- Jarak antar-spawn.
- Visibility.
- Jalur eksplorasi.
- Posisi reward.
- Pilihan jalur pemain.

## Deliverables

- World Design Specification v1.0.
- Room Category Specification.
- Coordinate and Grid Standard.
- Connector Standard.
- Spatial Gameplay Rules.

## Acceptance Criteria

Seluruh programmer dan environment artist menggunakan spesifikasi yang sama.

---

# PHASE 02 — ROOM MODULE SYSTEM

**Prioritas: P0**

## Objective

Membuat standar prefab modular yang dapat dibaca dan digunakan generator.

## Tasks

1. Buat template Room Prefab.
2. Buat `RoomConnector`.
3. Buat connector North, South, East, West.
4. Buat standar `RoomRoot`.
5. Buat room anchor.
6. Buat player entry anchor.
7. Buat enemy spawn anchor.
8. Buat loot anchor.
9. Buat gate anchor.
10. Buat room footprint metadata.
11. Buat validasi prefab.
12. Uji perubahan rotasi room.

## Scripts

```
RoomConnector.cs
RoomPrefabValidator.cs
RoomFootprint.cs
RoomAnchor.cs
```

`RoomPrefabValidator` dapat menggunakan Editor validation atau pemeriksaan otomatis ketika melakukan validasi asset.

## Important Decisions

Room prefab harus siap digunakan sebelum generator berjalan.

Tidak boleh ada ketergantungan terhadap script generator untuk melengkapi struktur dasar prefab.

## Deliverables

- Room Prefab Template.
- Connector System.
- Anchor System.
- Prefab Validation.
- Dokumentasi prefab hierarchy.

## Acceptance Criteria

Prefab dapat diperiksa dan diketahui apakah:

- Connector lengkap.
- Footprint valid.
- Entry anchor tersedia.
- Collider tidak bermasalah.
- Struktur root konsisten.
- Komponen wajib telah terpasang.

---

# PHASE 03 — ROOM DATA & CATALOG

**Prioritas: P0**

## Objective

Mengimplementasikan sistem definisi dan katalog seluruh room prefab.

## Scripts

```
RoomDefinitionSO.cs
RoomCatalogSO.cs
MapGenerationConfigSO.cs
```

## Tasks

1. Buat RoomDefinitionSO.
2. Hubungkan prefab pada RoomDefinitionSO.
3. Tambahkan room role.
4. Tambahkan room tags.
5. Tambahkan footprint.
6. Tambahkan allowed rotations.
7. Tambahkan generation weight.
8. Tambahkan room depth constraints.
9. Buat RoomCatalogSO.
10. Buat catalog filtering.
11. Buat asset konfigurasi Level 01 Hutan.
12. Buat validasi seluruh asset.

## Initial Asset Target

| Asset           | Jumlah Awal |
| --------------- | ----------- |
| Starting Room   | 1           |
| Open Combat     | 2           |
| Obstacle Combat | 2           |
| Corridor        | 1           |
| Reward Room     | 1           |
| Exit Room       | 1           |

Total target awal: 8 definisi room.

Ini merupakan target produksi awal, bukan jumlah room maksimal.

## Deliverables

- RoomDefinitionSO.
- RoomCatalogSO.
- ForestMapConfigSO.
- Validated Room Assets.

## Acceptance Criteria

Generator dapat mendapatkan semua room yang kompatibel melalui catalog tanpa melakukan pencarian object secara manual di scene.

---

# PHASE 04 — GREYBOX ROOM LIBRARY

**Prioritas: P0**

## Objective

Membuat library ruangan pertama menggunakan geometri sementara sebelum production art.

## Tools

- ProBuilder.
- Unity Scene View.
- Unity Physics.
- Gizmos.
- Blender untuk aset modular ketika diperlukan.

## Tasks

1. Buat Starting Room.
2. Buat Open Combat Room A.
3. Buat Open Combat Room B.
4. Buat Obstacle Combat Room.
5. Buat Corridor.
6. Buat Reward Room.
7. Buat Exit Room.
8. Tambahkan collision.
9. Tambahkan connector.
10. Tambahkan anchor.
11. Tambahkan area navigasi.
12. Uji seluruh koneksi secara manual.

## Design Validation

Uji:

- Player dapat berpindah di setiap area.
- Tidak ada collision tidak terlihat.
- Enemy dapat mengejar Player.
- Tidak ada obstacle yang menutup jalur.
- Kamera dapat mengikuti Player.
- Setiap pintu mempunyai clearance yang sesuai.
- Spawn point dapat digunakan.

## Important Decision

Jangan menunggu seluruh aset hutan final.

Buat greybox sampai layout dan gameplay sudah benar.

Setelah lolos, environment artist dapat mengganti geometri menggunakan model final tanpa mengubah struktur prefab.

## Deliverables

Minimal enam sampai delapan room prefab greybox.

## Acceptance Criteria

Seluruh room dapat dimainkan dan memiliki kualitas spatial yang cukup berbeda.

---

# PHASE 05 — ENCOUNTER & SPAWN INTEGRATION

**Prioritas: P0**

## Objective

Menghubungkan room dengan komposisi enemy secara data-driven.

## Scripts

```
EncounterDefinitionSO.cs
EncounterCatalogSO.cs
EnemySpawnPoint.cs
RoomEncounterController.cs
EnemySpawner.cs
```

Sebagian script sudah ada. Fokusnya adalah perluasan kontrak dan konfigurasi, bukan membuat ulang semuanya.

## Tasks

1. Buat EncounterDefinitionSO.
2. Buat daftar enemy entries.
3. Tentukan jumlah enemy per entry.
4. Tentukan spawn requirements.
5. Tambahkan spawn tags.
6. Hubungkan encounter ke RoomDefinition.
7. Tambahkan compatibility validation.
8. Hubungkan EnemyPool.
9. Hubungkan EnemyDefinitionSO.
10. Uji encounter secara manual di ForestRun.
11. Uji encounter dengan beberapa room.
12. Uji room cleared dan reward.

## Spawn Point Tags

Contoh:

```
EntrancePerimeter
Center
OuterPerimeter
RangedPosition
ElitePosition
AmbushPosition
LootPosition
```

Spawn point menentukan karakteristik posisi.

Encounter definition menentukan jenis dan jumlah enemy.

Dengan demikian, spawn point tidak harus dikunci untuk satu archetype enemy.

## Initial Encounter Library

| Encounter    | Komposisi                               |
| ------------ | --------------------------------------- |
| Encounter_01 | Vanguard group                          |
| Encounter_02 | Melee group dengan komposisi berbeda    |
| Encounter_03 | Vanguard + Ranged                       |
| Encounter_04 | Ranged support                          |
| Encounter_05 | Elite challenge, setelah elite tersedia |

Jumlah dan kekuatan musuh harus mengikuti hasil balancing, bukan ditentukan hanya dari angka generator.

## Important Rules

- Enemy hanya aktif ketika encounter dimulai.
- Enemy menggunakan pooling.
- Spawn point harus valid.
- Enemy tidak boleh muncul terlalu dekat dengan Player.
- Spawn tidak boleh menutup entry atau exit.
- Reward tidak boleh diberikan dua kali.
- RoomEncounterController tidak boleh mengatur EnemyBrain secara langsung selain melalui kontrak aktivasi yang jelas.

## Deliverables

- EncounterDefinitionSO.
- EncounterCatalogSO.
- Spawn Point Validation.
- Encounter Compatibility.
- Enemy Pool Integration.

## Acceptance Criteria

Satu room dapat menggunakan beberapa encounter berbeda tanpa mengubah kode RoomController.

---

# PHASE 06 — ROOM GRAPH GENERATOR

**Prioritas: P0**

## Objective

Membentuk struktur logis map sebelum menentukan posisi fisik prefab.

## Scripts

```
RoomGraph.cs
RoomNodeData.cs
RoomEdgeData.cs
RoomGraphGenerator.cs
```

## Tasks

1. Buat RoomGraph.
2. Buat RoomNodeData.
3. Buat RoomEdgeData.
4. Buat Start Node.
5. Buat Exit Node.
6. Buat Main Path Generator.
7. Tambahkan Branch Generator.
8. Tambahkan Room Role Selection.
9. Tambahkan Depth Constraints.
10. Tambahkan Generation Rules.
11. Implementasikan deterministic seed.
12. Buat Graph Validator.

## Initial Graph Strategy

Gunakan dua tahap.

**Tahap A — Main Path**

Bentuk jalur utama terlebih dahulu.

```
Start
  |
Combat A
  |
Combat B
  |
Combat C
  |
Exit
```

Jalur utama wajib valid.

**Tahap B — Branch Generation**

Tambahkan cabang opsional.

```
             Reward
               |
Start — Combat A — Combat B — Combat C — Exit
                  |
                Combat D
```

Cabang opsional dapat berisi:

- Reward.
- Traversal.
- Combat tambahan.
- Elite, ketika sistem elite sudah siap.

Untuk versi pertama, gunakan graph yang tidak memiliki cycle.

Setelah stabil, baru pertimbangkan koneksi tambahan atau loop.

## Random Seed

Gunakan `System.Random` dengan seed yang diberikan.

Seed harus disimpan bersama hasil run.

Pastikan:

- Seed yang sama menghasilkan graph yang sama.
- Pemilihan node tidak dipengaruhi random global gameplay.
- Urutan kandidat selalu deterministik.
- Generator mempunyai batas percobaan.
- Kegagalan generation menghasilkan fallback atau error report.

## Deliverables

- RoomGraph.
- RoomGraphGenerator.
- RoomNodeData.
- RoomEdgeData.
- Deterministic Generation.
- Graph Validation.

## Acceptance Criteria

Setiap graph memiliki Start dan Exit yang terhubung, tidak ada node yang tidak dapat dijangkau, dan seluruh constraint terpenuhi.

---

# PHASE 07 — SPATIAL ROOM PLACEMENT SOLVER

**Prioritas: P0**

## Objective

Mengubah graph logis menjadi posisi fisik setiap room.

## Scripts

```
RoomPlacementData.cs
RoomPlacementSolver.cs
RoomPlacementValidator.cs
```

## Tasks

1. Ambil RoomNodeData.
2. Ambil room prefab definition.
3. Pilih connector yang akan digunakan.
4. Hitung rotasi room.
5. Sejajarkan connector kandidat.
6. Hitung room footprint.
7. Periksa occupied space.
8. Periksa collision antar-room.
9. Periksa compatibility connector.
10. Periksa jarak antar-room.
11. Simpan transform hasil placement.
12. Tangani placement failure.

## Connector Alignment

Konsepnya:

```
EXISTING ROOM
     |
     v
OPEN CONNECTOR
     |
     v
CANDIDATE ROOM
     |
     v
SELECT MATCHING CONNECTOR
     |
     v
CALCULATE ROTATION
     |
     v
ALIGN CONNECTOR TRANSFORMS
     |
     v
VALIDATE FOOTPRINT
     |
     v
ACCEPT / REJECT
```

Connector harus bertemu pada transform yang tepat, dengan arah pembukaan saling berlawanan.

## Overlap Validation

Gunakan kombinasi:

- Grid occupancy.
- Room footprint.
- World-space Bounds.
- Pemeriksaan connector.
- Pemeriksaan clearance.

Untuk layout reguler, grid occupancy menjadi metode utama.

Physics overlap dapat menjadi pemeriksaan tambahan, bukan satu-satunya sistem penempatan.

## Placement Failure

Jangan melakukan loop pencarian tanpa batas.

Gunakan:

- Maximum attempts.
- Candidate fallback.
- Backtracking terbatas apabila diperlukan.
- Valid fallback layout.

## Deliverables

- RoomPlacementSolver.
- RoomPlacementData.
- Placement Validation.
- Connector Alignment.
- Placement Failure Handling.

## Acceptance Criteria

Tidak ada dua room yang overlap secara tidak sengaja dan seluruh koneksi graph terwujud secara fisik.

---

# PHASE 08 — RUNTIME ROOM ASSEMBLY

**Prioritas: P0**

## Objective

Memasang seluruh room prefab berdasarkan hasil graph dan placement.

## Scripts

```
RoomPrefabAssembler.cs
GeneratedMapData.cs
WorldGenerationCoordinator.cs
```

`RunMapGenerator` dapat dipertahankan sebagai facade atau coordinator, tetapi jangan menjadikannya satu script monolitik yang mengerjakan graph, placement, spawning, dan navigation sekaligus.

## Tasks

1. Terima GeneratedMapData.
2. Terima RoomPlacementData.
3. Resolve RoomDefinitionSO.
4. Instantiate prefab yang sudah tersedia.
5. Tempatkan prefab sesuai transform hasil solver.
6. Simpan referensi room instance.
7. Hubungkan RoomController dengan RoomNodeData.
8. Hubungkan pasangan connector.
9. Hubungkan RoomExit dengan tujuan graph.
10. Bangun registry seluruh room.
11. Validasi hasil assembly.
12. Sediakan cleanup untuk generated run.

## Scene Architecture

Tambahkan scene:

`WorldGenerationTest.unity`

Susunan contoh:

```
WorldGenerationTest
│
├── SceneServices
│   ├── WorldGenerationCoordinator
│   ├── RunMapGenerator
│   ├── RoomManager
│   ├── CombatSystem
│   └── RuntimeNavMeshBuilder
│
├── GeneratedMapRoot
│
├── Player
│
├── Main Camera
│
├── HUD
│
└── Environment Lighting
```

Semua service utama dan Player sudah dipasang melalui Editor.

`GeneratedMapRoot` adalah object kosong yang disediakan di scene sebagai parent.

Generator hanya memasang prefab room ke dalam root tersebut.

Generator tidak membuat Player, Camera, HUD, atau layanan gameplay cadangan.

## Generation Lifecycle

```
Uninitialized
      |
      v
GeneratingGraph
      |
      v
SolvingPlacement
      |
      v
AssemblingRooms
      |
      v
Validating
      |
      v
PreparingNavigation
      |
      v
Ready
```

Jika salah satu tahap gagal:

```
GenerationFailed
      |
      v
Generation Report
      |
      v
Fallback / Retry / Return to Menu
```

## Deliverables

- Runtime Room Assembly.
- GeneratedMapData.
- Room Instance Registry.
- GeneratedMapRoot Integration.
- Generation Lifecycle.

## Acceptance Criteria

WorldGenerationTest dapat menghasilkan satu susunan map dari catalog dan konfigurasi tanpa membuat geometri secara prosedural.

---

# PHASE 09 — NAVMESH & ENEMY NAVIGATION INTEGRATION

**Prioritas: P0**

## Objective

Memastikan seluruh generated room mempunyai navigasi yang valid.

## Tools

- Unity AI Navigation.
- NavMeshSurface.
- NavMeshAgent.
- NavMeshObstacle.
- NavMeshLink jika diperlukan.

## Tasks

1. Tentukan satu NavMeshSurface untuk seluruh generated world.
2. Pastikan seluruh geometri room telah ditempatkan.
3. Pastikan collider dan modifier sudah aktif.
4. Build NavMesh.
5. Tunggu proses build selesai.
6. Validasi beberapa posisi navigasi.
7. Validasi Player start position.
8. Validasi seluruh enemy spawn point.
9. Aktifkan EnemySpawner setelah navigation ready.
10. Uji enemy melewati doorway.
11. Uji enemy mengitari obstacle.
12. Uji seluruh room connections.

## Navigation Rules

NavMesh tidak boleh dibangun sebelum room prefab selesai dirakit.

Urutan wajib:

```
Generate Graph
      |
Place Rooms
      |
Assemble Geometry
      |
Validate World
      |
Build NavMesh
      |
Validate Navigation
      |
Activate Enemy
```

Untuk baseline, satu NavMesh global lebih sederhana daripada membangun ulang NavMesh setiap kali pemain masuk ruangan.

Jangan mengaktifkan NavMeshAgent pada posisi yang belum valid.

Periksa bahwa titik spawn berada di dekat posisi yang dapat ditempati Agent menggunakan pemeriksaan NavMesh.

## Deliverables

- Generated World Navigation.
- NavMesh Validation.
- Spawn Position Validation.
- Enemy Navigation Test.

## Acceptance Criteria

Musuh dapat mengejar Player melewati semua jalur yang seharusnya terhubung dan tidak mencoba berjalan pada permukaan yang tidak dapat dilalui.

---

# PHASE 10 — ROOM LIFECYCLE & RUN PROGRESSION

**Prioritas: P0**

## Objective

Menghubungkan generated world dengan alur perjalanan pemain.

## Scripts

```
RoomManager.cs
RoomController.cs
RoomEncounterController.cs
RoomExit.cs
RoomGateController.cs
```

Sebagian besar komponen ini sudah ada. Perluas sesuai tanggung jawabnya.

## Tasks

1. RoomManager menerima graph hasil generation.
2. Tentukan Start Room.
3. Hubungkan seluruh room instance dengan node graph.
4. Tentukan current room.
5. Deteksi Player memasuki room.
6. Aktivasi encounter satu kali.
7. Kunci gate apabila dibutuhkan.
8. Dengarkan hasil encounter.
9. Ketika seluruh enemy mati, ubah room menjadi Cleared.
10. Berikan reward sesuai konfigurasi.
11. Buka akses room berikutnya.
12. Catat room yang sudah dikunjungi.
13. Tangani pilihan branch.
14. Tangani player death dan reset run.

## Room Lifecycle Example

```
Player Enters Room
       |
       v
Room Ready?
       |
       v
Start Encounter
       |
       v
Lock Required Gates
       |
       v
Track Active Enemies
       |
       v
All Enemies Defeated?
       |
       v
Room Cleared
       |
       v
Grant Reward
       |
       v
Unlock Exits
       |
       v
Player Selects Next Route
```

## Important Decisions

Tidak semua room membutuhkan encounter.

Starting Room, Traversal Room, dan Reward Room dapat memiliki aturan khusus.

RoomManager tidak mengatur reward calculation. RewardCollector yang tetap menangani reward.

EnemyBrain tidak mengetahui graph map.

RoomEncounterController tidak mengetahui keseluruhan struktur dunia.

## Deliverables

- Graph-Based Room Progression.
- Current Room Tracking.
- Gate Integration.
- Branch Traversal.
- Room Clear Lifecycle.
- Run Reset Handling.

## Acceptance Criteria

Pemain dapat menjelajahi seluruh map secara fisik, menyelesaikan encounter, membuka jalur, dan melanjutkan perjalanan tanpa scene reload per room.

---

# PHASE 11 — FOREST ENVIRONMENT & SET DRESSING

**Prioritas: P1**

## Objective

Mengembangkan greybox menjadi lingkungan Hutan yang memiliki identitas visual tanpa mengganggu gameplay.

## Environment Categories

### Natural Environment

- Trees.
- Roots.
- Rocks.
- Bushes.
- Grass.
- Fallen Logs.

### Architectural Environment

- Ruins.
- Stone Structures.
- Ancient Markers.
- Broken Walls.
- Shrine Structures.

### Gameplay Environment

- Solid Obstacles.
- Cover.
- Gates.
- Loot Pedestals.
- Interaction Points.

### Atmospheric Environment

- Fog.
- Lighting.
- Ambient Particles.
- Ground Decals.
- Background Silhouettes.

## Tasks

1. Tentukan visual reference biome Hutan.
2. Buat material palette.
3. Buat reusable modular architecture.
4. Buat environment prop library.
5. Buat obstacle prefab.
6. Buat decoration prefab.
7. Tambahkan collision sesuai kebutuhan.
8. Tambahkan set dressing anchors.
9. Buat deterministic decoration variation.
10. Integrasikan lighting.
11. Periksa readability dari kamera isometrik.
12. Uji ulang seluruh NavMesh setelah geometri gameplay berubah.

## SetDressingGenerator

Sistem ini boleh mengatur variasi visual di dalam room.

Contoh:

```
Room Prefab
     |
     v
Set Dressing Anchors
     |
     v
Select Compatible Props
     |
     v
Seeded Variation
     |
     v
Place Authored Prefabs
```

Pisahkan dua jenis objek:

**Gameplay Geometry**

Objek yang memengaruhi collision, jalur, visibility, atau NavMesh.

**Cosmetic Dressing**

Objek yang hanya berfungsi visual.

Gameplay geometry harus divalidasi sebelum NavMesh dibangun.

Cosmetic dressing yang tidak memengaruhi navigasi dapat diatur secara terpisah.

## Important Rules

- Jangan menempatkan pohon besar di doorway.
- Jangan menutup seluruh arena menggunakan dekorasi.
- Jangan menambahkan collider yang tidak diperlukan.
- Jangan membuat environment menutupi Player dari kamera.
- Jangan mengubah ukuran connector akibat visual dressing.
- Jangan membiarkan variasi visual mengubah footprint room.

## Deliverables

- Forest Environment Kit.
- Forest Materials.
- Obstacle Library.
- Decoration Library.
- Set Dressing Configuration.
- Forest Room Art Pass.

## Acceptance Criteria

Semua room tetap bisa dimainkan setelah production art diterapkan.

---

# PHASE 12 — MAP VALIDATION & AUTOMATED TESTING

**Prioritas: P0**

## Objective

Menjamin procedural generation menghasilkan dunia yang valid, konsisten, dan tidak menimbulkan softlock.

## Test Infrastructure

Gunakan Unity Test Framework dengan Edit Mode dan Play Mode tests.

## A. Graph Tests

- Graph mempunyai tepat satu Start Node.
- Graph memiliki Exit Node.
- Exit dapat dicapai dari Start.
- Tidak ada node yang tidak terjangkau.
- Tidak ada edge duplikat.
- Room role mengikuti constraint.
- Branch tidak menghasilkan node terisolasi.

## B. Placement Tests

- Tidak ada overlap footprint.
- Connector berpasangan.
- Connector menghadap arah yang benar.
- Room rotation sesuai izin.
- Semua room mempunyai posisi valid.
- Tidak ada room yang keluar dari batas generation yang ditetapkan.

## C. Encounter Tests

- Encounter hanya menggunakan room yang kompatibel.
- Jumlah spawn tidak melebihi anchor valid.
- Spawn point tidak berada di posisi terlarang.
- Enemy hanya diaktifkan satu kali.
- Reward tidak diberikan dua kali.
- Room cleared terjadi satu kali.

## D. Navigation Tests

- NavMesh berhasil dibuat.
- Player start position valid.
- Enemy spawn point dapat dijangkau.
- Doorway tidak memutus jalur.
- Enemy dapat bergerak di antara room yang terhubung.

## E. Runtime Tests

- Map dapat dibuat ketika scene dijalankan langsung.
- Map dapat dibuat melalui Main Menu.
- Player muncul pada start anchor.
- Room progression bekerja.
- Player death membersihkan run.
- Retry tidak meninggalkan enemy atau state lama.
- Seed yang sama menghasilkan layout yang sama.

## F. Seed Stress Testing

Jalankan generation dengan kumpulan seed pengujian.

Contoh pengujian awal:

```
Seed 1001
Seed 1002
Seed 1003
...
Seed 1100
```

Catat:

- Generation Success.
- Graph Validity.
- Placement Validity.
- Connector Validity.
- NavMesh Result.
- Encounter Compatibility.
- Generation Time.

Jangan hanya menguji satu seed.

## Deliverables

- Edit Mode Tests.
- Play Mode Tests.
- Map Validation Report.
- Seed Test Report.
- Known Issues List.

## Acceptance Criteria

Seluruh test kritis lulus dan tidak ditemukan layout yang membuat pemain terjebak atau tidak dapat menyelesaikan run.

---

# PHASE 13 — OPTIMIZATION, BALANCING & RELEASE

**Prioritas: P1**

## Objective

Memastikan map generation layak dimasukkan ke dalam demo.

## A. Performance

Ukur:

- World generation time.
- Room prefab instantiation.
- NavMesh build time.
- Active GameObjects.
- CPU Usage.
- GPU Usage.
- Memory.
- Garbage Collection.
- Rendering cost.
- Enemy AI cost.

Gunakan Unity Profiler untuk menentukan bottleneck sebelum optimasi.

## B. Optimization Direction

Prioritaskan:

1. Mengurangi Instantiate/Destroy yang tidak diperlukan.
2. Memanfaatkan EnemyPool.
3. Mengurangi komponen dan collider yang tidak diperlukan.
4. Mengurangi jumlah GameObject decoration.
5. Menggunakan LOD jika jumlah aset dan jarak kamera memang membutuhkannya.
6. Mengatur shadow distance.
7. Menonaktifkan proses encounter pada room yang belum aktif.
8. Menghindari NavMesh rebuild berulang.
9. Mengurangi alokasi memory pada algoritma generation.

Static room geometry tidak harus masuk object pool.

Object Pool terutama digunakan untuk objek yang sering diaktifkan ulang seperti Enemy, Projectile, dan efek tertentu.

## C. Gameplay Balancing

Uji kombinasi:

- Room size.
- Enemy count.
- Enemy composition.
- Spawn distribution.
- Branch availability.
- Reward placement.
- Room sequence.
- Encounter difficulty.

Pastikan procedural generation tidak menghasilkan run yang terlalu mudah atau terlalu sulit secara ekstrem.

## D. Build Validation

Uji pada Windows standalone build.

Periksa:

- Main Menu.
- Run initialization.
- Generation.
- Player placement.
- Enemy navigation.
- Encounter.
- Room clear.
- Reward.
- Exit.
- Player death.
- Retry.
- Save data.

## Final Deliverables

- Modular Room Generation v1.0.
- Forest Room Catalog.
- Forest Encounter Catalog.
- Map Generation Config.
- Validated Graph Generator.
- Validated Placement Solver.
- Runtime Room Assembly.
- Generated NavMesh Integration.
- Room Progression Integration.
- Automated Test Suite.
- Performance Report.
- Windows Demo Build.

## Final Acceptance Criteria

The Last Sasak mampu menghasilkan satu run bioma Hutan dengan variasi layout dan encounter, dapat dimainkan dari awal sampai akhir, serta dapat direproduksi menggunakan seed yang sama.

---

# 7. COMPLETE TECHNICAL STACK

## 7.1 Core Engine

| Technology         | Usage                                                   |
| ------------------ | ------------------------------------------------------- |
| Unity 6.3 LTS      | Main game engine, jika sesuai versi proyek yang dikunci |
| C#                 | Generation logic dan gameplay integration               |
| Unity Physics      | Bounds, collision, overlap, validation                  |
| Unity Scene System | WorldGenerationTest dan ForestRun                       |
| ScriptableObject   | Room, encounter, dan map configuration                  |
| UnityEngine.Pool   | Object pooling                                          |

## 7.2 World Generation

| Technology              | Usage                          |
| ----------------------- | ------------------------------ |
| System.Random           | Deterministic seed             |
| Custom C# Graph         | Room graph                     |
| Custom Placement Solver | Room positioning               |
| Unity Transform         | Connector alignment            |
| Unity Bounds / Grid     | Spatial occupancy              |
| Custom Map Validator    | Graph dan placement validation |

Tidak diperlukan plugin procedural generation eksternal untuk versi pertama.

## 7.3 Navigation

| Technology      | Usage                                      |
| --------------- | ------------------------------------------ |
| AI Navigation   | NavMesh generation dan navigation          |
| NavMeshSurface  | Membuat navigasi dunia                     |
| NavMeshAgent    | Pergerakan musuh                           |
| NavMeshObstacle | Dynamic obstacle apabila diperlukan        |
| NavMeshLink     | Koneksi navigasi khusus apabila diperlukan |

## 7.4 Environment Creation

| Technology       | Usage                                            |
| ---------------- | ------------------------------------------------ |
| Blender          | Modeling modular room dan environment            |
| ProBuilder       | Greybox dan prototyping                          |
| Unity Materials  | Material dan tekstur                             |
| Shader Graph     | Shader environment apabila diperlukan            |
| URP              | Pertahankan pipeline proyek yang sudah digunakan |
| Unity Scene View | Layout authoring                                 |
| Unity Gizmos     | Connector dan spawn visualization                |

## 7.5 Optional Tools

| Tool            | Kapan digunakan                                                    |
| --------------- | ------------------------------------------------------------------ |
| Unity Splines   | Jalur lingkungan, placement mengikuti kurva, atau dekorasi organik |
| Addressables    | Ketika kebutuhan asset loading dan memory management membesar      |
| FBX Exporter    | Pertukaran atau ekspor geometry ProBuilder                         |
| Memory Profiler | Investigasi memory lebih mendalam                                  |
| Project Auditor | Pemeriksaan kualitas dan potensi masalah project                   |

## 7.6 Testing & Debugging

| Tool                     | Usage                                |
| ------------------------ | ------------------------------------ |
| Unity Test Framework     | Edit Mode dan Play Mode tests        |
| Unity Profiler           | CPU, GPU, memory, dan rendering      |
| Frame Debugger           | Investigasi rendering                |
| Gizmos                   | Room bounds, connector, spawn points |
| Unity Console            | Runtime warning dan error            |
| Custom Generation Report | Hasil setiap seed                    |

## 7.7 Version Control

Gunakan Git dan Git LFS sesuai setup repository proyek.

File yang perlu diperhatikan:

```
*.fbx
*.blend
*.psd
*.tif
*.tiff
*.exr
*.hdr
*.png
*.wav
*.mp3
*.ogg
*.mp4
*.mov
```

Pastikan Unity menggunakan:

- Visible Meta Files.
- Force Text Serialization.
- Asset dan Scene Version Control.

Jangan mengabaikan file `.meta` ketika melakukan commit.

---

# 8. RECOMMENDED PROJECT STRUCTURE

```
Assets/
│
└── _Project/
    │
    ├── Scenes/
    │   ├── MainMenu.unity
    │   ├── CombatTest.unity
    │   ├── ForestRun.unity
    │   ├── WorldGenerationTest.unity
    │   └── Hub.unity
    │
    ├── Scripts/
    │   │
    │   ├── World/
    │   │   │
    │   │   ├── Generation/
    │   │   │   ├── WorldGenerationCoordinator.cs
    │   │   │   ├── RunMapGenerator.cs
    │   │   │   ├── RoomGraphGenerator.cs
    │   │   │   ├── RoomPlacementSolver.cs
    │   │   │   ├── RoomPrefabAssembler.cs
    │   │   │   └── MapValidator.cs
    │   │   │
    │   │   ├── Graph/
    │   │   │   ├── RoomGraph.cs
    │   │   │   ├── RoomNodeData.cs
    │   │   │   └── RoomEdgeData.cs
    │   │   │
    │   │   ├── Rooms/
    │   │   │   ├── RoomController.cs
    │   │   │   ├── RoomConnector.cs
    │   │   │   ├── RoomManager.cs
    │   │   │   ├── RoomExit.cs
    │   │   │   └── RoomGateController.cs
    │   │   │
    │   │   ├── Encounters/
    │   │   │   ├── RoomEncounterController.cs
    │   │   │   ├── EncounterEntry.cs
    │   │   │   └── EncounterValidator.cs
    │   │   │
    │   │   ├── Navigation/
    │   │   │   └── RuntimeNavMeshBuilder.cs
    │   │   │
    │   │   └── Environment/
    │   │       └── SetDressingGenerator.cs
    │   │
    │   └── ...
    │
    ├── ScriptableObjects/
    │   │
    │   └── World/
    │       ├── Rooms/
    │       ├── RoomCatalogs/
    │       ├── Encounters/
    │       ├── Maps/
    │       └── Biomes/
    │
    ├── Prefabs/
    │   │
    │   └── World/
    │       ├── Rooms/
    │       │   └── Forest/
    │       ├── Connectors/
    │       ├── Gates/
    │       ├── Environment/
    │       ├── Obstacles/
    │       └── Loot/
    │
    ├── Art/
    │   └── Environment/
    │       └── Forest/
    │
    ├── Materials/
    │   └── Environment/
    │
    ├── Tests/
    │   ├── EditMode/
    │   └── PlayMode/
    │
    └── Documentation/
        ├── WorldArchitecture.md
        ├── RoomPrefabStandard.md
        ├── EncounterDesign.md
        └── GenerationRules.md
```

Struktur ini merupakan target pengembangan. Tidak harus memindahkan seluruh script yang sudah ada sekaligus. Migrasikan secara bertahap untuk menghindari konflik referensi Unity.

---

# 9. SYSTEM DEPENDENCY & IMPLEMENTATION ORDER

Urutan dependency yang wajib diikuti:

```
WORLD DESIGN SPECIFICATION
          |
          v
ROOM MODULE STANDARD
          |
          v
ROOM DATA & CATALOG
          |
          v
GREYBOX ROOM LIBRARY
          |
          v
ENCOUNTER & SPAWN INTEGRATION
          |
          v
ROOM GRAPH GENERATOR
          |
          v
SPATIAL PLACEMENT SOLVER
          |
          v
RUNTIME ROOM ASSEMBLY
          |
          v
NAVMESH INTEGRATION
          |
          v
ROOM PROGRESSION
          |
          v
ENVIRONMENT ART & SET DRESSING
          |
          v
VALIDATION & TESTING
          |
          v
OPTIMIZATION & RELEASE
```

Jangan membangun graph generator sebelum room definition mempunyai struktur yang jelas.

Jangan mengaktifkan EnemySpawner sebelum NavMesh siap.

Jangan membuat final environment art sebelum greybox melewati pengujian navigasi dan combat.

---

# 10. GLOBAL ACCEPTANCE CHECKLIST

## Architecture

- Setiap sistem memiliki tanggung jawab terpisah.
- Room prefab mengikuti standard hierarchy.
- Room definition tidak menyimpan runtime state.
- Encounter definition terpisah dari room definition.
- Generator tidak membuat Player, Camera, HUD, atau service cadangan.
- Tidak ada ketergantungan tersembunyi antar-scene.

## Generation

- Graph valid.
- Seed deterministik.
- Start dan Exit selalu tersedia.
- Branching memenuhi aturan.
- Room placement valid.
- Connector cocok.
- Footprint tidak overlap.
- Fallback tersedia.

## Gameplay

- Player dapat bergerak di seluruh map.
- Semua room memiliki navigasi valid.
- Encounter sesuai room.
- Enemy spawn valid.
- Enemy menggunakan pooling.
- Room clear berfungsi.
- Reward tidak duplikat.
- Exit dapat diakses.
- Retry mereset run.

## Environment

- Forest biome mempunyai identitas visual.
- Obstacle tidak merusak navigasi.
- Jalur gameplay mudah dibaca.
- Connector tidak tertutup dekorasi.
- Spawn point tetap valid setelah art pass.

## Quality

- Automated tests lulus.
- Banyak seed telah diuji.
- Tidak ada softlock.
- Performance telah diprofil.
- Windows build berhasil dimainkan.

---

# 11. DEFINITION OF DONE

Milestone Modular World Generation v1.0 dinyatakan selesai apabila:

1. Developer dapat menambahkan room baru hanya melalui room prefab dan RoomDefinitionSO.
2. Developer dapat menambahkan encounter baru melalui EncounterDefinitionSO.
3. Generator dapat menyusun map dari room catalog.
4. Map dapat direproduksi menggunakan seed yang sama.
5. Semua room saling terhubung dengan benar.
6. NavMesh terbentuk sebelum Enemy diaktifkan.
7. Pemain dapat menyelesaikan run dari Start sampai Exit.
8. Encounter, reward, dan room lifecycle bekerja.
9. Tidak ada softlock pada test seeds.
10. Build Windows dapat dimainkan tanpa bergantung pada Unity Editor.

**Final Milestone: MODULAR WORLD GENERATION v1.0 — COMPLETE.**

Dokumen ini menjadi baseline engineering untuk pengembangan procedural world Level 01 Hutan. Penambahan biome baru, bentuk generator yang lebih kompleks, serta sistem streaming dunia menjadi pekerjaan milestone terpisah setelah baseline ini terbukti stabil.

## **12. Verifikasi stack dan keputusan versi**

Berdasarkan dokumentasi Unity yang tersedia pada 24 November 2026, stack yang direkomendasikan dapat menggunakan:

| Package              | Versi terverifikasi untuk Unity 6000.3 |
| -------------------- | -------------------------------------- |
| Unity Editor         | 6.3 LTS                                |
| AI Navigation        | 2.0.16                                 |
| ProBuilder           | 6.0.7                                  |
| Splines              | 2.9.2                                  |
| Addressables         | 4.2.0                                  |
| Unity Test Framework | Core package                           |

Unity 6.3 LTS merupakan rilis LTS terbaru. AI Navigation, ProBuilder, dan Splines memiliki rilis yang kompatibel dengan Editor 6000.3.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://activation.unity3d.com\&sz=32)

Unity+4



Unity Test Framework mendukung Edit Mode dan Play Mode testing. Addressables tersedia, tetapi menurutku belum perlu menjadi dependency wajib pada milestone pertama.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity.com\&sz=32)

Unity Editor+1



**Penting sebelum instalasi:** periksa `ProjectSettings/ProjectVersion.txt` dan `Packages/packages-lock.json`. Jangan meng-upgrade versi Unity atau package di tengah pengerjaan hanya untuk mengikuti angka rekomendasi di dokumen.

## **13. Dari mana mulai mengerjakannya?**

Aku menyarankan jangan langsung masuk ke Phase 06 dan menulis graph generator.

Urutan pekerjaan pertama:

**Immediate Implementation**

Start Here

1. **Phase 00 — World Foundation Audit**

   Periksa semua script World yang sudah ada. Tentukan mana yang dipertahankan dan mana yang perlu diperluas.
2. **Phase 01 — World Design Specification**

   Kunci standar grid, connector, ukuran room, kategori room, dan aturan spatial gameplay.
3. **Phase 02 — Room Module System**

   Buat satu template prefab yang menjadi standar untuk semua room berikutnya.
4. **Phase 03–05 — Data, Library & Encounter**

   Buktikan beberapa room dan encounter bekerja sebelum masuk ke procedural generation.

**Satu keputusan tambahan:** pertahankan `CombatTest` sebagai test environment permanen. Jangan menjadikannya scene generator hanya karena ingin mencoba layout baru.

Dengan roadmap ini, hasil akhirnya bukan sekadar generator yang bisa membuat map acak. Kamu akan mempunyai sebuah **World Generation Framework** yang bisa digunakan untuk terus menambahkan konten Level 01 Hutan, kemudian dikembangkan untuk biome berikutnya tanpa perlu menulis ulang fondasi sistem.

Menurutku, itulah investasi engineering yang paling bernilai untuk tahap pengembangan The Last Sasak sekarang.