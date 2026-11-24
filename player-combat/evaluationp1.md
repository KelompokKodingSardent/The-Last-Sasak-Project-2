Ini merupakan perkembangan yang cukup besar. Berdasarkan laporan refactoring tersebut, aku akan **mengubah rekomendasi roadmap sebelumnya**.

Kita tidak lagi berada pada tahap merancang sistem Basic Attack, Combo, Dodge, dan Parry dari nol. Sebagian besar mekanik tersebut sudah diimplementasikan dan bahkan sudah mengalami perbaikan arsitektur.

Menurutku, pekerjaan selanjutnya harus bergeser dari *Feature Development* menjadi **Player Domain Stabilization, Data Configuration & Combat Validation**.

Ada beberapa hal yang ingin aku evaluasi terlebih dahulu karena walaupun arsitekturnya sudah jauh lebih baik, masih ada sejumlah risiko yang perlu diselesaikan sebelum integrasi EnemyBrain.

# **1. Evaluasi hasil refactoring**

## **1.1. Perubahan terbesar: dari prototipe menjadi data-driven architecture**

Menurutku keputusan arsitektur yang kamu ambil sudah tepat.

Terutama empat perubahan berikut.

**A. PlayerStatController sebagai pusat perhitungan statistik**

Sebelumnya, masing-masing sistem dapat mengubah Max HP secara langsung.

Ini berbahaya karena sistem progression, artifact, armor, dan permanent upgrade bisa saling menimpa perubahan statistik.

Sekarang alurnya lebih baik:

```
Base Player Stats
       |
       v
Weapon Modifiers
       |
       v
Armor Modifiers
       |
       v
Artifact Modifiers
       |
       v
Final Player Stats
       |
       v
PlayerStatController
       |
       v
Health / Combat / Movement
```

Semua modifier dihitung melalui satu jalur.

Keuntungannya adalah perubahan equipment atau artifact tidak perlu membuat setiap komponen menghitung statistik secara terpisah.

**B. Pemisahan Movement Direction dan Attack Direction**

Ini keputusan yang sangat penting untuk game isometrik.

Sebelumnya, arah hadap mengikuti arah pergerakan. Sekarang Player bisa bergerak ke satu arah sambil melakukan serangan ke arah lain.

