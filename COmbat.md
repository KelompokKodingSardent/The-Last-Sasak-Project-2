Setelah melihat struktur folder Unity dan membaca dokumen sistem gameplay kamu, menurutku `The Last Sasak` sudah memiliki fondasi yang cukup bagus untuk dikembangkan menjadi **isometric action roguelike dengan combat yang cepat, responsif, dan penuh variasi**.

Aku justru tidak menyarankan kamu mengganti seluruh sistem yang sudah ada.

Yang perlu kita tingkatkan adalah desain combat di atas sistem tersebut.

Aku akan memperlakukan `The Last Sasak` sebagai game dengan identitas:

[Chơi lại bom tấn indie Hades (2020): Vẫn cuốn như ngày mới ra](https://images.openai.com/static-rsc-4/jewRBbfaIvQWOxthJ1dgYNYv2A2VULOwR0LA5VNFikzfUu3nDrUX36dzUgWWpicvCrnkkf2AS8LSV12xYEm0eKyXCjrVnYIB_tAmfIbV1YciE_I2GEVYCUaIeCJKrfzjIhKlV4p4EGV3ThtjT0djGlCT1Y_llgDq0Gy-ZDvhsgI?purpose=inline)

[젠레스 「전투 조작」 쉽게 이해하기 | Vortex Gaming](https://images.openai.com/static-rsc-4/ejHVHKgUesBQ4WTsblL1qFYgFBNpwosCVuuJmZhmh18GrWzUkVxxdeA6VpvekUMuLz3XmsQ1fiZu9MFH3Ms2HQSnjywjquVyeTLzjJUNYDo2awRNoJTdCh7gE_LSAuV0d2Zd5bN8drAs--AMj9C31-CIWbnhTAFGGF_LGS4bqe4?purpose=inline)

[Hades (2020) | Switch eShop Game | Nintendo Life](https://images.openai.com/static-rsc-4/A_Fh_XPN-W9Ds1srWigwoiJqXj-i8hF9feipot7RyZhzWD2T3XzT77M86w_E6v8AiKO4T-7Mja48iKXK5iI5AwXH3qjxXGMeQNOyv59OhLS6e7FGEC_mNo0X1QqK5LQz25CLa58ekmNGOF7J91fOOFUIDLFkcmZ5BFvXzEHRxx0?purpose=inline)

11

# **The Last Sasak**

## Isometric Action Roguelike

Pertarungan cepat, agresif, responsif, tetapi tetap memiliki ruang untuk membaca gerakan musuh, melakukan parry, dodge, dan membangun kombo.

# **1. Identitas combat yang sebaiknya dimiliki**

Menurutku ada empat pilar yang perlu menjadi pegangan desain.

**Aggressive Combat**

Player didorong untuk terus melakukan serangan, kombo, dan mengejar kesempatan menyerang.

**Reactive Combat**

Dodge, parry, dan counter bukan sekadar tombol bertahan. Keberhasilannya harus menghasilkan keuntungan.

**Intelligent Enemy Behavior**

Musuh mempunyai karakter, tujuan, jarak ideal, dan pola serangan yang berbeda.

**Roguelike Variety**

Setiap run menghasilkan kombinasi build, enemy composition, room modifier, dan tantangan berbeda.

Ada satu hal yang perlu dijaga: jangan sampai game menjadi terlalu defensif hanya karena musuh mempunyai banyak telegraph dan recovery.

Kita ingin player merasa sedang menguasai pertarungan, bukan terus-menerus menunggu kesempatan menyerang.

## **2. Combat loop utama**

Aku menyarankan pola dasar pertarungan seperti ini:

## **The Last Sasak — Combat Loop**

**1. Engage**

Player dan musuh memasuki pertarungan

**2. Positioning**

Musuh mengejar, flank, menjaga jarak, atau mencari sudut serang

**3. Threat**

Musuh melakukan telegraph dan memberikan ancaman yang dapat dibaca

**4. Player Response**

Dodge, parry, counter, atau menghindari area serangan

**5. Punish Opportunity**

Musuh terkena stagger atau memasuki recovery, player dapat melanjutkan kombo

**6. Reset / Escalate**

Musuh mengubah posisi, memilih serangan berikutnya, dan pertarungan berlanjut

Ini mengambil filosofi ZZZ tentang dodge, counter, Daze, dan Stun, tetapi disesuaikan dengan game kamu yang menggunakan satu karakter player, bukan sistem pergantian tiga Agent. Pada ZZZ, Daze menjadi dasar untuk membuat musuh masuk ke kondisi Stun dan membuka kesempatan serangan lanjutan.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://www.prydwen.gg\&sz=32)

Zenless Zone Zero+1



# **3. Desain enemy roster yang aku rekomendasikan**

Jangan mulai dengan membuat sepuluh musuh.

Lebih baik buat lima archetype yang benar-benar berbeda perilaku.

Aku menyarankan roster awal berikut.

[7 best roguelike games to try out in 2025](https://images.openai.com/static-rsc-4/VKBSVjKgQjZN6-HG-5gkpqrtn4eBAQNkjgdI_kcxa_dng1aBKe3urELl5CNJ0I7kLP1xanJK8ebb_uzxS72qeN1-OadKI34aOI9n0J_4urZkvuafovogCGSepnohuBV0ltGI94tDzZbeqRrMSMh76UjT9-HXwQPcC-C5OgpVR3k?purpose=inline)

**01 — Vanguard**

Basic melee

Musuh standar yang menjadi dasar pembelajaran combat.

**Behavior:**

- Mendekati player.
- Menjaga jarak melee.
- Melakukan kombinasi satu atau dua serangan.
- Recovery setelah serangan.
- Sesekali melakukan reposition.\
  **Tujuan desain:** mengajarkan timing serangan, dodge, dan counter.

[Hades: Elysium - gamepressure.com](https://images.openai.com/static-rsc-4/iJi_ekf7sYsp-PieIyC9IH0ISwkS_nTTEOhzSzVDfJIs6Yd75K3hW4kVOKIosGcy_LVDHvZpzBLViyD1LZ0N2BIPDtf-AYwQS56T_Jboan8gPyYOdYAAmPTBynpixcNP3G25ZQlwNJ2rfZvIiKXX0lGZbPvxZ7tMgI1J3vDFHJk?purpose=inline)

**02 — Skirmisher**

Agile / Flanker

Musuh yang membuat player tidak bisa hanya berdiri di satu posisi.

**Behavior:**

- Mengitari area player secara tidak konstan.
- Mencari sisi kiri atau kanan.
- Melakukan short dash.
- Menyerang dengan cepat.
- Mundur untuk reposition.\
  **Tujuan desain:** menciptakan variasi posisi dan tekanan dari berbagai arah.

[Hades: 10 Hardest Enemies & How To Deal With Them – FandomSpot](https://images.openai.com/static-rsc-4/m5zmtye8DMbi6-XDbE1b5gQIRm515walU0w3suygXNF65xq-DP7eh_e4KHT7lAaPx16xZjCKS-sM01gOBorrnBVlXagDoHRHIwUOkVIPvCswDzyc7g4CdlfnFqB8Fo69uphd_ALQwfLaPOtdGWH4Gk0kZPN_L3qEmHd-5Ql1y8o?purpose=inline)

**03 — Ranger**

Ranged pressure

Musuh yang mengontrol ruang dari kejauhan.

**Behavior:**

- Mempertahankan jarak ideal.
- Bergerak lateral untuk mencari garis pandang.
- Menembakkan projectile dengan telegraph.
- Mundur jika player mendekat.
- Memilih posisi baru setelah menembak.\
  **Tujuan desain:** memaksa player memperhatikan seluruh arena.

[Hades: 10 Hardest Enemies & How To Deal With Them – FandomSpot](https://images.openai.com/static-rsc-4/wYQe9Di50hyAmtxa5CAq7Rl_TgXi9mMxQNu-YgdVIlJCcvOtSomm5cBnyzw9Ex5uKY-FHGpe8TYKtDpCoIBxZTfn0T-7VxZitrPXvpTr5pMDl8QUs4EVvrvryGRn1vqy5Us28-1nqcN9c48FICQooicmSV3ZAxgCFkjU8bDkwD0?purpose=inline)

**04 — Bulwark**

Heavy / Defender

Musuh lambat yang mengontrol area dengan serangan besar.

**Behavior:**

- Bergerak perlahan.
- Mempertahankan arah hadap.
- Memiliki serangan dengan area luas.
- Telegraph cukup jelas.
- Memiliki ketahanan atau stagger resistance.\
  **Tujuan desain:** mengubah ritme pertarungan dan memaksa player memilih waktu menyerang.

[Hades: 10 Hardest Enemies & How To Deal With Them – FandomSpot](https://images.openai.com/static-rsc-4/Efj1O2vdYxFz4dxX1HL1vxVDL4L2bIaqd4o6Lbxc0uRFCrYxdEuK4BOS2YYiuRCOHWP9fhB8-A-tpTJD8RQ29hG9froxW1HhW6N7zyPTIeoPPIYyirBGMib-IuVxV7wO8Y8Bxk1DJ1HMRCFJvifheLGlSoprFyja4KixZUUbPJg?purpose=inline)

**05 — Controller**

Support / Area control

Musuh yang mengubah kondisi arena.

**Behavior:**

- Menjaga jarak dari player.
- Memberikan support kepada musuh lain.
- Memunculkan hazard atau projectile.
- Memaksa player mengubah posisi.\
  **Tujuan desain:** menciptakan prioritas target dan mengubah komposisi pertarungan.

Kelima archetype tersebut mengambil pelajaran dari variasi peran enemy Hades, seperti Strongbow, Greatshield, Brightsword, dan Soul Catcher. Di Minecraft Dungeons, Vindicator juga menjadi contoh menarik karena mempunyai fase charge, menyerang, lalu retreat; perilakunya dapat berubah ketika target berada lebih jauh.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://kotaku.com\&sz=32)

Kotaku+3



**Penting:** Vanguard tidak harus mempunyai flanking yang kompleks. Skirmisher justru harus menjadi contoh utama implementasi flanking.

Dengan begitu, masing-masing enemy punya alasan untuk bergerak dengan cara berbeda.

# **4. Desain Flanking / Orbit khusus The Last Sasak**

Ini perlu perhatian khusus karena perspektif isometric membuat pergerakan melingkar terlihat berbeda dibandingkan perspektif third-person.

Aku tidak ingin musuh bergerak seperti satelit yang terus berputar mengelilingi player.

Aku ingin musuh bergerak seperti sedang mencari kesempatan untuk menyerang.

## **A. Positioning berbasis peran**

Gunakan konsep posisi relatif terhadap player.

## **Combat positioning map**

Ilustrasi konseptual posisi musuh pada arena isometrik.

A Front pressure

B Side flank

C Reposition

D Rear approach

Bukan berarti empat posisi ini harus selalu diisi.

Justru kita ingin posisi berubah berdasarkan keadaan.

Contoh:

- Vanguard dapat mempertahankan posisi depan.
- Skirmisher mencari sisi kiri atau kanan.
- Ranger memilih area yang mempunyai garis pandang.
- Bulwark dapat menahan area tertentu.

## **B. Flanking harus mempunyai keputusan**

Aku menyarankan Skirmisher memiliki tiga kebiasaan:

**Observe:** mengevaluasi posisi player dan musuh lain.

**Reposition:** bergerak menyamping menuju posisi yang lebih menguntungkan.

**Commit:** ketika mendapatkan kesempatan, melakukan serangan pendek.

Setelah serangan, musuh tidak perlu langsung kembali berputar. Ia dapat mengambil jarak, mengamati, lalu kembali masuk.

Hasilnya:

[Review: Hades | The Nexus](https://images.openai.com/static-rsc-4/SoiYki3B1n-krLQ2LmoTqlE4oWjJtZ_MoaCORgAZVWtqKSg_8X8L50ZojqKL_tzU9dst0Js6R6x1NlWXnm7LYcPMLfuq15Tn4s-K2J_8IZxGQMxDWbmWfsQSDWw51O7jpXh1dR-Q9PE3IOV0X9Dh3HGIyTQv6ng5OmR4ruiHj1w?purpose=inline)

[nexushub.co.za](https://nexushub.co.za/nexus/review-hades?srsltid=AfmBOoqjxAPM8wuidQTXE9MdzeEnGIASk4e4KvXsgQxbZY49VFa16ZHw\&utm_source=chatgpt.com)

Musuh terlihat seperti mempunyai tujuan, bukan mengikuti satu titik koordinat.

## **C. Jangan hanya menggunakan jarak untuk menentukan keputusan**

Untuk positioning, aku merekomendasikan beberapa faktor:

| Faktor             | Pertanyaan yang dievaluasi                      |
| ------------------ | ----------------------------------------------- |
| Distance           | Apakah posisi terlalu dekat atau terlalu jauh?  |
| Player Facing      | Apakah player sedang menghadap musuh?           |
| Occupancy          | Apakah area sudah ditempati musuh lain?         |
| Navigation         | Apakah tersedia jalur menuju posisi?            |
| Attack Opportunity | Apakah ada kesempatan untuk menyerang?          |
| Archetype          | Apakah posisi tersebut cocok dengan tipe musuh? |

Dengan demikian, Flanking menjadi keputusan taktis, bukan gerakan otomatis.

# **5. Desain attack pattern setiap enemy**

Ini yang menurutku paling penting setelah positioning.

Setiap musuh harus mempunyai beberapa serangan dengan fungsi yang berbeda. Jangan hanya membuat satu serangan yang diulang setiap cooldown.

Aku menyarankan setiap serangan memiliki enam bagian:

1. **Approach:** Mengambil jarak serang.
2. **Telegraph:** Memberikan tanda serangan.
3. **Startup:** Menyiapkan animasi serangan.
4. **Execution:** Serangan benar-benar aktif.
5. **Recovery:** Musuh memiliki jeda setelah menyerang.
6. **Reposition:** Musuh memilih posisi berikutnya.

Berikut contoh rancangan pattern.

## **Vanguard — Basic melee**

[Hades im Test: Göttliche Action jetzt auch auf allen Konsolen](https://images.openai.com/static-rsc-4/FxZwoRC42VUWJjc14qUMIVP-navEIQJH3q7M6zcfS6-uNEMvzMlLh2XBRdBZakKc2dI90HLIlWg6oHAmvJDG_80LKRNnT25DWQBZzygEMdVrA-oRmfanN5SnEF-pBuMM29e0-lmoRCt9MpcbaJKcS3ANev7-bWsl-ae5TDWQKdY?purpose=inline)

[pcgames.de](https://www.pcgames.de/Hades-Spiel-62075/Tests/Review-Rogue-like-Rogue-lite-Action-Bastion-PC-Switch-Wertung-1358547/?utm_source=chatgpt.com)

Pola dasar

| Serangan     | Pattern                                       | Kesempatan player                    |
| ------------ | --------------------------------------------- | ------------------------------------ |
| Quick Slash  | Satu ayunan cepat                             | Dodge                                |
| Double Slash | Dua serangan berurutan                        | Dodge pertama, counter setelah kedua |
| Heavy Swing  | Ancang-ancang lebih lama, serangan lebih luas | Parry atau dodge                     |

Vanguard jangan terlalu sering melakukan serangan berat. Tugasnya memberikan tekanan dasar, bukan menjadi boss kecil.

## **Skirmisher — Agile melee**

[《哈迪斯》评测：失败和胜利同样有趣 - GameRes游资网](https://images.openai.com/static-rsc-4/nPPIVUZXeMBIp2lhEZOjFnqWosp7DfaDvA5vx24dAeU7nddSP3opcNY8yj3kET65rfI2FCEu6peo3wDiCkj-ZXABXAJ-JT_uUQ6GC-9EeJzBgKPrd5JOaAKJipAl5ZDP1MWaAPk6xR5L6DVs3EK8bhMzL-z5aZjPBhNYlnW4V4A?purpose=inline)

[gameres.com](https://www.gameres.com/833635.html?utm_source=chatgpt.com)

Pola positioning

| Serangan    | Pattern                            | Kesempatan player                     |
| ----------- | ---------------------------------- | ------------------------------------- |
| Side Dash   | Bergerak menyamping lalu menyerang | Membaca arah dash                     |
| Cross Slash | Mendekat dengan dua serangan cepat | Dodge atau interrupt                  |
| Backstep    | Menjauh setelah menyerang          | Player mengejar dan melanjutkan kombo |

Skirmisher harus menjadi musuh yang paling aktif mengubah posisi.

Namun, jangan membuatnya terus melakukan dash. Berikan jeda sehingga player bisa memahami pergerakannya.

## **Ranger — Ranged**

[Hades: Łuk Poszukujący Serc (Heart-Seeking Bow) - GRYOnline.pl](https://images.openai.com/static-rsc-4/26YiFLOY6FTy9A07A01S5XiikCicHau-I73EgO9Zs3vK_WK0-oI_rg4ZrI5r-dCpJ6hgUFvDDLo9DIgYYO_vccPQ5LOjWbqXKymL7tlQ0kXVwm1mqfm0zvsAVkTxydSkY_fA4sOf8SCNMxXnIotz3s3fRogISYgFhs5drrbguF4?purpose=inline)

[gry-online.pl](https://www.gry-online.pl/poradniki/hades/luk-poszukujacy-serc/z01b73c?utm_source=chatgpt.com)

Pola projectile

| Serangan     | Pattern                                          | Kesempatan player             |
| ------------ | ------------------------------------------------ | ----------------------------- |
| Focused Shot | Membidik posisi player lalu menembak             | Dodge                         |
| Burst Shot   | Menembakkan beberapa projectile                  | Membaca celah antarprojectile |
| Ground Mark  | Memberi peringatan area sebelum projectile jatuh | Keluar dari area              |
| Retreat Shot | Menjauh sambil melepaskan projectile             | Mengejar melalui jalur aman   |

Ranger sebaiknya menjadi musuh yang memperhatikan jarak.

Kalau player mendekat, ia berusaha mempertahankan ruang, bukan terus berdiri diam menembak.

## **Bulwark — Heavy**

[Spelunky 2 and Hades use chaos to spark conversation, making them the perfect lockdown games | GamesRadar+](https://images.openai.com/static-rsc-4/ekIEDKWrFC2XGNKT7nSkeAxXnuGyvtFVq2H_q75APe05TRGYBrMVXio9XwZDnzPLJvXPdE84OZ6UKi-1hYKp85ai5C_fza75f1hcuHsP0NZ-swY2L9XCLecsDfnZcgMkO4mWz0UEImnX5J6W4hfvv4ZcIRPUotSt92iMnBGUWLs?purpose=inline)

[gamesradar.com](https://www.gamesradar.com/spelunky-2-and-hades-use-chaos-to-spark-conversation-making-them-the-perfect-lockdown-games/?utm_source=chatgpt.com)

Heavy pressure

| Serangan        | Pattern                            | Kesempatan player          |
| --------------- | ---------------------------------- | -------------------------- |
| Shield Bash     | Dorongan jarak dekat               | Dodge samping              |
| Ground Slam     | Serangan area berbentuk lingkaran  | Keluar dari area           |
| Charging Strike | Bergerak lurus menuju target       | Menghindari jalur serangan |
| Guard Stance    | Mengurangi tekanan dari arah depan | Mengambil posisi lain      |

Bulwark adalah kandidat bagus untuk menggunakan `StaggerComponent`.

Ketika berhasil melakukan counter atau mengumpulkan stagger yang cukup, musuh kehilangan kesempatan mempertahankan tekanan untuk sementara.

## **Controller — Area control**

[Hades Review (PS5) - Softpedia](https://images.openai.com/static-rsc-4/X-tQazlMxjM1caYcjuHSZoqinmqnF_8nv5mnn4rNLI0nog8XD8nfoQBBCuvqJgC0GwnEH2v9bI7I_v0WnFOcaPIl8sG3b04ypTcax4K2JJfKm2ZDyJLGhmYOCdHAlrbvw9Szn-RV6rs_XFQA0g0CpavHCbjFMKPg2s748EwI31w?purpose=inline)

[softpedia.com](https://www.softpedia.com/reviews/games/playstation-5/hades-review-533843.shtml?utm_source=chatgpt.com)

Arena control

| Serangan        | Pattern                         | Kesempatan player                        |
| --------------- | ------------------------------- | ---------------------------------------- |
| Summon Hazard   | Membuat area berbahaya          | Reposition                               |
| Projectile Rain | Menyerang beberapa area         | Bergerak antarzona aman                  |
| Empower         | Memperkuat musuh lain sementara | Menghentikan atau mengalahkan Controller |

Controller memberikan alasan bagi player untuk menentukan prioritas target.

Mereka tidak harus memiliki damage tertinggi. Keberadaan mereka saja sudah cukup mengubah kondisi pertarungan.

# **6. CombatDirector harus lebih dari pembatas jumlah penyerang**

Ini salah satu perubahan desain yang paling aku rekomendasikan untuk sistem kamu.

Sekarang `CombatDirector` digunakan untuk mengatur siapa yang boleh menyerang.

Menurutku, sistem tersebut perlu berkembang menjadi **Encounter Coordinator**.

Artinya, selain membatasi penyerang, ia juga mengatur pembagian peran dalam kelompok musuh.

Contohnya, ada lima enemy di satu ruangan.

## **Enemy group coordination**

**Enemy A**

Attack

Sedang melakukan telegraph

**Enemy B**

Flank

Mencari posisi samping

**Enemy C**

Pressure

Menyiapkan projectile

**Enemy D**

Hold

Menjaga area

**Enemy E**

Reposition

Mencari posisi baru setelah serangan berikutnya

Dengan begitu, tidak semua enemy harus melakukan sesuatu yang sama.

Aku juga menyarankan konsep *attack budget*:

| Ancaman          | Aturan desain                                            |
| ---------------- | -------------------------------------------------------- |
| Melee commitment | Batasi jumlah serangan melee besar yang aktif bersamaan  |
| Ranged pressure  | Boleh memberi tekanan tambahan, selama telegraph terbaca |
| Area attack      | Diperhitungkan sebagai ancaman aktif juga                |
| Flanking         | Tidak harus mengambil slot serangan                      |
| Recovery         | Tidak perlu meminta slot agresor                         |

Untuk encounter biasa, kita bisa mulai dengan satu serangan melee utama dan maksimal satu ancaman jarak jauh yang sedang dipersiapkan. Jumlah tersebut bisa berubah berdasarkan komposisi musuh dan tingkat kesulitan.

Ini hanya titik awal balancing, bukan aturan permanen.

Poin terpentingnya: **musuh yang tidak sedang menyerang tetap mempunyai pekerjaan.**

# **7. Isometric camera membutuhkan desain telegraph khusus**

Ada hal yang sering dilupakan ketika mengambil inspirasi dari ZZZ atau Hades.

Kamera isometrik memiliki tantangan keterbacaan yang berbeda.

Player harus bisa mengetahui:

- Musuh berada di mana.
- Serangan datang dari arah mana.
- Area mana yang berbahaya.
- Ke mana sebaiknya bergerak.

Aku menyarankan lima bentuk telegraph utama.

[NAISU Selection Bundle (Simplified Chinese, English, Korean, Japanese, Traditional Chinese)](https://images.openai.com/static-rsc-4/x_2ASP1sgaNEWDtfVdKHhxy3dpDQSEtuUaJQbpxemQKjXnOvkZ9y0G2_Y9yQW6KZuHtqAvd7GCs5VKkZ5Wt1TozXuGO6phNhiqLUWkZzKIsveTtoYJjYUg8XsfNSFXpg4wsv9M2rhoKBCoFxKLfUj8H8aQ5BlU9noxr3eScNYyU?purpose=inline)

**Circular Telegraph**

Serangan area melingkar. Cocok untuk Ground Slam dan ledakan.

[Defend Earth: Xenos Survivors - Prelude on Steam](https://images.openai.com/static-rsc-4/GBbd-5aQe8mSQWuRmtigL4Fl9gzEZ2CqF0iyA2lxWgjY2gFuv5F67FnE47r3ljM3_6IunCBKmWX2X0S0LMDVEzkHjC75pYKR-gZwA7KnfrRrmHy0SdtmSs4-RrFvzgLFsb1bFCvGBCiAe6bdzDXkkGsGHjxYkQAJFubIgAXABdE?purpose=inline)

**Linear Telegraph**

Serangan lurus atau charge. Cocok untuk Bulwark.

[Crashlands 2: Hand-to-Pan Combat](https://images.openai.com/static-rsc-4/HZKMNaY9RFYuW-POf96rlsIXqO8Wuv5O5z7BhvlkIg0HGRtEMhbGJBlvMDPmTDUsRBsG_CJ5qCWslelMELOuoONbGiBT_SUV5jTIfxNO3fHemJ3tcLqQggKIUQf2K25wTiDVyaSDmeegKPBw7ipu4b3PrW2gd_96ktm116ZET9Q?purpose=inline)

**Cone Telegraph**

Serangan berbentuk kipas di depan musuh.

[Soulstone Survivors (Soulstone Survivors: Prologue) — обзоры и отзывы, описание, дата выхода, официальный сайт игры, системные требования и оценки игроков | StopGame](https://images.openai.com/static-rsc-4/cryOWNA8RvpMQFM7nwg5DCz5q-2iycxdD2KHKK9QtdvVuXKJMjewLDc0YVlurZB5kRVKqAfBc8nU5y1AcCBJFiLazys4uoNSRpPynrFKt5Q_D9UwxgsxKh3ffW7X5PIq5ooqqpCMa_aPZg8RWzQ3YNK1u5cIrcwgTeHnyX-yzPg?purpose=inline)

**Zone Telegraph**

Beberapa area menjadi berbahaya secara bergantian.

[ローグライクアクション『Ira』がSwitch 2で今夏配信。BitSummitで先行試遊も - 電撃オンライン](https://images.openai.com/static-rsc-4/mdSlv72yX-Gn2iD6csDYzXPseI5KL4X9RrMCQquqLM8m2w9sCiV6iWJC4U271rY5-jyAMoOHt7sYiiiKdKTqf0bWwRoTJN5lFQBRbLV7c9sY93hVy4ESh_mx5njgseJnFyDV-wT21EiylHpkItFk31lMYD13KbWOKIdyPGzlRs8?purpose=inline)

**Targeted Telegraph**

Musuh mengunci posisi player sebelum projectile dilepaskan.

Ada tiga aturan visual:

1. Gunakan warna sekaligus bentuk, jangan mengandalkan warna saja.
2. Pastikan telegraph berada di permukaan arena dan tidak tertutup efek serangan.
3. Jangan terlalu sering memberikan serangan dari luar layar tanpa peringatan.

Untuk kamera isometrik, aku lebih menyarankan telegraph berbasis proyeksi ground-plane, dengan posisi dan arah yang konsisten pada bidang permainan.

# **8. Room design: arena harus ikut menjadi bagian dari combat**

Menurutku, ini kesempatan besar untuk membuat roguelike kamu berbeda.

Jangan membuat setiap room hanya berupa lantai kosong dengan musuh yang ditempatkan secara acak.

Arena harus mempunyai fungsi.

Hades memberikan contoh bagus: lingkungan Asphodel berupa pulau-pulau yang dipisahkan lava membuat setiap jenis musuh membutuhkan cara mobilitas tertentu. Tim Hades bahkan memberikan metode mobilitas tersendiri kepada musuh agar sesuai dengan lingkungan tersebut.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://kotaku.com\&sz=32)

Kotaku+1



Aku menyarankan empat tipe arena awal.

[Shiny Shoe announces co-op roguelike RPG Inkbound | RPG Site](https://images.openai.com/static-rsc-4/13hOuH5fxAUaw6pidSfKFL23Nxmd9knLfhanNWMKl7VipggrXtKfEF_Z9Pu3mOxBgduaRbMWtyAZVe4-OM6J4kHqxFWl-AsnbARtfmRfMtQfBRwicDX-IG2XGsxIGW4SGPRBit1n-YKNYKEWZuraJaQtOAn7ivc4qsGDnG7WSi0?purpose=inline)

[rpgsite.net](https://www.rpgsite.net/news/12851-shiny-shoe-announces-co-op-roguelike-rpg-inkbound?utm_source=chatgpt.com)

**01. Open Arena**

Combat dasar

Area terbuka, banyak ruang untuk dodge dan reposition.

Cocok untuk Vanguard, Skirmisher, dan Bulwark.

[How Rock Band influenced Hades' soundtrack  – Laced Records](https://images.openai.com/static-rsc-4/cLecHJsdDu2frIfIJrDVpQjlSVKoKIuJk7NeyM0iAHZ5WAznq4Lqslw5JFne85UvNsX34JYOvqDMtauE5NEL3UW6qcYnFCucSoeNNiJK1KQ-bOdwbShPNnkzuFpiipPjybxbcB3ZK1uj20hMjW3zlx95qhfX9ED3xvtu4Qx6w2o?purpose=inline)

[lacedrecords.co](https://www.lacedrecords.co/blogs/blog/how-rock-band-influenced-hades-soundtrack?utm_source=chatgpt.com)

**02. Pillar Arena**

Spatial challenge

Memiliki pilar dan obstacle untuk memengaruhi garis pandang.

Cocok untuk Ranger dan Controller.

[The Mixer on Steam](https://images.openai.com/static-rsc-4/yVErP2gsZX3_U9ojcExzRplSuOzHmi7rmmhtlDXRgQNfwwY___3uUG9XkgrpuQYm8w-HiyW1fmXNBTZ4o3TTA8_wlqXtTvJ9qvrie9iTQ5S5B7aYyqMyEKFC9kVt0EBPTk-pHYoyMHBS5nBvgXyCwuamqdbgz6AQ8bszxnzhOR4?purpose=inline)

[store.steampowered.com](https://store.steampowered.com/app/2167400/The_Mixer/?utm_source=chatgpt.com)

**03. Narrow Arena**

Movement challenge

Ruang gerak terbatas dengan beberapa jalur.

Cocok untuk Charger dan heavy enemy.

[Wizard of Legend 2 zaubert ab 25. November auf die Switch - Der magische Roguelite-Hit von Dead Mage erscheint bald mobil | Spiele | News | Spielemagazin.de](https://images.openai.com/static-rsc-4/yHl0faF01ky1hcUjhuf39crEIXmWxBgqFeO4GuyZI6MNjo0M9oTq0wimR2441kCOBFP4oogpQdj25Gq_tHWzP9mzzhJbZUWbM4doXB4xUzgpw6T9tzz5P5Rgjbh1HNOUi9OLrzoPvPdDydC-Re18b7JjbhtI5iZgG-1P-Z2SyjM?purpose=inline)

[spielemagazin.de](https://www.spielemagazin.de/spiele/news/wizard-of-legend-2-zaubert-ab-25-november-auf-die-switch/20862?utm_source=chatgpt.com)

**04. Hazard Arena**

Environmental challenge

Beberapa bagian arena mempunyai kondisi khusus.

Cocok untuk Controller, Ranger, dan kombinasi banyak enemy.

Namun, jangan membuat obstacle sampai menghalangi player memahami serangan musuh.

Setiap arena harus tetap memberi ruang untuk dodge dan positioning.

## **Desain isi room**

Aku merekomendasikan room memiliki komposisi yang sudah dirancang, bukan mengambil enemy sepenuhnya secara acak.

Contoh:

| Encounter | Komposisi                   | Tujuan                          |
| --------- | --------------------------- | ------------------------------- |
| Basic     | 3 Vanguard                  | Mengajarkan combat dasar        |
| Pressure  | 2 Vanguard + 1 Ranger       | Mengenalkan ancaman gabungan    |
| Flanking  | 2 Skirmisher + 1 Vanguard   | Menguji kesadaran posisi        |
| Heavy     | 1 Bulwark + 2 Vanguard      | Memaksa player membaca serangan |
| Control   | 1 Controller + 2 Skirmisher | Menguji prioritas target        |
| Elite     | 1 Elite + 2 enemy pendukung | Menguji kemampuan build         |

Dengan desain seperti ini, setiap ruangan punya karakter tersendiri.

# **9. Rogue-like harus memengaruhi combat, bukan sekadar statistik**

Dari dokumenmu, kamu sudah memiliki:

- `ArtifactController`
- `ExperienceSystem`
- `RewardCollector`
- `CurrencyWallet`
- `PermanentUpgradeSystem`
- `RoomEncounterController`

Ini fondasi yang bagus.

Aku menyarankan progression tidak hanya meningkatkan HP dan Attack Damage.

Buat artefak yang mengubah cara player bertarung.

Contohnya:

| Jenis artefak     | Efek gameplay yang direkomendasikan              |
| ----------------- | ------------------------------------------------ |
| Parry Artifact    | Parry berhasil menghasilkan stagger lebih banyak |
| Dodge Artifact    | Dodge counter mendapatkan efek tambahan          |
| Combo Artifact    | Kombo beruntun meningkatkan damage               |
| Critical Artifact | Serangan tertentu mendapatkan bonus critical     |
| Recovery Artifact | Membantu player pulih setelah kondisi tertentu   |
| Stagger Artifact  | Meningkatkan kemampuan memecahkan stagger musuh  |

Dengan cara ini, setiap run bisa memiliki identitas berbeda.

Contohnya:

**Run A — Aggressive Build**

Player fokus pada combo dan damage beruntun.

**Run B — Counter Build**

Player fokus pada parry, stagger, dan counter.

**Run C — Mobility Build**

Player fokus pada dodge, pergerakan, dan serangan setelah reposition.

**Run D — Critical Build**

Player fokus pada peluang critical dan serangan dengan damage tinggi.

Ini jauh lebih menarik daripada hanya mendapatkan artefak +10% damage.

## **Variasi roguelike untuk enemy**

Enemy juga dapat memiliki modifier.

Misalnya:

| Modifier   | Perubahan perilaku                             |
| ---------- | ---------------------------------------------- |
| Swift      | Memiliki reposition lebih cepat                |
| Armored    | Lebih tahan terhadap stagger                   |
| Aggressive | Lebih sering memilih kesempatan menyerang      |
| Empowered  | Memperoleh kemampuan tambahan                  |
| Tactical   | Lebih sering mencari posisi yang menguntungkan |

Perlu diingat bahwa modifier sebaiknya mengubah perilaku atau cara menghadapi musuh, bukan hanya memperbesar HP dan damage.

# **10. Bagaimana semua ini cocok dengan script project kamu?**

Aku tidak menyarankan membuat ulang semua komponen.

Berikut pembagian yang menurutku paling tepat.

| Script yang sudah ada   | Rekomendasi peran                                            |
| ----------------------- | ------------------------------------------------------------ |
| `EnemyBrain`            | Menentukan intent, memilih pattern, dan mengelola state      |
| `EnemyMovement`         | Chase, flank, orbit, strafe, reposition, retreat             |
| `EnemyAttackController` | Mengeksekusi attack pattern melee atau ranged                |
| `EnemyDefinitionSO`     | Menyimpan archetype, preferensi posisi, dan daftar pattern   |
| `EnemyState`            | State perilaku utama                                         |
| `CombatDirector`        | Mengatur koordinasi kelompok, positioning, dan attack budget |
| `StaggerComponent`      | Mengelola stagger dan kesempatan punish                      |
| `CombatSystem`          | Memproses damage                                             |
| `CombatResolver`        | Memproses parry, damage, stagger, dan hitstop                |

Sementara sistem player kamu sudah mempunyai komponen yang mendukung:

| Sistem player            | Kegunaan dalam desain combat           |
| ------------------------ | -------------------------------------- |
| `AttackController`       | Basic attack dan kombo                 |
| `DodgeController`        | Menghindari ancaman                    |
| `ParryController`        | Menghadapi serangan yang dapat diparry |
| `InvulnerabilityHandler` | Mengatur i-frame                       |
| `PlayerActionController` | Mengatur aksi dan cancel               |
| `TimeEffectController`   | Memberikan feedback hitstop            |
| `CameraEventManager`     | Memberikan feedback kamera             |

Satu catatan: `EnemyAttackController` saat ini merupakan executor serangan sederhana. Ia belum dengan sendirinya menjadi sistem pattern. Konsep pemilihan pattern dan timing serangan perlu dirancang terpisah dari eksekusinya.

Dan `EnemyController.cs` lama sebaiknya tetap dianggap legacy. Jangan jadikan sumber utama AI baru.

# **11. Prioritas pengembangan yang aku rekomendasikan**

Jangan langsung membuat kelima archetype dan seluruh variasi arena.

Aku ingin kamu membuat satu vertical slice combat yang benar-benar terasa menyenangkan terlebih dahulu.

## **The Last Sasak — Combat Design Roadmap**

1. **Phase 1 — Core Combat Identity**

   Prioritas tertinggi
   - Vanguard
   - Chase dan approach
   - Telegraph
   - Attack dan recovery
   - Player dodge dan parry response

   Tujuan: satu enemy saja sudah terasa menyenangkan dilawan.
2. **Phase 2 — Spatial Combat**
   - Skirmisher
   - Flanking dan reposition
   - Combat positioning
   - Enemy spacing
   - CombatDirector yang mengatur peran musuh

   Tujuan: tiga sampai lima musuh tidak hanya menumpuk dan mengejar player.
3. **Phase 3 — Enemy Variety**
   - Ranger
   - Bulwark
   - Controller
   - Beberapa attack pattern per archetype

   Tujuan: setiap kombinasi musuh menghasilkan tantangan berbeda.
4. **Phase 4 — Elite & Boss**
   - Attack chain
   - Stagger phase
   - Multi-stage pattern
   - Enemy modifier

   Tujuan: menghadirkan pertarungan yang lebih intens dan mempunyai fase.
5. **Phase 5 — Roguelike Encounter**
   - Room composition
   - Room modifier
   - Artefak yang mengubah gameplay
   - Encounter difficulty progression

   Tujuan: setiap run terasa berbeda, bukan sekadar mengulang pertarungan yang sama.

# **12. Target akhir: seperti apa The Last Sasak seharusnya terasa?**

Aku membayangkan satu encounter seperti ini:

[Hades II Screenshot Gallery - Page 1 | XboxAchievements.com](https://images.openai.com/static-rsc-4/lvUV6N1Dj-74GZV6tahWD0F2EcbZkt7qGQEyZvFeTbL-9CIr10ADWQlYu0eL95JeutSSKxSpHsMB3bdgO1wa4iHQYLbQaB9p7u8TRiGZIIO-J2_c2TroSTLjxujxBrnrFIxiOxsFP9EYQjKOhE9rVYzpwz3PddkfKfrWcaqb9Js?purpose=inline)

[xboxachievements.com](https://www.xboxachievements.com/game/hades-2/screenshots/?utm_source=chatgpt.com)

Player masuk ke arena.

Dua Vanguard mendekat dari depan. Skirmisher mencoba mencari sisi kosong. Ranger menjaga jarak sambil menyiapkan projectile.

Vanguard mulai melakukan telegraph.

Player melakukan parry dan berhasil menginterupsi serangan. Efek hitstop membuat keberhasilan terasa kuat.

Player melanjutkan kombo kepada Vanguard kedua. Skirmisher datang dari arah samping, sehingga player harus melakukan dodge.

Dodge berhasil. Player mendapatkan kesempatan melakukan counter.

Sementara itu, Ranger memaksa player berpindah posisi dengan projectile.

Player berhasil mengalahkan beberapa musuh. Stagger Bulwark mencapai batasnya. Musuh tersebut kehilangan kesempatan menyerang dan membuka kesempatan kombo besar.

Setelah encounter selesai, player memilih artefak yang akan membentuk strategi untuk ruangan berikutnya.

**Itulah jenis combat yang menurutku cocok untuk The Last Sasak.**

# **13. Keputusan desain yang ingin aku kunci**

Aku akan memprioritaskan lima hal berikut:

| Prioritas | Sistem                | Keputusan                                                     |
| --------- | --------------------- | ------------------------------------------------------------- |
| 1         | Enemy identity        | Setiap archetype harus punya perilaku khas                    |
| 2         | Positioning           | Musuh tidak selalu mengejar langsung                          |
| 3         | Attack patterns       | Setiap enemy mempunyai beberapa pattern yang saling mendukung |
| 4         | Combat rhythm         | Ada ancaman, respons, punish, dan reposition                  |
| 5         | Encounter composition | Kombinasi enemy dan arena menghasilkan tantangan berbeda      |

Satu hal yang paling penting: jangan mengejar jumlah enemy atau jumlah attack pattern terlebih dahulu.

Lebih baik mempunyai tiga musuh dengan perilaku yang sangat berbeda daripada sepuluh musuh yang semuanya mengejar player dan menggunakan serangan serupa.

**Visi desainku untuk The Last Sasak:**

> A fast-paced isometric action roguelike where every enemy has a purpose, every attack tells a story, every dodge creates an opportunity, and every encounter challenges the player in a different way.

Menurutku, fondasi paling tepat untuk mulai kita matangkan berikutnya adalah **Enemy Behavioral Design**: mendefinisikan secara detail bagaimana Vanguard, Skirmisher, dan Ranger berpikir, memilih posisi, menentukan target, mengatur agresivitas, serta memilih serangan. Setelah desain perilaku itu solid, barulah kita menerjemahkannya menjadi sistem `EnemyBrain` yang modular.