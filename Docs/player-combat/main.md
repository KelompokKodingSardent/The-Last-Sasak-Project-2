# **THE LAST SASAK: UNKNOWN LAND**

# **Player Combat Design & Implementation Blueprint v1.0**

Kasantara Studio · Game Design & Engineering Specification · 21 November 2026

Prioritas produksi berikutnya

Combat Development

Unity

Isometric Action Roguelike

**Status dokumen:** Rancangan pengembangan dan rekomendasi baseline. Mekanik yang belum ditentukan dalam GDD harus disahkan oleh Project Lead sebelum dianggap sebagai keputusan final.

# **BAGIAN I — ANALISIS DAN DASAR PERANCANGAN**

## **1.1. Kondisi proyek saat ini**

Sebelum menentukan apa yang harus dibuat, kita perlu memahami posisi proyek sekarang.

Dari ringkasan scene dan sistem gameplay yang kamu berikan, The Last Sasak sudah memiliki fondasi teknis yang cukup lengkap.

Kamu sudah mempunyai:

| Domain         | Sistem yang sudah tersedia                                           |
| -------------- | -------------------------------------------------------------------- |
| Core           | GameManager, GameState, GameEvents                                   |
| Player         | InputReader, Motor, FacingController, ActionController               |
| Player Combat  | AttackController, DodgeController, ParryController, HitboxController |
| Player Defense | InvulnerabilityHandler, ParryWindow                                  |
| Health         | Health, PlayerDeathHandler                                           |
| Combat         | CombatSystem, CombatResolver, ParryResolver, Projectile              |
| Enemy          | EnemyBrain, EnemyMovement, EnemyAttackController                     |
| Enemy Data     | EnemyDefinitionSO, AttackDefinitionSO                                |
| World          | RoomController, RoomManager, RoomEncounterController                 |
| Progression    | CurrencyWallet, ExperienceSystem, RewardCollector                    |
| Presentation   | Cinemachine, PlayerAnimationController, TimeEffectController         |
| UI             | HUDController, HealthBarUI, GameOverUI                               |

Kamu juga sudah mempunyai beberapa scene pengujian yang bisa digunakan tanpa perlu membangun arena baru melalui kode.

Ini merupakan kondisi awal yang bagus.

Namun, ada satu hal yang perlu kita bedakan.

**Memiliki sistem combat tidak sama dengan memiliki combat yang matang.**

Contohnya:

- `AttackController` sudah ada, tetapi apakah rangkaian serangannya menyenangkan?
- `DodgeController` sudah ada, tetapi apakah perpindahannya terasa tepat?
- `ParryController` sudah ada, tetapi apakah keberhasilannya memberikan kepuasan?
- `HitboxController` sudah ada, tetapi apakah posisi dan jangkauan serangannya sesuai animasi?
- `PlayerActionController` sudah ada, tetapi apakah perpindahan antaraksi terasa natural?
- `PlayerAnimationController` sudah ada, tetapi apakah animasi dan hasil perhitungan gameplay benar-benar sinkron?

Itulah pekerjaan yang perlu kita lakukan berikutnya.

### **Penilaian kondisi sekarang**

# **Fondasi engineering sudah tersedia**

Penilaian kualitatif berdasarkan struktur yang kamu laporkan, bukan hasil audit Play Mode.

**Arsitektur gameplay**

Sudah dibangun

**Player movement**

Sudah tersedia

**Damage architecture**

Sudah tersedia

**Combat action system**

Sudah tersedia

**Enemy encounter**

Sudah tersedia

**Combat design maturity**

Perlu dikembangkan

**Animation integration**

Perlu dievaluasi

**Game feel**

Perlu dikembangkan

Jadi, rekomendasiku bukan membangun ulang semua script yang sudah tersedia.

Kita akan meningkatkan kualitas perilaku gameplay melalui sistem yang sudah kamu bangun.

## **1.2. Mengapa Player Combat harus menjadi prioritas?**

Ada tiga alasan utama.

**Pertama, Player adalah pusat interaksi permainan.**

Pemain akan menghabiskan hampir seluruh waktunya mengendalikan satu karakter.

Apabila karakter tersebut terasa kaku, tidak responsif, atau membosankan ketika menyerang, variasi arena dan musuh tidak akan cukup untuk memperbaiki pengalaman bermain.

**Kedua, desain Enemy bergantung pada kemampuan Player.**

Misalnya, kamu ingin mempunyai:

- Vanguard yang melakukan serangan jarak dekat.
- Skirmisher yang melakukan flanking.
- Ranger yang mengontrol jarak.
- Bulwark yang memiliki serangan berat.
- Controller yang memberikan tekanan area.

Namun, bagaimana musuh-musuh tersebut akan menjadi menyenangkan untuk dilawan apabila Player belum mempunyai respons combat yang matang?

Enemy dan Player harus didesain sebagai dua bagian yang saling melengkapi.

**Ketiga, sistem progression bergantung pada identitas combat.**

Dokumen combat sebelumnya sudah mengusulkan beberapa contoh build:

- Combo Build.
- Counter Build.
- Mobility Build.

Ketiganya memiliki mekanik yang berbeda, tetapi membutuhkan fondasi Player Combat yang solid terlebih dahulu.&#x20;

Pasted markdown (3).md



Karena itu, urutan pengembangan yang paling rasional adalah:

```
Player Combat
      |
      v
Enemy Combat Interaction
      |
      v
Combat Encounter
      |
      v
Player & Enemy Variety
      |
      v
Roguelike Build & Progression
      |
      v
Procedural Encounter Variety
```

Kita tidak perlu membangun semua cabang tersebut sekarang.

Kita harus memastikan satu bagian bekerja dengan sangat baik sebelum bagian lainnya bergantung kepadanya.

## **1.3. Identitas combat yang harus dipertahankan**

Combat Design sebelumnya sudah menetapkan empat pilar:

1. Aggressive Combat.
2. Reactive Combat.
3. Intelligent Enemy Behavior.
4. Roguelike Variety.

Dokumen tersebut juga menekankan bahwa Player harus merasa menguasai pertempuran, bukan terus menunggu kesempatan menyerang.&#x20;

Pasted markdown (3).md



Aku akan mempertahankan empat pilar tersebut dan menurunkannya menjadi enam prinsip implementasi.

| Prinsip    | Makna dalam gameplay                                          |
| ---------- | ------------------------------------------------------------- |
| Responsive | Input segera mendapatkan respons yang sesuai                  |
| Aggressive | Pemain didorong menyerang dan mempertahankan momentum         |
| Tactical   | Pemain harus membaca ancaman dan menentukan respons           |
| Fluid      | Transisi antaraksi tidak terasa patah                         |
| Impactful  | Serangan dan keberhasilan mekanik memiliki feedback yang kuat |
| Readable   | Pemain selalu dapat memahami apa yang sedang terjadi          |

Enam prinsip ini menjadi standar evaluasi seluruh mekanik yang akan kita buat.

### **Identitas yang ingin dicapai**

## **The Last Sasak Combat Identity**

*A fast-paced isometric action roguelike where every attack carries weight, every movement matters, and every defensive action creates an opportunity.*

Player harus merasa agresif ketika menyerang, percaya diri ketika melakukan counter, dan tetap harus memperhatikan ancaman dari berbagai arah.

Kita boleh mengambil inspirasi dari Hades dan Zenless Zone Zero dalam hal respons serangan, momentum, dan pertarungan.

Namun, bukan berarti The Last Sasak harus meniru seluruh sistem mereka.

Identitas permainanmu harus tetap dibangun berdasarkan kombinasi karakteristik yang sudah ditetapkan.

# **BAGIAN II — TARGET DAN SCOPE PLAYER COMBAT**

## **2.1. Tujuan utama**

Kita akan membangun satu sistem Player Combat yang mempunyai:

1. Offensive Combat.
2. Defensive Combat.
3. Combat Action Management.
4. Animation Integration.
5. Combat Feedback.
6. Enemy Interaction.
7. Combat Testing.

Hasil akhirnya adalah Player yang mempunyai rangkaian aksi lengkap dan mampu digunakan untuk menjalankan satu encounter tanpa bergantung pada sistem tambahan yang belum selesai.

## **2.2. Target gameplay**

Gambaran pengalaman yang ingin dicapai:

