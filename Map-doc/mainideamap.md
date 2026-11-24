Menurutku, **iya. Fokus berikutnya memang sudah waktunya bergeser ke World Generation dan Room Design.** Bahkan, ini merupakan salah satu milestone paling menarik untuk The Last Sasak.

Namun, aku ingin sedikit mengubah pendekatannya.

Jangan langsung membuat *Procedural Map Generator* yang menghasilkan lingkungan secara acak. Bangun terlebih dahulu sebuah **Modular Room & Encounter Generation System**.

Tujuan akhirnya: kamu bisa terus menambahkan desain ruangan, variasi pola, komposisi musuh, dan variasi encounter tanpa harus mengubah sistem generator setiap kali menambahkan konten baru.

# **1. Arah pengembangan berikutnya**

Next Major Milestone

# **Modular World Generation v1.0**

**Procedural Room, Encounter & Spatial Gameplay**

Membangun sistem yang mampu menyusun ruangan secara procedural, menghubungkan antar-ruangan, menentukan encounter, mengatur spawn musuh, dan menghubungkannya dengan progression run.

CURRENT FOUNDATION

**Player & Combat System**

Player, combat resolver, enemy behavior, health, reward, room controller, object pooling, dan NavMesh.

NEXT

**Room Architecture & Pattern Library**

Room prefab, connector, spawn point, room definition, dan encounter definition.

THEN

**Procedural Generation**

Room graph, room placement, random seed, validasi konektivitas, dan NavMesh.

FINAL INTEGRATION

**Gameplay Encounter Loop**

Explore → Encounter → Combat → Reward → Room Cleared → Next Room.

Roadmap engineering yang kamu lampirkan sebenarnya sudah mengarah ke sini: Enemy & Encounter → Vertical Slice → Procedural Generation. Jadi, sebelum generator diperluas, pastikan satu encounter sudah terbukti berjalan dari spawn sampai room cleared. Tidak perlu mengulang sistem Player yang sudah selesai; cukup pastikan pengujian regresinya lolos.&#x20;

Pasted text(20261124-120517).txt



# **2. Konsep utama: Room Pattern Library**

Menurutku, ini bagian yang harus kamu rancang terlebih dahulu.

Bayangkan setiap ruangan merupakan satu modul yang sudah dirancang secara manual, tetapi bisa disusun dengan banyak kombinasi.

Kamu tidak membuat arena dari nol setiap kali game dimulai.

Kamu membuat katalog ruangan, kemudian generator memilih dan menyusunnya.

## **A. Jenis pattern ruangan**

Aku menyarankan mulai dengan beberapa archetype berikut.