[Acquista Hades - PC & Mac (Steam)](https://images.openai.com/static-rsc-4/F1kv0jPgTUri0btZipyAhsSucJbtfjJ4hBF-YG7W4kV78UBlI1wOoVkkXIQT8tb5Pdr2JmI8BXXesbkqATcjEUPLGilRFWjyBheW7T2mHIN0LZdXYdCDfwVnyKEBjFxT72KB6TuK1z7fXdahdwNJKnGe1aidIuFciVui-Pb47JY?purpose=inline)

[instant-gaming.com](https://www.instant-gaming.com/it/5972-comprare-hades-pc-mac-gioco-steam/?utm_source=chatgpt.com)

Ini membuka kemungkinan gameplay seperti:

- Bergerak mundur sambil menyerang.
- Melakukan strafe.
- Menghindari serangan sambil tetap menghadap musuh.
- Mengarahkan serangan ke musuh tertentu tanpa harus menghentikan movement.

Menurutku fitur ini harus dipertahankan.

Ada satu penyempurnaan: arah serangan sebaiknya diambil ketika serangan dimulai dan dipertahankan setidaknya selama fase startup dan active, bukan hanya selama active. Dengan begitu, animasi dan arah hitbox tidak berbeda.

**C. WeaponDefinitionSO dan ArmorDefinitionSO**

Ini merupakan pengembangan yang bagus karena sekarang equipment mempunyai identitas data sendiri.

Weapon bukan lagi sekadar kumpulan `AttackDefinitionSO[]`.

Weapon dapat mempunyai:

- Identitas senjata.
- Rangkaian serangan.
- Statistik senjata.
- Model visual.
- Animator override jika diperlukan.
- Modifier serangan.

Sementara armor dapat mempunyai modifier pertahanan dan atribut Player.

**D. PlayerActionController sebagai otoritas state**

Ini salah satu keputusan paling penting.

Animator tidak menentukan apakah Player boleh melakukan tindakan.

PlayerActionController menentukan tindakan yang valid, sedangkan Animator menampilkan gerakannya.

Artinya, kita memiliki pemisahan antara gameplay logic dan visual presentation.

Pertahankan arsitektur ini

Aku tidak menyarankan refactoring ulang terhadap keempat fondasi tersebut kecuali ditemukan bug pada pengujian.

# **2. Evaluasi enam pilar combat**

Berdasarkan implementasi yang kamu laporkan, berikut penilaianku.

| Pilar      | Kondisi sekarang                              | Evaluasi               |
| ---------- | --------------------------------------------- | ---------------------- |
| Responsive | Input buffer dan cancel                       | Sudah memiliki fondasi |
| Aggressive | Micro-lunge dan parry recovery cut            | Sesuai arah desain     |
| Tactical   | Attack movement multiplier dan whiff recovery | Bagus, perlu balancing |
| Fluid      | Centralized action arbitration                | Arsitektur tepat       |
| Impactful  | Per-attack hitstop dan camera shake           | Sudah data-driven      |
| Readable   | Isometric aiming dan animasi                  | Perlu validasi visual  |

Ada beberapa perhatian khusus.

### **Responsive**

Input buffer `0.16s` belum tentu cukup untuk semua senjata.

Kamu sudah menemukan contoh masalah pada Klewang. Ini harus diperbaiki sebelum melakukan tuning menyeluruh.

### **Aggressive**

Micro-lunge merupakan fitur yang menarik.

Namun, jangan sampai setiap serangan membuat Player otomatis mengejar musuh terlalu jauh.

Perhatikan:

- Apakah Player bisa melewati target?
- Apakah jarak serangan tetap konsisten?
- Apakah arah serangan tetap terkunci?
- Apakah micro-lunge bekerja ketika Player menyerang udara atau meleset?

Micro-lunge harus membantu serangan terasa agresif, bukan mengambil kendali gerakan dari pemain.

### **Tactical**

Movement multiplier `0.35x` ketika menyerang cukup penting untuk diuji.

Pastikan pemain masih merasa mempunyai kendali terhadap posisi, terutama ketika menghadapi beberapa musuh.

Jangan langsung mengunci nilai tersebut sebagai angka final.

### **Fluid**

Centralized state arbitration merupakan fondasi yang benar.

Tetapi pergantian senjata tetap harus mengikuti aturan state.

Aku tidak menyarankan pergantian senjata di tengah animasi aktif apabila pergantian tersebut menyebabkan AttackDefinition, visual senjata, dan Animator menggunakan definisi yang berbeda.

### **Impactful**

Per AttackDefinitionSO sudah mempunyai parameter feedback sendiri.

Bagus. Sekarang tinggal memastikan efek tersebut benar-benar terasa berbeda saat dimainkan.

### **Readable**

Mouse aiming dan Right Stick aiming merupakan peningkatan besar.

Hal yang harus diperhatikan adalah jangan membuat rotasi otomatis terlalu cepat atau terlalu sensitif.

Pemain harus memahami ke mana serangannya diarahkan.

# **3. Lima celah teknis: evaluasi dan keputusan**

Menurutku, kelima celah yang ditemukan harus ditangani, tetapi tidak semuanya memiliki tingkat urgensi sama.

Aku juga ingin menambahkan beberapa catatan agar solusi yang dipilih tidak menimbulkan masalah baru.

## **3.1. Scene Singleton Dependency**

Prioritas P0 — Blocker

Ini merupakan masalah yang paling penting.

Kamu memiliki `CombatSystem`, `CombatResolver`, `ParryResolver`, dan `TimeEffectController` di MainMenu.

Sementara itu, CombatTest bisa dijalankan secara langsung.

Artinya ada dua jalur penggunaan:

```
MainMenu
   |
   v
Load CombatTest
   |
   v
Gameplay Services tersedia?
```

dan:

```
Play CombatTest langsung
   |
   v
Gameplay Services tersedia?
```

Kalau services hanya ada di MainMenu dan tidak dipertahankan ketika scene berganti, jalur pertama juga bisa mengalami masalah.

### **Rekomendasi**

Karena prinsip proyekmu adalah menempatkan objek melalui scene, aku lebih menyukai pendekatan berikut:

Buat kelompok objek khusus untuk layanan gameplay pada setiap scene yang membutuhkannya.

Contohnya:

```
CombatTest
│
├── SceneServices
│   ├── CombatSystem
│   ├── CombatResolver
│   ├── ParryResolver
│   └── TimeEffectController
│
├── Player
├── Environment
├── Enemies
└── UI
```

Lakukan hal yang sama pada ForestRun untuk sistem yang dibutuhkan scene tersebut.

MainMenu memiliki layanan yang memang diperlukan oleh MainMenu.

Tidak perlu semua scene mempunyai seluruh service.

Alternatifnya adalah menggunakan bootstrap scene dan layanan persistent. Namun, itu membutuhkan pengaturan siklus hidup dan pencegahan singleton ganda yang lebih teliti.

**Keputusan rekomendasiku: gunakan scene-owned services terlebih dahulu.**

Itu paling selaras dengan arsitektur scene yang telah kamu pilih.

Acceptance criteria:

- Play langsung dari CombatTest bekerja.
- CombatSystem.Instance tersedia.
- CombatResolver tersedia.
- ParryResolver tersedia.
- TimeEffectController tersedia.
- Play melalui MainMenu tetap bekerja.
- Tidak ada singleton ganda ketika berpindah scene.

## **3.2. Input Buffer dan Senjata Lambat**

Prioritas P0 — Gameplay

Perhitungan yang kamu berikan benar.

Dengan durasi serangan `0.55s` dan ComboWindowStartNormalized `0.45`:

\\[ 0.55 \times 0.45 = 0.2475s \\]

Jendela kombo baru terbuka pada detik ke-0.2475.

Apabila input dilakukan pada detik ke-0.05 dan buffer hanya `0.16s`:

\\[ 0.05 + 0.16 = 0.21s \\]

Input kedaluwarsa sebelum jendela kombo terbuka.

Selisihnya adalah:

\\[ 0.2475 - 0.21 = 0.0375s \\]

Ini berarti input pemain yang sebenarnya masuk akal bisa hilang.

### **Rekomendasi**

Jangan langsung menaikkan seluruh input buffer secara global.

Buat tuning yang mempertimbangkan jenis senjata.

| Weapon Type     | Karakteristik                      | Pendekatan                                        |
| --------------- | ---------------------------------- | ------------------------------------------------- |
| Fast Weapon     | Serangan relatif cepat             | Buffer lebih pendek                               |
| Balanced Weapon | Tempo sedang                       | Buffer standar                                    |
| Heavy Weapon    | Startup dan recovery lebih panjang | Buffer lebih panjang atau combo window lebih awal |

Angka `0.22s` yang kamu usulkan dapat digunakan sebagai nilai kandidat pengujian untuk senjata lambat.

Namun, jangan mengunci nilai tersebut sebelum diuji.

Yang harus dipastikan:

1. Input tidak hilang terlalu cepat.
2. Combo tidak otomatis berjalan ketika pemain tidak menginginkannya.
3. Input hanya menyimpan satu permintaan.
4. Input kedaluwarsa tetap dibuang.
5. Buffer bekerja konsisten pada setiap senjata.

## **3.3. Hitbox OverlapSphere terlalu luas**

Prioritas P1 — Combat Accuracy

Aku setuju dengan temuan ini.

`Physics.OverlapSphereNonAlloc` merupakan solusi praktis untuk deteksi serangan, tetapi bentuk bola tidak selalu mewakili bentuk ayunan senjata.

Misalnya:

```
       Musuh A

           |
           |
      [PLAYER] -------- Musuh B

           |
        Musuh C
```

Ketika Player menyerang ke kanan, sistem tidak seharusnya otomatis mengenai musuh di belakang hanya karena jaraknya masih masuk radius.

### **Rekomendasi**

Untuk baseline, pertahankan OverlapSphereNonAlloc dan tambahkan filter sudut.

Secara konseptual:

\\[ d = \frac{targetPosition - attackOrigin}{\lVert targetPosition - attackOrigin \rVert} \\]

\\[ dot = \operatorname{Dot}(attackDirection,d) \\]

Kemudian:

\\[ dot \geq minDot \\]

Target yang lolos pemeriksaan jarak dan sudut dapat diteruskan ke proses damage.

Pastikan arah serangan menggunakan bidang horizontal sehingga perbedaan ketinggian kecil tidak mengubah sudut secara tidak wajar.

Untuk ke depannya, bentuk hitbox juga bisa berkembang menjadi:

- Box.
- Capsule.
- Sector / arc.
- Multi-point overlap.

Namun, jangan langsung membuat sistem hitbox generik yang rumit.

Satu OverlapSphere ditambah filter sudut sudah cukup untuk tahap ini.

Hal yang lebih penting adalah memastikan `Health` menjadi identitas target yang dilacak, bukan `transform.root`.

## **3.4. Weapon Visual Instantiation**

Prioritas P1 — Architecture Cleanup

Aku setuju bahwa `Destroy` dan `Instantiate` setiap pergantian senjata tidak ideal.

Tetapi ada satu penyesuaian terhadap prinsip scene-authored yang kamu gunakan.

Karena sistemmu menghindari pembuatan objek scene melalui script, solusi yang paling sesuai adalah menempatkan kedua model senjata sebagai child pada WeaponSocket sejak awal.

Contohnya:

```
Player
│
└── WeaponSocket
    │
    ├── WeaponSlot_01
    │   └── WeaponVisual
    │
    └── WeaponSlot_02
        └── WeaponVisual
```

Ketika berganti senjata:

```
Select Slot 01
     |
     v
Enable WeaponSlot_01
Disable WeaponSlot_02
```

Tidak diperlukan Instantiate atau Destroy ketika pemain menekan tombol 1 atau 2.

Pastikan:

- Visual sesuai definisi weapon.
- AttackDefinition aktif sesuai weapon.
- Animator menggunakan konfigurasi yang tepat.
- Perpindahan slot tidak menghasilkan state yang tidak valid.
- Pergantian senjata ketika Attack aktif ditolak atau ditunda sesuai aturan.

## **3.5. PoiseResistance dan Knockback**

Prioritas P1 — Design Decision

Ini satu-satunya bagian dari lima temuan yang menurutku perlu dibicarakan kembali sebelum langsung diimplementasikan.

Kamu mengusulkan:

```
impulseVelocity * (1f - stats.FinalPoiseResistance)
```

Secara matematis masuk akal apabila nilai resistance berada pada rentang 0–1 dan memang dimaksudkan untuk mengurangi impulse.

Namun, ada perbedaan antara dua konsep:

**Poise Resistance**

Kemampuan karakter untuk menahan stagger atau interupsi.

**Knockback Resistance**

Kemampuan karakter untuk mengurangi perpindahan akibat dorongan.

Keduanya tidak harus selalu sama.

Contohnya, armor berat mungkin sangat tahan stagger, tetapi masih bisa terdorong ketika terkena serangan yang sangat kuat.

### **Rekomendasiku**

Jangan langsung menghubungkan PoiseResistance dengan PlayerMotor sebelum definisi gameplay-nya disepakati.

Lebih baik tetapkan terlebih dahulu:

| Stat                 | Efek                                 |
| -------------------- | ------------------------------------ |
| Poise Resistance     | Mengurangi stagger yang diterima     |
| Knockback Resistance | Mengurangi impulse atau displacement |
| Damage Reduction     | Mengurangi damage masuk              |

Apabila GDD memang menginginkan armor berat mengurangi knockback sekaligus stagger, boleh menghubungkan keduanya melalui modifier armor.

Tetapi secara arsitektur, kedua atribut tetap sebaiknya dibedakan.

# **4. Tambahan audit yang menurutku wajib dilakukan**

Selain kelima celah di atas, ada empat hal yang sebaiknya tidak terlewat.

## **4.1. Health dan Max HP Policy**

Sistem `SetMaxHealth(newMax, healDifference)` perlu mempunyai definisi perilaku yang jelas.

Ada beberapa kebijakan ketika Max HP berubah:

| Kebijakan           | Hasil                                      |
| ------------------- | ------------------------------------------ |
| Preserve Current HP | HP sekarang tidak berubah                  |
| Preserve Missing HP | Selisih HP yang hilang tetap dipertahankan |
| Heal Difference     | HP bertambah sebesar peningkatan Max HP    |
| Restore Full        | HP diisi penuh                             |

Jangan mengandalkan boolean yang tidak jelas maknanya.

Kalau diperlukan, gunakan enum policy atau metode terpisah sehingga setiap sumber modifier menentukan perilaku secara eksplisit.

Contoh pengujian:

- Player memiliki HP 50/100.
- Max HP meningkat menjadi 120.
- Pastikan hasil sesuai kebijakan upgrade yang disepakati.

Hal ini penting karena kamu sudah berusaha menghilangkan exploit full-heal gratis.

## **4.2. Modifier Calculation**

Pastikan seluruh modifier dihitung dengan urutan deterministik.

Contoh konseptual:

```
Base Stats
    |
    v
Equipment Modifiers
    |
    v
Armor Modifiers
    |
    v
Artifact Modifiers
    |
    v
Temporary Effects
    |
    v
Final Stats
```

Tetapkan apakah suatu modifier bersifat additive atau multiplicative.

Misalnya, attack speed dapat dihitung menggunakan urutan yang disepakati:

\\[ FinalAttackSpeed = BaseAttackSpeed \times (1 + AdditiveBonus) \times Multipliers \\]

Itu hanya contoh formula. Nilai dan arti setiap modifier harus mengikuti desain statistik game.

Yang harus dihindari adalah urutan modifier berubah-ubah karena pemanggilan fungsi yang berbeda.

## **4.3. Runtime Mutation pada ScriptableObject**

Ini sangat penting karena sekarang semakin banyak sistem menggunakan ScriptableObject.

Pastikan runtime tidak mengubah asset konfigurasi secara langsung.

Misalnya, jangan mengubah `AttackDefinitionSO.StartupDuration` ketika artifact aktif.

Lebih aman apabila:

```
AttackDefinitionSO
      |
      v
Read Base Configuration
      |
      v
Calculate Effective Values
      |
      v
Runtime Attack Data
```

Dengan demikian, perubahan ketika run berlangsung tidak merusak konfigurasi dasar asset.

## **4.4. Animator Parameter Validation**

Pendekatan hash parameter sudah benar.

Pastikan validasi dilakukan ketika Animator diinisialisasi, bukan berulang kali setiap frame.

Periksa juga:

- Parameter wajib tersedia.
- Parameter opsional tidak menghasilkan warning berulang.
- Trigger tidak tertinggal.
- AttackIndex sesuai dengan serangan yang dimainkan.
- AttackSpeed benar-benar memengaruhi animasi apabila memang dirancang demikian.
- Parameter Hit tidak membuat animasi berjalan bertentangan dengan action state.

# **5. Roadmap baru: Player Domain Stabilization v1.0**

Sekarang aku akan menyusun urutan pekerjaan berikutnya berdasarkan kondisi aktual yang kamu laporkan.

Ini menggantikan sebagian besar roadmap Player Combat sebelumnya. Kita tidak perlu mengulang pembuatan combo, Dodge, dan Parry dari nol.

# **New Development Milestone**

**PLAYER DOMAIN STABILIZATION & VALIDATION v1.0**

Tahap berikutnya

1. **Stage 01 — Scene Dependency Stabilization**

   Pastikan CombatSystem dan seluruh service tersedia ketika scene dijalankan sendiri atau melalui MainMenu.
2. **Stage 02 — PlayerStat & Equipment Configuration**

   Pasang PlayerStatController, buat WeaponDefinitionSO, AttackDefinitionSO dan ArmorDefinitionSO.
3. **Stage 03 — Combat Timing & Hit Detection**

   Perbaiki input buffer senjata lambat, hitbox angle filter dan orientasi serangan.
4. **Stage 04 — Equipment Visual Management**

   Hilangkan Instantiate/Destroy berulang pada pergantian weapon dan pastikan pergantian state aman.
5. **Stage 05 — Defensive & Stat Validation**

   Uji Dodge, Parry, Deflect, damage reduction, poise, knockback dan HP recalculation.
6. **Stage 06 — Combat Presentation Validation**

   Uji Animator, hitstop, camera impulse, VFX dan SFX.
7. **Stage 07 — T01–T12 Play Mode Testing**

   Lakukan pengujian terpadu di CombatTest.
8. **Stage 08 — Player Domain Freeze**

   Dokumentasikan hasil, perbaiki bug, dan tetapkan baseline Player Domain v1.0.

## **5.1. Rincian konfigurasi asset**

Untuk tahap ini, jangan membuat arsenal yang besar.

Cukup buat asset yang mampu membuktikan sistem.

| Asset              | Jumlah awal             | Tujuan                                                                |
| ------------------ | ----------------------- | --------------------------------------------------------------------- |
| WeaponDefinitionSO | 2                       | Menguji dua slot dan pergantian senjata                               |
| AttackDefinitionSO | 6 atau sesuai kebutuhan | Tiga attack per weapon jika masing-masing memiliki moveset tersendiri |
| ArmorDefinitionSO  | 1                       | Menguji modifier armor                                                |
| Player Base Stats  | 1 konfigurasi           | Baseline statistik Player                                             |

Jika dua weapon memang memiliki moveset berbeda, setiap weapon harus memiliki referensi serangan yang tepat.

Untuk pengujian awal, salah satu weapon dapat menggunakan Klewang apabila itu memang senjata yang sedang dikembangkan.

Weapon kedua sebaiknya dipilih berdasarkan kebutuhan pengujian, misalnya senjata dengan karakteristik serangan lebih cepat atau lebih berat.

Jangan membuat banyak variasi sebelum konfigurasi dua slot berhasil.

## **5.2. Susunan pekerjaan teknis**

| Urutan | Task                                       | Prioritas |
| ------ | ------------------------------------------ | --------- |
| 1      | Menyediakan SceneServices pada CombatTest  | P0        |
| 2      | Memastikan PlayerStatController terpasang  | P0        |
| 3      | Membuat dan assign asset equipment         | P0        |
| 4      | Memperbaiki input buffer slow weapon       | P0        |
| 5      | Menguji Health recalculation               | P0        |
| 6      | Menambahkan hitbox angle filter            | P1        |
| 7      | Memperbaiki weapon visual switching        | P1        |
| 8      | Memastikan aim dan rotation lock konsisten | P1        |
| 9      | Memvalidasi poise dan knockback semantics  | P1        |
| 10     | Memeriksa seluruh Animator parameter       | P1        |
| 11     | Tuning hitstop dan feedback                | P2        |
| 12     | Regression testing                         | P0        |

Urutan ini sedikit berbeda dari urutan temuan awal karena masalah scene dependency dan konfigurasi statistik merupakan penghalang pengujian yang lebih mendasar.

# **6. Rancangan pengujian T01–T12**

Aku menyarankan setiap pengujian memiliki kondisi awal, tindakan, hasil yang diharapkan, dan status.

Gunakan tabel berikut sebagai test plan.

| ID  | Nama pengujian       | Hal yang diperiksa                                                                  |
| --- | -------------------- | ----------------------------------------------------------------------------------- |
| T01 | Scene Initialization | Singleton dan service tersedia saat CombatTest dimainkan langsung                   |
| T02 | Player Stats         | Base stats dan Final Stats terhitung benar                                          |
| T03 | Health Recalculation | Perubahan Max HP tidak melakukan heal yang tidak diinginkan                         |
| T04 | Isometric Aiming     | Mouse dan Right Stick mengendalikan arah serangan secara benar                      |
| T05 | Combo & Buffer       | Three-stage combo bekerja pada weapon cepat dan lambat                              |
| T06 | Hitbox Accuracy      | Target di luar sudut serangan tidak terkena damage                                  |
| T07 | Weapon Switching     | Slot 1/2, visual, animasi dan AttackDefinition tersinkronisasi                      |
| T08 | Armor & Damage       | Damage reduction dan modifier statistik berfungsi                                   |
| T09 | Dodge & I-Frame      | Durasi gerakan dan invulnerability benar                                            |
| T10 | Parry & Recovery     | Perfect Parry, kegagalan timing dan recovery berfungsi                              |
| T11 | Projectile Deflect   | Projectile berpindah faction dan arah dengan benar                                  |
| T12 | Combat Lifecycle     | Hitstop, death, pooling, retry dan perpindahan scene tidak menghasilkan state rusak |

### **Contoh detail T05**

**T05 — Combo & Input Buffer**

Functional Test

**Kondisi awal:**

- Player aktif.
- Weapon slot 1 tersedia.
- Weapon slot 2 tersedia.
- Setiap weapon mempunyai konfigurasi attack sendiri.

**Prosedur:**

1. Jalankan Attack 01.
2. Masukkan input lanjutan sebelum combo window.
3. Ulangi dengan input yang masuk pada combo window.
4. Uji input terlalu awal.
5. Uji input ketika recovery.
6. Ulangi pada kedua weapon.

**Expected result:**

- Input valid tersimpan.
- Input kedaluwarsa dibuang.
- Combo tidak melompati serangan.
- Tidak ada double activation.
- Senjata lambat tidak kehilangan input yang seharusnya valid.

**Status:** Belum diuji.

### **Contoh detail T06**

**T06 — Hitbox Accuracy**

Kondisi awal: Player dan dua target Enemy ditempatkan pada jarak yang hampir sama.

Prosedur:

1. Tempatkan satu target di depan Player.
2. Tempatkan satu target di belakang atau di sisi yang tidak termasuk arc serangan.
3. Lakukan serangan.
4. Periksa Health masing-masing target.

Expected result:

- Target yang masuk radius dan sudut serangan menerima damage.
- Target di luar arah serangan tidak terkena.
- Satu target tidak menerima damage ganda dari satu aktivasi.

### **Contoh detail T10**

**T10 — Parry & Recovery**

Kondisi awal: satu musuh dengan serangan yang bisa diparry.

Prosedur:

1. Lakukan Parry terlalu awal.
2. Lakukan Parry terlalu terlambat.
3. Lakukan Perfect Parry.
4. Uji berulang kali.
5. Periksa PlayerActionState setelah setiap hasil.

Expected result:

- Parry yang gagal diproses melalui damage resolution normal.
- Perfect Parry berhasil.
- ParryWindow ditutup setelah tindakan selesai.
- Player tidak terjebak pada state Parrying.
- Recovery terpotong sesuai aturan desain.
- GDD tanpa cooldown tetap dipertahankan.

# **7. Setelah T01–T12: Integrasi EnemyBrain**

Ini tahap setelah Player Domain dinyatakan stabil.

**Jangan langsung mengintegrasikan semua archetype.**

Kita mulai dengan satu Vanguard.

[Hades - Nintendo Switch - Games - Nintendo](https://images.openai.com/static-rsc-4/yjcguqe20dr_pxCfZi00c4hA74IbNai5HuAccNHJu-I4p0QUuaj694ZqcovHruW_0gF88Vikj0esrswldZ8hm3D0FrSnh631zULSIig3tZMQzmwYYjIiygY2VOrtew5Z6GqlyWvTQ3RCfn2YxVIXqQ9Q7VUg_h3IFqP44LFUSKE?purpose=inline)

[nintendo.com.au](https://www.nintendo.com.au/games/nintendo-switch/hades?utm_source=chatgpt.com)

Tujuan integrasi ini adalah memastikan Player Combat dan Enemy Combat bekerja bersama.

## **Tahap integrasi Vanguard**

| Urutan | Pekerjaan                                          |
| ------ | -------------------------------------------------- |
| 1      | Gunakan satu Vanguard yang sudah ada di CombatTest |
| 2      | Hubungkan EnemyBrain dengan EnemyMovement          |
| 3      | Uji Chase dan Attack                               |
| 4      | Uji Quick Slash                                    |
| 5      | Uji Double Slash                                   |
| 6      | Uji Heavy Swing                                    |
| 7      | Uji Player Dodge dan Parry                         |
| 8      | Uji Enemy Hit Reaction                             |
| 9      | Uji Player Counter                                 |
| 10     | Uji kematian, reward dan Room Cleared              |

Pastikan EnemyBrain tidak memproses UI, reward, atau Player state secara langsung.

Musuh hanya menjalankan perilaku sesuai domainnya.

Alur interaksinya:

```
EnemyBrain
    |
    v
EnemyAttackController
    |
    v
Attack Detection
    |
    v
CombatSystem
    |
    v
CombatResolver
    |
    v
Player Health
    |
    v
Damage / I-Frame Result
    |
    v
Player Feedback
```

Untuk serangan yang dapat diinterupsi, sistem Parry juga harus memberikan hasil yang dapat diterima oleh EnemyBrain atau komponen serangannya.

Setelah satu Vanguard bekerja dengan baik, barulah kita masuk ke Phase 2 Spatial Combat.

# **8. Apa yang tidak perlu dikerjakan sekarang?**

Ini juga penting agar scope tidak melebar.

Untuk sementara, jangan prioritaskan:

| Fitur                         | Alasan ditunda                                 |
| ----------------------------- | ---------------------------------------------- |
| Lima atau lebih jenis senjata | Belum diperlukan untuk validasi core           |
| Skill tree                    | Membutuhkan baseline combat                    |
| Puluhan artifact              | Modifier perlu dibuktikan dengan sedikit asset |
| Boss kompleks                 | Belum membutuhkan semua mekanik elite          |
| Procedural map                | Tidak memperbaiki masalah Player Combat        |
| Multiplayer                   | Di luar scope gameplay saat ini                |
| Rework GameManager            | Tidak ada alasan mengubahnya sekarang          |
| Rebuild CombatSystem          | Sudah ada arsitektur yang lebih baik           |

Jangan refactor ulang sesuatu hanya karena ada cara lain yang lebih kompleks.

Fokus pada masalah yang benar-benar ditemukan dalam pengujian.

# **9. Kriteria keberhasilan milestone baru**

Aku tidak akan menyebut milestone ini selesai hanya karena semua script dapat dikompilasi.

Ada empat hasil yang harus tercapai.

## **Player Domain Stabilization v1.0**

**Architecture**

Stabil

Seluruh layanan tersedia, tidak ada ketergantungan scene yang tersembunyi, dan tidak ada mutasi statistik yang tidak terkontrol.

**Data Configuration**

Tervalidasi

Weapon, Armor, AttackDefinition dan PlayerStat mempunyai konfigurasi yang benar.

**Gameplay**

Tervalidasi

Combo, Aiming, Dodge, Parry, Deflect, dan Equipment Switching bekerja sesuai aturan.

**Testing**

Menunggu T01–T12

Seluruh test case lolos dan regression test tidak menemukan bug baru.

Satu catatan tentang klaim kesiapan `~95%`: angka ini masuk akal sebagai estimasi kematangan arsitektur menurut laporanmu, tetapi belum dapat dianggap 95% demo-ready tanpa hasil tes dan pemeriksaan build.

Arsitektur, konfigurasi, gameplay, dan kualitas presentasi merupakan empat hal yang berbeda.

# **10. Rekomendasi keputusan akhir**

Menurutku, perubahan yang kamu lakukan sudah membawa proyek ke tahap yang lebih maju daripada rencana Player Combat awal.

Karena itu, aku menetapkan arah pekerjaan berikut:

Next Milestone

# **Player Domain Stabilization & Combat Validation v1.0**

**Fokus utama:**

Memastikan keseluruhan sistem Player yang sudah di-refactor bekerja stabil melalui konfigurasi data, integrasi scene, perbaikan edge cases, dan pengujian gameplay.

**Target akhir:**

Player siap diintegrasikan dengan EnemyBrain tanpa perlu mengubah fondasi Player Domain lagi.

**Urutan mulai:**

1. Perbaiki Scene Singleton Dependency.
2. Pasang PlayerStatController dan konfigurasi asset.
3. Perbaiki input buffer senjata lambat.
4. Perbaiki hitbox angle filtering.
5. Validasi weapon visual switching.
6. Putuskan semantik Poise dan Knockback.
7. Jalankan T01–T12.

Setelah itu:

**Player Domain Stabilization → Vanguard Integration → Enemy Behavioral Design → Spatial Combat → Enemy Variety → Elite & Boss → Roguelike Encounter.**

Itulah urutan yang menurutku paling aman dan efisien.

Satu prinsip yang ingin aku pertahankan selama pengembangan berikutnya:

**Jangan lagi menambah kompleksitas arsitektur sebelum kita membuktikan kualitas gameplay yang sudah ada.**

Sekarang tugas kita bukan membuat Player mempunyai lebih banyak kemampuan. Tugas kita adalah memastikan seluruh kemampuan tersebut bekerja konsisten, saling terhubung, dan benar-benar menyenangkan ketika dimainkan.