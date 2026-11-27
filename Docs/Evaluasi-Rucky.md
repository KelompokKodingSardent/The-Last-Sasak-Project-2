Menurutku, Rucky, The Last Sasak sekarang seharusnya memasuki tahap pengembangan yang berbeda: **bukan lagi membangun fondasi sistem, tetapi meningkatkan kualitas game sebagai sebuah produk yang benar-benar siap dimainkan.**

Setelah mempelajari seluruh rancangan dan ringkasan implementasi yang kamu berikan, aku melihat bahwa cakupan engineering-nya sudah cukup luas:

- Player Movement, Dodge, Parry, Combat dan Health.
- Enemy FSM, Navigation, Projectile dan Pooling.
- Combat Director dan Room Encounter.
- Modular World Generation dan Room Progression.
- Inventory 20 slot, Weapon Switching, Armor dan Artifact.
- Player Stat Calculation dan Stat Modifiers.
- Loot, EXP, Souls dan Permanent Upgrade.
- Save/Load dan Item Persistence.
- HUD, Inventory UI, Game Over dan Hub.

Artinya, menambahkan banyak sistem baru belum tentu membuat game lebih bagus.

Justru sekarang ada pekerjaan yang lebih penting: **game feel, balancing, enemy intelligence, level design, visual identity, audio, UX, progression dan quality assurance.**

Aku akan menyusun kelanjutannya sebagai roadmap baru.

# **The Last Sasak — Post-Implementation Polish Roadmap v1.0**

Recommended next milestone

# **Gameplay Quality & Production Polish**

Kasantara Studio · The Last Sasak: The Unknown Land

Tujuan: mengubah fondasi gameplay dan engineering yang telah dibangun menjadi pengalaman bermain yang konsisten, menarik, responsif, memiliki identitas visual, dan siap dipresentasikan.

# **P0**

Core experience

Wajib diprioritaskan

# **P1**

Presentation quality

Peningkatan kualitas

# **P2**

Quality of life

Penyempurnaan tambahan

# **10**

Polish phases

Sampai release candidate

## **1. Analisis: bagian mana yang sebenarnya perlu ditingkatkan?**

Aku membagi kebutuhan pengembangan menjadi empat kelompok.

| Kelompok        | Kondisi berdasarkan dokumen       | Fokus selanjutnya                       |
| --------------- | --------------------------------- | --------------------------------------- |
| Engineering     | Fondasi sistem telah dibangun     | Stabilitas dan integrasi                |
| Gameplay        | Mekanik utama tersedia            | Feel dan balancing                      |
| Presentation    | HUD, kamera dan feedback tersedia | Visual, animasi dan audio               |
| Product Quality | Core loop telah dirancang         | UX, accessibility dan release readiness |

Ada beberapa hal spesifik yang perlu diperhatikan sebelum masuk roadmap.

**Pertama — Combat masih memiliki ruang untuk dipoles.**

Dalam laporan Player Domain, sudah ada enam pilar combat: Responsive, Aggressive, Tactical, Fluid, Impactful dan Readable.

Namun, beberapa hal masih memerlukan validasi, seperti input buffer senjata lambat, micro-lunge, attack movement multiplier, akurasi hitbox, dan keterbacaan arah serangan.

Ini berarti kita tidak perlu membuat ulang CombatSystem. Kita perlu meningkatkan kualitas interaksi yang terjadi ketika pemain bertarung.

**Kedua — Enemy AI harus berkembang dari sekadar bekerja menjadi menyenangkan untuk dilawan.**

EnemyBrain sudah memiliki Idle, Chase dan Attack. Namun, AI yang dapat mengejar pemain belum tentu AI yang memiliki perilaku pertarungan berkualitas.

**Ketiga — Map generation harus menghasilkan perjalanan yang menarik, bukan sekadar map yang berbeda-beda.**

Graph, connector, placement, seed dan encounter compatibility merupakan fondasi teknis. Tantangan selanjutnya adalah pacing, variasi pengalaman, distribusi tantangan, dan reward.

**Keempat — Seluruh sistem presentation perlu mempunyai standar kualitas yang sama.**

Combat, inventory, menu, environment, audio dan animasi tidak boleh terasa seperti bagian dari game yang berbeda.

# **2. Catatan audit sebelum memulai polish**

Ada beberapa perbedaan antara dokumen implementasi dan baseline arsitektur yang perlu kamu rapikan.

| Dokumentasi lama                            | Baseline yang lebih baru                             |
| ------------------------------------------- | ---------------------------------------------------- |
| `Inventory` menyimpan daftar item sederhana | `InventoryController` memiliki 20 slot               |
| `ItemSwitching` mengatur pergantian senjata | `ItemSwitching` merupakan input adapter              |
| Belum mencantumkan `PlayerStatController`   | Satu-satunya kalkulator FinalStats                   |
| `RunMapGenerator` membaca room scene        | Arsitektur baru memiliki graph, solver dan assembler |
| `ForestRun` mempunyai dua room contoh       | Target world generation adalah susunan procedural    |

Perlu dipastikan juga bahwa implementasi Modular World Generation saat ini memang sudah menggunakan keseluruhan alur graph → placement → assembly → validation → NavMesh. Ringkasan gameplay yang kamu sertakan masih menggambarkan ForestRun berbasis ruangan yang diletakkan sebelumnya.

Kalau implementasi terbarunya sudah berbeda, cukup perbarui dokumentasi agar tidak menunjukkan keadaan yang sudah kedaluwarsa.

Ada juga satu catatan dari laporan Player Domain: pada snapshot tersebut, pengujian T01–T12 masih dicatat sebagai menunggu. Kalau sekarang seluruh pengujian sudah kamu selesaikan, perbarui status dan simpan hasilnya sebagai regression report.&#x20;

Pasted text (2)(6).txt



Ini penting karena kita tidak ingin melakukan polish di atas sistem yang sebenarnya belum stabil.

# **3. Roadmap pengembangan selanjutnya**

## **PHASE 14 — Integrated Quality Audit & System Freeze**