[Victor Vran: Fractured Worlds on Steam](https://images.openai.com/static-rsc-4/dn6bzxMvzsdMQH2zITWvjiTRquDPsVdW7kTZOC6McZmsQqiwxAhVdht2ARLvGNKt2aB2bikU_vQx5PN2ehW0DPwxD0hHwfWnt-MLGEFl5gK-b4Rs7TTH9BbtkpUXG_1ePT9rwHJeH-zYb0nk8GW9WlwxyHRjTLZD7O51Rly7Wi4?purpose=inline)

**01. Starting Room**

Ruangan awal pemain. Tidak memiliki encounter agresif. Berfungsi sebagai titik persiapan dan akses menuju area berikutnya.

[Achilles: Legends Untold is an isometric action RPG set to release for Steam Early Access in early 2022 | RPG Site](https://images.openai.com/static-rsc-4/rEfRXYCAJ3_9tOvkiaTpfPLNsyMCTrkRH_l_8kEYEneAi0qR1bqDaxqDYjKG6cYyg8qFlkwV_mYeyJEpP0uhpMKwO1v9irrpo-vO2y0C3upUAWRCyO0MNhwY2KDgBLZHEd8z1xgNoBaYb-4hwKIngBQ_dJ2VogYEr0G9kSiHT1Y?purpose=inline)

**02. Open Combat Room**

Arena relatif luas. Cocok untuk menghadapi beberapa musuh melee dan menguji dodge, parry, serta positioning.

[After Light Fades on Steam](https://images.openai.com/static-rsc-4/j4gBbtcQFe6Iz4c_wE-urxuPvZ15ZqM0Zft9U1abHAa3etzGHI89mJISZOUbnroeXrYpYNg7nFXHnfyI6tElGv38TZX7gRDQFnb8TcWLeqBPdN_YH_uipMmzTzgnswWYRew5qowEX3d1VUGjDOmUr1yAlTiRIQkQ_yV28JDRcRs?purpose=inline)

**03. Obstacle Combat Room**

Memiliki pohon, reruntuhan, pilar, dan penghalang. Menguji positioning, jalur serangan, dan navigasi musuh.

[新手 1～100 攻略｜升級路線・轉職與裝備目標｜無盡冒險Online](https://images.openai.com/static-rsc-4/crim0mP0F4vGB-RTtMDR-neEsdLWtfWwxM31jwQU__OqgMmf9ZEg004Gog4-Jie-7igXZxuLhUFegZt2dUp7Rf9-tbgjpI2lRN-_K-QY7pdRo4Tt3wjufVGk3OaFLSVFN4L0hBi59UsD4u_qkYDC5zhz6ygVhOInmDp1O4iovZo?purpose=inline)

**04. Corridor / Ambush Room**

Jalur lebih sempit dan memiliki beberapa titik pertemuan. Cocok untuk encounter yang menuntut perhatian terhadap arah serangan.

[Optillusion (@PlayOptillusion) on X](https://images.openai.com/static-rsc-4/D5k6uWDdZQC1niuuhzK64F0CqMHjLHV39_D_1f42ruGsovJMqWKzXF2gbHjJvDYvaheUQog0JvF0qA-EKRAR7GmL_TbdWNINx5ci7Kt_q3jsNdVZmngxMjcn4EMhMxGt5VOnO6-lAaK_dOXZv43a7lVt1pUJ7g-aTUeoo9XDzG0?purpose=inline)

**05. Reward Room**

Ruangan hadiah, loot, atau resource. Tidak harus selalu menghasilkan pertarungan.

[240+ Enchanted Forest RPG Battle Maps Bundle, Dungeons and Dragons, Iso, High Detail Bundle, Varied Terrains, Campaign, Aerial Top View. - Etsy UK](https://images.openai.com/static-rsc-4/JCEArpnrNybJNERlRPZNyN26OSA1BD2UGb7I7x-AZfzVZjDXo5k0FeMHqgNrpdb4aTuTuOvSlj0NwtUP8NKQS1i-PCXSjt7tpmgHxV1YfPw0-Va8QzpR8UhbhQgLuz9vj-_vjHqL_YtiQ0QMuUTZqLWKkhqyv38mqrkhQxLKxyM?purpose=inline)

**06. Elite / Boss Room**

Ruangan khusus yang memiliki ruang gerak, arena, dan titik spawn sesuai kebutuhan encounter yang lebih sulit.

Enam jenis itu tidak perlu langsung dibuat semuanya. Mulailah dari empat: Starting, Open Combat, Obstacle Combat, dan Reward.

Yang penting bukan jumlahnya, tetapi setiap ruangan memberikan pengalaman spatial yang berbeda.

## **B. Buat connector system**

Setiap ruangan perlu memiliki titik sambungan yang konsisten.

Contohnya:

[《暗黑3》是如何设计随机地下城的？ - GameRes游资网](https://images.openai.com/static-rsc-4/d2Jo-SjdBlFY1KGSysZ-gBIgBVLs25SpHcvsOZk1NDf75A53mrjNcuGHtsik0MzArfHaK28Xs-uzTgr8ZsvpJ6VIsNsupHOi_RxEm8ZmrRvSVDx0WGGCkr_hB8P1XO0CUms6niIVykWe-owS7GNllktbGA3vNh2C0jpJn8jXqQQ?purpose=inline)

[gameres.com](https://www.gameres.com/869806.html?utm_source=chatgpt.com)

Bayangkan struktur prefab:

```
Room_OpenCombat_01
│
├── RoomGeometry
│   ├── Floor
│   ├── Walls
│   └── Environment
│
├── RoomConnectors
│   ├── Entrance
│   ├── Exit_North
│   ├── Exit_East
│   └── Exit_West
│
├── SpawnPoints
│   ├── EnemySpawn_01
│   ├── EnemySpawn_02
│   └── EnemySpawn_03
│
├── LootPoints
│
├── EncounterPoints
│
└── RoomController
```

Setiap connector harus mempunyai informasi arah, ukuran, dan kecocokan sambungan.

Dengan begitu, generator tidak sekadar menempelkan ruangan secara acak. Generator harus mengetahui apakah dua ruangan memang dapat disambungkan.

Ini sejalan dengan rancangan Modular Room-Based Generation di engineering roadmap milikmu.&#x20;

Pasted text (2)(2).txt



# **3. Pisahkan Room Pattern dari Encounter Pattern**

Ini keputusan arsitektur yang menurutku sangat penting.

**Map Pattern menentukan bentuk dan struktur ruangan. Encounter Pattern menentukan pertarungan yang terjadi di dalamnya.**

Keduanya jangan digabung menjadi satu sistem.

Contoh:

**Room: OpenCombat_01**

[Dive into D&D's Crossroads maps: unravel tales, face thrilling quests, and discover legendary loot — Inked Insights](https://images.openai.com/static-rsc-4/hiCsNyalyiTr7eJbtTIKDMOztGE3wY4OPgIFLZt8exn5jKK8d8-TfZI8hiKhyKilt8vxqlczyPhIPXR-oBYm7wQxUeJKKj0FOmn1ZPSaTs6O4GvW13a-ugZgtrMYyIyOb-1eMuIL3QyThsilTdEt6v2nmuGLaXEu1pLSdibAiik?purpose=inline)

[inked-insights.com](https://www.inked-insights.com/resourcelibrary/crossroads-chronicles-epic-adventures-amp-hidden-treasures-in-dampd-battle-maps?utm_source=chatgpt.com)

**Encounter A — Melee Rush**

- 3 Vanguard
- Spawn dari beberapa titik berbeda.
- Agresivitas dikontrol CombatDirector.

  **Encounter B — Ranged Support**
- 2 Vanguard
- 1 Ranged Enemy
- Kombinasi tekanan melee dan proyektil.

  **Encounter C — Elite Challenge**
- 1 Elite Enemy
- 2 Vanguard
- Encounter dengan tingkat kesulitan lebih tinggi.

Satu ruangan bisa memiliki beberapa encounter yang kompatibel.

Sebaliknya, satu encounter bisa digunakan di beberapa ruangan selama kondisi spasialnya sesuai.

Ini yang membuat konten game bisa bertambah terus tanpa membuat kode generator semakin kompleks.

## **Struktur data yang disarankan**

| ScriptableObject        | Tanggung jawab                                   |
| ----------------------- | ------------------------------------------------ |
| `RoomDefinitionSO`      | Identitas dan karakteristik ruangan              |
| `RoomCatalogSO`         | Daftar modul ruangan                             |
| `EncounterDefinitionSO` | Komposisi musuh dan aturan encounter             |
| `EnemySpawnProfileSO`   | Pengaturan distribusi spawn                      |
| `MapGenerationConfigSO` | Aturan pembentukan satu run                      |
| `RoomPatternSO`         | Kategori atau aturan pola layout jika diperlukan |

Tidak harus semuanya langsung dibuat. `RoomPatternSO` bahkan bisa ditunda apabila `RoomDefinitionSO` sudah cukup menampung metadata layout.

# **4. Sistem procedural generation**

Setelah room library tersedia, baru bangun generator.

Aku menyarankan menggunakan pendekatan **Graph-Based Room Generation**.

[Example 2 | Edgar - Unity](https://images.openai.com/static-rsc-4/f8zGEHU1_N9QtCz1VFSTeoOhMcpk9Gn_7wsvsPaTIlzpLWAjAB8xbBLJymNUf7e2VYf1KAP_Fyh-4mlzJppbRPnyRZVDqg8pSzdriywyAxDuQ4m278Kk0DXwmZDFLMFV9EHNpuhMHFoQTobIXI46qYJKytmdTm7o5wCyoU-RIJw?purpose=inline)

[ondrejnepozitek.github.io](https://ondrejnepozitek.github.io/Edgar-Unity/docs/examples/example-2/?utm_source=chatgpt.com)

Contoh struktur hasil generator:

```
                [Reward]
                   |
[Start] — [Combat A] — [Combat B]
                   |          |
              [Combat C] — [Elite]
                              |
                            [Exit]
```

Setiap node mewakili satu ruangan.

Generator bertanggung jawab untuk:

1. Memilih ruangan dari RoomCatalog.
2. Menentukan konektivitas berdasarkan connector.
3. Menempatkan ruangan tanpa overlap.
4. Menjamin jalur Start sampai Exit tersedia.
5. Menentukan encounter yang kompatibel.
6. Menyimpan random seed untuk reproduksi hasil.

Penting: gunakan **seed-based generation**. Kalau menemukan bug di suatu layout, kamu bisa menghasilkan ulang layout yang sama menggunakan seed yang sama.

Aku juga merekomendasikan generator tidak membangun lantai atau dinding menggunakan `CreatePrimitive`. Ia cukup menggunakan prefab ruangan yang memang sudah dirancang di Editor.

# **5. Integrasi dengan gameplay**

Inilah bagian yang akan membuat sistemnya terasa benar-benar hidup.

Alur yang aku rekomendasikan:

**RunMapGenerator**

Membentuk graph dan menempatkan room prefab.

**RuntimeNavMeshBuilder**

Membangun navigasi untuk susunan level yang sudah selesai.

**RoomManager**

Mengaktifkan ruangan sesuai progres pemain.

**RoomEncounterController**

Membaca encounter dan menentukan musuh yang diaktifkan.

**EnemySpawner + ObjectPool**

Mengaktifkan prefab musuh pada spawn point yang sesuai.

**EnemyBrain + CombatSystem**

Pertarungan berlangsung.

**Room Cleared → Reward → Exit Unlocked**

Pemain melanjutkan perjalanan.

Beberapa aturan yang jangan terlewat:

- Musuh hanya diaktifkan ketika encounter dimulai.
- Spawn point harus berada pada area NavMesh yang valid.
- Musuh tidak boleh muncul terlalu dekat dengan Player.
- Encounter tidak boleh dimulai sebelum navigasi siap.
- Room Manager tidak perlu mengetahui detail FSM EnemyBrain.
- RoomEncounterController tidak perlu mengetahui bagaimana graph map dibuat.

Ini menjaga sistem yang sekarang sudah kamu bangun tetap terpisah dan bisa dikembangkan.

# **6. Roadmap implementasi yang aku rekomendasikan**

Aku akan membaginya menjadi enam stage.

| Stage | Nama                       | Output                                   |
| ----- | -------------------------- | ---------------------------------------- |
| 00    | Combat & Enemy Validation  | Satu encounter lengkap terbukti bekerja  |
| 01    | Room Module Architecture   | Struktur prefab dan connector standar    |
| 02    | Room Pattern Library       | 4–6 room prefab pertama                  |
| 03    | Encounter Configuration    | Beberapa komposisi musuh berbasis data   |
| 04    | Procedural Room Generation | Graph, placement, seed dan validasi      |
| 05    | Gameplay Integration       | NavMesh, spawn, reward, room progression |

**Stage 00 hanya menjadi syarat masuk.** Karena sebagian besar sistem tersebut sudah ada pada scene-mu, tidak perlu membangun ulang.

## **Acceptance criteria milestone**

Menurutku, milestone ini baru dianggap selesai apabila:

- Satu run bisa menghasilkan layout berbeda.
- Seed yang sama menghasilkan layout yang sama.
- Semua ruangan terhubung dan dapat dijangkau.
- Tidak ada prefab ruangan yang overlap secara tidak sengaja.
- Connector antar-ruangan cocok.
- Encounter sesuai dengan kategori ruangan.
- Musuh spawn melalui sistem pooling.
- NavMesh mendukung navigasi di semua ruangan.
- Room Cleared membuka akses ke ruangan berikutnya.
- Reward dapat diperoleh melalui gameplay.
- Retry dan regenerasi run tidak menyisakan state sebelumnya.

# **7. Setelah milestone ini**

Aku melihat pengembangannya bisa berkembang seperti berikut:

## **The Last Sasak — Development Roadmap**

1. **Player & Combat Domain**

   Fondasi sudah tersedia
2. **Enemy & Encounter Integration**

   Memvalidasi interaksi Player, musuh, reward, dan room lifecycle.
3. **Modular World Generation**

   Room library, pattern, graph, dan procedural placement.
4. **Spatial Combat & Enemy Variety**

   Enemy flanking, ranged pressure, variasi komposisi, dan penyesuaian encounter terhadap layout.
5. **Roguelike Progression**

   Reward, artefak, pilihan upgrade, dan peningkatan tingkat kesulitan.
6. **Elite & Boss Encounter**

   Arena khusus dan pertarungan yang lebih kompleks.
7. **Vertical Slice Polish**

   Environment, audio, VFX, UI, performance, dan balancing.

## **Keputusan akhirku**

Aku setuju dengan idemu untuk mulai membangun map pattern yang terus bisa diregenerasi dan ditambahkan. Justru sistem ini bisa menjadi fondasi replayability The Last Sasak.

Namun, ada satu prinsip desain yang ingin aku tekankan:

**Jangan membuat random map yang hanya menghasilkan bentuk berbeda. Buat random map yang menghasilkan pengalaman pertarungan berbeda.**

Ruangan terbuka harus punya fungsi gameplay. Koridor harus punya fungsi gameplay. Ruangan dengan pilar dan penghalang harus punya fungsi gameplay.

Nantinya, kombinasi antara:

- Spatial layout.
- Enemy composition.
- Enemy spawn positions.
- Encounter difficulty.
- Reward placement.

akan menghasilkan variasi permainan yang jauh lebih bermakna daripada sekadar menukar urutan prefab.

**Langkah pertama yang sebaiknya langsung kamu kerjakan adalah menyusun dokumen&#x20;**`Modular Room Architecture & Encounter Design v1.0`**.** Kunci kontrak Room, Connector, SpawnPoint, RoomDefinition, dan EncounterDefinition sebelum mulai menulis generator utama.