[Hades II - Nintendo Switch 2 Edition Review (Switch 2) | Nintendo Life](https://images.openai.com/static-rsc-4/kj_VHJl5oL8hB83NkiXedQMRN-UzEGuYOJ5_582GUKGKUc6lF2Brp96skO1TUp15p2WzYWg5w7-SS-43WS_lG99XLIVOntPPzegJOJbgfmiUByntCJJQ5uJvdaoZtQgpbjxtGPHGyiQ7cOVjZ7II1a_bD4sYk30VN-D2Ji5Nr_M?purpose=inline)

[Hades](https://images.openai.com/static-rsc-4/oSPAqRPVtAPgPI59wlUNVvL0WYruDkyTJt8CJMIIphcruCJ2VAD9xnzJJx03N4WFcVWHIZ98O8XsWW1xUV7daMtKz1JJ1hMIWZuruR0RqU0M2CaNnAQc-I55n05BpcP02xVLR6krghNnczKu_I9sOo6SDTecVXm0kUZeptFjkZU?purpose=inline)

[How to Beat Zenless Zone Zero Shiyu Defense - Best Teams and More](https://images.openai.com/static-rsc-4/1LzexMvmc87ewBfbgIPiypOULiwtx9d9sxtI4vVedENZVqgMlXhzpz_atNBgdmys-4xhBTdbGI0WnWx8OZ9wqFdRphTgio5leaeVbozjv5SYzxg8KVqMMwEi2m-ZEVoju2mwQSiOwMjgfoG7H0Q4HYFl-AikZHUzpa1TEs6LFDk?purpose=inline)

9

Ketika bermain, pemain seharusnya dapat:

- Bergerak sambil mencari posisi serangan.
- Melakukan beberapa serangan secara berurutan.
- Mengubah arah serangan sesuai situasi.
- Melakukan dodge ketika menghadapi ancaman.
- Melakukan parry dengan timing yang tepat.
- Mengembalikan projectile melalui deflect.
- Memanfaatkan recovery musuh.
- Melanjutkan serangan setelah mendapatkan kesempatan.
- Menghadapi beberapa musuh dengan kemampuan yang dimiliki.

Semua harus terasa sebagai satu kesatuan, bukan kumpulan script terpisah.

## **2.3. Batasan scope**

Aku sangat menyarankan agar tahap ini tidak melebar terlalu jauh.

| Termasuk dalam milestone      | Belum menjadi prioritas     |
| ----------------------------- | --------------------------- |
| Basic Attack                  | Lima jenis senjata          |
| Combo System                  | Skill tree kompleks         |
| Dodge dan i-frame             | Banyak variasi dodge        |
| Perfect Parry                 | Sistem counter bertingkat   |
| Projectile Deflect            | Berbagai jenis projectile   |
| Attack Cancel                 | Animation framework generik |
| Animation Integration         | Motion matching kompleks    |
| Hitstop dan Feedback          | Cinematic finisher          |
| Satu Vanguard untuk pengujian | Seluruh enemy roster        |
| Satu arena statis             | Procedural map              |

Keputusan ini bukan membatasi potensi game. Ini mencegah pekerjaan melebar sebelum core gameplay matang.

# **BAGIAN III — PLAYER COMBAT DESIGN**

## **3.1. Gambaran besar mekanik Player**

Aku menyarankan pembagian combat Player sebagai berikut.

## **Player Combat Architecture**

**Player Input Reader**

Membaca seluruh perintah pemain

**Player Action Controller**

Validasi dan pengelolaan action state

**Offense**

Attack / Combo

**Defense**

Dodge / Parry

**Movement**

Motor / Facing

**Animation & Combat Presentation**

Animator, VFX, SFX, hitstop, camera impulse

**Combat System → Combat Resolver → Health**

Penyelesaian interaksi dan hasil serangan

Ini merupakan pembagian konseptual. Bukan instruksi untuk membuat seluruh komponen baru.

Kita menggunakan komponen yang sudah ada.

## **3.2. Player Control Specification**

Kontrol yang sudah ditetapkan tetap dipertahankan.

| Aksi          | Keyboard / Mouse   | Gamepad          |
| ------------- | ------------------ | ---------------- |
| Movement      | WASD               | Left Stick       |
| Basic Attack  | Left Mouse / J     | West             |
| Parry         | Right Mouse / K    | North            |
| Dodge         | Space / Left Shift | East / RB        |
| Interact      | E                  | South            |
| Switch Weapon | 1 / 2              | Belum ditetapkan |

Ada satu hal yang perlu ditentukan sebelum implementasi: **arah serangan.**

Pada game isometrik, arah gerakan dan arah serangan belum tentu sama.

Misalnya, Player sedang bergerak ke bawah layar, tetapi ingin menyerang musuh yang berada di sebelah kanan.

Jika Player selalu menghadapkan serangan sesuai arah gerakan, kendali akan terasa kurang fleksibel.

### **Rekomendasi sistem aiming**

Untuk keyboard dan mouse:

- Raycast dari posisi kursor menuju bidang gameplay.
- Ambil titik tujuan pada bidang horizontal arena.
- Hitung arah dari Player ke titik tersebut.
- Gunakan arah itu untuk menghadapkan Player dan menentukan orientasi hitbox.

Untuk gamepad:

- Idealnya gunakan Right Stick sebagai arah serangan.
- Apabila Right Stick belum diimplementasikan, gunakan arah gerakan terakhir sebagai fallback.

Dengan demikian:

```
Mouse Position
      |
      v
World Raycast
      |
      v
Ground Position
      |
      v
Attack Direction
      |
      v
Player Facing
      |
      v
Attack Execution
```

**Keputusan yang perlu dikunci:** apakah gamepad akan mendapatkan arah serangan independen melalui Right Stick. Aku merekomendasikannya untuk pengalaman isometrik yang lebih presisi, tetapi hal ini merupakan tambahan dari pemetaan kontrol saat ini.

### **Aturan arah serangan**

Aku merekomendasikan:

1. Arah serangan ditentukan ketika aksi dimulai.
2. Selama fase aktif, arah hitbox tidak berubah secara bebas.
3. Perubahan arah hanya diperbolehkan pada batas transisi yang ditentukan.
4. Serangan tidak otomatis mengunci target secara berlebihan.
5. Movement dan attack direction tidak harus selalu identik.

Tujuannya adalah menghasilkan kontrol yang presisi dan dapat diprediksi.

# **BAGIAN IV — OFFENSIVE COMBAT**

## **4.1. Tujuan Offensive Combat**

Offensive Combat adalah sistem utama yang menentukan cara Player menyerang.

Kita ingin membuat serangan yang:

- Memiliki ritme.
- Memiliki variasi.
- Memiliki timing.
- Memberikan dampak ketika mengenai musuh.
- Memiliki konsekuensi ketika meleset.
- Bisa disambungkan menjadi kombo.

Sistem ini tidak boleh hanya berisi `if attackPressed then dealDamage`.

Serangan harus menjadi sebuah rangkaian tindakan yang mempunyai fase dan aturan.

## **4.2. Basic Attack — Three-Stage Combo**

Aku merekomendasikan satu rangkaian Basic Attack yang mempunyai tiga tahap.

Ini adalah rekomendasi moveset awal, bukan mekanik yang sudah ditentukan secara eksplisit oleh GDD.

[ウォーロン（Wo Long）于吉＆邪龍と合一せしものの攻略方法 - 敵・ボス](https://images.openai.com/static-rsc-4/yqKxjVA7EFnLy6yXnhiCwgdY5jCN0Aai0YpMBQZi_2AmZA3XCDtl9DBdE__c7DrUuvQcmG1thAa7C8NuIK5iULwMUuDeGnt5kYzGwlctnqs3cyGj7AdV-NRId-VDQMt7fvnX69s2QRUy7nMDrnGYii7LT4adXPAb-iwx2KffuCw?purpose=inline)

**Attack 01**

Opener

Serangan cepat untuk memulai rangkaian.

[Ys X: Nordics Looks Slick in Loads of New Gameplay Screenshots | Push Square](https://images.openai.com/static-rsc-4/ilUkpNrrUEMHGkxfYd6VX07Ts7IHqLOCpCNvRdLbdO5p6Ug-rsVr5CbbatTbdI7EV-dv8gOjDCzDhFZ_EfLOzqdA0MDn_mHd1TOT5-gd2oDSSS5v_gDYkMPNFzw5BkxWZHkyjogTt_a6gWMWre7yrXLf8DC0EDF3yJQtE3dtZ2Q?purpose=inline)

**Attack 02**

Follow-up

Serangan lanjutan untuk menjaga momentum.

[Upcoming Eastern Fantasy MMORPG Perfect New World Entering Closed Beta, Sign-Up Available Now](https://images.openai.com/static-rsc-4/yKawLD8MPnStQFvt142GZ6BatZe1iZU-QyXRL2lIGBoUYH-zfKE557JGMLs4LwkFE-Qp_etYRp32OUFrTy52A4pPFa3MIZAiMfXAtwcKBLtzMSm5gNpQ7cB4wLY6NJ-1eckuM2wo7btg3Nal7TLgmXiKw1PrTzEv1585jtVTMVY?purpose=inline)

**Attack 03**

Finisher

Penutup dengan dampak lebih kuat.

### **Attack 01 — Opener**

Fungsi:

- Menjadi serangan pertama.
- Memulai rangkaian combo.
- Memiliki startup relatif pendek.
- Memiliki jangkauan serangan dasar.

Karakteristik:

| Parameter  | Rancangan                 |
| ---------- | ------------------------- |
| Damage     | Baseline                  |
| Startup    | Relatif cepat             |
| Active     | Singkat                   |
| Recovery   | Relatif pendek            |
| Hitbox     | Frontal                   |
| Cancel     | Mengikuti aturan serangan |
| Combo Next | Attack 02                 |

Attack 01 harus terasa ringan dan cepat.

### **Attack 02 — Follow-up**

Fungsi:

- Mempertahankan momentum serangan.
- Memberikan variasi gerakan.
- Menjadi transisi menuju finisher.

Karakteristik:

| Parameter  | Rancangan                   |
| ---------- | --------------------------- |
| Damage     | Baseline hingga meningkat   |
| Startup    | Sedikit berbeda dari opener |
| Active     | Menyesuaikan animasi        |
| Recovery   | Menyesuaikan tempo          |
| Hitbox     | Variasi frontal             |
| Cancel     | Mengikuti aturan serangan   |
| Combo Next | Attack 03                   |

Attack 02 sebaiknya memiliki animasi dan arah ayunan berbeda dari Attack 01.

Jangan sekadar mengulang animasi yang sama dengan damage lebih tinggi.

### **Attack 03 — Finisher**

Fungsi:

- Menutup satu rangkaian serangan.
- Memberikan impact lebih besar.
- Menghasilkan payoff setelah kombo berhasil dilakukan.

Karakteristik:

| Parameter  | Rancangan                             |
| ---------- | ------------------------------------- |
| Damage     | Lebih tinggi dari opener              |
| Startup    | Dapat lebih panjang                   |
| Active     | Sesuai bentuk serangan                |
| Recovery   | Lebih panjang sebagai trade-off       |
| Hitbox     | Lebih luas atau lebih kuat            |
| Cancel     | Lebih terbatas                        |
| Combo Next | Kembali ke Attack 01 setelah recovery |

Finisher bisa memiliki efek tambahan seperti knockback atau stagger, tetapi jangan otomatis memberikan semuanya.

Efek tambahan harus disesuaikan dengan peran senjata dan keseimbangan combat.

## **4.3. Alur kombo**

```
                  PLAYER
                     |
                     v
              Basic Attack Input
                     |
                     v
                ATTACK 01
                     |
              Combo Input Window
                     |
             Input berikutnya?
               /          \
             Ya            Tidak
              |              |
              v              v
          ATTACK 02        Recovery
              |
        Combo Input Window
              |
       Input berikutnya?
          /       \
        Ya         Tidak
         |           |
         v           v
     ATTACK 03     Recovery
         |
      Finisher
         |
      Recovery
         |
         v
        IDLE
```

Sistem ini harus mendukung input buffering.

### **4.4. Input Buffering**

Input Buffering adalah kemampuan sistem untuk menyimpan input yang dilakukan sedikit lebih awal daripada waktu aksi berikutnya dapat dimulai.

Contoh:

Player sedang menjalankan Attack 01.

Sebelum animasinya selesai, Player menekan tombol Attack lagi.

Tanpa input buffer:

- Sistem bisa mengabaikan input.
- Player harus menekan ulang.
- Kombo terasa tidak responsif.

Dengan input buffer:

- Input dicatat.
- Sistem menyimpan permintaan untuk satu aksi berikutnya.
- Ketika combo window dibuka, permintaan diperiksa.
- Attack 02 dimulai apabila syaratnya terpenuhi.

Rekomendasi aturan:

1. Maksimal satu input serangan disimpan.
2. Input hanya digunakan apabila aksi berikutnya legal.
3. Input yang sudah kedaluwarsa dibuang.
4. Input tidak boleh menghasilkan dua serangan sekaligus.
5. Input yang disimpan tidak boleh menembus kondisi Dead atau state yang melarang serangan.

Jangan membuat queue input yang terlalu panjang karena bisa menyebabkan Player menyerang terus tanpa kontrol pemain.

## **4.5. Combo Window**

Setiap serangan memiliki rentang waktu ketika serangan berikutnya dapat diminta.

Misalnya:

```
Attack 01

| Startup | Active | Recovery |
                    |---------|
                    Combo Window
```

Combo Window harus diatur berdasarkan animasi dan ritme permainan.

Kita tidak boleh menentukan angka final sebelum mengetahui durasi animasi yang digunakan.

Nilainya harus disimpan sebagai konfigurasi yang dapat disesuaikan.

## **4.6. Attack Phase**

Setiap serangan memiliki tiga fase fundamental.

**Attack Timeline**

**Startup**

Persiapan

Player memulai gerakan. Hitbox belum aktif.

**Active**

Serangan aktif

Hitbox aktif dan dapat menghasilkan damage.

**Recovery**

Konsekuensi

Player menyelesaikan serangan sebelum aksi berikutnya diizinkan.

Ilustrasi urutan fase, bukan proporsi timing final.

Pembagian waktu tersebut sudah sesuai dengan tanggung jawab yang dirancang untuk `AttackDefinitionSO` di Engineering Roadmap.&#x20;

Pasted text(9).txt



### **Startup**

- Hitbox tidak aktif.
- Animasi persiapan berjalan.
- Arah serangan sudah ditentukan.
- Sistem menunggu fase aktif.

### **Active**

- Hitbox diaktifkan.
- Target yang valid diperiksa.
- Damage diselesaikan melalui CombatSystem.
- Target tidak boleh menerima damage berulang dari satu aktivasi serangan.

### **Recovery**

- Hitbox dinonaktifkan.
- Player menyelesaikan animasi.
- Input berikutnya diproses sesuai aturan cancel dan combo.
- Player kembali ke state yang memungkinkan aksi berikutnya.

**Catatan:** jangan menggunakan `HitboxController` untuk menangani seluruh fase serangan sekaligus. Ia cukup bertanggung jawab mendeteksi target yang terkena.

## **4.7. Hit Detection**

Hit Detection menentukan apakah serangan mengenai target.

Pada proyekmu, `HitboxController` sudah tersedia. Kita perlu memastikan perilakunya.

Siklusnya:

```
Attack Starts
     |
     v
Startup Complete
     |
     v
Enable Hitbox
     |
     v
Detect Valid Targets
     |
     v
CombatSystem
     |
     v
CombatResolver
     |
     v
Health.TakeDamage()
     |
     v
Disable Hitbox
```

Hal yang wajib diperhatikan:

**A. Tidak ada double hit yang tidak disengaja.**

Satu aktivasi serangan tidak boleh memberikan damage berkali-kali kepada target yang sama.

Gunakan pencatatan target yang sudah terkena selama aktivasi serangan tersebut.

Pencatatan tersebut direset ketika serangan baru dimulai.

**B. Deteksi sesuai bentuk serangan.**

Serangan frontal dapat menggunakan overlap area berbentuk sesuai kebutuhan.

Serangan yang lebih lebar dapat menggunakan area deteksi yang lebih luas.

Jangan menggunakan satu radius yang sama untuk semua gerakan.

**C. Layer harus benar.**

Gunakan pemisahan layer yang sudah tersedia. Periksa collision mask agar:

- Serangan Player dapat mendeteksi Enemy.
- Serangan Player tidak mengenai Player sendiri.
- Hitbox tidak menganggap lingkungan sebagai target damage.
- Projectile diproses menggunakan jalur interaksi yang tepat.

**D. Hit detection tidak boleh bergantung pada animasi visual saja.**

Collider atau overlap detection harus mempunyai aturan gameplay yang jelas.

# **BAGIAN V — DEFENSIVE COMBAT**

## **5.1. Prinsip dasar**

Defensive Combat bukan hanya bertujuan mencegah damage.

Pertahanan harus menciptakan peluang untuk mengambil kembali kendali pertarungan.

Dokumen Combat Design menetapkan bahwa dodge, parry, dan counter harus memberikan keuntungan ketika berhasil dilakukan.&#x20;

Pasted markdown (3).md



Kita akan mempertahankan dua sistem yang sudah ada:

- Dodge.
- Parry.

Kemudian menyempurnakan interaksi keduanya dengan CombatSystem.

## **5.2. Dodge System**

[Hands-on: Hades 2 is a Herculean follow-up to one of 2020’s best games | VGC](https://images.openai.com/static-rsc-4/uQu01mN6DygA9-PJyFN86ftRi0ongFspcgzrsCJzCJzNOlrMjs6tI13l2u9wuvLN8kHcJWO9MwF8da26ykp-CfeBimLan13dInqkoufumSlFIE1qslG8WaQqHdoPAQ0HvNzuKn7vzn_VTgzxCdjkMQBT2nYyW0GiovyufQCgX50?purpose=inline)

[videogameschronicle.com](https://www.videogameschronicle.com/features/hands-on-hades-2-is-a-herculean-follow-up-to-one-of-2020s-best-games/?utm_source=chatgpt.com)

Tujuan Dodge:

- Menghindari serangan.
- Mengubah posisi.
- Keluar dari area ancaman.
- Menciptakan peluang serangan balik.

### **Alur Dodge**

```
Dodge Input
     |
     v
PlayerActionController
     |
     v
Check Dodge Eligibility
     |
     v
DodgeController
     |
     v
Determine Dodge Direction
     |
     v
Apply Movement
     |
     v
Activate I-Frame
     |
     v
End I-Frame
     |
     v
Dodge Recovery
     |
     v
Return to Available State
```

### **Peraturan yang perlu diterapkan**

1. Dodge mempunyai arah yang jelas.
2. Dodge tidak boleh berjalan ketika Player Dead.
3. Durasi gerakan terpisah dari durasi i-frame.
4. i-frame dimulai dan berakhir sesuai konfigurasi.
5. Dodge harus berinteraksi benar dengan collision lingkungan.
6. Dodge tidak boleh membuat Player menembus penghalang tanpa aturan traversal.
7. Recovery Dodge harus terdefinisi.

Poin pentingnya adalah memisahkan dua konsep:

**Dodge Movement**

Perpindahan fisik karakter.

**Invulnerability Window**

Periode ketika karakter tidak menerima damage tertentu.

Jangan menyamakan seluruh durasi dash dengan durasi invulnerability tanpa dasar desain.

### **Dodge Direction**

Prioritaskan input gerakan pemain.

| Kondisi                   | Arah Dodge                      |
| ------------------------- | ------------------------------- |
| Player menekan kanan      | Ke kanan                        |
| Player menekan kiri       | Ke kiri                         |
| Player menekan atas       | Ke atas                         |
| Player menekan bawah      | Ke bawah                        |
| Player tidak memberi arah | Menggunakan arah hadap terakhir |

Arah ini harus dihitung pada bidang horizontal arena isometrik.

### **Dodge dan Attack**

Rekomendasi awalku:

- Player dapat membatalkan serangan menuju Dodge ketika cancel window terbuka.
- Player tidak bisa membatalkan setiap frame serangan.
- Dodge mempunyai fase akhir yang perlu diselesaikan.
- Serangan berikutnya baru diterima ketika aturan transisi mengizinkannya.

Dengan cara ini, Dodge tetap responsif, tetapi bukan jalan keluar gratis dari semua komitmen serangan.

## **5.3. Perfect Parry System**

[《艾爾登法環 黑夜君臨》妖刀渡夜者「執行者」披露，可像《隻狼》一樣玩節奏招架 | 4Gamers](https://images.openai.com/static-rsc-4/054nXxGqZvVI6VSasHiaADH77xQw2i5QncH_U_L-l19ZnAYJWm9FqHUiXa3xfk7bfapnXBlwZ7ceZEHZHnbfY3Um2F4Qxv2arreVz69YVtI_k2N5p0138RBu-GGeRS9ypaI0VZif5BIZsnsAsVxQsxv8gIAWmHlBIGMuMunR778?purpose=inline)

[4gamers.com.tw](https://www.4gamers.com.tw/news/detail/72008/elden-ring-nightreign-shows-executor-character-trailer?utm_source=chatgpt.com)

Perfect Parry merupakan salah satu mekanik yang berpotensi menjadi identitas combat The Last Sasak.

Engineering Roadmap memang memisahkan `ParryController`, `ParryWindow`, `ParryResolver`, dan `IParryable`.&#x20;

Pasted text(9).txt



### **Tujuan Perfect Parry**

Player dapat menangkis serangan yang valid ketika input dilakukan pada jendela waktu yang tepat.

Keberhasilan menghasilkan:

- Pembatalan damage yang seharusnya diterima.
- Interupsi serangan lawan apabila tipe serangannya mendukung.
- Feedback visual dan audio.
- Hitstop.
- Peluang menyerang balik.

### **Parry Flow**

```
Parry Input
     |
     v
Check Parry Eligibility
     |
     v
Activate Parry Window
     |
     v
Incoming Attack
     |
     v
ParryResolver
     |
     +-------------------+
     |                   |
   Valid               Invalid
     |                   |
     v                   v
Perfect Parry        Normal Damage
     |                   |
     v                   v
Deflect /            Health Damage
Interrupt
     |
     v
Combat Feedback
     |
     v
Counter Opportunity
```

### **Parry harus memiliki tiga hasil yang jelas**

| Kondisi        | Hasil                                               |
| -------------- | --------------------------------------------------- |
| Perfect timing | Berhasil menangkis                                  |
| Terlalu awal   | Jendela parry sudah berakhir ketika serangan datang |
| Terlambat      | Serangan masuk melalui resolusi damage normal       |

Perlu diperhatikan bahwa GDD yang dirujuk Engineering Roadmap menetapkan Perfect Parry tanpa cooldown, tetapi belum menetapkan durasi window dan detail kondisi kegagalannya.

Jadi, jangan menganggap angka tertentu sebagai ketetapan resmi sebelum disetujui.&#x20;

Pasted text(9).txt



### **Rekomendasi desain Parry**

Aku menyarankan parry menjadi tindakan berbasis timing, bukan kemampuan menahan serangan tanpa batas.

Namun, karena GDD sudah menetapkan tidak adanya cooldown, kita harus mempertahankan keputusan itu.

Tidak ada cooldown bukan berarti tidak boleh ada:

- Animasi aksi.
- Jendela parry.
- Recovery.
- Aturan perpindahan state.

Tujuannya adalah menjaga mekanik tetap sesuai GDD tanpa menghilangkan kebutuhan timing dan keterampilan.

### **Perfect Parry Feedback**

Ketika berhasil:

1. Efek visual parry muncul.
2. SFX khusus dimainkan.
3. Hitstop digunakan sebagai aksentuasi.
4. Serangan musuh dihentikan apabila dapat diinterupsi.
5. Musuh mengalami respons yang sesuai.
6. Player mendapat kesempatan melanjutkan combat.

Jangan mengandalkan perubahan angka damage sebagai satu-satunya bentuk feedback.

## **5.4. Projectile Deflect**

[Screenshot of Trine 4: The Nightmare Prince (Windows, 2019) - MobyGames](https://images.openai.com/static-rsc-4/Qz2G6mo3uCt7Kd8Q8YobTVKvDUQEThoVKQwtkz1GF6eqBsFy7958Yd4d42LG2TQAYRe_V2eyxtAsjmrSJpFxkH4iQ-4mrWwTMc2IIa_2eLQSQVY65xiWQSC-7O7pbeO01ry3Ttm9vyuIWQdCBC3eiHI8qCaSrn-Rr5KZZKTKMfA?purpose=inline)

[mobygames.com](https://www.mobygames.com/game/135359/trine-4-the-nightmare-prince/screenshots/windows/1094669/?utm_source=chatgpt.com)

Karena projectile sudah tersedia dalam sistemmu, kita tidak perlu membuat mekanik projectile baru dari awal.

Yang perlu dipastikan adalah transisi kepemilikan dan arah serangan.

Ketika projectile berhasil ditangkis:

```
Enemy Projectile
      |
      v
Parry Window Check
      |
      v
IParryable
      |
      v
OnParried()
      |
      v
Update Owner / Faction
      |
      v
Update Direction
      |
      v
Projectile Returns
      |
      v
Enemy Damage Resolution
```

Syarat teknis:

- Projectile tidak lagi dianggap milik Enemy.
- Collision lama tidak langsung memicu damage berulang.
- Arah projectile diperbarui.
- Projectile tidak mengenai sumber parry secara salah.
- Target dan damage dihitung melalui CombatSystem.
- Ketika dikembalikan ke pool, seluruh state projectile direset.

Hal ini penting karena satu objek projectile dapat digunakan ulang oleh Object Pooling.

## **5.5. Counter System**

Counter tidak harus menjadi tombol tambahan pada versi pertama.

Aku merekomendasikan counter sebagai **kesempatan ofensif yang muncul setelah Player berhasil membaca dan mengatasi ancaman.**

Contohnya:

```
Enemy Begins Attack
       |
       v
Player Parry / Dodge
       |
       v
Enemy Attack Interrupted
       |
       v
Enemy Recovery / Stagger
       |
       v
Player Punish Opportunity
       |
       v
Player Attacks
```

Dengan cara ini, counter menjadi bagian dari combat loop, bukan mekanik yang berdiri sendiri tanpa hubungan dengan sistem lainnya.

Namun, perlu dibedakan:

- Parry adalah respons defensif.
- Deflect adalah interaksi terhadap serangan yang dapat dipantulkan.
- Counter adalah tindakan ofensif setelah mendapatkan kesempatan.

Pemisahan istilah ini akan membantu desain, implementasi, dan balancing.

# **BAGIAN VI — ACTION STATE DAN ANIMATION CANCEL**

## **6.1. Action State**

`PlayerActionController` harus tetap menjadi pusat validasi tindakan Player.

Animator tidak boleh menjadi sumber kebenaran utama untuk menentukan apakah Player boleh menyerang, dodge, atau parry.

Ini juga sesuai dengan Engineering Roadmap.&#x20;

Pasted text(9).txt



State yang sudah kamu miliki:

```
public enum PlayerActionState
{
    Idle,
    Attacking,
    Parrying,
    Dodging,
    Dead
}
```

Kita tidak perlu buru-buru menggantinya dengan FSM generik yang kompleks.

### **Fungsi masing-masing state**

| State     | Tanggung jawab                     |
| --------- | ---------------------------------- |
| Idle      | Menunggu input                     |
| Attacking | Mengatur seluruh fase serangan     |
| Parrying  | Mengatur parry window              |
| Dodging   | Mengatur dash dan i-frame          |
| Dead      | Mengunci seluruh tindakan gameplay |

Startup, Active, dan Recovery sebaiknya menjadi fase internal serangan, bukan langsung diperlakukan sebagai lima state gameplay baru yang terpisah.

### **Transisi utama**

```
             IDLE
          /    |    \
         /     |     \
     ATTACK   PARRY   DODGE
         \      |      /
          \     |     /
                IDLE

       Semua state → DEAD
```

Transisi dari satu aksi ke aksi lainnya harus diperiksa berdasarkan aturan.

Contoh:

| Dari   | Ke     | Ketentuan                     |
| ------ | ------ | ----------------------------- |
| Idle   | Attack | Input valid                   |
| Idle   | Dodge  | Input valid                   |
| Idle   | Parry  | Input valid                   |
| Attack | Attack | Combo window terbuka          |
| Attack | Dodge  | Cancel diizinkan              |
| Attack | Parry  | Cancel diizinkan              |
| Dodge  | Attack | Sesuai recovery dan aturan    |
| Parry  | Attack | Sesuai hasil parry dan aturan |
| Semua  | Dead   | HP mencapai nol               |

Ini adalah rekomendasi transition matrix untuk baseline, bukan semua transisi harus diimplementasikan sekaligus.

## **6.2. Animation Cancel**

Animation Cancel harus mempunyai aturan yang jelas.

Tujuan utamanya adalah menghindari rasa kaku tanpa menghilangkan konsekuensi setiap aksi.

Aku menyarankan tiga kategori:

**A. Tidak dapat dibatalkan**

Fase tertentu yang memang harus diselesaikan.

Contoh: startup serangan ketika Player sudah berkomitmen melakukan gerakan.

**B. Dapat dibatalkan menuju aksi defensif**

Fase yang mengizinkan transisi menuju Dodge atau Parry.

**C. Dapat dilanjutkan menuju kombo**

Fase yang membuka input untuk serangan selanjutnya.

Buat aturan tersebut berbasis konfigurasi, bukan kondisi `if` yang tersebar di banyak script.

Contoh konseptual:

```
Attack Definition

CanCancelIntoDodge = true
CanCancelIntoParry = true
CanContinueCombo = true
```

Sebaiknya properti ini disesuaikan per serangan.

Finisher, misalnya, tidak harus memiliki aturan cancel yang sama dengan opener.

# **BAGIAN VII — ANIMATION INTEGRATION**

## **7.1. Mengapa animasi menjadi tahap penting?**

Setiap serangan mempunyai dua sisi:

1. Apa yang dilihat pemain.
2. Apa yang benar-benar diproses oleh gameplay.

Kedua sisi tersebut harus sinkron.

Misalnya, Player terlihat sudah melakukan ayunan senjata, tetapi hitbox baru aktif terlalu lambat.

Hasilnya, serangan terasa tidak akurat.

Sebaliknya, hitbox sudah aktif sebelum senjata mendekati target. Pemain mungkin merasa serangannya mengenai musuh secara tidak masuk akal.

Ini adalah salah satu hal yang harus diperiksa pada setiap animasi.

## **7.2. Animator Controller**

Aku merekomendasikan Animator Controller yang mengelompokkan animasi berdasarkan tindakan Player.

Struktur konseptual:

```
Player Animator
│
├── Locomotion
│   ├── Idle
│   ├── Walk
│   └── Run (jika dibutuhkan)
│
├── Combat
│   ├── Attack_01
│   ├── Attack_02
│   └── Attack_03
│
├── Defense
│   ├── Dodge
│   └── Parry
│
└── Reaction
    └── Hit Reaction
```

Tidak perlu seluruh state langsung dibangun apabila animasinya belum tersedia.

Bangun struktur sesuai aset yang benar-benar digunakan.

## **7.3. Animator Parameters**

Parameter yang dapat digunakan:

| Parameter   | Jenis   | Fungsi                   |
| ----------- | ------- | ------------------------ |
| MoveSpeed   | Float   | Kecepatan gerakan        |
| IsMoving    | Bool    | Kondisi bergerak         |
| AttackIndex | Integer | Nomor rangkaian serangan |
| Attack      | Trigger | Memulai serangan         |
| Dodge       | Trigger | Memulai Dodge            |
| Parry       | Trigger | Memulai Parry            |
| Hit         | Trigger | Respons terkena serangan |
| Dead        | Bool    | Kondisi mati             |

Nama dan jumlah parameter harus disesuaikan dengan Animator Controller yang sesungguhnya.

## **7.4. Sinkronisasi animasi dengan gameplay**

Aku merekomendasikan satu sumber kebenaran timing.

Untuk proyekmu, `AttackDefinitionSO` sudah memiliki tanggung jawab menyimpan:

- Startup Duration.
- Active Duration.
- Recovery Duration.
- Cancel Information.
- Animation Reference.
- Feedback Reference.

Karena itu, jadikan data serangan sebagai sumber konfigurasi gameplay.

Animasi harus disesuaikan dengan timing tersebut.

Jangan membiarkan Animator dan `AttackController` memiliki dua jadwal aktif hitbox yang berbeda.

Animation Event boleh digunakan untuk membantu efek visual, suara, atau notifikasi presentasi. Tetapi jangan sampai dua sistem mengaktifkan hitbox secara tidak konsisten.

### **Root Motion**

Untuk baseline ini, aku merekomendasikan tetap menggunakan movement yang dikendalikan `PlayerMotor`.

Animasi menjadi representasi gerakan.

Ini akan membuat kontrol gerak, Dodge, dan tabrakan lingkungan lebih mudah dikendalikan.

Root Motion dapat dievaluasi kemudian apabila memang ada kebutuhan gerakan khusus.

# **BAGIAN VIII — GAME FEEL DAN COMBAT FEEDBACK**

## **8.1. Tujuan**

Setelah sistem dasarnya bekerja, kita perlu membuat setiap aksi terasa mempunyai dampak.

Ini adalah salah satu bagian yang akan sangat menentukan kualitas akhir The Last Sasak.

[Combat | Hades Wiki](https://images.openai.com/static-rsc-4/fSmVlssjQMjVQJNMmZwopeYW3sA5g_lcz5NCoRgih_Awn1IQlKf-Zj7dfppddawG5a5Xp_rEb1sszj4vEuG3LYrylYsitB0d4ExSHzW3TSy8e7-b20mNlfzf0E7MKpybL0HkGO7p_Kf0_smjWRhKkw7FE9oPZN88KMdb7qNsb4I?purpose=inline)

[绝区零闪避反击怎么玩_绝区零_九游手机游戏](https://images.openai.com/static-rsc-4/EX6ExpWMoxqzHLYteHjA4I6Jg6ggeTnznK07_qHo-bkhyHFFYGWjMRgcNihH6PFeWz1iI7CHZvOXRo0-MzrS1WuD7ZJJ3AZ79JMYJGcmOhRE6y0NnR0ok-3koFYn2o6LJA099QBE4l_ukss_zqpX-V6cAPnMgoHp9RbO_WaQ2CA?purpose=inline)

[【Hades II】敵との戦い方【ハデス2】 - 暇人雑記](https://images.openai.com/static-rsc-4/f_RzuB0bzq96s4nVG8R32NymhhF7pJheIa_X6GexjdwOf-uuVrXYNW1m9-T91f5SctVRfHlEG8HkZJ-EFpowXTfNl8vqPFcg0Jdw-wJvibbzFeYjymqDjm3AoaOGNStv1poPFwVsvd8TqZUF11mNs1I8LiaviAh0nL6cku7Mxjo?purpose=inline)

10

Aku membagi Game Feel menjadi tujuh bagian.

## **8.2. Hitstop**

Hitstop adalah jeda singkat pada simulasi gameplay untuk menekankan momen impact.

Contoh:

Player mengenai Vanguard dengan serangan ketiga.

Ketika hit terkonfirmasi:

- Gerakan pertarungan berhenti sebentar.
- Visual impact ditampilkan.
- SFX dimainkan.
- Setelah itu gameplay berlanjut.

Hitstop tidak boleh diterapkan secara sembarangan.

Gunakan tingkatan feedback:

| Kejadian                       | Rancangan feedback         |
| ------------------------------ | -------------------------- |
| Serangan normal mengenai musuh | Hitstop ringan             |
| Finisher mengenai target       | Hitstop lebih kuat         |
| Perfect Parry                  | Feedback khusus            |
| Enemy terkena stagger          | Respons lebih tegas        |
| Serangan meleset               | Tidak ada hit confirmation |

Nilai durasi dan time scale harus diuji secara empiris.

### **Integrasi**

Gunakan `TimeEffectController` yang sudah tersedia.

Jangan memasukkan pengaturan time scale ke `AttackController`.

AttackController hanya mengirim informasi bahwa sebuah kejadian combat terjadi.

TimeEffectController menangani efek waktunya.

## **8.3. Hit Reaction**

Ketika musuh terkena serangan, ia harus memberikan respons.

Contoh:

- Recoil.
- Flinch.
- Knockback.
- Stagger.
- Death Reaction.

Tidak semua serangan harus memberikan respons yang sama.

Respons juga perlu mempertimbangkan jenis musuh dan jenis serangan.

Vanguard dapat lebih mudah mengalami flinch dibandingkan musuh berat seperti Bulwark.

## **8.4. Camera Impulse**

Gunakan Cinemachine Impulse untuk memberi aksentuasi pada kejadian tertentu.

Contoh:

- Finisher.
- Perfect Parry.
- Stagger.
- Serangan berat musuh.

Jangan membuat kamera terus bergetar ketika Player melakukan serangan normal.

Kamera harus tetap membantu pemain membaca pertempuran.

`CameraEventManager` yang sudah tersedia dapat menangani pemicu efek tersebut tanpa memasukkan kamera ke dalam tanggung jawab PlayerMotor.

## **8.5. VFX**

VFX harus menjelaskan kejadian yang berlangsung.

Jenis efek yang perlu disiapkan:

| Event              | VFX                      |
| ------------------ | ------------------------ |
| Attack Start       | Trail atau slash         |
| Attack Hit         | Impact effect            |
| Parry Success      | Spark / parry burst      |
| Projectile Deflect | Deflection effect        |
| Dodge              | Motion trail             |
| Enemy Stagger      | Impact/stagger indicator |
| Enemy Death        | Death effect             |

Mulailah dengan sedikit efek yang mempunyai fungsi jelas.

## **8.6. SFX**

Siapkan satu kelompok audio untuk:

- Attack Swing.
- Attack Hit.
- Finisher Hit.
- Parry Success.
- Dodge.
- Projectile Deflect.
- Enemy Stagger.

Audio serangan yang meleset harus berbeda dari audio serangan yang berhasil mengenai musuh.

Ini membantu pemain mendapatkan informasi tanpa selalu mengandalkan visual.

## **8.7. Feedback priority**

Urutan prioritas implementasinya:

1. Hit confirmation.
2. Hit Reaction.
3. Parry Feedback.
4. Hitstop.
5. SFX.
6. VFX.
7. Camera Impulse.
8. Finishing polish.

Jangan mulai dengan efek kamera yang kuat apabila hitbox dan hit reaction masih bermasalah.

# **BAGIAN IX — ENEMY INTERACTION UNTUK MENGUJI PLAYER COMBAT**

## **9.1. Mengapa kita memerlukan Vanguard?**

Kita tidak perlu membuat lima archetype Enemy sekaligus.

Untuk mengembangkan Player Combat, kita membutuhkan satu musuh yang mampu menguji seluruh mekanik dasar.

Aku menyarankan menggunakan **Vanguard**.

Vanguard sebelumnya sudah dirancang sebagai basic melee enemy yang memiliki tugas mengajarkan timing serangan, dodge, dan counter.&#x20;

Pasted markdown (3).md



[Hades: Elysium - gamepressure.com](https://images.openai.com/static-rsc-4/iJi_ekf7sYsp-PieIyC9IH0ISwkS_nTTEOhzSzVDfJIs6Yd75K3hW4kVOKIosGcy_LVDHvZpzBLViyD1LZ0N2BIPDtf-AYwQS56T_Jboan8gPyYOdYAAmPTBynpixcNP3G25ZQlwNJ2rfZvIiKXX0lGZbPvxZ7tMgI1J3vDFHJk?purpose=inline)

[guides.gamepressure.com](https://guides.gamepressure.com/hades/guide.asp?ID=60343\&utm_source=chatgpt.com)

Vanguard akan menjadi partner pengujian Player.

Ia harus mempunyai beberapa pola serangan yang bisa dibaca.

## **9.2. Rancangan attack pattern Vanguard**

Dokumen Combat Design sebelumnya mengusulkan tiga pola dasar: Quick Slash, Double Slash, dan Heavy Swing.&#x20;

Pasted markdown (3).md



Kita pertahankan ketiganya.

| Attack Pattern | Fungsi desain                                | Respons yang ingin diuji |
| -------------- | -------------------------------------------- | ------------------------ |
| Quick Slash    | Serangan dasar cepat                         | Dodge                    |
| Double Slash   | Dua serangan berurutan                       | Timing dodge dan counter |
| Heavy Swing    | Serangan lebih lambat dengan area lebih luas | Parry atau dodge         |

Ketiga serangan tersebut harus mempunyai karakter visual yang berbeda.

### **Quick Slash**

[Thymesia screenshots | Hooked Gamers](https://images.openai.com/static-rsc-4/mtzF_9aJPkzZw4o6yp7m5utDMj5rQqfZt6oSaKB_IGZ7tyCps_naJRzcgVBvljKfEwhGIWzdZGuAOjs75zN7o4jgxE2nk6AaTidGD-cUMLmjO8uitGE0IKn8lXRXByv1LB8Wx-fa_Z4VBuUPfJngPXyvzW6k0qJnwbQsgHWf_wc?purpose=inline)

[hookedgamers.com](https://www.hookedgamers.com/pc/thymesia/screenshots.html?utm_source=chatgpt.com)

Tujuan:

- Menguji kemampuan pemain membaca ancaman.
- Menguji Dodge.
- Menguji apakah Player dapat kembali menyerang setelah menghindar.

### **Double Slash**

[9/1に発売する台湾のOverBorder Studioによる注目の3D ARPG 《Thymesia》のゲームシステムを公開！ | Game Source Entertainment](https://images.openai.com/static-rsc-4/ieSQH5rxh55v9y4495Vaxs044SCK2nj6xX5J8nwf8A_hGDoadXXjn_KnNNUNi9ulFA4W8iFGuYLyddzCkNKEbeTC-jxuqOhKo1yh80aEfgIrTJ2jHXyMsFp20K-t4hPeR0GRPu1YTl8oYGtZKkXfgsNhtIaY90-eqhSgHbQOwJc?purpose=inline)

[atpress.ne.jp](https://www.atpress.ne.jp/news/314257?utm_source=chatgpt.com)

Tujuan:

- Menguji apakah Player memahami bahwa satu ancaman dapat terdiri dari beberapa serangan.
- Menguji timing respons kedua.
- Menguji kemampuan mencari celah setelah rangkaian selesai.

### **Heavy Swing**

[Perfect New World - Images & Screenshots | GameGrin](https://images.openai.com/static-rsc-4/IR1eUf6vPC4AgrTZnltsydWuZQQeGhcy5QUCQLvXLpAVQuLJ-65_JOwGV5IU49POQNSJ3myqN7-iHaqm5rVHOcRvznkyZBdWQ7Js6COCXAyscMrv2WufEyYzQ313u0iTNy3bQgo6GJDpVpdo_63gT1l_DWnQoEZZLcysEom3Ug0?purpose=inline)

[gamegrin.com](https://www.gamegrin.com/directory/game/perfect-new-world/images?utm_source=chatgpt.com)

Tujuan:

- Menguji respons parry.
- Menguji pengenalan telegraph.
- Menguji konsekuensi ketika Player salah menentukan timing.
- Menguji kesempatan menyerang setelah musuh recovery.

## **9.3. Enemy Attack Timeline**

Setiap serangan Vanguard mengikuti enam tahap:

```
Approach
   |
   v
Telegraph
   |
   v
Startup
   |
   v
Execution
   |
   v
Recovery
   |
   v
Reposition
   |
   v
Next Decision
```

Pembagian ini sudah menjadi bagian rancangan Combat Design sebelumnya.&#x20;

Pasted markdown (3).md



Contohnya, ketika Vanguard melakukan Heavy Swing:

1. Musuh mengambil jarak serang.
2. Musuh memperlihatkan tanda bahwa serangan akan dilakukan.
3. Animasi persiapan berlangsung.
4. Serangan aktif.
5. Musuh menyelesaikan gerakan dan memasuki recovery.
6. Player mendapatkan kesempatan menyerang.

Jangan langsung membuat Vanguard mempunyai terlalu banyak gerakan.

Tiga pattern tersebut sudah cukup untuk menguji sistem Player Combat tahap pertama.

## **9.4. Hubungan Enemy dan Player**

Kita membutuhkan hubungan respons yang konsisten.

| Kejadian Enemy | Respons Player               | Hasil yang diharapkan                   |
| -------------- | ---------------------------- | --------------------------------------- |
| Quick Slash    | Dodge                        | Tidak terkena damage saat i-frame valid |
| Double Slash   | Dodge pada timing yang tepat | Mampu mengatasi kedua ancaman           |
| Heavy Swing    | Perfect Parry                | Serangan tertangkis apabila parry valid |
| Heavy Swing    | Dodge                        | Keluar dari area serangan               |
| Enemy Recovery | Basic Attack                 | Mendapatkan kesempatan menyerang        |
| Enemy Stagger  | Combo                        | Kesempatan punish lebih besar           |

Inilah bentuk awal Combat Interaction yang harus kita capai.

# **BAGIAN X — IMPLEMENTATION ARCHITECTURE**

Sekarang kita masuk ke bagian engineering.

Tujuannya adalah menerjemahkan rancangan gameplay ke sistem Unity yang sudah ada.

Prinsip utama tetap sama:

**Object sudah tersedia di scene. Script tidak membuat Player, arena, kamera, HUD, atau Enemy baru untuk merakit level.**

Gunakan komponen dan referensi yang telah disiapkan.

## **10.1. Pemetaan seluruh script**

Berikut pembagian tanggung jawab yang aku rekomendasikan.

| Script                      | Tanggung jawab setelah pengembangan    |
| --------------------------- | -------------------------------------- |
| `PlayerInputReader`         | Membaca input dan menghasilkan command |
| `PlayerMotor`               | Mengelola pergerakan fisik             |
| `PlayerFacingController`    | Mengatur orientasi Player              |
| `PlayerActionController`    | Memvalidasi dan mengelola tindakan     |
| `AttackController`          | Mengatur serangan dan rangkaian kombo  |
| `HitboxController`          | Mendeteksi target selama attack active |
| `DodgeController`           | Mengelola pergerakan Dodge             |
| `ParryController`           | Mengelola tindakan Parry               |
| `ParryWindow`               | Mengatur jendela validasi Parry        |
| `InvulnerabilityHandler`    | Mengatur periode i-frame               |
| `Health`                    | Menangani HP dan kematian              |
| `CombatSystem`              | Menjadi pintu masuk resolusi interaksi |
| `CombatResolver`            | Menentukan hasil damage                |
| `ParryResolver`             | Menentukan hasil Parry                 |
| `Projectile`                | Mengelola perilaku projectile          |
| `PlayerAnimationController` | Mengendalikan presentasi animasi       |
| `TimeEffectController`      | Menangani hitstop                      |
| `CameraEventManager`        | Menangani event kamera                 |
| `AttackDefinitionSO`        | Menyimpan konfigurasi setiap serangan  |

Ini sengaja mempertahankan arsitektur yang sudah dibuat. Tidak ada alasan membuat `MegaPlayerCombatController` yang menangani seluruh fitur sekaligus.

## **10.2. Alur komunikasi serangan**

```
PlayerInputReader
       |
       | Attack Input
       v
PlayerActionController
       |
       | Check Action Rules
       v
AttackController
       |
       | Load AttackDefinitionSO
       v
Attack Startup
       |
       | Active
       v
HitboxController
       |
       | Target Detected
       v
CombatSystem
       |
       v
CombatResolver
       |
       v
IDamageable.TakeDamage()
       |
       v
Health
       |
       +-------------------+
       |                   |
       v                   v
 Health Changed         Entity Died
       |                   |
       v                   v
 Enemy Reaction        Reward / Encounter
```

`CombatSystem` tidak perlu mengetahui seluruh detail animasi Player.

`Health` tidak perlu mengetahui bagaimana serangan dilakukan.

`PlayerActionController` tidak perlu menghitung reward.

Pemisahan ini harus tetap dipertahankan.

Engineering Roadmap juga menetapkan pemisahan antara komponen yang mengatur serangan, deteksi hitbox, resolusi combat, dan data serangan.&#x20;

Pasted text(9).txt



## **10.3. AttackDefinitionSO — Struktur data yang diperlukan**

Menurutku, bagian ini perlu mendapatkan perhatian khusus.

Kamu sudah menggunakan `AttackDefinitionSO`. Kita harus mengembangkannya menjadi sumber data serangan, bukan membuat setiap jenis serangan mempunyai kode yang berbeda.

Berikut rancangan field yang direkomendasikan.

| Kelompok  | Field konseptual     | Fungsi                                |
| --------- | -------------------- | ------------------------------------- |
| Identity  | Attack ID            | Identitas serangan                    |
| Identity  | Attack Name          | Nama serangan                         |
| Damage    | Damage Value         | Damage dasar                          |
| Damage    | Stagger Value        | Nilai stagger jika didukung           |
| Timing    | Startup Duration     | Waktu persiapan                       |
| Timing    | Active Duration      | Waktu hitbox aktif                    |
| Timing    | Recovery Duration    | Waktu pemulihan                       |
| Hitbox    | Hitbox Shape         | Bentuk deteksi                        |
| Hitbox    | Hitbox Offset        | Posisi hitbox relatif terhadap Player |
| Hitbox    | Hitbox Size / Radius | Dimensi serangan                      |
| Combo     | Next Attack          | Serangan berikutnya                   |
| Combo     | Combo Window         | Rentang input kombo                   |
| Cancel    | Can Cancel To Dodge  | Aturan cancel                         |
| Cancel    | Can Cancel To Parry  | Aturan cancel                         |
| Animation | Animation Reference  | Animasi yang digunakan                |
| Feedback  | VFX Reference        | Efek serangan                         |
| Feedback  | SFX Reference        | Audio serangan                        |

Jangan merasa semua field harus dibuat dalam satu waktu.

Bangun field berdasarkan kebutuhan implementasi.

### **Contoh konfigurasi konseptual**

```
AttackDefinitionSO
│
├── Attack_01
│   ├── Damage
│   ├── Startup
│   ├── Active
│   ├── Recovery
│   ├── Hitbox
│   ├── ComboWindow
│   ├── NextAttack = Attack_02
│   └── Animation
│
├── Attack_02
│   ├── Damage
│   ├── Startup
│   ├── Active
│   ├── Recovery
│   ├── Hitbox
│   ├── ComboWindow
│   ├── NextAttack = Attack_03
│   └── Animation
│
└── Attack_03
    ├── Damage
    ├── Startup
    ├── Active
    ├── Recovery
    ├── Hitbox
    ├── ComboWindow
    ├── NextAttack = None
    └── Animation
```

Data serangan harus dapat diatur melalui Inspector.

Jangan menyimpan kondisi runtime seperti target yang sudah terkena atau waktu aktif serangan di dalam ScriptableObject.

ScriptableObject merupakan data konfigurasi. Kondisi setiap serangan yang sedang berlangsung harus berada pada komponen runtime masing-masing Player.

Pemisahan konfigurasi data dan state runtime juga telah direkomendasikan dalam Engineering Roadmap.&#x20;

Pasted text(9).txt



## **10.4. Data combo**

Untuk tahap pertama, kamu tidak perlu membuat sistem combo graph yang kompleks.

Cukup gunakan referensi `NextAttack` pada masing-masing definisi serangan.

Dengan demikian:

- Attack 01 menunjuk ke Attack 02.
- Attack 02 menunjuk ke Attack 03.
- Attack 03 tidak mempunyai serangan lanjutan.

Ke depannya, sistem dapat diperluas menjadi:

```
          Attack 01
          /       \
         v         v
     Attack 02   Attack X
         |
         v
     Attack 03
```

Namun, branching combo belum perlu dibuat sekarang.

Kita ingin mendapatkan satu rangkaian kombo yang matang terlebih dahulu.

## **10.5. Pemisahan gameplay dan presentation**

Bagian ini sangat penting.

Aku merekomendasikan pemisahan sebagai berikut:

| Kejadian gameplay     | Sistem yang menangani      |
| --------------------- | -------------------------- |
| Player menyerang      | AttackController           |
| Hitbox mengenai musuh | HitboxController           |
| Damage dihitung       | CombatResolver             |
| Musuh menerima damage | Health                     |
| Musuh bereaksi        | Enemy Animation / Reaction |
| Efek impact muncul    | Combat Feedback            |
| Hitstop berjalan      | TimeEffectController       |
| Kamera bereaksi       | CameraEventManager         |

Dengan desain ini, kita bisa mengganti animasi atau efek visual tanpa harus merombak perhitungan damage.

Begitu juga sebaliknya.

### **Catatan event**

Gunakan C# Events untuk hubungan lokal.

Gunakan GameEvents atau event global untuk informasi yang memang perlu diketahui sistem lain.

Misalnya:

- `Health.OnHealthChanged` untuk HUD.
- `Health.OnDied` untuk komponen yang memerlukan informasi kematian.
- Event global untuk `OnPlayerDied` atau perubahan GameState.

Jangan membuat setiap frame serangan menjadi event global.

# **BAGIAN XI — STRUKTUR PLAYER COMBAT YANG AKAN DIUJI**

## **11.1. CombatTest sebagai laboratory scene**

Gunakan scene yang sudah tersedia:

`Assets/_Project/Scenes/CombatTest.unity`

Scene ini menjadi tempat utama pengembangan dan validasi.

Tidak perlu membuat scene baru.

Pertahankan:

- Player.
- Cinemachine Camera.
- ArenaFloor.
- Obstacle.
- Example_Melee.
- Example_Ranged.
- Example_Room.
- HUD.
- Game Over.

Untuk tahap awal, nonaktifkan atau keluarkan sementara Enemy yang belum dibutuhkan dari encounter test.

Kita ingin satu Vanguard menjadi target utama.

## **11.2. Pengaturan pengujian**

Buat beberapa skenario di scene yang sama.

| Scenario | Objek atau situasi          | Tujuan                                    |
| -------- | --------------------------- | ----------------------------------------- |
| Test 01  | Player tanpa Enemy          | Menguji movement dan animasi              |
| Test 02  | Satu Vanguard               | Menguji Basic Attack                      |
| Test 03  | Vanguard dengan Quick Slash | Menguji Dodge                             |
| Test 04  | Vanguard dengan Heavy Swing | Menguji Parry                             |
| Test 05  | Ranged Enemy                | Menguji Projectile Deflect                |
| Test 06  | Arena dengan obstacle       | Menguji posisi serangan dan Dodge         |
| Test 07  | Beberapa target             | Menguji hit detection dan target registry |

Masing-masing skenario harus dapat diulang dengan kondisi yang sama.

Ini akan memudahkan proses debugging dan balancing.

# **BAGIAN XII — ROADMAP PENGERJAAN DETAIL**

Bagian ini merupakan urutan kerja yang benar-benar aku rekomendasikan untuk kamu ikuti.

Kita tidak mengerjakan semua hal sekaligus.

Satu tahap harus mencapai hasil yang dapat diuji sebelum dilanjutkan ke tahap berikutnya.

# **Player Combat Development Roadmap**

Delapan tahap produksi dari audit hingga baseline combat selesai.

1. **Stage 0 — Combat Audit & Design Lock**

   Evaluasi sistem yang sudah ada dan kunci aturan gameplay.

   Dilakukan pertama
2. **Stage 1 — Offensive Foundation**

   Satu Basic Attack, hit detection, damage, dan arah serangan.
3. **Stage 2 — Combo System**

   Three-stage combo, input buffer, combo window, dan recovery.
4. **Stage 3 — Defensive Integration**

   Dodge, i-frame, Parry, Perfect Parry, dan Deflect.
5. **Stage 4 — Action Rules & Animation**

   Action state, animation transition, dan cancel rules.
6. **Stage 5 — Combat Feel**

   Hitstop, hit reaction, VFX, SFX, dan camera impulse.
7. **Stage 6 — Vanguard Integration**

   Tiga pola serangan musuh untuk menguji seluruh respons Player.
8. **Stage 7 — Validation & Baseline Freeze**

   Regression test, playtest, tuning, dan dokumentasi hasil.

## **STAGE 0 — Combat Audit & Design Lock**

Harus dilakukan pertama

### **Tujuan**

Mengetahui apakah semua sistem yang ada sudah siap dikembangkan.

Jangan langsung menulis script baru.

### **Pekerjaan**

**0.1. Periksa PlayerActionController**

Pastikan:

- Bisa membaca input.
- Memeriksa state Player.
- Menolak aksi ketika Dead.
- Memiliki aturan transisi.
- Tidak memiliki logika damage yang seharusnya berada di CombatSystem.

**0.2. Periksa AttackController**

Pastikan:

- Dapat menjalankan satu serangan.
- Menjalankan fase startup, active, recovery.
- Dapat menghentikan serangan ketika dibatalkan.
- Tidak menimbulkan serangan ganda.
- Dapat menggunakan AttackDefinitionSO.

**0.3. Periksa HitboxController**

Pastikan:

- Hitbox hanya aktif pada fase yang tepat.
- Target tidak menerima damage berulang dari satu aktivasi.
- Filter layer benar.
- Hitbox selalu dinonaktifkan ketika serangan selesai atau dibatalkan.

**0.4. Periksa Defensive System**

Pastikan:

- Dodge berjalan.
- i-frame berfungsi.
- ParryWindow berjalan.
- ParryResolver memeriksa serangan yang valid.
- Deflect dapat mengubah interaksi projectile.

**0.5. Periksa PlayerAnimationController**

Periksa apakah animasi yang diperlukan sudah tersedia.

Apabila belum, gunakan animasi sementara sebagai placeholder.

Jangan menganggap sistem animasi sudah matang hanya karena script-nya sudah ada.

**Hasil Stage 0:**

Dokumen `Combat Audit` yang mencatat fitur yang sudah bekerja, yang belum bekerja, dan yang perlu diperbaiki.

Jangan menyalin seluruh script atau membuat ulang sistem hanya untuk menyelesaikan audit.

## **STAGE 1 — Offensive Foundation**

### **Tujuan**

Membuat satu serangan Player yang benar-benar bekerja dengan benar.

Belum perlu tiga kombo.

Belum perlu variasi serangan.

Fokus pada satu Basic Attack.

### **Pekerjaan**

1. Tentukan arah serangan.
2. Hubungkan input Attack ke AttackController.
3. Gunakan AttackDefinitionSO.
4. Implementasikan startup.
5. Aktifkan hitbox pada fase active.
6. Matikan hitbox setelah active selesai.
7. Hubungkan hitbox ke CombatSystem.
8. Periksa DamageResult.
9. Pastikan musuh menerima damage tepat satu kali per aktivasi.
10. Pastikan serangan meleset tidak memberikan damage.

### **Hasil yang diharapkan**

Player bisa menyerang satu Vanguard.

Serangan dapat mengenai musuh dari arah yang benar.

Damage konsisten.

Tidak ada hitbox yang tertinggal aktif.

### **Acceptance Criteria**

- Tombol Attack bekerja.
- Player menghadap arah serangan.
- Startup berfungsi.
- Hitbox aktif sesuai fase.
- Damage diterapkan dengan benar.
- Target tidak terkena double hit.
- Serangan meleset tidak menyebabkan damage.
- Tidak ada error saat spam input.
- Tidak ada error ketika target mati di tengah interaksi.

## **STAGE 2 — Combo System**

Prioritas utama setelah Stage 1

### **Tujuan**

Membuat rangkaian serangan yang terasa mengalir.

### **Pekerjaan**

**2.1. Siapkan tiga AttackDefinitionSO**

Buat:

```
Attack_01
Attack_02
Attack_03
```

Semua menggunakan sistem konfigurasi yang sama.

**2.2. Implementasikan Combo Chain**

Hubungkan:

```
Attack_01 → Attack_02 → Attack_03
```

**2.3. Implementasikan Combo Window**

Atur kapan input berikutnya dapat diterima.

**2.4. Implementasikan Input Buffer**

Simpan satu input serangan yang valid.

**2.5. Implementasikan Recovery**

Pastikan Player tidak dapat melakukan aksi yang tidak diizinkan.

**2.6. Periksa transisi animasi**

Setiap serangan harus mempunyai identitas visual berbeda.

### **Acceptance Criteria**

- Attack 01 dapat dilanjutkan ke Attack 02.
- Attack 02 dapat dilanjutkan ke Attack 03.
- Input Buffer bekerja.
- Input tidak tersimpan tanpa batas.
- Combo tidak melompat ke serangan yang tidak valid.
- Recovery berfungsi.
- Combo kembali ke Attack 01 setelah selesai.
- Kombo bekerja saat bergerak dan saat diam.

**Hasil Stage 2:** Player sudah mempunyai satu offensive moveset yang lengkap untuk versi awal.

## **STAGE 3 — Defensive Integration**

### **Tujuan**

Menyatukan Offensive dan Defensive Combat.

### **Pekerjaan**

**3.1. Dodge Integration**

Periksa interaksi Dodge dengan Attack.

Tentukan fase yang memungkinkan cancel.

**3.2. Invulnerability**

Periksa apakah i-frame dimulai dan berhenti sesuai konfigurasi.

**3.3. Parry Integration**

Pastikan Player dapat melakukan parry ketika kondisi state mengizinkan.

**3.4. Perfect Parry**

Uji timing yang berhasil, terlalu awal, dan terlambat.

**3.5. Projectile Deflect**

Uji perpindahan kepemilikan projectile.

**3.6. Combat Response**

Ketika berhasil melakukan parry, pastikan Player mendapat feedback dan musuh mendapatkan respons yang sesuai.

### **Acceptance Criteria**

- Dodge tidak merusak state Attack.
- i-frame berlaku pada waktu yang benar.
- i-frame tidak berlaku setelah Dodge selesai.
- ParryWindow tidak tertinggal aktif.
- Perfect Parry membatalkan interaksi damage yang valid.
- Serangan yang tidak dapat diparry tetap diproses normal.
- Projectile dapat di-deflect.
- Tidak ada interaksi collision berulang yang salah.
- Player dapat melanjutkan combat setelah Dodge dan Parry.

## **STAGE 4 — Action Rules & Animation**

### **Tujuan**

Membuat seluruh tindakan terasa konsisten.

### **Pekerjaan**

1. Finalisasi state Player.
2. Tentukan transition matrix.
3. Finalisasi cancel rules.
4. Hubungkan setiap action dengan animasi.
5. Selaraskan timing serangan.
6. Perbaiki transisi locomotion dan combat.
7. Pastikan input tetap terkendali ketika animasi sedang dimainkan.

### **Acceptance Criteria**

- Animasi dan fase gameplay sinkron.
- Hitbox tidak aktif terlalu awal atau terlambat.
- Dodge dapat dibatalkan sesuai aturan.
- Parry dapat dibatalkan sesuai aturan.
- Combo memiliki transisi yang natural.
- Player tidak dapat melakukan aksi yang bertentangan.
- Kematian dapat menghentikan seluruh tindakan.

## **STAGE 5 — Combat Feel**

### **Tujuan**

Memberikan dampak visual, audio, dan respons terhadap setiap tindakan.

### **Pekerjaan**

1. Hitstop untuk serangan normal.
2. Hitstop khusus Perfect Parry.
3. Enemy Hit Reaction.
4. SFX serangan.
5. SFX Parry.
6. VFX impact.
7. VFX Dodge.
8. Camera Impulse.
9. Evaluasi kejelasan visual.

### **Acceptance Criteria**

- Serangan yang mengenai musuh terasa berbeda dari serangan yang meleset.
- Parry mempunyai feedback tersendiri.
- Dodge terbaca dengan jelas.
- Hitstop tidak membuat gameplay macet.
- Kamera tidak mengganggu pembacaan arena.
- VFX tidak menutupi serangan musuh atau posisi Player.

## **STAGE 6 — Vanguard Integration**

### **Tujuan**

Memastikan seluruh sistem Player bekerja dalam pertarungan nyata.

### **Pekerjaan**

1. Aktifkan satu Vanguard.
2. Implementasikan Quick Slash.
3. Implementasikan Double Slash.
4. Implementasikan Heavy Swing.
5. Pastikan ketiga serangan mempunyai telegraph.
6. Uji Dodge dan Parry.
7. Uji kesempatan counter.
8. Uji pertarungan berulang kali.

### **Acceptance Criteria**

- Vanguard dapat mendekati Player.
- Vanguard dapat melakukan tiga pattern serangan.
- Serangannya dapat dibaca.
- Player dapat menghindar.
- Player dapat menangkis serangan yang valid.
- Musuh memberikan recovery opportunity.
- Player dapat menyerang balik.
- Pertarungan tidak terlalu mudah maupun terlalu sulit.
- Kedua pihak dapat menerima damage dan mati dengan benar.

## **STAGE 7 — Validation & Baseline Freeze**

### **Tujuan**

Mengunci baseline Player Combat yang dapat dijadikan fondasi pengembangan selanjutnya.

### **Pekerjaan**

- Pengujian seluruh combo.
- Pengujian semua transisi.
- Pengujian parry.
- Pengujian projectile.
- Pengujian Dodge.
- Pengujian obstacle.
- Pengujian death dan retry.
- Pengujian object pooling.
- Perbaikan bug.
- Playtest.
- Dokumentasi konfigurasi final.

Pada tahap ini, jangan memasukkan fitur baru yang tidak berhubungan langsung dengan kualitas baseline combat.

# **BAGIAN XIII — TESTING DAN VALIDASI**

Tahap ini penting karena combat tidak cukup diuji berdasarkan keberhasilan kode.

Kita juga harus menguji apakah pemain benar-benar mendapatkan pengalaman yang diinginkan.

## **13.1. Functional Testing**

Berikut matriks pengujian yang perlu dibuat.

| ID  | Pengujian          | Hasil yang diharapkan                 |
| --- | ------------------ | ------------------------------------- |
| F01 | Basic Attack       | Serangan berjalan                     |
| F02 | Attack Hit         | Damage diterapkan                     |
| F03 | Attack Miss        | Tidak ada damage                      |
| F04 | Combo 01 → 02      | Transisi valid                        |
| F05 | Combo 02 → 03      | Transisi valid                        |
| F06 | Combo Input Buffer | Input tersimpan dengan benar          |
| F07 | Combo Recovery     | Aksi berikutnya mengikuti aturan      |
| F08 | Dodge              | Karakter berpindah                    |
| F09 | Dodge I-frame      | Damage ditolak pada window valid      |
| F10 | Parry Success      | Serangan valid tertangkis             |
| F11 | Parry Failure      | Damage diproses normal                |
| F12 | Projectile Deflect | Projectile berubah kepemilikan        |
| F13 | Animation Cancel   | Hanya transisi yang diizinkan terjadi |
| F14 | Enemy Death        | Kematian terdeteksi satu kali         |
| F15 | Player Death       | Game Over aktif                       |

## **13.2. Edge Case Testing**

Jangan hanya menguji kondisi normal.

Bug combat sering muncul ketika beberapa input dan kejadian terjadi hampir bersamaan.

Berikut kasus yang wajib diperiksa:

| Kondisi                                          | Hal yang harus dipastikan                         |
| ------------------------------------------------ | ------------------------------------------------- |
| Attack ditekan berulang kali                     | Tidak membuat serangan bertumpuk secara ilegal    |
| Player mati ketika sedang Attack                 | Hitbox dan aksi berhenti sesuai aturan            |
| Enemy mati ketika terkena combo                  | Tidak ada damage setelah target mati              |
| Player melakukan Dodge ketika hitbox aktif       | Cancel dan damage tetap konsisten                 |
| Parry terjadi saat Projectile mendekat           | Tidak ada double collision                        |
| Player terkena damage ketika animasi berlangsung | Action state tetap konsisten                      |
| Scene berpindah                                  | Tidak ada event subscription yang tertinggal      |
| Projectile kembali ke pool                       | State kepemilikan dan collision direset           |
| Player menabrak obstacle ketika Dodge            | Tidak menembus lingkungan secara tidak semestinya |
| Dua musuh berdekatan                             | Hitbox memproses target dengan benar              |

Perhatikan juga kondisi input ketika Game Over aktif.

Tidak boleh ada serangan atau aksi gameplay yang tetap bisa dilakukan setelah Player mati.

## **13.3. Playtesting: Mengukur Kualitas Combat**

Functional testing menjawab:

*Apakah sistem bekerja?*

Playtesting menjawab:

*Apakah sistem ini menyenangkan?*

Aku merekomendasikan setiap sesi pengujian mempunyai beberapa pertanyaan.

| Aspek          | Pertanyaan evaluasi                              |
| -------------- | ------------------------------------------------ |
| Responsiveness | Apakah karakter merespons input dengan baik?     |
| Control        | Apakah arah serangan mudah dikendalikan?         |
| Combo          | Apakah rangkaian serangan terasa natural?        |
| Impact         | Apakah serangan yang mengenai musuh terasa kuat? |
| Dodge          | Apakah perpindahan terasa dapat dikendalikan?    |
| Parry          | Apakah timing mudah dipahami?                    |
| Readability    | Apakah serangan musuh dapat dibaca?              |
| Consistency    | Apakah tindakan memberikan hasil yang konsisten? |

Kamu juga bisa meminta anggota tim mencoba combat tanpa penjelasan terlebih dahulu.

Amati:

- Apakah mereka memahami cara menyerang?
- Apakah mereka mengerti kapan melakukan Dodge?
- Apakah mereka memahami Parry?
- Apakah mereka dapat menyambungkan kombo?
- Apakah mereka merasa ada input yang tidak terdaftar?

Jangan langsung menjelaskan solusinya ketika pengujian berlangsung.

Perhatikan kesulitan yang muncul secara alami. Itu bisa menjadi petunjuk adanya masalah desain atau kontrol.

## **13.4. Gunakan Unity Test Framework**

Tidak semua pengujian harus dilakukan secara manual.

Beberapa bagian logika dapat diuji secara otomatis menggunakan Unity Test Framework.

Contoh yang layak dijadikan unit atau integration test:

- Damage tidak diterapkan dua kali pada satu target dalam satu aktivasi serangan.
- Health tidak menjadi negatif apabila damage melebihi HP tersisa.
- OnDied hanya dipancarkan sekali.
- Parry menolak serangan yang tidak memenuhi persyaratan.
- Input buffer tidak mengeluarkan aksi ilegal.
- Projectile tidak kembali menyerang pemilik lama setelah deflect.
- Pooled projectile dibersihkan ketika diambil kembali.

Sementara itu, kualitas animasi, kenyamanan kontrol, dan rasa impact tetap membutuhkan pengujian langsung.

Engineering Roadmap juga merekomendasikan Unity Test Framework serta profiling untuk memastikan kualitas hasil akhir.&#x20;

Pasted text(9).txt



# **BAGIAN XIV — COMBAT BALANCING**

## **14.1. Jangan melakukan balancing hanya dari damage**

Sering terjadi ketika developer merasa combat kurang menarik, solusi pertamanya adalah menaikkan damage.

Padahal masalahnya bisa berasal dari:

- Startup terlalu panjang.
- Recovery terlalu lama.
- Jangkauan hitbox tidak sesuai.
- Animasi tidak sinkron.
- Pergerakan terlalu terbatas.
- Enemy terlalu agresif.
- Input buffer tidak bekerja.

Karena itu, balancing perlu dilakukan berdasarkan variabel yang tepat.

## **14.2. Parameter yang perlu dapat diatur**

### **Player Movement**

| Parameter                  | Fungsi                                |
| -------------------------- | ------------------------------------- |
| Movement Speed             | Kecepatan normal                      |
| Attack Movement Multiplier | Pengaruh serangan terhadap pergerakan |
| Dodge Speed                | Kecepatan Dodge                       |
| Dodge Distance / Duration  | Besarnya perpindahan                  |
| Rotation Speed             | Kecepatan perubahan arah              |

### **Offensive**

| Parameter             | Fungsi                   |
| --------------------- | ------------------------ |
| Base Damage           | Damage dasar             |
| Startup               | Persiapan serangan       |
| Active                | Waktu serangan aktif     |
| Recovery              | Pemulihan                |
| Combo Window          | Jendela input berikutnya |
| Input Buffer Duration | Lama penyimpanan input   |
| Hitbox Size           | Jangkauan deteksi        |
| Hitbox Offset         | Posisi deteksi           |

### **Defensive**

| Parameter        | Fungsi                    |
| ---------------- | ------------------------- |
| Dodge Duration   | Durasi gerakan            |
| I-Frame Duration | Durasi invulnerability    |
| I-Frame Start    | Awal periode perlindungan |
| Parry Window     | Durasi validasi parry     |
| Parry Recovery   | Pemulihan setelah parry   |
| Deflect Behavior | Respons projectile        |

### **Feedback**

| Parameter      | Fungsi                      |
| -------------- | --------------------------- |
| Normal Hitstop | Penekanan impact normal     |
| Heavy Hitstop  | Penekanan impact lebih kuat |
| Parry Hitstop  | Penekanan perfect parry     |
| Camera Impulse | Intensitas respons kamera   |
| VFX Scale      | Ukuran efek                 |

Nilai final belum bisa dipastikan hanya berdasarkan dokumen karena animasi, kecepatan permainan aktual, dan hasil playtest belum diperiksa.

## **14.3. Prinsip balancing**

Aku menyarankan lima prinsip:

**1. Respons lebih penting daripada damage besar.**

Pastikan serangan terasa tepat sebelum menaikkan damage.

**2. Komitmen tetap harus ada.**

Serangan kuat harus mempunyai konsekuensi yang sepadan.

**3. Defense harus memberikan keuntungan, bukan sekadar menghindari kerugian.**

Perfect Parry dan Dodge harus membuka peluang.

**4. Musuh harus memberikan ancaman yang dapat dipahami.**

Kesulitan tidak boleh hanya berasal dari serangan yang tidak terbaca.

**5. Semua angka harus bisa diuji dan diubah.**

Hindari magic number yang tersebar pada script.

# **BAGIAN XV — ACCEPTANCE CRITERIA AKHIR**

Inilah kriteria yang menurutku paling penting untuk menyatakan Player Combat v1.0 selesai.

Tidak cukup hanya seluruh script berhasil di-compile.

Seluruh kriteria berikut harus terpenuhi.

## **15.1. Offensive Combat**

- Player mempunyai satu moveset utama.
- Basic Attack bekerja.
- Three-stage combo dapat digunakan.
- Setiap serangan mempunyai timing tersendiri.
- Input Buffer bekerja.
- Combo Window bekerja.
- Hitbox sesuai serangan.
- Tidak ada double damage yang tidak disengaja.
- Recovery bekerja.
- Arah serangan dapat dikendalikan.

## **15.2. Defensive Combat**

- Dodge bekerja.
- Arah Dodge benar.
- i-frame bekerja.
- i-frame berakhir sesuai konfigurasi.
- Parry bekerja.
- Perfect Parry bekerja.
- Parry failure diproses normal.
- Projectile dapat di-deflect.
- Cancel rule bekerja.

## **15.3. Animation & Game Feel**

- Animator terhubung dengan action state.
- Animasi serangan berbeda.
- Animasi Dodge bekerja.
- Animasi Parry bekerja.
- Hit Reaction bekerja.
- Hitstop bekerja.
- SFX tersedia.
- VFX tersedia.
- Camera Impulse bekerja.
- Tidak ada feedback yang mengganggu pembacaan combat.

## **15.4. Integration**

- CombatSystem bekerja.
- Health menerima damage dengan benar.
- Death bekerja.
- Reward tidak diberikan dua kali.
- Enemy dapat menerima serangan.
- Player dapat menerima serangan.
- Room Cleared bekerja setelah encounter selesai.
- Game Over bekerja.
- Tidak ada Missing Script.
- Tidak ada error kritis saat Play Mode.

# **BAGIAN XVI — SETELAH PLAYER COMBAT v1.0 SELESAI**

Setelah baseline ini stabil, kita masuk ke pengembangan combat yang lebih luas.

Aku tetap mempertahankan lima fase dari Combat Design Roadmap yang sudah kamu buat.

## **The Last Sasak — Combat Expansion Roadmap**

1. **Phase 1 — Core Combat Identity**

   Target terdekat

   Vanguard, chase, approach, telegraph, attack, recovery, Dodge, dan Parry.

   Hasil: satu musuh sudah menyenangkan untuk dilawan.
2. **Phase 2 — Spatial Combat**

   Skirmisher, flanking, reposition, combat positioning, enemy spacing, dan pengembangan CombatDirector.

   Hasil: tiga sampai lima musuh tidak hanya menumpuk di sekitar Player.
3. **Phase 3 — Enemy Variety**

   Ranger, Bulwark, Controller, beserta attack pattern yang berbeda.

   Hasil: kombinasi musuh memberikan tantangan yang berbeda.
4. **Phase 4 — Elite & Boss**

   Attack chain, stagger phase, multi-stage pattern, dan enemy modifier.

   Hasil: pertarungan lebih intens dan memiliki fase.
5. **Phase 5 — Roguelike Encounter**

   Room composition, room modifier, artefak yang mengubah gameplay, dan encounter difficulty progression.

   Hasil: setiap run mempunyai variasi tantangan.

Tahapan tersebut mengikuti rancangan Combat Design yang sudah kamu lampirkan.

Pasted markdown (3).md

## **Mengapa urutan ini penting?**

Lihat hubungan dependensinya:

```
PLAYER COMBAT
      |
      v
ENEMY INTERACTION
      |
      v
SPATIAL COMBAT
      |
      v
ENEMY VARIETY
      |
      v
ELITE & BOSS
      |
      v
ROGUELIKE ENCOUNTER
      |
      v
PROGRESSION & BUILD VARIETY
```

Apabila Player Combat belum matang, kita belum mempunyai dasar yang cukup untuk menilai apakah suatu musuh menyenangkan untuk dilawan.

Apabila Enemy Behavior belum matang, kita belum mempunyai dasar untuk membuat kombinasi encounter yang seimbang.

Apabila semua itu belum matang, procedural generation hanya akan menghasilkan variasi dari pertarungan yang belum tentu menyenangkan.

Karena itu, aku tidak menyarankan memperluas map generator sekarang.

# **BAGIAN XVII — KEPUTUSAN DESAIN YANG HARUS DIKUNCI**

Ada beberapa hal yang perlu dibedakan antara rancangan yang sudah disepakati dan rekomendasi yang baru kita buat.

Jangan sampai seluruh rekomendasi baru langsung dianggap sebagai mekanik GDD.

Berikut daftar keputusan yang perlu dikonfirmasi sebelum production.

| No. | Keputusan                                 | Status                                             |
| --- | ----------------------------------------- | -------------------------------------------------- |
| 1   | Combat berorientasi agresif dan responsif | Sudah menjadi pilar desain                         |
| 2   | Dodge dengan i-frame                      | Sudah ada dalam rancangan gameplay                 |
| 3   | Perfect Parry tanpa cooldown              | Sudah dinyatakan dalam GDD yang dirujuk            |
| 4   | Projectile Deflect                        | Sudah ada dalam rancangan                          |
| 5   | Three-stage Basic Attack                  | Rekomendasi moveset v1.0                           |
| 6   | Input Buffer                              | Rekomendasi implementasi                           |
| 7   | Combo Window                              | Rekomendasi implementasi                           |
| 8   | Attack Cancel Rules                       | Perlu dikunci berdasarkan GDD dan animasi          |
| 9   | Mouse Aiming                              | Rekomendasi untuk kontrol PC                       |
| 10  | Gamepad Right Stick Aiming                | Rekomendasi tambahan                               |
| 11  | Dodge Counter                             | Perlu dibedakan dari Dodge biasa                   |
| 12  | Durasi Parry Window                       | Belum ditentukan                                   |
| 13  | Durasi I-frame                            | Harus disesuaikan dengan spesifikasi dan pengujian |
| 14  | Cooldown Dodge                            | Perlu dikonfirmasi dengan aturan GDD               |
| 15  | Heavy Attack / Special Attack             | Belum menjadi scope baseline                       |
| 16  | Moveset masing-masing Weapon              | Dikembangkan setelah baseline                      |

### **Keputusan yang menurutku paling penting**

**A. Jangan langsung memasukkan banyak jenis serangan.**

Three-stage combo sudah cukup untuk menguji fondasi offensive combat.

**B. Jangan langsung membuat sistem combo branching.**

Mulai dari rangkaian sederhana. Kita dapat memperluasnya ketika sistem dasarnya sudah terbukti.

**C. Jangan menjadikan Perfect Parry terlalu mudah.**

Karena tidak ada cooldown sesuai GDD, timing dan recovery perlu dirancang secara hati-hati.

**D. Jangan membuat animation cancel tanpa batas.**

Setiap serangan harus mempunyai komitmen dan konsekuensi.

**E. Jangan menambah skill dan senjata sebelum mengetahui kualitas moveset awal.**

Kalau satu moveset belum menyenangkan, banyaknya senjata hanya akan memperbesar jumlah pekerjaan yang harus diperbaiki.

# **BAGIAN XVIII — PELAKSANAAN LANGSUNG DI UNITY**

Sekarang kita turunkan seluruh dokumen ini menjadi tindakan konkret.

Aku tidak ingin kamu langsung membuka banyak file dan mengubah semuanya.

Ikuti prosedur berikut.

## **Langkah 1 — Buat milestone GitHub**

Buat satu milestone:

`MILESTONE — PLAYER COMBAT V1.0`

Deskripsi:

> Develop and validate the core player combat experience for The Last Sasak. This milestone focuses on offensive combat, combo system, defensive mechanics, animation integration, combat feedback, and validation against the Vanguard enemy.

Kemudian buat beberapa Issue:

| Issue | Judul                                       |
| ----- | ------------------------------------------- |
| 01    | Audit Existing Player Combat Architecture   |
| 02    | Define Player Combat Specification          |
| 03    | Implement Basic Attack Foundation           |
| 04    | Implement Three-Stage Combo System          |
| 05    | Refine Dodge and Invulnerability            |
| 06    | Refine Perfect Parry and Projectile Deflect |
| 07    | Define and Implement Animation Cancel Rules |
| 08    | Integrate Player Combat Animations          |
| 09    | Implement Combat Feedback                   |
| 10    | Integrate Vanguard Combat Test              |
| 11    | Player Combat Functional Testing            |
| 12    | Player Combat v1.0 Validation               |

Jangan membuat kedua belas Issue tersebut menjadi dua belas proyek terpisah. Semuanya berada di dalam milestone yang sama.

Gunakan struktur branch GitHub yang sudah ditetapkan di TDD:

```
dev
 |
 ├── feature/player-combat-audit
 |
 ├── feature/player-basic-attack
 |
 ├── feature/player-combo-system
 |
 ├── feature/player-defense-refinement
 |
 ├── feature/player-animation
 |
 └── feature/player-combat-feedback
```

Setiap pekerjaan diuji sebelum Pull Request digabungkan ke `dev`.

## **Langkah 2 — Audit scene dan referensi**

Buka:

`CombatTest.unity`

Periksa seluruh object Player.

Checklist:

- PlayerInputReader terpasang.
- PlayerMotor terpasang.
- PlayerFacingController terpasang.
- PlayerActionController terpasang.
- AttackController terpasang.
- HitboxController terpasang.
- DodgeController terpasang.
- ParryController terpasang.
- InvulnerabilityHandler terpasang.
- Health terpasang.
- PlayerAnimationController terpasang.
- CameraTarget tersedia.
- AttackPoint tersedia.
- Cinemachine Follow sudah mengarah ke CameraTarget.
- AttackDefinitionSO sudah terhubung.
- Tidak ada Missing Script.

Periksa juga `CombatSystem`, `CombatResolver`, dan seluruh referensi Combat yang digunakan oleh scene.

Ingat, jangan menambahkan komponen dari kode secara massal.

Pasang komponen melalui scene dan Inspector sesuai prinsip engineering proyekmu.

## **Langkah 3 — Uji Basic Attack yang sudah ada**

Sebelum membuat combo, pastikan satu serangan dasar benar-benar berfungsi.

Lakukan pengujian ini:

1. Jalankan CombatTest.
2. Tekan Basic Attack satu kali.
3. Amati animasi.
4. Periksa kapan hitbox aktif.
5. Periksa apakah damage diterapkan.
6. Serang kembali target yang sama.
7. Pastikan damage tidak berulang pada satu aktivasi serangan.
8. Uji serangan yang meleset.
9. Uji ketika Player sedang bergerak.
10. Uji ketika Player berada di samping obstacle.

Catat masalah yang ditemukan.

Jangan langsung memperbaiki banyak bagian sekaligus.

## **Langkah 4 — Definisikan moveset**

Buat dokumen terpisah:

`Player Combat Specification v1.0`

Masukkan:

- Player Control Specification.
- Attack Definition.
- Combo Chain.
- Startup, Active, Recovery.
- Input Buffer.
- Combo Window.
- Dodge Rules.
- Parry Rules.
- Projectile Deflect.
- Animation Cancel.
- Animation Requirements.
- Combat Feedback.
- Acceptance Criteria.

Setelah definisi ini disepakati, implementasi bisa dilakukan dengan lebih konsisten.

## **Langkah 5 — Implementasikan bertahap**

Urutan yang harus diikuti:

```
Basic Attack
     |
     v
Three-Stage Combo
     |
     v
Input Buffer
     |
     v
Combo Window
     |
     v
Attack Cancel
     |
     v
Dodge Integration
     |
     v
Parry Integration
     |
     v
Animation Integration
     |
     v
Combat Feedback
     |
     v
Vanguard Testing
     |
     v
Player Combat v1.0
```

Jangan melompati tahap hanya karena beberapa script sudah tersedia.

# **BAGIAN XIX — GAMBARAN HASIL AKHIR YANG HARUS DICAPAI**

Aku ingin target akhirnya jelas.

Bayangkan kamu menjalankan `CombatTest`.

Player berada di tengah arena.

Satu Vanguard mendekat.

[Curse of the Dead Gods - Action Game | ABGames.io](https://images.openai.com/static-rsc-4/-AMgcCSDBN1l1-1iuXSSk1qL5oZ-9Ys7caG4eZoUDbY55QWGuT6J8iacOUe_euki6aIS6PQiGykzmTbfnSni6FWR3RCocTspAmf_o9eOz8uZXEw247x7LbMFLhIskdMipn7jLxnYvk6bZl1RNG6SzQPSiJsjDWE07oVZGHDJXas?purpose=inline)

[abgames.io](https://abgames.io/curse-dead-gods/?utm_source=chatgpt.com)

Player mulai dengan Attack 01.

Serangan pertama mengenai Vanguard.

Hit reaction muncul.

Player melanjutkan ke Attack 02 melalui input buffer.

Serangan kedua berhasil mengenai target.

Player melanjutkan ke Attack 03.

Finisher memberikan impact yang lebih kuat.

Vanguard kemudian mulai melakukan Quick Slash.

Player melakukan Dodge dan berhasil menghindari ancaman.

Player kembali mendekat.

Vanguard menyiapkan Heavy Swing.

Player membaca telegraph dan melakukan Perfect Parry.

Terjadi hitstop.

Efek parry muncul.

Serangan Vanguard terinterupsi dan musuh mengalami recovery.

Player memanfaatkan kesempatan tersebut untuk melanjutkan serangan.

Vanguard akhirnya mati.

HUD diperbarui dan sistem reward bekerja sesuai aturan.

Itulah hasil yang kita inginkan.

[Hades 2 Gets a Major Patch: New Characters, Region & Weapons](https://images.openai.com/static-rsc-4/TkwTwnNJ0fvXlCucTEm_fEYIvAlf18ERrZktFEFpI1s8e92SvJvj7GMKSCt6cwlMCs4qtEZT0a6idZfR2kbw0CrV_dwwEasSLfv3uFfsKCrmwLQw3fNQiCWZNZlw7Nxv2bSi7maW1wjP5ofbexPlYr2AdGOHaufQyVw4kdRq7_8?purpose=inline)

[Review – Hades II | Atomix](https://images.openai.com/static-rsc-4/o13I_b16bTsG0r6g0DNB4UCaAMZWlrJ9yJKLE61qVrB8enfJJlugS3RNOJkPvJbMAdwpEmjzcjZIn9OmXbnQ9rjp42C5SzWUJJZwJ_AyloDNlw-9DJ0tDaoEl1Yo0D6ZpngIuR5x5VjeOUaH3PP9NvkooRBvlLnxwxrwKnJUViY?purpose=inline)

[Hades](https://images.openai.com/static-rsc-4/oSPAqRPVtAPgPI59wlUNVvL0WYruDkyTJt8CJMIIphcruCJ2VAD9xnzJJx03N4WFcVWHIZ98O8XsWW1xUV7daMtKz1JJ1hMIWZuruR0RqU0M2CaNnAQc-I55n05BpcP02xVLR6krghNnczKu_I9sOo6SDTecVXm0kUZeptFjkZU?purpose=inline)

11

Bukan hanya karena Player dapat mengurangi HP Enemy.

Tetapi karena ada rangkaian interaksi yang saling berhubungan:

```
PLAYER OFFENSE
      |
      v
ENEMY THREAT
      |
      v
PLAYER DEFENSE
      |
      v
ENEMY RECOVERY
      |
      v
PLAYER PUNISH
      |
      v
COMBAT CONTINUES
```

Itulah gameplay loop yang akan menjadi dasar bagi pengembangan encounter selanjutnya.

# **BAGIAN XX — DEFINISI SELESAI PLAYER COMBAT V1.0**

Aku akan menyebut milestone ini berhasil apabila tiga kondisi terpenuhi.

### **1. Engineering Success**

Sistem bekerja secara teknis.

Tidak ada kerusakan state, hitbox bermasalah, damage ganda, input yang tidak konsisten, maupun bug interaksi kritis.

### **2. Gameplay Success**

Pemain bisa menyerang, membuat kombo, menghindar, melakukan parry, melakukan deflect, dan memanfaatkan kesempatan counter.

Semua dapat dilakukan dalam satu pertarungan yang berkesinambungan.

### **3. Experience Success**

Combat terasa responsif, mudah dibaca, memiliki impact, dan memberikan pemain ruang untuk membuat keputusan.

Ketiga hal tersebut harus terpenuhi.

# **PENUTUP — ARAH PRODUKSI YANG KITA AMBIL**

Menurutku, ini merupakan keputusan pengembangan yang paling tepat untuk kondisi proyekmu sekarang.

Kamu sudah mempunyai banyak sistem dasar. Jangan sampai pekerjaan berikutnya hanya menambah jumlah script, prefab, atau fitur.

Mulai sekarang, fokuskan kualitas gameplay.

**Target pertama bukan membuat seluruh The Last Sasak selesai. Target pertama adalah membuat satu pertarungan yang membuat orang ingin terus bermain.**

Urutan keseluruhan pengembangan yang aku rekomendasikan:

## **Production Direction**

1. Fondasi tersedia

   **Gameplay Engineering Foundation**

   Scene, Player, CombatSystem, Enemy dasar, Room, HUD, dan sistem pendukung.
2. Sekarang

   **Player Combat v1.0**

   Moveset, Combo, Dodge, Parry, Animation, Feedback, dan testing.
3. **Enemy Behavioral Design**

   Membuat Vanguard dan kemudian Skirmisher mempunyai perilaku yang khas.
4. **Spatial Combat & Encounter**

   Enemy spacing, flanking, CombatDirector, komposisi kelompok.
5. **Combat Variety**

   Ranger, Bulwark, Controller, Elite, dan Boss.
6. **Roguelike Systems**

   Artefak, build, room modifier, encounter progression.
7. **Vertical Slice Integration**

   Menggabungkan combat, progression, room, dan hub menjadi satu pengalaman permainan.
8. **Procedural Generation & Polish**

   Variasi hutan, kualitas presentasi, optimasi, dan build demo.

**Jadi, pekerjaan yang harus kamu mulai sekarang adalah Stage 0: Combat Audit & Design Lock.**

Jangan langsung membuat serangan ketiga atau efek hitstop. Pastikan terlebih dahulu bahwa sistem yang sudah kamu bangun mempunyai tanggung jawab dan perilaku yang benar.

Setelah itu, kita implementasikan satu Basic Attack, menyempurnakannya menjadi combo, mengintegrasikan pertahanan, dan mengujinya melawan Vanguard.

Itu akan menjadi fondasi yang jauh lebih kuat untuk seluruh pengembangan The Last Sasak berikutnya.


ABAIKAN SEMUA HAL TTG GITHUB. FOKUS MEMBAUT RANCANGAN ENGINEERING SAJA