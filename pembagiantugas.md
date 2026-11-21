Menurutku, cara paling aman adalah **membagi pekerjaan berdasarkan domain sistem, bukan membagi jumlah script secara rata**. Jadi kamu dan temanmu punya wilayah kerja masing-masing, bisa melakukan review, perbaikan, dan testing secara bersamaan tanpa harus menunggu satu sama lain.

Aku sudah membaca kedua dokumen: `README.md` dan `SISTEM_GAMEPLAY.md`. Keduanya menggambarkan proyek **The Last Sasak: The Unknown Land**, termasuk arsitektur, status fitur, scene, script, dan roadmap-nya.

Ada satu hal penting: pembagian di bawah ini berdasarkan dokumentasi proyek. Untuk memastikan bug atau kualitas implementasi yang sebenarnya, tetap perlu memeriksa source code dan Inspector Unity.

# **1. Pembagian tugas yang aku rekomendasikan**

Dokumentasi sebenarnya sudah mencantumkan PIC awal:

- Bagas: GameManager, Inventory, SaveSystem.
- Keanu: Player, ItemSwitching, Enemy AI, CombatSystem, UI, Camera.
- Assassin Lead: arsitektur, integrasi, dan review.&#x20;

  Pasted markdown.md



Aku sarankan pembagian tersebut dipertahankan, tetapi ruang lingkupnya diperjelas.

**TRACK A — Keanu**

Gameplay & Feel

Fokus: bagaimana game dimainkan, dirasakan, dan merespons aksi pemain.

**Tanggung jawab utama**

- Player input, movement, facing, dan action controller.
- Attack, dodge, parry, hitbox, dan i-frame.
- CombatSystem dan CombatResolver.
- EnemyBrain, EnemyMovement, dan EnemyAttackController.
- Camera, Cinemachine, hitstop, dan camera shake.
- HUD dan feedback gameplay.

**Review dan upgrade**

- Hilangkan potensi konflik `PlayerMovement` lama dengan `PlayerMotor`.
- Periksa alur serangan, parry, dodge, dan cancel.
- Evaluasi perilaku AI Idle → Chase → Attack.
- Perbaiki telegraph serangan dan respons musuh.
- Tuning `AttackDefinitionSO` dan `EnemyDefinitionSO`.
- Pastikan camera shake dan hitstop tidak mengganggu kontrol.

**Scene utama untuk testing:** `CombatTest` dan `ForestRun`.

**TRACK B — Bagas**

Core & Progression

Fokus: stabilitas game, alur run, penyimpanan data, dan progression.

**Tanggung jawab utama**

- GameManager dan GameState.
- GameEvents dan lifecycle service.
- SaveSystem, SaveData, dan SaveValidator.
- CurrencyWallet, Souls Coin, dan ExperienceSystem.
- Inventory, InventorySlot, dan InventoryItem.
- PermanentUpgradeSystem.
- MainMenu, Hub, dan scene transition.

**Review dan upgrade**

- Pastikan Retry kembali ke scene run yang benar.
- Periksa perpindahan scene dan singleton `DontDestroyOnLoad`.
- Pastikan data Souls dan permanent HP tidak hilang setelah aplikasi ditutup.
- Uji EXP yang harus reset ketika run baru dimulai.
- Evaluasi validasi save dan penanganan data tidak valid.
- Kembangkan sistem Inventory dari model sederhana menjadi sistem yang lebih siap dipakai UI.
- Pastikan UI Inventory dan HUD membaca data dari sistem yang sama.

**Scene utama untuk testing:** `MainMenu` dan `Hub`.

Pembagian ini cukup seimbang dari sisi tanggung jawab, meskipun Track A cenderung lebih banyak pekerjaan gameplay dan Track B lebih banyak pekerjaan stabilitas sistem.

# **2. Bagaimana supaya kalian tidak saling menunggu?**

Ini justru bagian paling penting. Gunakan sistem **parallel development dengan kontrak antar-sistem**.

