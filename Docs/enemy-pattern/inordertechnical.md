# **Engineering Roadmap — The Last Sasak: Unknown Land**

Kasantara Studio · Technical Development Guide · Berdasarkan GDD v1.0 dan TDD v1.0 · 18 November 2026

## **1. Evaluasi rancangan pengerjaanmu**

Menurutku, arah pengerjaan yang kamu susun sudah benar. Terutama keputusan untuk **menguji combat di satu arena statis terlebih dahulu sebelum mengerjakan procedural generation**.

Itu keputusan engineering yang bagus karena sistem map generation, AI, combat, dan progression saling bergantung. Kalau semuanya dibuat sekaligus, ketika terjadi bug akan sulit menentukan sumber masalahnya.

Namun, ada beberapa hal yang perlu diperbaiki agar arsitekturnya lebih aman, tidak terlalu kompleks, dan mudah dikerjakan bersama tim.



Rekomendasi

&#x20;Pertahankan konsep tiga fase, tetapi susun ulang urutan pengerjaannya menjadi **Project Foundation → Player & Combat → Enemy & Encounter → Vertical Slice → Procedural Generation → Progression → Polish**.



### **1.1 Kesesuaian dengan GDD dan TDD**

Dokumen GDD menetapkan empat mekanik Must Have:

1. Movement.
2. Combat.
3. Progression.
4. Sistem Loot/Reward.

Core Loop-nya adalah eksplorasi, melawan monster, mengumpulkan loot, melakukan upgrade, kemudian menghadapi area yang lebih sulit.&#x20;

Pasted markdown(4).md



TDD kemudian mengatur bagaimana sistem tersebut dibangun, antara lain melalui Component-Based Architecture, FSM, Singleton, Observer/Event-driven, Object Pooling, pemisahan UI dan gameplay, serta Save/Load.&#x20;

Pasted markdown(4).md



Artinya, pembagian tanggung jawabnya harus seperti ini:

| Dokumen             | Menentukan                                               |
| ------------------- | -------------------------------------------------------- |
| GDD                 | Apa yang harus terjadi dalam permainan.                  |
| TDD                 | Bagaimana sistem permainan disusun secara teknis.        |
| Engineering Roadmap | Urutan pembangunan, dependensi, pengujian dan integrasi. |
| Art Bible           | Bagaimana aset dan visual permainan dibuat.              |

Panduan ini menggunakan GDD dan TDD sebagai dasar. Rekomendasi teknis tambahan di bawah merupakan pengembangan engineering, bukan perubahan mekanik yang otomatis dianggap sudah disahkan GDD.

### **1.2 Hal yang perlu diperbaiki sebelum produksi**

Ada tujuh hal yang perlu kamu perhatikan.

**A. Versi Unity pada GDD perlu dikoreksi.**

GDD menulis Unity 3.0 LTS. Ini tidak sesuai dengan penamaan versi Unity modern. Per tanggal 18 November 2026, Unity mencantumkan **Unity 6.3 LTS** sebagai rilis LTS terbaru, dengan dukungan sampai Desember 2027. Untuk proyek demo yang ingin mengunci versi selama produksi, aku merekomendasikan Unity 6.3 LTS dan satu versi patch yang disepakati seluruh tim.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://unity.com\&sz=32)

Unity+1



Jangan sampai anggota tim menggunakan versi Editor berbeda karena perbedaan versi bisa mengakibatkan perubahan project settings atau konflik package.

**B. Pisahkan EventBus dengan event lokal komponen.**

EventBus bukan berarti semua komunikasi antar-script harus melewatinya.

Contohnya:

- Perubahan HP Player cukup menggunakan event milik komponen Health.
- UI mendengarkan perubahan HP Player.
- Kematian musuh dapat diterima oleh pengelola encounter.
- Perubahan status permainan seperti Game Over dapat menggunakan event global.

Dengan demikian, EventBus tidak menjadi sistem komunikasi yang terlalu besar dan sulit dilacak.

**C. Jangan menjadikan GameManager sebagai pusat semua sistem.**

GameManager bertanggung jawab atas state permainan dan perpindahan scene. Jangan memasukkan logika serangan, AI, inventory, dan perhitungan damage ke dalamnya.

**D. Jangan menggabungkan Camera dengan PlayerController.**

Cinemachine harus menjadi sistem kamera tersendiri. PlayerController hanya mengelola pergerakan pemain. Kamera mengikuti target pemain tanpa mengambil alih tanggung jawab movement.

**E. Jangan langsung membuat Auto-Jump menggunakan banyak Raycast.**

GDD tidak menginginkan parkour kompleks. Untuk undakan kecil, `CharacterController.stepOffset` sudah menyediakan kemampuan melewati undakan otomatis sampai batas ketinggian tertentu. Mekanik lompatan otomatis untuk rintangan yang lebih besar dapat dibuat sebagai logika tambahan setelah pergerakan dasarnya stabil.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

Scripting API: CharacterController.stepOffset+1



**F. Jangan membuat CombatSystem menjadi script serba bisa.**

Pisahkan:

- Logika tindakan karakter.
- Deteksi serangan.
- Resolusi damage.
- Health.
- Feedback animasi dan visual.

CombatSystem menjadi tempat penyelesaian interaksi combat, bukan tempat semua fungsi combat ditumpuk.

**G. Terdapat inkonsistensi istilah reward di GDD.**

Pada bagian MVP, terdapat istilah `In-game Exp` dan `Souls Coin`. Sementara bagian Hub menyebut `Souls Exp`. Ketiganya perlu diperjelas.&#x20;

Pasted markdown(4).md



Saran penamaannya adalah memisahkan EXP untuk progression sementara selama satu run dan Souls Coin untuk mata uang permanen. Namun, keputusan final tentang kapan masing-masing diperoleh harus dikonfirmasi kepada Project Lead.

## **2. Target engineering architecture**

Sebelum mengerjakan fitur, kita harus memiliki gambaran bagaimana seluruh sistem berkomunikasi.

Aku merekomendasikan arsitektur **Component-Based + Event-Driven dengan tanggung jawab per domain**. Tidak perlu menggunakan arsitektur Enterprise atau Dependency Injection Framework yang terlalu berat untuk proyek ini.

### **2.1 Gambaran hubungan sistem**

**A. Player domain**

**Input System**

Keyboard / Gamepad

**PlayerInputReader**

Input commands

**PlayerActionController**

Attack / Dodge / Parry

**PlayerMotor + Animator**

Physical movement and animation presentation

**B. Combat domain**

**Attack Detection**

Hitbox / Projectile

**Parry Resolver**

Perfect Parry

**CombatSystem**

Validates combat interactions and resolves damage

**Health Component**

Health / Invulnerability / Death

**C. World and progression domain**

**Enemy AI**

FSM

**Spawner**

Object Pooling

**Room Manager**

Encounter state

**Map Generator**

Room graph

**D. Global services and presentation**

**GameManager**

Global game state

**Event Channels**

Cross-system events

**UIManager**

HUD and screens

**SaveSystem**

Persistent data

Diagram ini menunjukkan pembagian tanggung jawab yang direkomendasikan, bukan urutan eksekusi program secara literal.

Prinsip utamanya: sebuah sistem hanya mengerjakan tanggung jawabnya sendiri.

Contohnya, ketika musuh mati:

1. Health mengetahui bahwa HP sudah mencapai nol.
2. Komponen kematian musuh menangani animasi dan penghentian aktivitasnya.
3. Sistem reward menangani hadiah.
4. Room Manager mengetahui bahwa salah satu musuh sudah mati.
5. Kalau seluruh musuh habis, Room Manager mengubah status ruangan menjadi cleared.
6. UI menerima pemberitahuan untuk memperbarui tampilannya.

Jangan membuat `EnemyAI` langsung mengubah UI, menambahkan mata uang, memindahkan scene, dan mengatur inventory.

### **2.2 Arsitektur komunikasi yang direkomendasikan**

Gunakan dua jenis komunikasi:

| Jenis                     | Kapan digunakan                                                         |
| ------------------------- | ----------------------------------------------------------------------- |
| C# Events                 | Komunikasi lokal antar-komponen yang memiliki hubungan jelas.           |
| Event Channels / EventBus | Mengirim peristiwa global yang perlu diketahui beberapa sistem berbeda. |

Contoh event yang layak disediakan:

| Event                | Pengirim             | Penerima             |
| -------------------- | -------------------- | -------------------- |
| `OnHealthChanged`    | Health               | HUD                  |
| `OnEntityDied`       | Health               | Enemy / Room Manager |
| `OnRoomCleared`      | Room Manager         | UI / Map             |
| `OnPlayerDied`       | Player Death Handler | GameManager / UI     |
| `OnCurrencyChanged`  | Currency Wallet      | HUD / Inventory      |
| `OnGameStateChanged` | GameManager          | UI                   |

Jangan mengirim setiap perubahan kecil melalui EventBus global. Untuk sistem lokal, gunakan event milik komponen itu sendiri.

Pastikan setiap subscription dilepas ketika objek dinonaktifkan atau dihancurkan. Ini penting karena proyek menggunakan Object Pooling dan objek bisa aktif kembali.

## **3. Technology Stack dan Unity Tools**

Bagian ini merupakan rekomendasi tools yang paling relevan dengan kebutuhan GDD dan TDD.

### **3.1 Unity Editor dan packages**