P0 · Prioritas pertama

**Objective:** Memastikan seluruh domain yang sudah dibangun benar-benar terintegrasi dan memiliki baseline yang stabil.

Aku ingin menjadikan fase ini sebagai titik terakhir untuk perbaikan arsitektur berskala besar.

### **Tasks**

1. Audit Player Domain.
2. Audit Combat Domain.
3. Audit Enemy Domain.
4. Audit Item & Equipment Domain.
5. Audit World Generation Domain.
6. Audit Progression & Save Domain.
7. Audit seluruh UI.
8. Audit semua scene.
9. Audit dependency antardomain.
10. Periksa seluruh event subscription, terutama objek pooling.
11. Periksa reset state saat Retry dan memulai run baru.
12. Perbarui seluruh dokumentasi berdasarkan implementasi aktual.

### **Integration Test**

Uji skenario lengkap:

```
MAIN MENU
    |
    v
START RUN
    |
    v
GENERATE WORLD
    |
    v
INITIALIZE PLAYER
    |
    v
EXPLORE ROOM
    |
    v
START ENCOUNTER
    |
    v
COMBAT
    |
    v
ENEMY DEFEATED
    |
    v
REWARD COLLECTED
    |
    v
EQUIPMENT / PROGRESSION
    |
    v
NEXT ROOM
    |
    v
FINAL ROOM
    |
    v
RUN COMPLETED
```

Uji juga jalur Player Death → Game Over → Retry.

### **Deliverables**

- Integrated System Audit.
- Domain Dependency Map.
- Regression Test Report.
- Updated Gameplay Documentation.
- System Baseline v1.0.

**Acceptance Criteria:** Semua scene dan sistem bekerja secara konsisten, baik ketika dimainkan langsung maupun melalui MainMenu.

## **PHASE 15 — Advanced Combat Feel & Weapon Balancing**

P0 · Sangat penting

Ini salah satu fase yang menurutku paling berpengaruh terhadap kualitas The Last Sasak.

Game Action RPG sangat bergantung pada bagaimana setiap input terasa ketika dimainkan.

Kita ingin serangan terasa responsif, memiliki bobot, mudah dibaca, dan memberikan kontrol yang cukup kepada pemain.