Artinya, kalian tidak perlu menunggu fitur teman selesai. Kalian hanya perlu menyepakati cara sistem kalian berkomunikasi.

Contoh:

**Contoh: Inventory**

**Bagas**

Mengerjakan data Inventory, item, slot, penambahan dan penghapusan item.

**Keanu**

Mengerjakan tampilan Inventory, slot UI, ikon, deskripsi, dan interaksi.

**Kontrak yang disepakati bersama**

- Bagas menentukan struktur data item dan cara membaca Inventory.
- Keanu menggunakan struktur tersebut untuk menampilkan UI.
- Nama class, properti publik, dan event yang digunakan disepakati terlebih dahulu.
- Keduanya tidak mengubah API yang sama secara sepihak.

  Bisa dikerjakan bersamaan

Pola yang sama juga berlaku untuk sistem lainnya.

| Integrasi               | Pemilik utama | Yang boleh dikerjakan paralel                             |
| ----------------------- | ------------- | --------------------------------------------------------- |
| GameManager ↔ UI        | Bagas ↔ Keanu | Bagas mengurus state dan scene; Keanu menyiapkan tampilan |
| Inventory ↔ InventoryUI | Bagas ↔ Keanu | Data dan tampilan                                         |
| Health ↔ CombatSystem   | Keanu         | Testing HUD dan feedback tanpa mengubah kontrak Health    |
| EnemyBrain ↔ Encounter  | Keanu         | AI dan pengujian encounter dengan kontrak yang tetap      |
| SaveSystem ↔ Hub        | Bagas         | Penyimpanan dan tampilan interaksi upgrade                |

**Aturan penting:** jangan mengubah nama class, signature event, enum, atau field publik yang digunakan sistem lain tanpa memberi tahu pemilik sistem tersebut.

Dokumentasi juga sudah menentukan beberapa kontrak seperti `EnemyBrain.Bind(...)`, `Health.OnDied`, dan `RoomController`. Jadikan kontrak tersebut sebagai batas kerja, bukan langsung dirombak ketika sedang review.&#x20;

Pasted markdown.md



# **3. Setup Git agar pekerjaan tidak saling menimpa**

Aku sangat menyarankan menggunakan Git dengan dua branch terpisah.

```

main
│
├── review/keanu-gameplay
│   ├── player-review
│   ├── combat-review
│   ├── enemy-ai-review
│   └── camera-polish
│
└── review/bagas-core
    ├── game-manager-review
    ├── save-system-review
    ├── inventory-review
    └── progression-review

```

Alur kerjanya:

1. Kalian berdua memulai dari commit `main` yang sama.
2. Masing-masing mengerjakan branch sendiri.
3. Commit perubahan kecil dan spesifik.
4. Tes perubahan di branch masing-masing.
5. Setelah lolos, buat merge request atau pull request ke `main`.
6. Lakukan integration testing setelah perubahan digabung.

**Khusus Unity:** hindari mengedit scene yang sama secara bersamaan. File `.unity` dan `.prefab` bisa menghasilkan konflik Git yang cukup merepotkan.

Pembagian scene yang aku rekomendasikan:

| Area                                | Pemilik edit |
| ----------------------------------- | ------------ |
| CombatTest                          | Keanu        |
| ForestRun                           | Keanu        |
| MainMenu                            | Bagas        |
| Hub                                 | Bagas        |
| ProjectSettings                     | Bagas        |
| Player dan Enemy prefab             | Keanu        |
| Save / Inventory / Progression data | Bagas        |

Kalau satu orang perlu memeriksa scene milik orang lain, lakukan secara read-only terlebih dahulu. Catat temuan, lalu serahkan perbaikannya kepada pemilik scene.

Pastikan kedua komputer menggunakan Unity `6000.3.23f1`, versi package yang sama, dan aturan `.meta` tetap terjaga. Jangan menghapus atau membuat `.meta` secara manual. Hal ini juga sudah ditekankan dalam README.&#x20;

Pasted markdown.md



# **4. Potential review points yang paling layak di-upgrade**