[Open the Unity Essentials project - Unity Learn](https://images.openai.com/static-rsc-4/XPwQpZQviyFxSTgjyxTX_Y5q0xyG0fv5jgsoX26CtEMU8xmO2MOfjYVeZcbQPwZ_CZdcefBQ_1_bTilBLAwR_nU1u3FHqz4Fzx56LAyWTBAykI-QDt87JgTHpOTOU1BKxRQNO8L1qklgK8ZCW9gGZYJC-a_pJeIo3r5tNdJXZOY?purpose=inline)

**Unity 6.3 LTS**

Rekomendasi editor

Kunci versi Editor untuk seluruh tim.

[Setting up the Input System - Unity Learn](https://images.openai.com/static-rsc-4/BxsYz9zC4_u6riT6gdb6IUk4FWPLNDLf48WYN5UBHEVyxNcz_2_9zo0Bm3sf-excvCovv8njuuv4hYGK0naDXCp5BMI_FSiBfdf8stw8cbyzI_kgO2RHlK1Ao_z8Nie-TSCxh93LNSHndNzvXSPxKiNN-pkEi5Yzb_BDWQSUaJc?purpose=inline)

**Input System**

Input keyboard, mouse, dan gamepad melalui Input Actions.

[Dokumentasi Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest?utm_source=chatgpt.com)

[Unity Cinemachine & Timeline 制作镜头动画-腾讯云开发者社区-腾讯云](https://images.openai.com/static-rsc-4/OxY4STralmX41BWd0wOOd-Ec3eJPZhweqddYTdmK8C_pAk03sSTnjFtaZ406T17LhmH5OQD1GShlIz5HuiDucB_m01jznZZYOPMKzEo8iYf7k4daIFkiniPZ_s-AkMokoes9-iYsqhmm83SxgT1vEM43_1w2Tw7vKbcoZS6_opk?purpose=inline)

**Cinemachine**

Sudah digunakan

Follow camera, damping, camera composition, blends, dan Impulse.

[Dokumentasi Cinemachine](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html?utm_source=chatgpt.com)

[Free Video: Getting Started with AI Navigation 2.0 and NavMesh in Unity from Unity | Class Central](https://images.openai.com/static-rsc-4/T7-Qx3c8Q8eW6qILvSHuLruEXfu_O5OguviN3HAB1fIICjhx_P4WZ2Fe8J6oXm9VgWeNY9TP80MeJh0iPErrG1xy46OS7hS2bVBdWaL87joaZPyiszJflrMnv9uz-jzgMG0ZQBi9faQtunFrZYi3xB5ePQMir1VMoS7xfolM-MU?purpose=inline)

**AI Navigation**

Direkomendasikan

NavMesh, NavMesh Agent, NavMesh Surface, obstacle dan link navigasi musuh.

[Dokumentasi AI Navigation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html?utm_source=chatgpt.com)

[Install Instructions | FLOBUK](https://images.openai.com/static-rsc-4/COW4eS2iSbgN5MOqM8cnxkYaa_Z7QNYRodp5k1gTLapmAKyUbIKcc_bzR5Dc4uk34qdvMNJi5K7OJ2Mn53a9Yvb7LTPcUYqklkjhK4pLCL-2-P8SDCVKQXlwTbxkpSon6_rbDofBqeRw3Ao-LTCEkMt4wli62KE7s70spquTVzQ?purpose=inline)

**TextMeshPro**

Teks HUD, health value, damage indicator, dialog dan menu.

[Dokumentasi TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest?utm_source=chatgpt.com)

[What's new in version 1.4 | Test Framework | 1.4.6](https://images.openai.com/static-rsc-4/oI7cvwrQobf5P0yHqH8kZnM839fInmiQaCZnZcsqPZNNHeqxBUlJryd372RL_zqGXDdRvCVqR2QLZTiW0uOCsgyzh2_AmNpYPcIW19_TDcARylo8UrtvTYqwZBfIvLXZWgULIt03R0Kgp9dcOWoXbjVzxKbg2JVUUDIrk0GIaw4?purpose=inline)

**Unity Test Framework**

Pengujian otomatis Edit Mode dan Play Mode.

[Dokumentasi Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@latest?utm_source=chatgpt.com)

[Unity Logo PNG Transparent & SVG Vector - Freebie Supply](https://images.openai.com/static-rsc-4/qd8qvYmqJMC4Ky6x0OMXL-9vCQ-NlbVylrIR2BYx1G1Qe-aqr20FrWApTkZH1pT6AN1lwNu3dHHdKF8iBE7rSmOV8tZEmznf1bKI-3sre7yeyHG2PxCkK7G3Y_eb4U0oNbg9Ethq_Axpujzd-j1t6t11GRj--mBJ8Bpm0h1365k?purpose=inline)

**Unity Profiler**

Memantau CPU, GPU, Memory, Physics dan performa build.

[Dokumentasi Profiler](https://docs.unity3d.com/6000.3/Documentation/Manual/Profiler.html?utm_source=chatgpt.com)

Catatan versi: dokumentasi Unity mencatat Input System 1.17.0 dan Cinemachine 3.1.5 untuk Editor 6000.0. Versi yang digunakan di proyek Unity 6.3 harus dipilih dari Package Manager sesuai kompatibilitas yang ditampilkan untuk Editor tersebut. Jangan memaksakan nomor versi dari tutorial lama.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

マニュアル: Cinemachine+1



### **3.2 Tools bawaan Unity yang sebaiknya dimanfaatkan**

| Tools               | Penggunaan di proyek                                                      |
| ------------------- | ------------------------------------------------------------------------- |
| ScriptableObject    | Data serangan, statistik musuh, konfigurasi movement, reward dan artefak. |
| Animator Controller | State animasi karakter dan musuh.                                         |
| Unity Physics       | Collision, Raycast, Overlap dan Trigger.                                  |
| CharacterController | Pergerakan karakter yang dikendalikan kode.                               |
| NavMeshAgent        | Pergerakan AI berdasarkan NavMesh.                                        |
| ObjectPool          | Penggunaan ulang musuh dan proyektil.                                     |
| SceneManager        | Perpindahan Main Menu, Level, dan Hub.                                    |
| Gizmos              | Visualisasi area serangan, jarak AI, dan posisi spawn.                    |
| Profiler            | Pengujian performa.                                                       |

**Kenapa ScriptableObject penting?**

Aku sangat menyarankan penggunaannya untuk data yang relatif tetap.

Misalnya, daripada menulis karakteristik setiap musuh di dalam script:

- Enemy A memiliki nilai statistik tertentu.
- Enemy B mempunyai nilai statistik berbeda.
- Serangan memiliki konfigurasi masing-masing.
- Artefak memiliki jenis efek tertentu.

Buat `EnemyDefinitionSO`, `AttackDefinitionSO`, dan `ArtifactDefinitionSO`.

Nanti masing-masing prefab cukup mendapatkan referensi ke data tersebut.

Keuntungannya adalah programmer bisa mengubah konfigurasi melalui Inspector tanpa harus mengubah kode. ScriptableObject memang dirancang sebagai data container yang bisa digunakan bersama oleh berbagai instance.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

Manual: ScriptableObject+1



**Penting:** ScriptableObject digunakan untuk definisi data, bukan untuk menyimpan kondisi runtime bersama. Contohnya, jangan menaruh HP musuh yang sedang dimainkan di dalam asset konfigurasi musuh. Simpan HP aktual di komponen Health masing-masing instance.

### **3.3 Tools yang tidak perlu diprioritaskan**

Untuk demo ini, aku tidak merekomendasikan langsung memakai:

- DOTS / ECS untuk seluruh gameplay.
- Netcode / Multiplayer Framework.
- Addressables untuk semua aset.
- Dependency Injection Framework yang kompleks.
- Custom Physics Engine.
- Sistem procedural generation yang berbasis algoritma terlalu rumit.

Bukan berarti tools tersebut buruk. Namun, manfaatnya belum sebanding dengan kompleksitas tambahan untuk scope demo satu bioma dan permainan single-player.

# **4. ROADMAP UTAMA — URUTAN IMPLEMENTASI**

Berikut urutan yang aku rekomendasikan. Perhatikan bahwa setiap tahap memiliki tujuan, struktur script, tools, dan kriteria selesai.

## **PHASE 0 — Engineering Foundation**

Wajib sebelum produksi fitur

**Tujuan:** Menyiapkan lingkungan development agar seluruh anggota bisa bekerja pada fondasi teknis yang sama.

### **Point 0.1 — Kunci versi dan konfigurasi project**

Yang harus dilakukan:

1. Sepakati Unity 6.3 LTS dan patch Editor yang digunakan.
2. Pastikan seluruh anggota menginstal versi yang sama.
3. Periksa package yang sudah digunakan, khususnya Cinemachine.
4. Aktifkan Input System dan sepakati penggunaannya.
5. Tentukan render pipeline berdasarkan project yang sudah ada. Jangan mengganti URP/Built-in hanya untuk mencoba fitur baru jika tidak ada kebutuhan visual yang jelas.
6. Buat file konfigurasi layer dan collision matrix.

Layer yang disarankan:

| Layer        | Fungsi                      |
| ------------ | --------------------------- |
| Player       | Pemain                      |
| Enemy        | Musuh                       |
| Environment  | Lantai dan lingkungan       |
| PlayerHitbox | Deteksi serangan pemain     |
| EnemyHitbox  | Deteksi serangan musuh      |
| Projectile   | Objek proyektil             |
| Collectible  | Objek yang bisa dikumpulkan |
| Interactable | Interaksi dengan objek/NPC  |

Atur Layer Collision Matrix agar hanya pasangan objek yang memang perlu berinteraksi yang diproses oleh Physics. Unity menyediakan collision matrix untuk mengurangi pasangan collision yang tidak diperlukan.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

Unity Documentation



### **Point 0.2 — Siapkan struktur project**

Mengikuti TDD, seluruh script custom harus masuk ke dalam `Assets/_Project/`.&#x20;

Pasted markdown(4).md



Aku merekomendasikan pengembangan struktur tersebut menjadi:

```
Assets/
│
├── _Project/
│   │
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   ├── CombatTest.unity
│   │   ├── Level_01.unity
│   │   └── Hub.unity
│   │
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs
│   │   │   ├── GameState.cs
│   │   │   └── EventChannels/
│   │   │
│   │   ├── Player/
│   │   │   ├── Input/
│   │   │   ├── Movement/
│   │   │   ├── Combat/
│   │   │   └── Health/
│   │   │
│   │   ├── Enemy/
│   │   │   ├── AI/
│   │   │   ├── States/
│   │   │   ├── Combat/
│   │   │   └── Health/
│   │   │
│   │   ├── Combat/
│   │   │   ├── CombatSystem.cs
│   │   │   ├── DamageInfo.cs
│   │   │   └── Interfaces/
│   │   │
│   │   ├── Weapon/
│   │   ├── Artefak/
│   │   ├── Armor/
│   │   │
│   │   ├── World/
│   │   │   ├── Rooms/
│   │   │   └── Generation/
│   │   │
│   │   ├── Systems/
│   │   │   ├── Pooling/
│   │   │   ├── Progression/
│   │   │   └── Save/
│   │   │
│   │   ├── Camera/
│   │   └── UI/
│   │       ├── HUD/
│   │       ├── MainMenu/
│   │       ├── GameOver/
│   │       └── Dialogue/
│   │
│   ├── ScriptableObjects/
│   │   ├── Player/
│   │   ├── Enemy/
│   │   ├── Combat/
│   │   ├── Items/
│   │   └── Rooms/
│   │
│   ├── Prefabs/
│   │   ├── Player/
│   │   ├── Enemy/
│   │   ├── Projectile/
│   │   ├── Rooms/
│   │   └── UI/
│   │
│   ├── Art/
│   ├── Audio/
│   └── Materials/
│
└── ThirdParty/
```

Ini merupakan perluasan struktur TDD, bukan penggantian standar yang sudah disepakati.

### **Point 0.3 — Siapkan development scene**

Buat `CombatTest.unity`.

Untuk sementara gunakan:

- Plane sebagai lantai.
- Cube sebagai penghalang.
- Capsule sebagai Player.
- Capsule lain sebagai Enemy.
- Kamera Cinemachine.
- Satu Canvas untuk debug UI.

Jangan menunggu aset final dari Craftman.

**Output Phase 0:**

- Unity project dengan versi yang sama.
- Package sudah disepakati.
- Folder sudah tersedia.
- Collision layer sudah ditentukan.
- CombatTest siap digunakan seluruh programmer.

## **PHASE 1 — Player Foundation, Camera & Movement**

Must Have

**Tujuan:** Pemain sudah bisa dikendalikan dengan konsisten, baik untuk eksplorasi maupun pengujian pertarungan.

### **Point 1.1 — Refactor&#x20;**`char walk`

Ini harus menjadi salah satu pekerjaan pertama.

Jangan mempertahankan satu script `char walk` yang sekaligus membaca input, mengubah posisi, menangani animasi, dan mengendalikan kamera.

Pisahkan menjadi:

| Script                         | Tanggung jawab                            |
| ------------------------------ | ----------------------------------------- |
| `PlayerInputReader.cs`         | Membaca Input Actions.                    |
| `PlayerMotor.cs`               | Melakukan gerakan fisik.                  |
| `PlayerMovementConfig.cs`      | Menyimpan konfigurasi gerakan.            |
| `PlayerFacingController.cs`    | Mengatur orientasi pemain.                |
| `PlayerAnimationController.cs` | Menghubungkan kondisi pemain ke Animator. |

Aliran prosesnya:

```
Keyboard / Gamepad
       |
       v
PlayerInputReader
       |
       v
PlayerMotor
       |
       v
CharacterController
       |
       v
Player Transform
```

Gunakan Unity Input System, bukan menumpuk pembacaan tombol manual di seluruh script.

Buat Action Map `Gameplay` dengan tindakan:

| Action   | Input                                  |
| -------- | -------------------------------------- |
| Move     | WASD / Left Stick                      |
| Look     | Sesuai skema Free-Look yang disepakati |
| Attack   | Tombol serangan                        |
| Parry    | Tombol parry                           |
| Dodge    | Tombol dodge                           |
| Interact | Tombol interaksi                       |

Nama input di atas merupakan rancangan konfigurasi, bukan ketetapan tombol final.

Input System mendukung pemisahan input berdasarkan konsep tindakan, sehingga program tidak perlu bergantung langsung pada tombol fisik tertentu.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

1.4.3+1



### **Point 1.2 — Tentukan sistem fisik Player**

Untuk gameplay top-down 3D yang membutuhkan pergerakan langsung dan presisi, aku merekomendasikan **CharacterController sebagai pilihan awal**.

Alasannya:

- Gerakan diatur secara eksplisit melalui kode.
- Collision dengan lingkungan tetap ditangani.
- Tidak perlu mengelola gaya Rigidbody untuk setiap perubahan gerakan.
- Lebih mudah mengatur pergerakan khusus seperti Dodge.

Namun ada konsekuensinya: CharacterController tidak otomatis menerima gaya fisika seperti Rigidbody. Apabila ingin ada dorongan atau knockback, kamu perlu menangani perpindahannya melalui movement controller.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

Scripting API: CharacterController+1



Buat Player sebagai satu prefab terstruktur:

```
Player
│
├── CharacterController
├── PlayerInputReader
├── PlayerMotor
├── PlayerFacingController
├── PlayerActionController
├── Health
├── Animator
│
├── Model
│   └── Animator
│
└── CameraTarget
```

Jangan membuat seluruh komponen memiliki tanggung jawab yang tumpang tindih.

### **Point 1.3 — Integrasi Cinemachine**

Karena kamu sudah menggunakan Cinemachine, pertahankan sistem tersebut.

Arsitektur yang disarankan:

```
Player
  └── CameraTarget

Main Camera
  └── Cinemachine Brain

Cinemachine Camera
  ├── Follow: CameraTarget
  └── Camera configuration
```

Hal yang perlu diatur:

1. Posisi kamera sesuai sudut pandang top-down.
2. Follow target berada pada posisi yang sesuai terhadap Player.
3. Damping untuk mengurangi gerakan kamera yang terasa kaku.
4. Pengaturan update supaya kamera tidak tertinggal dari pergerakan pemain.
5. Pengujian kamera ketika Player melakukan Dodge dan berpindah area.

**Jangan menaruh kode pergerakan kamera di PlayerMotor.**

Perlu diperhatikan juga, Cinemachine 3 berbeda dari Cinemachine 2. Jika versi yang sekarang digunakan adalah Cinemachine 3, jangan mengikuti tutorial lama yang menggunakan API Cinemachine 2 tanpa memeriksa kesesuaiannya. Unity secara khusus mencatat bahwa migrasi dari versi 2 ke versi 3 dapat memerlukan penyesuaian.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

マニュアル: Cinemachine



### **Point 1.4 — Implementasi Auto-Jump**

GDD hanya menetapkan Auto-Jump berdasarkan ketinggian dan tidak menginginkan parkour kompleks.&#x20;

Pasted markdown(4).md



Karena itu, implementasikan dalam dua tingkatan.

**Tingkatan pertama: Step Handling**

Gunakan:

- `CharacterController.stepOffset`
- `slopeLimit`
- `isGrounded`

Tujuannya mengatasi undakan kecil dan permukaan yang tidak rata.

**Tingkatan kedua: Automatic Jump/Vault**

Baru dibuat apabila ada rintangan yang memang tidak dapat dilalui oleh step handling.

Komponen pendukung:

- Ground Detection.
- Obstacle Detection.
- Height Check.
- Movement State.
- Jump/Traversal Animation.

Gunakan Raycast atau SphereCast sesuai kebutuhan pemeriksaan rintangan.

Jangan langsung mengimplementasikan banyak jenis lompatan, karena mekanik tersebut berada di luar scope parkour yang ditetapkan GDD.

### **Point 1.5 — Implementasi Dodge/Dash dan I-Frame**

Ini termasuk Must Have dalam tabel Player Controller GDD.&#x20;

Pasted markdown(4).md



Buat script terpisah:

```
PlayerActionController.cs
DodgeController.cs
InvulnerabilityHandler.cs
```

Pembagian tanggung jawab:

- `PlayerActionController`: mengatur tindakan yang diminta pemain.
- `DodgeController`: mengatur gerakan Dodge.
- `InvulnerabilityHandler`: mengatur kapan pemain tidak menerima damage.

Ketika Dodge dimulai:

1. Periksa apakah pemain boleh melakukan Dodge.
2. Perbarui action state.
3. Jalankan perpindahan karakter.
4. Aktifkan periode i-frame sesuai konfigurasi.
5. Akhiri periode i-frame.
6. Kembalikan karakter ke state yang sesuai.

Semua durasi dan kecepatan disimpan sebagai konfigurasi, bukan magic number.

**Output Phase 1:**

Pemain sudah bisa:

- Bergerak.
- Berputar sesuai kontrol permainan.
- Menghadapi undakan.
- Melakukan Dodge.
- Mendapatkan perlindungan i-frame.
- Menggunakan kamera follow Cinemachine tanpa ketergantungan pada script movement.

Kriteria pengujian: pergerakan harus konsisten pada kondisi diam, bergerak diagonal, menabrak penghalang, melakukan Dodge, dan berpindah permukaan.

## **PHASE 2 — Health, Damage & Core Combat**

Must Have — Prioritas tertinggi setelah Movement

Ini bagian yang menurutku paling membutuhkan perancangan arsitektur dengan serius.

Jangan langsung membuat serangan dasar sebelum kontrak Health dan Damage sudah jelas.

### **Point 2.1 — Buat Health Component universal**

Buat:

```
Health.cs
DamageInfo.cs
DamageResult.cs
IDamageable.cs
```

`Health` harus dapat ditempelkan ke Player maupun Enemy.

Tanggung jawabnya:

- Menyimpan Max Health.
- Menyimpan Current Health.
- Menerima permintaan damage.
- Menangani kondisi Invulnerable.
- Mendeteksi kematian.
- Memancarkan event ketika HP berubah.

Contoh konsep API:

```
public interface IDamageable
{
    DamageResult TakeDamage(DamageInfo damageInfo);
}
```

Tidak perlu menaruh animasi kematian, loot, atau UI di dalam Health.

Contoh alurnya:

```
CombatSystem
     |
     v
IDamageable.TakeDamage()
     |
     v
Health
     |
     ├── HP Changed
     |
     └── Died
```

Sediakan event lokal:

```
OnHealthChanged
OnDied
```

Pastikan event `OnDied` hanya dipancarkan satu kali walaupun ada beberapa permintaan damage setelah HP mencapai nol.

### **Point 2.2 — Buat CombatSystem**

Script utama:

```
CombatSystem.cs
CombatResolver.cs
HitboxController.cs
AttackController.cs
AttackDefinitionSO.cs
```

Pembagian peran:

| Komponen           | Tanggung jawab                                        |
| ------------------ | ----------------------------------------------------- |
| AttackController   | Mengatur perintah serangan pemain.                    |
| HitboxController   | Mendeteksi objek yang terkena serangan.               |
| CombatSystem       | Menyediakan pengelolaan interaksi combat.             |
| CombatResolver     | Menentukan hasil interaksi sebelum damage diterapkan. |
| AttackDefinitionSO | Menyimpan data serangan.                              |

Gunakan `AttackDefinitionSO` untuk menyimpan data konfigurasi seperti:

- Damage.
- Startup duration.
- Active duration.
- Recovery duration.
- Informasi cancel yang diizinkan.
- Referensi animasi dan feedback.

Nilainya perlu disesuaikan bersama Project Lead berdasarkan desain GDD dan aset animasi.

Untuk deteksi serangan, manfaatkan Collider Trigger atau Physics Overlap sesuai bentuk serangan. Pastikan satu aktivasi serangan tidak memberikan damage berulang kepada target yang sama.

### **Point 2.3 — Implementasi Perfect Parry**

Perlu desain teknis khusus

Perfect Parry merupakan salah satu mekanik paling penting dalam core combat.

Aku merekomendasikan agar Perfect Parry bukan sekadar status `IsBlocking = true`.

Pisahkan menjadi:

```
ParryController.cs
ParryWindow.cs
ParryResolver.cs
IParryable.cs
```

Alur sistem:

```
Parry Input
    |
    v
ParryController
    |
    v
Parry Window Active
    |
    v
Incoming Attack
    |
    v
ParryResolver
    |
    ├── Valid Timing
    |      |
    |      v
    |   Perfect Parry
    |      |
    |      v
    |   Deflect / Feedback
    |
    └── Invalid Timing
           |
           v
       Normal Damage Resolution
```

Ada tiga hal yang wajib ditentukan sebelum implementasi:

1. Berapa lama jendela Perfect Parry berlangsung?
2. Bagaimana kondisi jika input dilakukan terlalu cepat atau terlambat?
3. Bagaimana proyektil yang berhasil di-deflect diperlakukan oleh sistem?

Untuk proyektil, sediakan interface terpisah agar sistem tidak perlu bergantung pada jenis prefab tertentu.

Contohnya secara konseptual:

```
public interface IParryable
{
    void OnParried(Transform parrySource);
}
```

Sebuah proyektil yang berhasil di-deflect harus memperbarui status pemilik/faksinya dan memastikan pemrosesan collision tidak menyebabkan interaksi berulang yang salah.

**Penting:** GDD menyatakan Perfect Parry tanpa cooldown. Namun, durasi window dan aturan kegagalannya belum dijelaskan. Jangan menentukan angka sendiri lalu menganggapnya sebagai ketetapan desain.&#x20;

Pasted markdown(4).md



### **Point 2.4 — Animation Canceling**

Buat sistem action state yang sederhana.

Contoh:

```
PlayerActionState

- Idle
- Attacking
- Parrying
- Dodging
- Dead
```

Jangan mengandalkan Animator sebagai satu-satunya sumber kebenaran state gameplay.

Animator bertugas menampilkan gerakan. Action Controller bertugas menentukan tindakan mana yang diperbolehkan.

Aturan cancel diletakkan pada data serangan atau konfigurasi aksi.

Contoh konsep:

```
Sedang Attacking
       |
       v
Dapatkan Dodge Input
       |
       v
Periksa Cancel Rule
       |
       ├── Boleh
       |    |
       |    v
       |  Beralih ke Dodge
       |
       └── Tidak boleh
            |
            v
        Tetap Attacking
```

Implementasikan mekanik tersebut untuk Dodge dan Parry sesuai ketentuan GDD.

**Output Phase 2:**

Satu pemain sudah memiliki:

- Basic Attack.
- Damage Resolution.
- Health.
- Death.
- Perfect Parry.
- Projectile Deflect.
- Animation Canceling.

Sebelum dilanjutkan, uji interaksi combat berulang kali tanpa AI kompleks.

## **PHASE 3 — Enemy AI & Combat Encounter**

Must Have

Sekarang barulah `enemy ngejar` dan `enemy spawner` diintegrasikan.

### **Point 3.1 — Refactor&#x20;**`enemy ngejar`**&#x20;menjadi FSM**

TDD secara eksplisit menetapkan FSM:

```
Idle → Chase → Attack
```

Pasted markdown(4).md

Struktur script:

```
EnemyBrain.cs
EnemyState.cs
EnemyMovement.cs
EnemyAttackController.cs
EnemyDefinitionSO.cs
```

Untuk permulaan, gunakan enum-based FSM terlebih dahulu.

Jangan membuat framework FSM generik yang terlalu besar sebelum memiliki kebutuhan.

Konsepnya:

| State  | Tanggung jawab                    |
| ------ | --------------------------------- |
| Idle   | Menunggu kondisi untuk bertindak. |
| Chase  | Mengikuti Player.                 |
| Attack | Menjalankan perilaku serangan.    |

Pemisahan state penting agar musuh tidak menjalankan banyak perilaku yang saling bertentangan.

### **Point 3.2 — Gunakan AI Navigation**

Untuk permainan 3D top-down yang menggunakan lingkungan dengan banyak penghalang, AI Navigation merupakan pilihan yang layak.

Komponen utama:

- NavMeshSurface.
- NavMeshAgent.
- NavMeshObstacle jika diperlukan.
- NavMeshLink jika suatu saat ada perpindahan antarpermukaan navigasi.

Paket AI Navigation mendukung pembangunan NavMesh di Editor maupun ketika permainan berjalan, termasuk dynamic obstacle dan NavMesh Link.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

手册: AI Navigation+1



Urutan penerapannya:

1. Siapkan lantai dan objek penghalang.
2. Tambahkan NavMeshSurface pada area navigasi.
3. Bake NavMesh.
4. Tambahkan NavMeshAgent pada musuh.
5. Hubungkan EnemyMovement dengan target Player.
6. Uji apakah musuh dapat mencapai pemain tanpa menembus penghalang.

Jangan langsung menghubungkan seluruh logika serangan ke NavMeshAgent.

NavMeshAgent hanya bertanggung jawab terhadap navigasi. EnemyBrain yang memutuskan kapan harus mengejar atau berhenti untuk menjalankan perilaku serangan.

Untuk procedural map nantinya, kamu dapat mempertimbangkan membangun satu NavMesh setelah seluruh room selesai disusun. Jangan menjalankan proses rebuild untuk setiap perubahan kecil atau setiap kali musuh muncul.

### **Point 3.3 — Buat dua variasi Enemy**

GDD mewajibkan satu jenis Melee dan satu jenis Ranged.&#x20;

Pasted markdown(4).md



Jangan membuat dua sistem AI yang berbeda sepenuhnya.

Gunakan:

- Satu EnemyBrain.
- Satu sistem Health.
- Satu sistem Damage.
- Dua konfigurasi/perilaku serangan.

[Low Poly 3D Antagonist Creature with Sword | AI Art Generator | Easy-Peasy.AI](https://images.openai.com/static-rsc-4/A1GA4r7PWL6aygl57jRpinUZmzAGVg-fuSg_aIt5TE8K6q6mrusYe8xRLAAO3ziW7y3DbqEaicZaVXEzmghBfePUEcddT9VRBER5mxPro__836HctvRKw5_YndEOknm7duoeNLJdlWC-GbPot7cPgHzwfIMOpfHB6JJ3HdKODKs?purpose=inline)

**Melee Enemy**

Menggunakan pendekatan jarak dekat, chase, dan attack.

[Free Skeleton Archer 3D Characters](https://images.openai.com/static-rsc-4/FRzhzmrU1vXYkUYzgJZXFYDrsrDXA5IxRJEmJX5rAB7zhHbX83lBQEC4yDhQ-ZpZQxA0werSa226NPKpIyLshp94D2eebo2XvTvIWHxKWN8Gyu2ZjOw8a-lpEcW0k9nD8gFuR9ib9WW_PQkuS0w9irN_tqvMoIrl0B9QAk5XK-8?purpose=inline)

**Ranged Enemy**

Menggunakan perilaku jarak jauh dan objek proyektil untuk menguji deflect.

Komponen tambahan:

```
Enemy
├── EnemyBrain
├── EnemyMovement
├── EnemyAttackController
├── Health
├── Animator
└── EnemyDefinitionSO
```

Ranged Enemy harus menggunakan sistem proyektil yang sama dengan sistem yang dapat berinteraksi dengan Perfect Parry.

### **Point 3.4 — Implementasi Object Pooling**

TDD mewajibkan Object Pooling untuk objek yang sering dimunculkan dan dihilangkan.&#x20;

Pasted markdown(4).md



Gunakan API bawaan:

```
UnityEngine.Pool.ObjectPool<T>
```

Kamu tidak perlu memasang plugin khusus untuk implementasi dasar ini. Unity menyediakan generic ObjectPool sebagai bagian dari engine.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

Scripting API: ObjectPool\<T0>



Prioritas pooling:

1. Projectile.
2. Enemy.
3. Hit Effect/Particle jika sudah banyak digunakan.

Siapkan:

```
ObjectPoolManager.cs
EnemyPool.cs
ProjectilePool.cs
```

Pastikan setiap objek yang dikembalikan ke pool dibersihkan dari kondisi sebelumnya.

Contohnya:

- HP/status musuh harus di-reset sesuai konfigurasi.
- Status invulnerability tidak boleh tertinggal.
- Event subscription harus dibersihkan.
- Projectile harus mendapatkan pemilik dan arah baru.
- Timer lama harus dihentikan.
- Animator harus kembali ke kondisi awal.

Ini sangat penting karena pooling bisa menimbulkan bug yang tidak terlihat jika hanya menguji objek yang baru di-Instantiate.

### **Point 3.5 — Bangun Room Encounter Controller**

Ini merupakan komponen yang perlu ditambahkan agar Spawner tidak bekerja sendiri.

Buat:

```
RoomEncounterController.cs
EnemySpawner.cs
EnemySpawnPoint.cs
```

Tanggung jawabnya:

1. Menerima permintaan memulai encounter.
2. Mengaktifkan spawn point.
3. Memunculkan musuh dari pool.
4. Mencatat musuh aktif.
5. Mendengarkan event kematian musuh.
6. Menentukan kapan seluruh encounter selesai.
7. Mengirim event `OnRoomCleared`.

Contoh:

```
Room Start
    |
    v
Spawn Enemies
    |
    v
Encounter Active
    |
    v
Track Active Enemies
    |
    v
All Enemies Defeated?
    |
    v
Room Cleared
```

**Output Phase 3:**

Satu ruangan sudah bisa menjalankan pertarungan penuh dengan dua variasi musuh, pooling, dan kondisi ruangan selesai.

## **PHASE 4 — GameManager, Health UI & Vertical Slice**

Penyelesaian MVP Arena

Setelah tiga fase sebelumnya selesai, baru integrasikan seluruh alur permainan.

### **Point 4.1 — GameManager**

Gunakan Singleton sesuai TDD, tetapi batasi tugasnya.

Contoh tanggung jawab:

```
GameManager
├── CurrentGameState
├── StartGame()
├── GameOver()
├── ReturnToMainMenu()
└── RestartRun()
```

Buat `GameState`:

```
MainMenu
Loading
InRun
GameOver
```

Jangan memindahkan logika combat ke GameManager.

Perhatikan juga agar tidak terjadi dua GameManager ketika berpindah scene. Jika objek dipertahankan dengan `DontDestroyOnLoad`, lakukan pemeriksaan duplikasi.

### **Point 4.2 — UIManager**

Buat pemisahan:

```
UI/
├── HUD/
│   ├── HUDController.cs
│   └── HealthBarUI.cs
│
├── MainMenu/
│   └── MainMenuUI.cs
│
└── GameOver/
    └── GameOverUI.cs
```

Untuk HUD, gunakan Canvas dan Slider atau Image Fill.

HealthBarUI cukup mendengarkan `OnHealthChanged`. Hindari terus-menerus memanggil `GetComponent` atau mencari objek melalui `Find` setiap frame.

Dengan demikian, UI tidak harus mengetahui implementasi internal Health.

### **Point 4.3 — Scene Flow**

Siapkan alur:

```
MainMenu
    |
    v
Start Game
    |
    v
Level_01
    |
    v
Combat Encounter
    |
    v
Player Death?
    |
    ├── Yes → Game Over
    |            |
    |            v
    |        Main Menu
    |
    └── No → Continue Run
```

Gunakan SceneManager untuk mengelola perpindahan scene. `LoadSceneAsync` dapat dipakai ketika nanti diperlukan proses loading tanpa memblokir alur utama permainan.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

Scripting API: SceneManager+1



### **Point 4.4 — Uji Vertical Slice di Arena Statis**

Sebelum membuat map random, pastikan satu arena sudah benar-benar bisa dimainkan.

**Vertical Slice Acceptance Checklist**

0 / 12 selesai

0%

Pemain bergerak dan bertabrakan dengan lingkungan secara benar.

Kamera Cinemachine mengikuti pemain dengan stabil.

Auto-Jump berjalan sesuai desain.

Dodge dan i-frame berfungsi.

Basic Attack menghasilkan damage yang benar.

Perfect Parry dapat diuji terhadap serangan dan proyektil.

Animation Canceling berfungsi sesuai aturan.

Melee Enemy dapat mengejar dan menyerang.

Ranged Enemy dapat melakukan serangan proyektil.

Health dan Death terdeteksi dengan benar.

Spawner menggunakan pooling dan melakukan reset objek.

Game Over dan kembali ke Main Menu berfungsi.

Reset checklist

**Jangan masuk ke map generation jika sistem arena ini belum lolos pengujian.**

## **PHASE 5 — Procedural Room Generation: Level 01 Hutan**

Must Have

**Tujuan:** Mengubah arena yang sebelumnya statis menjadi rangkaian ruangan roguelike yang memiliki variasi layout.

Ini merupakan tahap yang tepat untuk mengintegrasikan `map generate pattern` yang sudah kamu buat.

Aku menyarankan kamu tidak langsung membuat generator yang menghasilkan seluruh lingkungan secara bebas. Gunakan pendekatan **Modular Room-Based Generation**.

### **Point 5.1 — Buat Room Prefab System**

Sebelum membuat random generation, setiap ruangan harus memiliki struktur standar.

Contoh:

```
Room_Prefab
│
├── RoomGeometry
│   ├── Floor
│   ├── Walls
│   └── Environment
│
├── RoomConnections
│   ├── Entrance
│   ├── Exit_North
│   ├── Exit_South
│   └── Exit_East
│
├── SpawnPoints
│   ├── EnemySpawn_01
│   └── EnemySpawn_02
│
└── RoomController
```

Jangan membuat setiap ruangan mempunyai struktur berbeda. Dengan struktur standar, generator dapat memperlakukan setiap ruangan sebagai modul yang bisa dipasang.

Buat script:

| Script                | Fungsi                             |
| --------------------- | ---------------------------------- |
| `RoomDefinitionSO.cs` | Data dan identitas ruangan.        |
| `RoomController.cs`   | Mengelola kondisi ruangan.         |
| `RoomConnector.cs`    | Informasi sambungan antar-ruangan. |
| `RoomExit.cs`         | Transisi keluar ruangan.           |
| `RoomManager.cs`      | Mengelola ruangan aktif.           |

### **Point 5.2 — Buat Map Generator**

Gunakan struktur graph untuk menentukan konektivitas.

Konsep sederhana:

```
       [Room B]
          |
[Start]—[Room A]—[Room C]
          |
       [Exit]
```

Ini hanya contoh struktur, bukan layout final.

Generator harus menjamin bahwa:

- Ada satu titik awal.
- Ada jalur yang terhubung menuju titik akhir.
- Tidak terdapat ruangan yang tidak bisa dijangkau.
- Tidak ada dua prefab yang bertabrakan secara tidak sengaja.
- Setiap titik koneksi memiliki pasangan yang sesuai.

Buat:

```
RunMapGenerator.cs
RoomGraph.cs
RoomPlacement.cs
RoomCatalogSO.cs
MapGenerationConfigSO.cs
```

Untuk versi awal, cukup:

1. Mengambil sejumlah Room Prefab dari catalog.
2. Menentukan susunan berdasarkan koneksi.
3. Menempatkan prefab.
4. Memverifikasi konektivitas.
5. Mengaktifkan ruangan awal.

Tambahkan Random Seed agar hasil map bisa diulang saat menemukan bug. Ini sangat berguna untuk debugging.

### **Point 5.3 — Integrasi NavMesh dengan Procedural Map**

Ini bagian teknis yang jangan sampai terlewat.

Karena ruangan sekarang terbentuk secara dinamis, sistem navigasi harus mengenali permukaan yang dapat dilalui musuh.

Strategi yang aku rekomendasikan:

1. Generator menyelesaikan seluruh susunan ruangan.
2. Room Manager mengaktifkan seluruh geometri yang dibutuhkan.
3. NavMeshSurface membangun NavMesh untuk level yang sudah tersusun.
4. Sistem memastikan navigasi siap.
5. Spawner mulai mengaktifkan musuh.

Dengan demikian, AI tidak mencoba bergerak ketika navigasi belum terbentuk.

Untuk scope awal, satu NavMesh untuk satu susunan level lebih sederhana daripada melakukan rebuild setiap pemain berpindah ruangan.

### **Point 5.4 — Integrasikan Core Loop**

Setelah generator siap, hubungkan dengan sistem yang sudah kamu buat:

```
Main Menu
    |
    v
Generate Forest
    |
    v
Spawn Player
    |
    v
Explore Room
    |
    v
Encounter
    |
    v
Collect Reward
    |
    v
Room Cleared
    |
    v
Open Exit
    |
    v
Next Room
```

Pastikan pemain memiliki alasan untuk menjelajahi ruangan, menghadapi musuh, dan mendapatkan reward.

Untuk demo, fokus hanya pada satu bioma: Hutan.

GDD secara eksplisit menunda bioma Level 2–5.&#x20;

Pasted markdown(4).md



**Output Phase 5:**

Satu run Level 01 sudah dapat dimainkan dengan susunan ruangan yang bervariasi, musuh yang dapat bergerak, encounter yang bisa diselesaikan, dan jalur menuju ruangan selanjutnya.

# **PHASE 6 — In-Run Progression & Reward**

Should Have

**Tujuan:** Pemain mendapatkan perkembangan karakter selama permainan dan reward setelah menghadapi musuh.

Ini merupakan realisasi dari dua bagian Core Loop: mengumpulkan loot dan melakukan upgrade.

### **Point 6.1 — Buat Reward System**

Buat:

```
RewardDefinitionSO.cs
RewardCollector.cs
CurrencyWallet.cs
ExperienceSystem.cs
```

Pisahkan dua jenis data:

| Data        | Masa berlaku     |
| ----------- | ---------------- |
| In-game EXP | Selama satu run  |
| Souls Coin  | Progres permanen |

Alurnya secara teknis:

```
Enemy Defeated
      |
      v
Reward Handler
      |
      ├── EXP Reward
      |
      └── Currency Reward
```

Gunakan ScriptableObject untuk konfigurasi reward agar setiap jenis musuh dapat memiliki definisi reward berbeda.

Hal yang perlu diputuskan Project Lead:

- Apakah EXP langsung masuk setelah membunuh musuh?
- Kapan level-up terjadi?
- Apakah Souls Coin diberikan ketika musuh mati atau ketika sebuah area selesai?
- Apakah reward tetap diberikan jika pemain mati sebelum menyelesaikan run?

Pertanyaan tersebut belum semuanya dijelaskan dalam GDD. Engineering tidak boleh menetapkan jawabannya sendiri.

### **Point 6.2 — Buat In-Run Leveling**

Buat:

```
ExperienceSystem.cs
PlayerProgression.cs
StatModifier.cs
LevelUpHandler.cs
```

Sistem ini menangani:

- EXP yang terkumpul.
- Perhitungan level.
- Status karakter selama satu run.
- Penerapan perubahan statistik.

Saran arsitektur: pisahkan statistik dasar pemain dengan modifier.

Contohnya:

```
Base Stats
    |
    v
Progression Modifier
    |
    v
Equipment Modifier
    |
    v
Final Runtime Stats
```

Ini akan memudahkan pengembangan artefak maupun senjata berikutnya.

### **Point 6.3 — Loot Drop**

Buat:

```
LootDropController.cs
LootDefinitionSO.cs
LootCollector.cs
```

Loot dapat memiliki definisi data berupa:

- Identitas item.
- Jenis reward.
- Jumlah.
- Prefab visual.
- Kondisi pengambilan.

Jangan membuat setiap jenis loot memiliki sistem pengumpulan yang berbeda.

Gunakan satu pola pengumpulan umum.

# **PHASE 7 — Weapon Switching, Inventory & Artefak**

Should Have

**Tujuan:** Menghadirkan variasi pilihan perlengkapan tanpa memperumit core combat yang sudah selesai.

GDD menetapkan dua senjata awal untuk versi demo, serta satu sampai dua artefak.&#x20;

Pasted markdown(4).md



### **Point 7.1 — Weapon Switching**

Pertahankan nama `ItemSwitching` sesuai TDD.

Struktur tambahan:

```
ItemSwitching.cs
EquipmentController.cs
EquipmentDefinitionSO.cs
PlayerEquipmentState.cs
```

Tanggung jawab:

- Menyimpan pilihan perlengkapan yang aktif.
- Mengatur pergantian item.
- Mengirim informasi perlengkapan aktif ke sistem serangan.
- Memastikan pergantian tidak merusak action state pemain.

Penting: jangan membuat kerangka sistem yang berisi terlalu banyak kondisi khusus untuk setiap item.

Setiap perlengkapan harus mengimplementasikan kontrak yang konsisten dengan CombatSystem.

### **Point 7.2 — Inventory Dasar**

Buat:

```
Inventory.cs
InventorySlot.cs
InventoryItem.cs
InventoryUI.cs
```

Pisahkan:

- Inventory Model: menyimpan data item.
- Inventory Controller: mengatur perubahan data.
- Inventory UI: menampilkan item.

UI tidak boleh menjadi tempat penyimpanan data utama.

### **Point 7.3 — Artefak Pasif**

Buat:

```
ArtifactDefinitionSO.cs
ArtifactController.cs
IStatModifier.cs
```

Gunakan pendekatan modifier untuk memodifikasi statistik atau karakteristik gameplay.

Contoh pengujian cukup satu sampai dua artefak sesuai GDD.

Jangan langsung membuat delapan artefak karena GDD menempatkan konten tersebut di luar prioritas utama demo.

**Output Phase 7:**

Pemain sudah dapat memperoleh, menyimpan, dan mengaktifkan perlengkapan serta artefak dengan data dan UI yang terpisah.

# **PHASE 8 — Elite Enemy & Stagger System**

Should Have

**Tujuan:** Menambahkan variasi encounter yang lebih menantang tanpa menulis ulang seluruh Enemy AI.

GDD menetapkan satu Elite Enemy dengan mekanik Stagger Bar.&#x20;

Pasted markdown(4).md



### **Point 8.1 — Elite Enemy**

Gunakan kembali sistem yang sudah tersedia:

```
Base Enemy Architecture
    |
    ├── EnemyBrain
    ├── EnemyMovement
    ├── Health
    └── EnemyAttackController
```

Elite cukup menggunakan konfigurasi statistik dan perilaku khusus yang diperlukan.

Jangan mengkloning seluruh script Enemy AI hanya untuk membuat satu musuh elit.

### **Point 8.2 — Stagger System**

Buat:

```
StaggerComponent.cs
StaggerConfigSO.cs
StaggerBarUI.cs
```

Komponen ini bertugas:

- Menyimpan nilai Stagger.
- Menerima kontribusi Stagger dari interaksi combat.
- Menentukan ketika batas Stagger tercapai.
- Mengirim event kepada Elite Enemy untuk menampilkan kondisi tersebut.

Pisahkan Stagger dari Health.

**Health dan Stagger adalah dua jenis data yang berbeda.** Keduanya boleh saling berhubungan melalui event atau CombatSystem, tetapi tidak perlu digabung dalam satu script.

## **PHASE 9 — Game Feel, Cinemachine Impulse & Visual Feedback**

Nice to Have

Ini saatnya menggunakan kemampuan Cinemachine lebih jauh.

Jangan mengerjakan polish sebelum combat dan encounter stabil.

### **Point 9.1 — Cinemachine Impulse**

Siapkan:

- Impulse Source pada objek yang menghasilkan feedback.
- Impulse Listener pada kamera.
- Pengaturan channel.
- Pengaturan intensitas dan durasi.

Peristiwa yang dapat memicu camera shake:

| Event                  | Feedback              |
| ---------------------- | --------------------- |
| Hit berhasil           | Impulse ringan        |
| Perfect Parry          | Impulse khusus        |
| Serangan Elite         | Impulse lebih kuat    |
| Event benturan penting | Impulse sesuai desain |

Jangan membuat seluruh kejadian menghasilkan shake yang sama.

**Cinemachine bukan jaminan kamera tidak pernah patah atau nyangkut.** Kestabilan tetap bergantung pada konfigurasi Follow Target, damping, update mode, collision dan pengujian transisi.

### **Point 9.2 — Hitstop**

Buat:

```
TimeEffectController.cs
HitstopConfigSO.cs
```

Sistem ini bertanggung jawab atas jeda singkat saat interaksi penting terjadi.

Hal yang perlu diperhatikan:

- Jangan mengubah Time.timeScale secara sembarangan.
- Pastikan nilai waktu dikembalikan secara konsisten.
- Perhatikan coroutine yang menggunakan scaled time.
- Jangan sampai hitstop mengganggu sistem pause.

Gunakan satu pengelola efek waktu supaya beberapa peristiwa tidak saling mengacaukan Time.timeScale.

### **Point 9.3 — Visual dan Audio Feedback**

Integrasikan:

- Particle System.
- Trail.
- Screen-space/UI feedback jika diperlukan.
- Animator feedback.
- Audio Mixer.
- Efek visual benturan.

Referensi visual ZZZ dalam GDD sebaiknya menjadi referensi kualitas dan karakter feedback, bukan alasan untuk langsung membuat sistem VFX yang sangat kompleks.

# **PHASE 10 — Hub Area, Dialog & Permanent Upgrade**

Nice to Have

GDD memasukkan Hub Area sebagai Nice to Have. Jadi, kerjakan hanya setelah Must Have dan Should Have selesai.

### **Point 10.1 — Desa Lombok**

Buat scene terpisah:

```
Hub.unity
```

Komponen yang diperlukan:

- Player.
- NPC.
- Interactable system.
- Dialogue system.
- Upgrade interaction.
- Scene transition.

Kamu sudah memiliki `scene/dialogue`, jadi tidak perlu membuat ulang sistem dialog dari nol.

Yang diperlukan adalah memastikan dialog terhubung dengan sistem Interactable dan UI tanpa ketergantungan langsung terhadap CombatSystem.

### **Point 10.2 — Permanent Upgrade**

Buat:

```
PermanentUpgradeSystem.cs
UpgradeDefinitionSO.cs
UpgradeManager.cs
```

Gunakan wallet permanen sebagai sumber mata uang.

Pemisahan:

```
In-Run Progression
        |
        | Reset saat run selesai
        v
Temporary Player Stats

Permanent Progression
        |
        | Tetap ada antar-run
        v
Permanent Player Upgrades
```

Jangan mencampurkan statistik sementara dan permanen dalam variabel yang sama tanpa identitas sumber modifier.

# **PHASE 11 — Save/Load System**

Nice to Have

Walaupun TDD sudah menetapkan Save File dan Checkpoint sebagai pendekatan, implementasinya sebaiknya mengikuti sistem progression yang telah selesai.

### **Point 11.1 — Struktur Save Data**

Buat:

```
SaveSystem.cs
SaveData.cs
SaveValidator.cs
```

Isi data yang direkomendasikan:

```
SaveData
│
├── SaveVersion
├── SoulsCoin
├── PermanentUpgrades
├── UnlockedContent
└── Settings
```

Untuk scope awal, jangan menyimpan seluruh kondisi satu run.

Yang diprioritaskan adalah progres permanen sesuai kebutuhan GDD.

### **Point 11.2 — Penyimpanan**

Untuk demo single-player, penyimpanan JSON lokal sudah memadai.

Gunakan:

`Application.persistentDataPath`

Ketentuannya:

- Simpan data sebagai object serializable.
- Jangan langsung menyimpan referensi GameObject.
- Gunakan ID stabil untuk mengidentifikasi upgrade.
- Tambahkan version field.
- Sediakan validasi untuk file yang tidak bisa dibaca.
- Simpan ketika terjadi perubahan permanen penting.

Tidak perlu membuat cloud save, akun pemain, atau sinkronisasi jaringan.

# **5. PHASE 12 — Integration, Testing & Demo Release**

Ini merupakan fase yang sering terlupakan. Padahal fitur yang berfungsi sendiri-sendiri belum tentu berfungsi dengan baik ketika digabungkan.

**Tujuan:** Memastikan seluruh sistem stabil ketika dijalankan dalam satu build permainan.

### **Point 12.1 — Integrasi antar-divisi**

Ketika aset dari Craftman sudah tersedia, lakukan integrasi secara bertahap.

Urutan:

1. Integrasikan model karakter.
2. Sesuaikan ukuran CharacterController dengan model.
3. Hubungkan animasi dengan Animator Controller.
4. Integrasikan model musuh.
5. Sesuaikan ukuran hitbox dan NavMesh.
6. Integrasikan environment dan prefab ruangan.
7. Integrasikan VFX dan audio.
8. Uji seluruh gameplay dalam build.

Jangan mengubah sistem gameplay hanya agar bisa menyesuaikan satu aset tanpa memeriksa terlebih dahulu akar masalahnya.

### **Point 12.2 — Automated Testing**

Gunakan Unity Test Framework untuk pengujian yang dapat diotomatisasi.

Prioritaskan:

| Sistem  | Pengujian                                              |
| ------- | ------------------------------------------------------ |
| Health  | HP berkurang dan kematian hanya terjadi sekali.        |
| Combat  | Damage tidak diterapkan berulang secara tidak sengaja. |
| Parry   | Kondisi valid dan tidak valid dibedakan.               |
| Pooling | Objek kembali dalam kondisi yang benar.                |
| Reward  | Reward tidak diberikan dua kali.                       |
| Map     | Setiap ruangan dapat dijangkau.                        |
| Save    | Data tersimpan dan dapat dibaca kembali.               |

Unity Test Framework menyediakan pengujian Edit Mode dan Play Mode, sehingga sebagian logika dapat diuji tanpa selalu memainkan game secara manual.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

매뉴얼: Test Framework+1



### **Point 12.3 — Performance Profiling**

Gunakan Unity Profiler untuk mengamati:

- CPU Usage.
- GPU Usage.
- Memory.
- Garbage Collection.
- Physics.
- Rendering.

Jangan melakukan optimasi berdasarkan perasaan saja.

Contohnya, apabila terjadi penurunan FPS ketika banyak musuh muncul, periksa apakah masalahnya berasal dari:

- Terlalu banyak objek aktif.
- Proses Instantiate/Destroy.
- Perhitungan AI.
- Collision.
- Particle System.
- Rendering.

Object Pooling merupakan bagian dari standar TDD, tetapi bukan pengganti profiling.&#x20;

Pasted markdown(4).md



### **Point 12.4 — Build Release**

Sebelum demo:

- Pastikan Main Menu berfungsi.
- Pastikan Level 01 bisa dimulai.
- Pastikan tidak ada softlock pada ruangan.
- Pastikan pemain dapat menyelesaikan run.
- Pastikan Game Over dapat diproses.
- Pastikan save data tidak rusak.
- Pastikan tidak ada error kritis.
- Uji build Windows, bukan hanya Play Mode di Unity Editor.

**Hasil akhir:** Build demo The Last Sasak: Unknown Land yang bisa dimainkan dari awal sampai selesai sesuai scope yang disepakati.

# **6. Pemetaan fitur yang sudah kamu buat**

Sekarang kita hubungkan roadmap dengan pekerjaan yang sudah ada. Ini penting agar tidak membuang hasil pekerjaan sebelumnya.

| Fitur yang sudah ada   | Tindakan berikutnya                        | Urutan |
| ---------------------- | ------------------------------------------ | ------ |
| `char walk`            | Refactor Input dan Movement                | 1      |
| `camera`               | Rapikan Cinemachine Camera Rig             | 2      |
| `debug attack`         | Integrasikan Health dan CombatSystem       | 3      |
| `enemy ngejar`         | Refactor menjadi FSM + NavMesh             | 4      |
| `enemy spawner`        | Integrasikan Pooling dan Encounter         | 5      |
| `scene/dialogue`       | Pertahankan, integrasikan dengan Hub nanti | 10     |
| `map generate pattern` | Refactor ke Room-Based Generator           | 6      |

Menurutku, jangan langsung membuang script lama. Pertahankan bagian yang sudah bekerja, kemudian refactor sesuai tanggung jawab masing-masing sistem.

Urutan prioritas implementasi yang benar-benar perlu kamu ikuti:

## **Implementation Order**

1. **Project Foundation**

   Unity version, packages, folders, layer, scene dan standar kerja.
2. **Player Movement**

   Refactor char walk, Input System, CharacterController, Auto-Jump dan Dodge.
3. **Cinemachine Camera**

   Follow, damping, target dan pengujian kamera.
4. **Health & Damage Architecture**

   Health universal, DamageInfo, event, death.
5. **Core Combat**

   Basic Attack, Perfect Parry, Deflect dan Animation Canceling.
6. **Enemy AI**

   FSM, NavMesh, Melee dan Ranged.
7. **Spawner & Encounter**

   Pooling, enemy tracking dan room-cleared event.
8. **GameManager & UI**

   HP Bar, Death Screen dan Scene Flow.
9. **Static Arena Vertical Slice**

   Uji seluruh mekanik sebelum procedural generation.
10. **Forest Procedural Generation**

    Room prefab, graph, connections, seed dan NavMesh.
11. **Progression & Reward**

    EXP, Souls Coin dan leveling.
12. **Weapon & Inventory**

    Equipment switching dan artefak.
13. **Elite Enemy**

    Stagger System.
14. **Game Feel & Presentation**

    Impulse, hitstop, VFX dan audio.
15. **Hub, Dialogue & Save**

    Desa Lombok dan permanent upgrade.
16. **Integration & Release**

    Testing, profiling dan build Windows.

# **7. Pembagian pekerjaan tim dan integrasi GitHub**

TDD sudah menetapkan beberapa PIC. Sebaiknya pembagian tersebut dipertahankan sebagai pemilik utama sistem, sambil memecah pekerjaan menjadi task kecil.

Pasted markdown(4).md

| PIC sesuai TDD | Tanggung jawab awal                                                       |
| -------------- | ------------------------------------------------------------------------- |
| Bagas          | GameManager, Inventory, SaveSystem                                        |
| Keanu          | PlayerController, ItemSwitching, EnemyAI, CombatSystem, UIManager, Camera |
| Assassin Lead  | Koordinasi arsitektur, integrasi, review dan standar teknis               |

Tugas yang belum ditetapkan PIC-nya, seperti Health, Event Channels, Room Generator, Pooling, dan Reward, perlu dibagikan secara resmi agar tidak ada dua orang mengerjakan sistem yang sama.

### **7.1 Standar GitHub**

Gunakan branching yang sudah disepakati dalam TDD:

```
main
  |
  └── dev
       |
       ├── feature/player-motor
       ├── feature/health-system
       ├── feature/combat-system
       ├── feature/parry-system
       ├── feature/enemy-ai
       ├── feature/object-pooling
       └── feature/room-generator
```

Setiap fitur:

1. Buat issue.
2. Ambil branch dari `dev`.
3. Implementasikan fitur.
4. Self-test.
5. Buat Pull Request.
6. Minta review.
7. Merge ke `dev`.
8. Masuk ke build mingguan.

Tidak boleh push langsung ke `dev` dan `main`, mengikuti aturan TDD.&#x20;

Pasted markdown(4).md



### **7.2 Kontrak integrasi yang perlu dibuat**

Sebelum dua programmer mengerjakan sistem yang saling berhubungan, sepakati interface dan data yang mereka gunakan.

Contohnya:

**Health ↔ CombatSystem**

Sepakati:

- Bentuk `DamageInfo`.
- API untuk meminta damage.
- Perilaku ketika target invulnerable.
- Event ketika HP berubah.
- Event ketika target mati.

**EnemyAI ↔ EnemySpawner**

Sepakati:

- Cara inisialisasi musuh.
- Cara menentukan target.
- Event kematian musuh.
- Cara mengembalikan objek ke pool.
- Cara mereset musuh.

**RoomGenerator ↔ RoomManager**

Sepakati:

- Struktur data ruangan.
- Identitas dan koneksi ruangan.
- Cara menentukan ruangan aktif.
- Cara memulai encounter.
- Cara menyatakan ruangan selesai.

Ini mencegah tim baru mengetahui ketidaksesuaian ketika seluruh fitur sudah selesai dibuat.

# **8. Engineering Decisions yang masih perlu disahkan**

Sebelum implementasi penuh, ada beberapa ketentuan desain yang belum cukup spesifik untuk langsung diterjemahkan menjadi kode.

Aku menyarankan membuat dokumen tambahan bernama:

**Gameplay Technical Specifications v1.0**

Isinya adalah keputusan teknis yang berasal dari mekanik GDD.

| No. | Topik            | Keputusan yang perlu ditetapkan                        |
| --- | ---------------- | ------------------------------------------------------ |
| 1   | Free-Look        | Apakah arah pandang pemain terpisah dari arah kamera?  |
| 2   | Auto-Jump        | Batas undakan dan kondisi otomatis melakukan lompatan. |
| 3   | Dodge            | Durasi gerakan dan periode i-frame.                    |
| 4   | Perfect Parry    | Durasi parry window dan aturan kegagalan.              |
| 5   | Deflect          | Perlakuan proyektil setelah berhasil di-deflect.       |
| 6   | Animation Cancel | Kapan serangan bisa dipotong oleh Parry atau Dodge.    |
| 7   | Enemy AI         | Jarak deteksi, kondisi Chase dan Attack.               |
| 8   | Map Generation   | Jumlah ruangan, jenis ruangan dan kondisi keluar.      |
| 9   | EXP              | Kapan EXP diperoleh dan bagaimana level-up terjadi.    |
| 10  | Souls Coin       | Sumber reward dan aturan penyimpanannya.               |
| 11  | Save             | Kapan data permanen disimpan.                          |

Mengapa ini penting?

Karena **programmer tidak seharusnya menebak mekanik permainan**.

TDD sendiri menyatakan bahwa TDD tidak boleh membuat mekanik baru yang tidak berasal dari GDD.&#x20;

Pasted markdown(4).md



Jadi, setiap keputusan baru harus memiliki sumber keputusan yang jelas.

# **9. Referensi engineering yang perlu dipelajari**

Berikut referensi resmi Unity yang langsung berhubungan dengan pekerjaan di atas.

**Fundamental movement & input**

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

**Character Controller**

Movement, Collision, Step Offset dan Slope Limit.

[Unity Manual](https://docs.unity3d.com/6000.0/Documentation/Manual/class-CharacterController.html?utm_source=chatgpt.com)

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

**Input System**

Input Actions, Action Maps dan Player Input.

[Unity Manual](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/manual/index.html?utm_source=chatgpt.com)

**Camera & Animation**

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

**Cinemachine**

Camera follow, damping, blending dan Impulse.

[Package Manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html?utm_source=chatgpt.com)

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

**Animator Controller**

Animation clips, transitions dan Animator state machine.

[Unity Manual](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AnimatorController.html?utm_source=chatgpt.com)

**AI & Data Architecture**

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

**AI Navigation**

NavMesh, Agent, Surface dan dynamic obstacle.

[Package Manual](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html?utm_source=chatgpt.com)

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

**ScriptableObject**

Reusable data definitions dan configuration assets.

[Unity Manual](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html?utm_source=chatgpt.com)

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

**Object Pooling**

UnityEngine.Pool.ObjectPool.

[Scripting API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Pool.ObjectPool_1.html?utm_source=chatgpt.com)

**Testing & Optimization**

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

**Unity Test Framework**

Automated Edit Mode dan Play Mode Testing.

[Package Manual](https://docs.unity3d.com/Packages/com.unity.test-framework@latest/manual/index.html?utm_source=chatgpt.com)

[image](https://www.google.com/s2/favicons?domain=https://docs.unity3d.com\&sz=32)

**Unity Profiler**

CPU, GPU, Memory dan Physics profiling.

[Unity Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/Profiler.html?utm_source=chatgpt.com)

# **10. Kesimpulan dan keputusan engineering**

Menurutku, ada satu hal yang harus menjadi prinsip utama selama pengerjaan proyek ini:

**Jangan membangun semua sistem terlebih dahulu. Bangun satu alur permainan lengkap, kemudian perluas sistem tersebut.**

Untuk The Last Sasak, alur paling tepat adalah:

# **Your First Milestone**

Prioritas pengerjaan pertama

**Player + Combat Test Arena**

[Story-driven action RPG Traveler’s Refrain launches on April 11 for PC Steam | RPG Site](https://images.openai.com/static-rsc-4/gJtHUn3uW7Rr-3q53g-Kuu1jEBbazUp-KTiLAcqGEE9MVPBKTNgw_kMzTfnpKwxGcoFelLLs257gezSvWQ_M4rP6oKUe03LYer0H9adslIsI9ZXx7S8YX4IdgzeOP6NuJLMQTlCBEycpeqZyu94_6zRtw_X_hurJEzcN0ouwlLM?purpose=inline)

[rpgsite.net](https://www.rpgsite.net/news/17029-travelers-refrain-pc-release-date-action-rpg-steam-gameplay-screenshots?utm_source=chatgpt.com)

Target akhir milestone ini adalah:

- Player dapat dikendalikan dengan baik.
- Cinemachine sudah terintegrasi.
- Basic Attack berjalan.
- Health dan Damage bekerja.
- Perfect Parry dan Deflect bekerja.
- Dodge dan Animation Canceling bekerja.
- Satu musuh Melee dan satu Ranged dapat bertarung.
- Musuh bisa mati dan kembali ke pool.
- Game Over berfungsi.

  **Baru setelah ini:** Integrasikan generator Hutan.

Ada tiga keputusan yang paling ingin aku tekankan:

1\. **Health dan CombatSystem harus menjadi fondasi sebelum UI serta sistem kematian musuh.** Jangan membuat masing-masing sistem memiliki perhitungan damage sendiri.

2\. **Room Manager harus menjadi penghubung antara generator, spawner dan progression.** Dengan demikian, procedural generation tidak harus mengetahui detail internal Enemy AI.

3\. **Cinemachine tetap berdiri sendiri sebagai sistem kamera.** Manfaatkan Impulse ketika combat sudah stabil, bukan menjadikannya bagian dari PlayerController.

Dengan urutan ini, pekerjaan yang sudah dibuat tidak sia-sia. `char walk`, `camera`, `debug attack`, `enemy ngejar`, `enemy spawner`, dan `map generate pattern` semuanya masih dapat digunakan, tetapi diintegrasikan ke struktur yang lebih jelas.

**Target akhirnya bukan hanya game yang mempunyai banyak fitur, melainkan satu vertical slice yang stabil, dapat diuji, dapat dikembangkan oleh anggota tim, dan dapat dipresentasikan sebagai demo.**