[Hades II - Nintendo Switch 2 Edition Review (Switch 2) | Nintendo Life](https://images.openai.com/static-rsc-4/kj_VHJl5oL8hB83NkiXedQMRN-UzEGuYOJ5_582GUKGKUc6lF2Brp96skO1TUp15p2WzYWg5w7-SS-43WS_lG99XLIVOntPPzegJOJbgfmiUByntCJJQ5uJvdaoZtQgpbjxtGPHGyiQ7cOVjZ7II1a_bD4sYk30VN-D2Ji5Nr_M?purpose=inline)

[黑神话悟空九叶灵芝精精魄怎么获得-九叶灵芝精精魄在哪里-佩琪手游网](https://images.openai.com/static-rsc-4/HpqYBBkoFUIrLxbBot0S0OT7cBGEZxkQw76JmqinoLY8qWRTs08XDFNB5QignFBxamrSlMzvdNVwoTAlA4PZBuIsWF9AW_Zbk8CbO88ZrbA3lTtRI4605K5cyP2Js8U5vlzHlEhLrNB8EnM2FjAl2fb3jsEyqL06bWngiahF1Q8?purpose=inline)

[젠레스 「전투 조작」 쉽게 이해하기 | Vortex Gaming](https://images.openai.com/static-rsc-4/ejHVHKgUesBQ4WTsblL1qFYgFBNpwosCVuuJmZhmh18GrWzUkVxxdeA6VpvekUMuLz3XmsQ1fiZu9MFH3Ms2HQSnjywjquVyeTLzjJUNYDo2awRNoJTdCh7gE_LSAuV0d2Zd5bN8drAs--AMj9C31-CIWbnhTAFGGF_LGS4bqe4?purpose=inline)

12

### **A. Weapon Identity**

Kamu memiliki baseline dua senjata:

- Klewang.
- Pedang Cepat.

Keduanya tidak cukup hanya memiliki damage berbeda.

Harus ada perbedaan karakteristik gameplay.

| Aspek         | Klewang                   | Pedang Cepat          |
| ------------- | ------------------------- | --------------------- |
| Attack pacing | Karakteristik lebih berat | Lebih cepat           |
| Startup       | Menyesuaikan moveset      | Menyesuaikan moveset  |
| Recovery      | Sesuai desain             | Sesuai desain         |
| Combo feel    | Bobot dan momentum        | Kelincahan            |
| Movement      | Sesuai konfigurasi        | Sesuai konfigurasi    |
| Hit feedback  | Sesuai bobot serangan     | Sesuai bobot serangan |

Angka pastinya jangan ditentukan hanya berdasarkan perkiraan. Gunakan hasil playtest.

### **B. Attack Detection Refinement**

Perluas pengujian hit detection:

- Akurasi hitbox berdasarkan arah serangan.
- Konsistensi AttackPoint.
- Filter sudut serangan.
- Pencegahan double hit.
- Target identification.
- Jarak hitbox dengan model senjata.
- Konsistensi arah serangan sepanjang startup dan active.

### **C. Combat Timing**

Poles:

- Input buffer tiap senjata.
- Combo transition.
- Attack startup.
- Active frame.
- Recovery.
- Whiff recovery.
- Dodge cancel.
- Parry recovery.
- Attack movement.
- Micro-lunge.

### **D. Combat Impact**

Satu serangan harus memiliki tiga bagian presentasi:

**Anticipation**

Pemain mendapatkan sinyal bahwa serangan akan dilakukan.

**Impact**

Saat serangan mengenai target, respons visual, audio, animasi, dan hitstop bekerja secara konsisten.

**Recovery**

Pemain merasakan konsekuensi setelah melakukan serangan.

### **Deliverables**

- Weapon Balance Sheet.
- Combat Timing Configuration.
- Hitbox Validation.
- Combat Feedback Specification.
- Combat Playtest Report.

**Acceptance Criteria:** Dua senjata memiliki identitas permainan yang berbeda, sementara seluruh mekanik combat tetap responsif dan konsisten.

## **PHASE 16 — Enemy Intelligence & Behavioral Design**

P0 · Sangat penting

Menurutku, ini akan memberikan peningkatan besar terhadap pengalaman bermain.

Jangan langsung menambah sepuluh jenis musuh. Pertama-tama, tingkatkan kualitas perilaku musuh yang sudah tersedia.

[Hades: 10 Hardest Enemies & How To Deal With Them – FandomSpot](https://images.openai.com/static-rsc-4/m5zmtye8DMbi6-XDbE1b5gQIRm515walU0w3suygXNF65xq-DP7eh_e4KHT7lAaPx16xZjCKS-sM01gOBorrnBVlXagDoHRHIwUOkVIPvCswDzyc7g4CdlfnFqB8Fo69uphd_ALQwfLaPOtdGWH4Gk0kZPN_L3qEmHd-5Ql1y8o?purpose=inline)

[Lies of P boss guide – How to defeat the Parade Master | Windows Central](https://images.openai.com/static-rsc-4/_MQ7YyiXlKYYqxLNn_688hgUolwDHpA00SakMP_N7POLTZOAVP7wpd5JMXXYnMbihRC4_UVjnXfw1pQR3WiqXqCXo6cUy11bD7B3fIeY2UyCLP8W9-NJj2CbVWAIabe2e0JytKemHyutEbJ53cSIZb-gEdF9EQWpdc2JzhN1zEk?purpose=inline)

[Apa Itu Genre Game Roguelike dan Bagaimana Sejarahnya? – Kepedia.co.id](https://images.openai.com/static-rsc-4/4xa17FLvShVSpSlMbMWfrCAPAnAAwLRey6fygXE_0XKruiwg0eizIvV8EtUqJZ7J7JpLw6IfNSLiDfcD9zok-opoalI7Xf7ytRAku9anHhUhjLM38kX5W-7oEwzC5NW3TegIpshqlZtKEtMxLXGROXYa-AYSzl2841lHh0lnzCU?purpose=inline)

9

### **A. Enemy Attack Readability**

Setiap serangan harus memiliki komunikasi visual yang jelas.

Contoh:

| Tahap        | Fungsi                                       |
| ------------ | -------------------------------------------- |
| Anticipation | Menunjukkan persiapan serangan               |
| Attack       | Menjalankan serangan                         |
| Recovery     | Memberikan kesempatan membaca akhir serangan |

Pemain harus bisa mempelajari pola serangan melalui permainan.

### **B. Enemy Combat Behavior**

Kembangkan perilaku yang diperlukan untuk archetype yang telah ada.

**Melee Enemy**

- Menjaga jarak serang yang tepat.
- Tidak terus-menerus menabrak Player.
- Memiliki timing attack yang jelas.
- Dapat bereaksi terhadap situasi combat.

**Ranged Enemy**

- Mempertahankan jarak yang sesuai.
- Memilih posisi yang dapat ditempati NavMesh.
- Mempertimbangkan line of sight.
- Tidak menembak melalui obstacle yang seharusnya menghalangi serangan.

**Elite Enemy**

- Memiliki pola serangan yang berbeda.
- Mempunyai telegraph yang dapat dipahami.
- Memanfaatkan sistem stagger.
- Memberikan tantangan yang layak dibanding musuh biasa.

### **C. Multi-Enemy Behavior**

Ini penting ketika pemain menghadapi beberapa musuh sekaligus.

Poles `CombatDirector` supaya:

- Tidak seluruh musuh menyerang bersamaan tanpa alasan.
- Musuh ranged tidak selalu berada pada posisi yang tidak bisa dijangkau.
- Melee tidak menumpuk secara tidak masuk akal.
- Pemain masih mempunyai ruang untuk Dodge dan Parry.
- Musuh tidak berkeliaran tanpa tujuan.

### **D. Navigation Quality**

Uji:

- Enemy stuck pada obstacle.
- Enemy berkumpul di doorway.
- Enemy gagal mencapai target.
- Enemy berada di area yang tidak memiliki NavMesh.
- Enemy mengejar melewati berbagai room.
- Enemy melakukan reset dengan benar ketika kembali ke pool.

### **Deliverables**

- Enemy Behavior Specification.
- Enemy Telegraph Configuration.
- CombatDirector Balancing.
- Enemy Navigation Test.
- Enemy Behavioral Playtest.

**Acceptance Criteria:** Setiap musuh memiliki perilaku yang dapat dipahami pemain dan memberikan tantangan yang adil.

## **PHASE 17 — Encounter Design & Roguelike Run Pacing**

P0 · Sangat penting

Menurutku, ini fase yang membedakan map generator biasa dengan roguelike yang menarik.

Generator kamu sudah memiliki fondasi graph dan room selection. Sekarang kualitas perjalanan pemain yang perlu ditingkatkan.

[Darwinism and video game design. How does darwinian evolution apply to… | by Thibaut Charron | Medium](https://images.openai.com/static-rsc-4/R2-uZ9yGvOqCXg3MFYQ57tXNeN4Dx5luoLORtULH_nwKG8Swt5LNfjjSRiVQTUe9rR0eAtiSEBrCAFpV-54aKdcS69I2KfJmt_een-DVm6Ho5nJmaICzaivVzO3z8PdiRjEiLWRq_ONUnrcVh5swlzJnQGR1zyOykQHph-MEKLs?purpose=inline)

[How Rock Band influenced Hades' soundtrack  – Laced Records](https://images.openai.com/static-rsc-4/cLecHJsdDu2frIfIJrDVpQjlSVKoKIuJk7NeyM0iAHZ5WAznq4Lqslw5JFne85UvNsX34JYOvqDMtauE5NEL3UW6qcYnFCucSoeNNiJK1KQ-bOdwbShPNnkzuFpiipPjybxbcB3ZK1uj20hMjW3zlx95qhfX9ED3xvtu4Qx6w2o?purpose=inline)

[Hades : 10 astuces pour bien débuter vos aventures au cœur des Enfers - Millenium](https://images.openai.com/static-rsc-4/rc8SkA5dlfmSNnSMPtMFQDcZXz0ge0So2OC2dStXZw0r-LdLZKR86zMdvEwsCT4LSfaLX_6QpZQf0CtoowkIHOVtt6O5rrEthV0t6q6cVKIs-3VzH6vXuVfqB2c1e32nOo-r1dmULVDSkjii-WwZd64yCp4emiCMEuLiqH1M_lo?purpose=inline)

13

### **A. Encounter Composition**

Buat variasi komposisi berdasarkan kebutuhan encounter.

Contohnya:

| Encounter         | Komposisi                               |
| ----------------- | --------------------------------------- |
| Standard Melee    | Kelompok melee                          |
| Mixed Combat      | Melee + ranged                          |
| Ranged Pressure   | Ranged dengan perlindungan ruang        |
| Elite Challenge   | Elite + pendukung jika disetujui desain |
| Special Encounter | Variasi tantangan tertentu              |

Tidak perlu membuat semuanya menjadi Wave System. Gunakan mekanik yang telah disepakati dalam GDD.

### **B. Difficulty Progression**

Tantangan sebaiknya berkembang sepanjang run.

Contoh konsep:

Ilustrasi kurva tantangan per perjalanan

Kurva konseptual, bukan nilai balancing final.

0 poin2 poin4 poin6 poinStartEarlyMidAdvancedFinal

Perkembangan tantangan tidak harus selalu berupa penambahan jumlah HP enemy.

Bisa menggunakan variasi komposisi, posisi, dan kondisi arena.

### **C. Room Pacing**

Susunan room harus memiliki ritme.

Contoh:

```
Starting Room
     |
Combat Room
     |
Traversal Room
     |
Reward / Branch
     |
Combat Room
     |
Elite Room
     |
Final Room
```

Ini contoh pola desain, bukan ketentuan bahwa setiap run harus mempunyai susunan yang sama.

### **D. Branching Decision**

Cabang map sebaiknya menawarkan pilihan yang memiliki alasan.

Contohnya:

- Jalur yang lebih menantang.
- Jalur reward.
- Jalur eksplorasi.
- Jalur yang menawarkan variasi encounter.

Pastikan pilihan tersebut memiliki konsekuensi yang dapat dipahami pemain.

### **E. Procedural Generation Quality**

Gunakan seed stress testing untuk memeriksa:

- Terlalu banyak room dengan layout serupa.
- Encounter berulang.
- Reward tidak merata.
- Branch tidak bermanfaat.
- Jalur utama terlalu panjang.
- Komposisi musuh tidak kompatibel.
- Tingkat kesulitan tidak konsisten.

### **Deliverables**

- Encounter Design Library.
- Run Pacing Specification.
- Difficulty Progression Configuration.
- Seed Quality Report.
- Room Sequence Test.

**Acceptance Criteria:** Setiap run terasa memiliki variasi yang bermakna, tetapi tetap memiliki alur tantangan yang terkontrol.

## **PHASE 18 — Forest Biome Art Direction & Environmental Polish**

P1 · Visual quality

Ini fase ketika game mulai mempunyai identitas visual yang kuat.

Arsitektur procedural room yang sudah kamu buat harus mulai mendapatkan kualitas visual final.

[Best Roguelike Games of 2026 - by Nick Pasta](https://images.openai.com/static-rsc-4/TfuDwhl3Z23OC_Nm4B-VwBHE19j2uFvqZnhVHwF5ff4YyYHwL1T7t5y55kBW4z--wjiE8FYhsfwR248zM1hLTvGC92VOQuLxaA3cmP1XIsYXvmmOhXTiQU2C2oOipmVFkgGesvhrJmqKtRJxtJxD91nrRBpr1IWmrHsFlgpme_I?purpose=inline)

[Explore the Mysterious Jungle Temple](https://images.openai.com/static-rsc-4/d7JUwoCOvmzXz9MGeIWDet_HYxW-imSlcj0RmmqcU9pRdZB1BaO2Wua3q_EcWJADWxnNvNdC7PCU2YFk3IOFP8pAYSvmUEsgkCO4Xf5LKG9pRTshY7jka9n8n8OdcACizyZIByaXHyQrwx7LLo1eCGqYx-A_nhaNdzCr-FQwO6I?purpose=inline)

[Yaesha - Remnant: From the Ashes 日本語攻略 Wiki](https://images.openai.com/static-rsc-4/_vqJQQpaIb0E4tn16IWdUrnKJwPBoQCETh7l1LLu8EvQrIS7mPS9rOHtbjEqee054dSSBWgKWB4E2xq4-pvDPrmwIJUdi3B9NdbW1ytQbxxUso1P82SQqZMHbq7H4GQ7-FeMuStpeNSrlRW8BdygMapjxmAmuh3Y9xcyZfEOl-4?purpose=inline)

18

### **A. Forest Visual Identity**

Tentukan:

- Color palette.
- Material palette.
- Lighting style.
- Fog density.
- Vegetation style.
- Architectural language.
- Atmospheric effects.

Hindari seluruh room hanya menggunakan prefab yang sama dengan dekorasi acak.

### **B. Environmental Asset Library**

Kembangkan library:

| Category     | Asset                              |
| ------------ | ---------------------------------- |
| Vegetation   | Pohon, akar, semak, rumput         |
| Natural      | Batu, tebing, kayu tumbang         |
| Architecture | Reruntuhan, struktur batu, monumen |
| Gameplay     | Gate, obstacle, loot pedestal      |
| Atmosphere   | Fog, particle, decals              |

### **C. Environmental Storytelling**

Lingkungan sebaiknya memberikan informasi tanpa harus selalu menggunakan dialog.

Contohnya:

- Reruntuhan yang menggambarkan sejarah wilayah.
- Struktur bangunan yang menunjukkan keberadaan peradaban lama.
- Objek lingkungan yang mengisyaratkan kejadian masa lalu.
- Landmark yang membantu pemain mengenali area.

### **D. Isometric Readability**

Bagian ini sangat penting.

Pastikan:

- Pohon tidak menutupi Player secara berlebihan.
- Obstacle dapat dibaca.
- Loot terlihat.
- Enemy tidak menghilang di balik dekorasi.
- Jalur utama mudah dikenali.
- Pencahayaan tidak menghilangkan siluet karakter.

### **E. Room Art Pass**

Seluruh room prefab perlu menjalani:

1. Greybox validation.
2. Production geometry.
3. Materials.
4. Lighting.
5. Decoration.
6. Collision validation.
7. NavMesh validation.
8. Final gameplay test.

**Acceptance Criteria:** Forest Biome memiliki identitas visual konsisten dan seluruh room tetap memenuhi standar gameplay.

## **PHASE 19 — Animation, VFX, SFX & Cinematic Game Feel**

P1 · Presentation quality

Fase ini berfokus pada bagaimana game memberikan respons kepada pemain.

[Hades](https://images.openai.com/static-rsc-4/oSPAqRPVtAPgPI59wlUNVvL0WYruDkyTJt8CJMIIphcruCJ2VAD9xnzJJx03N4WFcVWHIZ98O8XsWW1xUV7daMtKz1JJ1hMIWZuruR0RqU0M2CaNnAQc-I55n05BpcP02xVLR6krghNnczKu_I9sOo6SDTecVXm0kUZeptFjkZU?purpose=inline)

[젠레스 「전투 조작」 쉽게 이해하기 | Vortex Gaming](https://images.openai.com/static-rsc-4/ejHVHKgUesBQ4WTsblL1qFYgFBNpwosCVuuJmZhmh18GrWzUkVxxdeA6VpvekUMuLz3XmsQ1fiZu9MFH3Ms2HQSnjywjquVyeTLzjJUNYDo2awRNoJTdCh7gE_LSAuV0d2Zd5bN8drAs--AMj9C31-CIWbnhTAFGGF_LGS4bqe4?purpose=inline)

[Enotria: The Last Song im Test - Schmierentheater oder große Soulslike-Kunst?](https://images.openai.com/static-rsc-4/llKRGRepXn4SpuT5CYiZf3VRwWt15Ijuf3_4DF5To1fsspk89NMJJdanqdemtZFe_nHHXx-5uvjCXg0FpW1XNwu4lXIRuwR0QR0bUpV-dGiC2Me1BQpIFi1vnNeVePBzf6hp-SfMfWA0PIhpVmSaLaggOPcnoCsrxk3Ws2-xzp4?purpose=inline)

16

### **A. Character Animation Polish**

Poles animasi:

- Idle.
- Walk.
- Run jika ada.
- Attack.
- Combo.
- Dodge.
- Parry.
- Hit Reaction.
- Death.
- Weapon Switching.
- Interact.

Perhatikan transition, blending, timing, dan sinkronisasi event animasi dengan gameplay.

### **B. Enemy Animation**

Setiap archetype harus mempunyai presentasi yang jelas.

Prioritaskan:

- Locomotion.
- Attack anticipation.
- Attack execution.
- Recovery.
- Hit reaction.
- Stagger.
- Death.

### **C. VFX Library**

Buat efek yang mempunyai karakter berbeda:

| Event              | Feedback             |
| ------------------ | -------------------- |
| Sword slash        | Trail / slash effect |
| Hit impact         | Impact particles     |
| Perfect Parry      | Deflect sparks       |
| Projectile Deflect | Projectile feedback  |
| Dodge              | Movement trail       |
| Elite Stagger      | Stagger burst        |
| Loot               | Collection effect    |

Jangan menjadikan setiap serangan menghasilkan particle yang terlalu besar. Pertahankan keterbacaan medan pertempuran.

### **D. Audio Design**

Menurutku, audio merupakan bagian yang jangan sampai tertinggal.

Kelompokkan:

- Combat SFX.
- Weapon SFX.
- Enemy SFX.
- Movement SFX.
- Environment ambience.
- UI feedback.
- Reward feedback.
- Music.

Gunakan Audio Mixer untuk pengelompokan dan pengaturan level.

### **E. Camera & Hitstop**

Kembangkan konfigurasi yang sudah kamu buat:

- Camera impulse.
- Hitstop.
- Damping.
- Camera transition.
- Feedback saat Perfect Parry.
- Feedback serangan Elite.

**Acceptance Criteria:** Pemain dapat merasakan perbedaan antara menyerang, mengenai musuh, meleset, melakukan parry dan menerima damage melalui feedback yang konsisten.

## **PHASE 20 — UI/UX, Player Guidance & Information Architecture**

P1 · User experience

UI yang berfungsi belum tentu merupakan UI yang nyaman digunakan.

Kamu sudah mempunyai HUD, HealthBarUI, InventoryUI, GameOverUI dan StaggerBarUI. Sekarang tingkatkan kualitas interaksi dan penyampaian informasinya.

[The UX of Hades: Why we run from hell and back | Medium](https://images.openai.com/static-rsc-4/FsPEuJ-kwQpHProB7oPL0O7V8TCVAysz0qiOJ0okOgr6ghz-psQlL1ZMMJXixVRPqyYkMjox9-v8u-aAfwHcyHpRYNLw8RZ1py7mYglJyPLYvU2PDsOPjgycCJmGLPxuHq-i0GA-Bh37KbwtEOOnkK6bbFzwhOey1eERrtbTWck?purpose=inline)

[Dark Lands - RPG UI Kit](https://images.openai.com/static-rsc-4/CwpN3VOWT2NQt0LuxtKbkBvgdNZ3hhUEizbpQEgDdY5ueIbk81ibbs5Gz-KJkG8qlQ44sJR4xokK80FKtn-zNbO-mUTfxAt4xrsCVwumQyhF6j9gSBGq07-xjp7jg9IdI-7Zj3ZtHW2jHKEcM5mDD9w2gaQeKvdYJhnPzNe6eqs?purpose=inline)

[Tower of Babel: Survivors of Chaos game revenue and stats on Steam – Steam Marketing Tool](https://images.openai.com/static-rsc-4/NQmCjION3d-8_COnLGcMIu8yBE_dSj_HgaIyAUFSHoQGMDlCimXtWqV70PeZnx3lcQ5NdhZz-PMaXx3ItLKbK-lVeFwx1kMREoSWzhSbynltLJXxezqGcAQTs3K6wDSf6ycy4D8_oYPnZkA_VOo4vevBN9-yXV9E0xWZ7LM3NTE?purpose=inline)

14

### **A. HUD Refinement**

Informasi yang perlu terlihat dengan jelas:

- Player Health.
- Souls.
- EXP.
- Current Level.
- Active Weapon.
- Second Weapon.
- Artifact.
- Room State.

Jangan menampilkan semua informasi dengan ukuran yang sama.

Informasi combat harus lebih mudah dibaca daripada informasi sekunder.

### **B. Inventory & Equipment UI**

Untuk inventory 20 slot:

- Item icon.
- Item quantity.
- Item name.
- Item description.
- Item type.
- Equipment status.
- Stat information.

Ketika pemain memilih weapon atau armor, tampilkan informasi perbandingan.

Contohnya:

```
CURRENT EQUIPMENT
Pedang Cepat

DAMAGE         20

CANDIDATE
Klewang

DAMAGE         30

STAT DIFFERENCE
Damage         +10
Attack Speed   -...
```

Angka tersebut hanya ilustrasi format UI.

### **C. Menu & Navigation**

Periksa:

- Main Menu.
- Pause Menu.
- Settings.
- Inventory.
- Equipment.
- Game Over.
- Hub.
- NPC Interaction.

Tambahkan dukungan navigasi gamepad pada seluruh UI yang relevan.

### **D. Player Guidance**

Pemain baru harus mengerti:

- Cara menyerang.
- Cara Dodge.
- Cara Parry.
- Cara mengambil loot.
- Cara menggunakan equipment.
- Cara berpindah room.
- Cara menggunakan Hub.

Gunakan petunjuk kontekstual seperlunya, bukan tutorial panjang yang memutus permainan terlalu sering.

### **Deliverables**

- UI Design Specification.
- HUD Revision.
- Inventory & Equipment UI.
- Pause and Settings UI.
- Interaction Guidance.

**Acceptance Criteria:** Pemain dapat memahami informasi penting, mengakses fitur, dan menggunakan inventory tanpa kebingungan.

## **PHASE 21 — Progression, Loot Economy & Persistence Balancing**

P1 · Gameplay balance

Sistem inventory, equipment, artifact, reward dan stats kamu sudah memiliki fondasi data-driven yang bagus.

Sekarang fokusnya adalah menjaga agar seluruh sistem tersebut memberikan progression yang menarik.

### **A. Progression Balancing**

Periksa:

- EXP requirement per level.
- Bonus statistik per level.
- Hubungan level dengan difficulty.
- Stat modifier dari equipment.
- Artifact modifier.
- Permanent upgrade.

### **B. Loot Economy**

Pastikan reward memiliki nilai yang proporsional.

Pertanyaan desain yang harus dijawab:

- Berapa sering pemain mendapatkan item?
- Apakah item harus diganti untuk mendapatkan benefit?
- Apakah reward dari musuh sepadan dengan tantangannya?
- Apakah Souls cukup untuk membeli upgrade?
- Apakah item yang sudah diperoleh tetap memiliki kegunaan?

### **C. Inventory & Equipment Rules**

Uji:

- Inventory penuh.
- Item duplicate.
- Item quantity.
- Equip dan unequip.
- Swap weapon slot.
- Armor replacement.
- Artifact slot penuh.
- Item tidak kompatibel.

### **D. Death & Persistence Policy**

Ada satu keputusan penting dalam dokumen Item & Equipment Domain:

`ItemPersistenceCoordinator.PersistInventoryAndEquipment`

Flag tersebut mengontrol apakah inventory dan equipment ikut dipertahankan.

Menurutku, sebelum balancing final, pastikan aturan ini sudah disahkan:

| Data              | Pertanyaan desain                           |
| ----------------- | ------------------------------------------- |
| Souls             | Apakah tetap tersimpan setelah Player mati? |
| Permanent Upgrade | Apakah selalu bertahan antar-run?           |
| EXP               | Apakah di-reset ketika run selesai?         |
| Inventory         | Apakah item hilang atau dipertahankan?      |
| Equipment         | Apakah pilihan perlengkapan dipertahankan?  |
| Artifact          | Apakah hanya berlaku selama run?            |

Jangan menambahkan Crafting, Shop & Trading atau Randomized Stats dahulu. Ketiganya memang berada dalam backlog pasca-v1.0.

### **E. Save Reliability**

Pastikan:

- Save version.
- JSON validation.
- Missing item ID.
- Unknown item ID.
- Corrupted save fallback.
- Safe write.
- Load consistency.

**Acceptance Criteria:** Progression terasa bermanfaat, reward memiliki nilai, dan data pemain tidak hilang akibat lifecycle yang tidak jelas.

## **PHASE 22 — Accessibility, Controls & Quality of Life**

P2 · Quality of life

Ini sering terlupakan pada game yang sudah mempunyai banyak sistem gameplay.

### **Tasks**

**Input**

- Keyboard/mouse.
- Gamepad.
- Input rebinding.
- Sensitivity.
- Controller navigation.

**Visual**

- Ukuran teks.
- Kontras informasi.
- Pengaturan camera shake.
- Pengaturan efek visual.

**Audio**

- Master volume.
- Music volume.
- SFX volume.
- Ambient volume.

**Gameplay**

- Pause behavior.
- Control guide.
- Tutorial reminder.
- Confirmation untuk tindakan penting.

### **Acceptance Criteria**

Pemain bisa mengatur pengalaman bermain sesuai kebutuhan dan seluruh kontrol berfungsi secara konsisten.

## **PHASE 23 — Performance, Stability & Windows Release Readiness**

P0 · Release requirement

Ini bukan sekadar optimasi FPS.

Kita perlu memastikan game stabil ketika dijalankan di luar Unity Editor.

### **A. Performance Profiling**

Gunakan Unity Profiler pada standalone build.

Periksa:

| Area       | Yang diukur                   |
| ---------- | ----------------------------- |
| CPU        | Gameplay, AI, generation      |
| GPU        | Rendering, lighting, effects  |
| Memory     | Assets, textures, objects     |
| GC         | Allocation dan spikes         |
| Physics    | Collision dan overlap         |
| Navigation | NavMesh dan Agent             |
| Generation | Graph, placement dan assembly |

### **B. Procedural Generation Stress Test**

Jalankan banyak seed.

Catat:

- Generation success rate.
- Generation duration.
- Room count.
- Placement failure.
- Navigation validity.
- Encounter compatibility.
- Memory usage.

### **C. Gameplay Stress Test**

Simulasikan kondisi:

- Banyak Enemy aktif.
- Banyak projectile.
- Banyak VFX.
- Banyak room dalam satu run.
- Pergantian weapon berulang.
- Retry berulang.
- Game Over berulang.

### **D. Stability**

Uji:

- Tidak ada missing reference.
- Tidak ada Missing Script.
- Tidak ada duplicate singleton.
- Tidak ada event subscription tertinggal.
- Tidak ada pooled object dengan state lama.
- Save tidak rusak.
- Scene tidak mengalami loading failure.

### **E. Build**

Uji Windows standalone dari awal sampai akhir.

**Acceptance Criteria:** Build dapat dimainkan tanpa Unity Editor, tidak memiliki error kritis dan memenuhi target performa yang sudah ditentukan.

## **PHASE 24 — External Playtesting, Balancing & Final Release Candidate**

P0 · Final milestone

Ini adalah fase yang menurutku paling menentukan apakah The Last Sasak sudah benar-benar bagus atau baru terlihat bagus bagi developer-nya sendiri.

Kamu dan tim sudah sangat memahami sistem internal game. Justru karena itu, kalian berpotensi tidak menyadari bagian yang membingungkan pemain baru.

### **A. External Playtesting**

Berikan build kepada orang yang belum pernah memainkan game.

Jangan langsung mengajari seluruh mekaniknya.

Amati:

- Apakah mereka memahami kontrol?
- Apakah mereka memahami tujuan run?
- Apakah mereka mengerti kapan harus parry?
- Apakah mereka tahu mengapa serangan gagal?
- Apakah mereka memahami inventory?
- Apakah mereka tahu apa yang harus dilakukan setelah room cleared?
- Apakah mereka bisa memahami reward dan upgrade?

### **B. Balancing Report**

Catat pengalaman tiap playtester.

| Aspek       | Yang dievaluasi              |
| ----------- | ---------------------------- |
| Movement    | Respons kontrol              |
| Combat      | Rasa serangan                |
| Difficulty  | Tingkat tantangan            |
| Enemy       | Keterbacaan serangan         |
| Map         | Navigasi dan eksplorasi      |
| Reward      | Kepuasan progression         |
| UI          | Kemudahan memahami informasi |
| Performance | Kelancaran permainan         |

Jangan mengubah sistem hanya karena satu komentar. Cari pola permasalahan yang muncul berulang.

### **C. Final Demo Acceptance**

The Last Sasak harus bisa:

1. Dibuka dari Main Menu.
2. Memulai run.
3. Menghasilkan world.
4. Memainkan seluruh core loop.
5. Menyelesaikan encounter.
6. Mendapatkan reward.
7. Menggunakan equipment.
8. Melanjutkan perjalanan.
9. Menyelesaikan final room.
10. Mengalami Game Over dan Retry.
11. Menyimpan data permanen.
12. Berjalan pada Windows standalone build.

**Final Deliverable:**

Final Milestone

# **The Last Sasak — Demo Release Candidate**

Gameplay Quality · Visual Quality · Audio · UX · Stability

- Integrated Gameplay Build.
- Final Forest Biome.
- Balanced Combat & Enemy.
- Completed UI/UX.
- Audio & Visual Effects.
- Validated Progression.
- Performance Report.
- External Playtest Report.
- Windows Standalone Build.

# **4. Urutan prioritas yang paling aku rekomendasikan**

Dari sepuluh fase di atas, jangan menjalankan semuanya sekaligus. Ini urutan dependensi yang paling masuk akal:

## **Recommended Development Order**

1. **Phase 14 — Integrated Quality Audit**

   Bekukan fondasi dan rapikan dokumentasi.
2. **Phase 15 — Combat Feel & Balancing**

   Buat pertarungan terasa responsif dan memuaskan.
3. **Phase 16 — Enemy Intelligence**

   Buat musuh memiliki perilaku dan tantangan yang menarik.
4. **Phase 17 — Encounter & Run Pacing**

   Buat setiap run mempunyai ritme dan variasi.
5. **Phase 18 — Environment Art Direction**

   Bentuk identitas visual dunia Hutan.
6. **Phase 19 — Animation, VFX & Audio**

   Tingkatkan kualitas presentasi dan feedback.
7. **Phase 20 — UI/UX**

   Buat seluruh sistem mudah digunakan.
8. **Phase 21 — Progression & Economy**

   Seimbangkan item, loot dan perkembangan pemain.
9. **Phase 22 — Accessibility & QoL**

   Tingkatkan kenyamanan bermain.
10. **Phase 23 — Performance & Stability**

    Siapkan build yang dapat diandalkan.
11. **Phase 24 — External Playtesting**

    Validasi kualitas permainan dari perspektif pemain.

# **5. Fitur yang belum perlu kamu tambahkan**

Ada beberapa hal yang sengaja akan aku tahan dulu.

| Fitur                        | Keputusan                              |
| ---------------------------- | -------------------------------------- |
| Biome kedua dan ketiga       | Tunda                                  |
| Open world                   | Tidak diperlukan                       |
| Multiplayer                  | Out of scope                           |
| Crafting                     | Tunda                                  |
| Randomized item stats        | Tunda                                  |
| Shop & Trading               | Tunda                                  |
| Skill Tree kompleks          | Tunda                                  |
| Puluhan weapon baru          | Tunda                                  |
| Puluhan artifact baru        | Tunda                                  |
| Boss framework yang kompleks | Tunda sampai kebutuhan desain jelas    |
| Rebuild seluruh CombatSystem | Jangan dilakukan tanpa masalah konkret |

Aku lebih memilih dua senjata yang terasa benar-benar berbeda daripada sepuluh senjata yang hanya berbeda angka damage.

Aku juga lebih memilih satu biome Hutan yang mempunyai kualitas visual dan gameplay bagus daripada lima biome yang semuanya masih terasa seperti prototipe.

# **6. Lima aspek yang paling menentukan kualitas akhir The Last Sasak**

[Hades II - Nintendo Switch 2 Edition Review (Switch 2) | Nintendo Life](https://images.openai.com/static-rsc-4/kj_VHJl5oL8hB83NkiXedQMRN-UzEGuYOJ5_582GUKGKUc6lF2Brp96skO1TUp15p2WzYWg5w7-SS-43WS_lG99XLIVOntPPzegJOJbgfmiUByntCJJQ5uJvdaoZtQgpbjxtGPHGyiQ7cOVjZ7II1a_bD4sYk30VN-D2Ji5Nr_M?purpose=inline)

**1. Combat Feel**

Pemain harus merasa memiliki kendali. Setiap serangan, dodge, parry dan hit harus memberikan respons yang tepat.

Prioritas tertinggi

[《哈迪斯》评测：失败和胜利同样有趣 - GameRes游资网](https://images.openai.com/static-rsc-4/nPPIVUZXeMBIp2lhEZOjFnqWosp7DfaDvA5vx24dAeU7nddSP3opcNY8yj3kET65rfI2FCEu6peo3wDiCkj-ZXABXAJ-JT_uUQ6GC-9EeJzBgKPrd5JOaAKJipAl5ZDP1MWaAPk6xR5L6DVs3EK8bhMzL-z5aZjPBhNYlnW4V4A?purpose=inline)

**2. Encounter Quality**

Musuh dan komposisi encounter harus memberikan tantangan yang menarik, bukan sekadar menambah jumlah HP.

Prioritas tertinggi

[Dark, architecture, nature, forest, growth, fantasy](https://images.openai.com/static-rsc-4/ZziiQXjRUp7fp9kDkdWxZ2EiSPOhfyHGXo2yms6S6I7xXE1YcDMHPEAbrB5EqFtUAifzTgdwFCo2lOf4YAk7y8DEPYiZk38SiL368j8sEyNtgN8ENDURJdBxs5l9hYF3sIOs8nMBnR7EOKS00wJ4faVD7MpQ6ThnPuEz-c3yaaY?purpose=inline)

**3. World Identity**

Dunia harus memiliki karakter yang mudah dikenali, dari environment hingga atmosfer.

Visual priority

[Sinder Blade | Deku Deals](https://images.openai.com/static-rsc-4/KTCy4bzgv_WTgBMhEqafuOY9CVmjDOGnmtC5XwCFXf8AJ8ecLjf4Q_5i2J4u9DQsb9xky4EKR6WEkVKw8iEdUz6ZlFqmWLRJfiEOYgpiExhEIw-mDd7DKQOCb93e8btN75tv5d2BYdGd9zyjvwWX7AbqCIVMJ_BKWkHWwfXgwqk?purpose=inline)

**4. Game Presentation**

Animasi, VFX, SFX, musik dan kamera harus bekerja bersama membangun kualitas pengalaman bermain.

Presentation priority

[Steam Community :: Guide :: Pro Tips To Escape The Underworld](https://images.openai.com/static-rsc-4/_P9H-SGPn6g3k0aekKFwzdlFhJySFmNtFH2kx2g1EIbnA3g3UFFyPA-FiEGoicaFzlVyo-sDe_a1kMurvqMJTZsbl9HTpa8jx1P7kXYaCDqquqBt2BmxVFLyPXW-ulNn_4gFvEax2o7rcsd2VwGr9SQqI8EKXcG032T5GOWN10g?purpose=inline)

**5. UX & Progression**

Pemain harus memahami sistem, menikmati perkembangan karakter, dan mempunyai alasan untuk melanjutkan perjalanan.

Experience priority

# **Kesimpulan: apa yang sebaiknya kamu kerjakan sekarang?**

Menurutku, kamu sudah melewati tahap yang cukup berat: menyusun arsitektur, memisahkan domain, membangun sistem gameplay, membangun inventory/equipment, dan mengembangkan fondasi dunia procedural.

Sekarang jangan terjebak dalam kebiasaan membuat semakin banyak sistem hanya karena masih ada banyak kemungkinan teknis.

**Aku ingin mengubah fokus pengembangan Kasantara dari Engineering Expansion menjadi Game Quality Development.**

Urutan yang paling tepat untuk segera dimulai:

1. Phase 14: pastikan implementasi seluruh domain konsisten dan dokumentasinya diperbarui.
2. Phase 15: poles combat sampai benar-benar nyaman dimainkan.
3. Phase 16: tingkatkan kualitas perilaku musuh.
4. Phase 17: seimbangkan encounter dan perjalanan procedural.
5. Phase 18–24: selesaikan visual, audio, UX, progression, performa dan pengujian pemain.

Satu hal yang ingin aku tekankan:

**Game yang bagus bukan game yang memiliki paling banyak sistem. Game yang bagus adalah game yang setiap sistemnya bekerja bersama untuk menghasilkan pengalaman bermain yang menarik.**

Untuk The Last Sasak, aku akan menjadikan kualitas combat, keadilan encounter, identitas dunia Hutan, dan kepuasan progression sebagai empat pilar utama polish berikutnya. Itu lebih bernilai sekarang daripada langsung menambahkan biome, puluhan item, atau sistem gameplay baru.