Aku kelompokkan menjadi tiga prioritas supaya kalian tidak terjebak memperbaiki hal kecil sementara fondasi game belum stabil.

## P&#x30;**&#x20;Prioritas pertama — Reliability dan bug**

| Review point         | Yang harus diperiksa                                                      | PIC           |
| -------------------- | ------------------------------------------------------------------------- | ------------- |
| Duplicate component  | Apakah `PlayerMovement` masih berjalan bersama `PlayerMotor`?             | Keanu         |
| Legacy Enemy AI      | Apakah `EnemyController` lama bentrok dengan `EnemyBrain`?                | Keanu         |
| Scene transition     | Apakah semua tombol menu membuka scene yang sesuai?                       | Bagas         |
| Retry system         | Apakah Retry kembali ke `LastRunScene`?                                   | Bagas         |
| Singleton            | Apakah service DDOL mengalami duplikasi atau kehilangan referensi?        | Bagas         |
| Event lifecycle      | Apakah subscription dilepas ketika object dinonaktifkan atau dihancurkan? | Bersama       |
| Inspector references | Missing Script, null reference, dan komponen yang salah terpasang         | Pemilik scene |
| Save system          | Apakah data tetap benar setelah aplikasi ditutup dan dibuka lagi?         | Bagas         |
| NavMesh              | Apakah semua musuh bisa mencapai target dengan benar?                     | Keanu         |

Ada satu hal yang cukup menarik dari dokumen kalian:

**Status Retry belum konsisten antara kedua dokumen.**

README menuliskan Retry menggunakan `GameManager.LastRunScene` sebagai pekerjaan roadmap, sedangkan `SISTEM_GAMEPLAY.md` menjelaskan `RestartRun()` sudah memuat scene run terakhir. Ini wajib diperiksa langsung di source code agar dokumentasi dan implementasi tidak berbeda.&#x20;

Pasted markdown.md

Pasted markdown (2).md



## P&#x31;**&#x20;Prioritas kedua — Gameplay quality dan sistem**

[Ravenswatch Review | NoobFeed](https://images.openai.com/static-rsc-4/PAcM_5TNCrNo8wqbD6SvMWU4hwhg4tQouOjXVyMeRyEayAEqjsx7awOgMibWvbIYVBSd_CCrl8uwNS385uQSPNACy4WZOUkSyZTOSioixLA5OSkFsDZP59KB2vdaSln_s5nvARRvnZRBXQ4Mt_iu3rJLsgw_tx6LNCUssYaI0Yk?purpose=inline)

**1. Combat feel**

- Startup, active, dan recovery attack.
- Hit detection tidak menghasilkan double hit.
- Parry punya feedback yang jelas.
- Dodge dan i-frame konsisten.
- Hitstop tidak merusak pause atau scene transition.

  PIC: Keanu

[Ruins of Albion on Steam](https://images.openai.com/static-rsc-4/zP3MywELI7q9YWJSggdHLQTiO_nl3QmxTI2pkf5GwffZ217gF0ciQBMaZ3aJLCcJhOSS7t_UsUSbs7zNcMLH1d5fEDnYTUQyfHKpbzVSRGetJWhncJt_Utt866m8u7ZbcuzzLBufGrMqiiJqS1O_ssHX-JUPgptRfa_UywKIWDQ?purpose=inline)

**2. Enemy AI**

- Perilaku Idle, Chase, dan Attack.
- Jarak detect dan attack berbeda tiap archetype.
- Musuh tidak terus-terusan bertabrakan di posisi yang sama.
- Ranged enemy menjaga jarak dengan baik.
- Elite mempunyai stagger dan telegraph yang jelas.

  PIC: Keanu

[🗡️ Armor? Ring? Sword? - Here's how my icons could look in your RPG inventory! - Release Announcements - itch.io](https://images.openai.com/static-rsc-4/xwdQOgYCV7KxFUczhCgDyb0AViGZzqI8ZnmJ0-EGeoMCIAC6LmjDcUtuj0SRNhU21kV262Co6QzdYBDloKoofgD663hUtZuq3dXlUva1egvud0-H23Yz44qOzxit9bmqoprQpuuoXZaHhEIPnM-IVBWF6H8WSmtfKbtND3wiPcw?purpose=inline)

**3. Inventory dan progression**

- Struktur item lebih jelas.
- Inventory UI tidak hanya menampilkan `Bag N`.
- Item mempunyai definisi dan perilaku yang konsisten.
- EXP dan Souls mempunyai aturan yang mudah dipahami.
- Permanent upgrade benar-benar bertahan setelah restart.

  Data PIC: Bagas · UI PIC: Keanu

[Stories: The Path Of Destinies im Test - Mehr Konsequenzen als bei Telltale!](https://images.openai.com/static-rsc-4/iGl5BUHv7TgT9-hgS22iKkWXTWBuuYPyRP_YC-qbG4kxG96Lv_LYf7KEGRQaEvRsLeKbxXHA69C4YUbWK_q2i5jptuntAjsFT_H6fV3tq3e4DFNKhJejno01rf45_RxP0wqL8XVACVvVieZ65WFT1xDkUZ_0Na1Bq3wuFenM7XQ?purpose=inline)

**4. Camera dan presentation**

- Follow camera tidak terlalu kaku.
- Cinemachine Impulse sesuai jenis serangan.
- Pergerakan kamera tetap nyaman saat combat.
- Feedback damage, parry, dan elite lebih informatif.

  PIC: Keanu

## P&#x32;**&#x20;Prioritas ketiga — Polish dan ekspansi**

| Area            | Potensi upgrade                                             | PIC     |
| --------------- | ----------------------------------------------------------- | ------- |
| Room generation | Modular room prefab, graph, seed, dan validasi konektivitas | Bersama |
| Elite encounter | Mengaktifkan elite dan StaggerBarUI sebagai tes terpisah    | Keanu   |
| Artifact        | Passive stat modifier menggunakan `IStatModifier`           | Bagas   |
| Hub             | Dialogue dan interaksi NPC                                  | Bagas   |
| VFX             | Attack, parry, dodge, hit, dan death effects                | Keanu   |
| Audio           | SFX combat, UI, ambience, dan feedback                      | Bersama |
| Build           | Build Windows dan testing di luar Unity Editor              | Bersama |

Catatan: `RunMapGenerator` masih berupa stub dan generator lama tidak digunakan di scene playable. Jangan langsung menghabiskan waktu untuk procedural generation sebelum vertical slice stabil. Itu memang urutan yang direkomendasikan di roadmap proyek.&#x20;

Pasted markdown.md



# **5. Manual setup dan audit Unity Editor**

Ini bagian yang sering terlewat ketika project dibuat dengan AI. Script bisa terlihat rapi, tetapi konfigurasi Inspector, prefab, layer, atau scene ternyata belum benar.

Aku sarankan membuat satu checklist audit khusus yang dikerjakan sambil kalian mengerjakan task masing-masing.

**Unity Editor audit checklist**

# **0/30**

**Project configuration**

Bagas

Unity Editor 6000.3.23f1 dan package sesuai manifest

Build Settings berisi scene yang benar

Input System aktif dan tidak ada konfigurasi input ganda

Tag dan Layer cocok dengan GameplayLayers

Physics collision matrix diperiksa

Asset Serialization dan Meta Files konsisten

**Scene dan Inspector**

Bagas + Keanu

Tidak ada Missing Script di scene playable

Semua serialized reference terisi dengan benar

Tidak ada komponen ganda yang menjalankan fungsi sama

Prefab override diperiksa

ScriptableObject menggunakan asset yang benar

Service DDOL tidak terduplikasi setelah pindah scene

**Player dan combat**

Keanu

CharacterController radius, height, center, dan step offset

PlayerMotor menjadi satu-satunya penggerak player

AttackPoint dan hitbox berada di posisi benar

Layer mask serangan dan collision sudah sesuai

Parry, dodge, dan i-frame diuji

Kamera mengikuti CameraTarget dengan benar

**Enemy dan world**

Keanu

NavMeshSurface meliputi area yang dapat dilalui

NavMeshAgent radius, speed, dan stopping distance

Spawn point berada pada posisi valid

Melee dan ranged enemy memakai definition yang benar

Encounter menghitung musuh hidup secara tepat

Gerbang hanya terbuka setelah room cleared

**Save dan progression**

Bagas

Souls bertahan setelah aplikasi ditutup

EXP reset ketika run baru dimulai

Permanent HP tersimpan setelah upgrade

SaveValidator menangani data yang tidak valid

Hub memeriksa jumlah Souls sebelum pembelian

Retry tidak menghapus data permanen

Reset checklist

Checklist tersebut adalah target pemeriksaan yang aku rekomendasikan, bukan klaim bahwa semua konfigurasi itu saat ini salah atau belum diterapkan.

# **6. Jadwal kerja yang bisa dilakukan bersamaan**

Supaya progres kalian kelihatan, aku sarankan menggunakan sprint kecil seperti ini.

| Tahap            | Keanu                    | Bagas                       |
| ---------------- | ------------------------ | --------------------------- |
| 1. Audit         | Player, combat, AI       | Core, save, inventory       |
| 2. Perbaikan     | Movement, combat, AI     | Scene flow, persistence     |
| 3. Polish        | Camera, HUD, feedback    | Inventory, progression, Hub |
| 4. Testing       | CombatTest, ForestRun    | MainMenu, Hub, save/reload  |
| 5. Integrasi     | Periksa koneksi gameplay | Periksa koneksi core        |
| 6. Release check | Gameplay regression      | Save/build regression       |

Setiap task harus memiliki *definition of done* yang sama:

- Perubahan sesuai scope.
- Tidak merusak sistem yang sudah bekerja.
- Tidak ada error compile.
- Inspector dan prefab yang relevan sudah dicek.
- Skenario testing berhasil.
- Perubahan sudah di-commit.
- Temuan atau keterbatasan dicatat.

Jangan menghitung progres hanya dari jumlah script yang diedit. Satu bug serius yang berhasil ditemukan dan diperbaiki jauh lebih bernilai daripada sepuluh script yang sekadar di-refactor.

# **7. Urutan yang paling aku rekomendasikan**

Menurutku, jangan langsung mengejar penambahan fitur baru. Target pertama kalian sebaiknya adalah membuat vertical slice yang benar-benar solid.

1. **Stabilkan fondasi**

   Bereskan Missing Script, Inspector reference, duplicate component, scene transition, dan save.
2. **Perbaiki kualitas gameplay**

   Movement, combat, AI, camera, serta respons UI.
3. **Rapikan progression**

   Inventory, EXP, Souls, permanent upgrade, dan save.
4. **Integrasikan dan regression test**

   Pastikan MainMenu → CombatTest / ForestRun → Game Over → Retry / Hub berjalan konsisten.
5. **Baru ekspansi**

   Modular procedural rooms, elite, artefak, VFX, audio, dan build final.

**Kesimpulan:** gunakan Keanu sebagai pemilik domain Gameplay dan Bagas sebagai pemilik domain Core & Progression. Keduanya bisa mengerjakan review serta upgrade secara bersamaan dengan syarat kontrak antar-sistem dibekukan sementara, kepemilikan file/scene jelas, dan perubahan masuk ke `main` melalui integrasi terkontrol.

Langkah paling efektif berikutnya adalah mengubah pembagian ini menjadi **task board proyek** berisi task, PIC, prioritas, file yang boleh diedit, dependency, status, dan acceptance criteria agar kalian tinggal mengikuti pekerjaan tanpa saling menunggu.

If you want, I can:

- Create a detailed task board based on the recommended sprint schedule
- Draft a communication plan to manage API and contract changes between teams
- Outline a Git workflow with branch strategy and merge process for collaboration