# **The Last Sasak — Item, Inventory, Equipment & Loot Engineering Roadmap**



Rancangan Milestone Baru

Item Domain v1.0



Menurutku, sekarang adalah waktu yang tepat untuk membangun sistem item secara serius. Fondasi combat sudah berkembang menjadi arsitektur data-driven dengan `WeaponDefinitionSO`, `ArmorDefinitionSO`, `PlayerStatController`, `AttackDefinitionSO`, dan `ArtifactController`.

Kita tidak perlu membangun ulang combat. Kita perlu membuat seluruh sistem perlengkapan terhubung dengan benar ke fondasi yang sudah ada.

Aku merekomendasikan milestone:

**ITEM & EQUIPMENT DOMAIN — INVENTORY, EQUIPMENT, LOOT & ITEM PROGRESSION v1.0**

Target akhirnya adalah pemain dapat memperoleh item, menyimpannya, melihat informasinya, membandingkan statistik, memasangnya, mengganti perlengkapan, memperoleh efek pasif, dan mempertahankan data sesuai kebijakan save game.

Rancangan ini mengikuti roadmap yang sudah menetapkan Weapon Switching, Inventory, dan Artefak sebagai Phase 7, dengan target dua senjata dan satu sampai dua artefak untuk versi demo.&#x20;

Pasted text(20261127-010202).txt



# **BAGIAN I — ANALISIS KONDISI SEKARANG**

## **1. Evaluasi implementasi yang kamu miliki**

Ada beberapa fondasi yang sudah benar, tetapi ada pula beberapa masalah struktural yang sebaiknya dibereskan sebelum menambahkan banyak item.

### **A. Inventory masih terlalu sederhana**

Kode saat ini:

```
public class InventoryItem
{
    public string Id;
    public string DisplayName;
    public int Souls;
    public int Exp;
}

public class Inventory : MonoBehaviour
{
    public readonly List<InventoryItem> Items = new List<InventoryItem>();
    public int Capacity = 20;

    public bool Add(InventoryItem item)
    {
        if (item == null || Items.Count >= Capacity)
            return false;
        Items.Add(item);
        return true;
    }
}
```

Ada lima kekurangan yang perlu diperbaiki.

| Masalah                               | Dampak                                                        |
| ------------------------------------- | ------------------------------------------------------------- |
| Item hanya berupa ID dan nama         | Belum mempunyai definisi item yang lengkap                    |
| Souls dan EXP berada di InventoryItem | Mencampurkan sistem item dengan sistem reward                 |
| Belum ada sistem stack                | Item yang bisa ditumpuk belum bisa dikelola                   |
| Belum ada validasi kategori           | Semua item dapat dimasukkan tanpa aturan                      |
| InventorySlot belum terintegrasi      | Ada dua representasi penyimpanan yang belum jelas hubungannya |

**Keputusan:** Inventory menyimpan kepemilikan item, sedangkan `CurrencyWallet` dan `ExperienceSystem` tetap mengelola sumber daya masing-masing.

Artinya, `Souls` dan `Exp` harus dikeluarkan dari representasi `InventoryItem`.

### **B. EquipmentController terlalu banyak tanggung jawab**

Sekarang `EquipmentController` mengelola:

- WeaponSlot1 dan WeaponSlot2.
- ActiveWeaponSlot.
- EquippedArmor.
- Weapon visual.
- Animator override.
- Attack combo.
- PlayerStatController.
- Equipment events.

Secara fungsional ini sudah bisa bekerja, tetapi tanggung jawabnya mulai bercampur.

Masalah paling jelas terdapat di:

```
spawnedWeaponVisual = Instantiate(weapon.WeaponPrefab, weaponSocket);
```

dan:

```
Destroy(spawnedWeaponVisual);
```

Ini tidak sesuai dengan prinsip scene-authored yang kamu tetapkan.

Karena demo awal hanya membutuhkan dua weapon, kita bisa memasang kedua visual senjata langsung di `WeaponSocket`, kemudian cukup mengaktifkan dan menonaktifkannya.

Rencana ini juga konsisten dengan hasil evaluasi Player Domain sebelumnya.&#x20;

Pasted text (2)(4).txt



### **C. ItemSwitching dan EquipmentController mempunyai kepemilikan data ganda**

Ini menurutku masalah paling penting setelah Inventory.

Sekarang terdapat:

**EquipmentController**

```
public WeaponDefinitionSO WeaponSlot1;
public WeaponDefinitionSO WeaponSlot2;

public int ActiveWeaponSlot;
```

**ItemSwitching**

```
public AttackDefinitionSO[] Attacks;
public int Index;
```

Keduanya menyimpan informasi perlengkapan aktif.

Akibatnya, ada kemungkinan:

- Slot 1 sudah berganti weapon, tetapi `Attacks` belum berubah.
- `Index` tidak sama dengan `ActiveWeaponSlot`.
- Visual menunjukkan senjata A, tetapi `AttackController` memakai serangan B.
- Animator menggunakan override yang tidak sesuai dengan weapon aktif.

Selain itu, fallback:

```
AttackDefinitionSO.CreateRuntime(...)
```

berpotensi menyembunyikan konfigurasi asset yang belum lengkap.

**Keputusan:** `EquipmentController` menjadi satu-satunya sumber kebenaran equipment. `ItemSwitching` hanya menerima input dan meminta pergantian slot.

### **D. PlayerEquipmentState belum mempunyai fungsi yang jelas**

Sekarang:

```
public class PlayerEquipmentState
{
    public int ActiveIndex;
    public EquipmentDefinitionSO Active;
}
```

Kita perlu menentukan apakah class ini merupakan:

1. State equipment saat runtime.
2. Data loadout awal.
3. Data untuk save/load.
4. Representasi equipment yang sedang aktif.

Jangan menggunakan satu class untuk keempat tujuan tersebut.

Nanti kita pisahkan `PlayerEquipmentState` dari `PlayerEquipmentSaveData`.

# **BAGIAN II — TARGET ARSITEKTUR**

## **2. Prinsip utama yang akan digunakan**

Ada tujuh prinsip yang menjadi dasar pembangunan.

| Prinsip                     | Aturan                                                            |
| --------------------------- | ----------------------------------------------------------------- |
| Data-driven                 | Definisi item menggunakan ScriptableObject                        |
| Single source of truth      | Satu sistem memiliki satu sumber data utama                       |
| Separation of concerns      | Inventory, Equipment, Stats, Loot dan UI terpisah                 |
| Event-driven                | Perubahan data diberitahukan melalui event                        |
| Immutable definitions       | Asset definisi tidak diubah saat runtime                          |
| Persistent identity         | Setiap item memiliki ID stabil                                    |
| Scene-authored presentation | Visual equipment menggunakan object yang sudah disiapkan di scene |

Ini mempertahankan pola Component-Based dan Event-Driven yang sudah dipakai dalam proyek.&#x20;

Pasted text(20261127-010202).txt



## **3. Gambaran besar sistem**

Ini target arsitektur yang aku rekomendasikan.

## **Item & Equipment Domain**

**ITEM DEFINITION LAYER**

ItemDefinitionSO · WeaponDefinitionSO · ArmorDefinitionSO · ArtifactDefinitionSO · ItemCatalogSO

**Inventory Domain**

Ownership · Slots · Quantity · Capacity · Add / Remove

**Equipment Domain**

Weapon Slots · Armor · Artifacts · Active Selection

**Stat Domain**

PlayerStatController · IStatModifier · Final Stats

**Combat Adapter**

AttackController · ComboChain · Animator

**LOOT & PRESENTATION**

LootPickup · InventoryUI · EquipmentUI · ItemDetail · VisualController

**PERSISTENCE**

InventorySaveData · EquipmentSaveData · SaveSystem

Hal penting: UI tidak boleh mengubah statistik secara langsung. UI meminta `Inventory` atau `EquipmentController` menjalankan operasi, kemudian UI memperbarui tampilan setelah menerima event.

## **4. Pembagian jenis item**

Aku merekomendasikan struktur berikut.

| Kategori   | Data                     | Kegunaan                                  |
| ---------- | ------------------------ | ----------------------------------------- |
| Weapon     | `WeaponDefinitionSO`     | Senjata aktif                             |
| Armor      | `ArmorDefinitionSO`      | Pertahanan dan atribut                    |
| Artifact   | `ArtifactDefinitionSO`   | Efek pasif                                |
| Consumable | `ConsumableDefinitionSO` | Item yang digunakan                       |
| Material   | `MaterialDefinitionSO`   | Bahan crafting atau kebutuhan lain, nanti |

Untuk versi demo, fokus implementasi hanya:

Prioritas

**Weapon, Armor, dan Artifact**

Consumable dan Material cukup disiapkan sebagai kemungkinan pengembangan, bukan langsung dibuat seluruh sistem gameplay-nya.

**Penting:** Souls Coin dan EXP bukan kategori item inventory. Keduanya tetap menjadi resource tersendiri.

# **BAGIAN III — STRUKTUR DATA FINAL**

## **5. Item Definition Architecture**

Saran utamaku adalah membangun satu hierarki definisi item.

**ItemDefinitionSO**

Identitas umum setiap item

**EquipmentDefinitionSO**

Item yang dapat dipasang

**Weapon**

WeaponDefinitionSO

**Armor**

ArmorDefinitionSO

**Artifact**

ArtifactDefinitionSO

ItemDefinitionSO juga dapat menjadi induk MaterialDefinitionSO dan ConsumableDefinitionSO pada pengembangan berikutnya.

### **5.1 ItemDefinitionSO**

Ini adalah definisi paling dasar.

Tanggung jawab:

- Identitas permanen item.
- Nama.
- Deskripsi.
- Icon.
- Kategori.
- Maksimum stack.
- Informasi dasar yang dibutuhkan UI.

Rancangan field:

| Field        | Tipe         | Fungsi                |
| ------------ | ------------ | --------------------- |
| ItemId       | string       | ID unik item          |
| DisplayName  | string       | Nama tampilan         |
| Description  | string       | Deskripsi             |
| Icon         | Sprite       | Gambar item           |
| Category     | ItemCategory | Kategori item         |
| MaxStackSize | int          | Batas jumlah per slot |

Contoh ID:

```
weapon.klewang
weapon.pedang_cepat
armor.besi_ambat
artifact.batu_ambang
material.serpihan_roh
```

ID tidak boleh bergantung pada nama file atau nama tampilan. Mengganti nama item tidak boleh merusak save data.

### **5.2 WeaponDefinitionSO**

Gunakan class yang sudah kamu miliki. Jangan membangun sistem Weapon Definition kedua.

Field yang direkomendasikan:

| Field             | Fungsi                       |
| ----------------- | ---------------------------- |
| WeaponId          | Identitas senjata            |
| WeaponType        | Jenis senjata                |
| ComboChain        | Rangkaian AttackDefinitionSO |
| BaseModifiers     | Modifier statistik senjata   |
| WeaponVisualId    | Identitas visual             |
| AnimatorOverride  | Animasi khusus               |
| WeaponDescription | Deskripsi gameplay           |

Ada pembagian yang harus dipertahankan:

- `WeaponDefinitionSO` menjelaskan karakteristik senjata.
- `AttackDefinitionSO` menjelaskan masing-masing serangan.
- `AttackController` menjalankan serangan.
- `CombatSystem` menyelesaikan interaksi damage.

Jangan memindahkan seluruh statistik serangan ke WeaponDefinitionSO. Damage, startup, active duration, recovery, radius, dan cancel window tetap dapat didefinisikan per serangan.

Contoh:

**Weapon: Klewang**

[Authentic Heiho klewang, kris, sword – Hilt of horn – Original leather sheath | paragonswords](https://images.openai.com/static-rsc-4/FGLL7EQ2bKph9T5gIK3VuGy8tw0zoBkr8jC6xgDpYc3VizwWq7FThi8tXKce-j6LRn9wZoAJDJqKCd0SG-45TT9oFBL-ZSR4lKDLMcF3IOPvzyVWHYh7VMG3_frycp6BiG6olIGdsXCcC9vZ_9HROgoxTQ2u5li9M9OqP80ZVrA?purpose=inline)

Contoh konfigurasi

**Klewang — Balanced Weapon**

WeaponDefinitionSO

ComboChain:

- Slash
- Cross Slash
- Heavy Finish

Masing-masing serangan memiliki AttackDefinitionSO sendiri. Gambar hanya ilustrasi konsep visual, bukan aset final game.

### **5.3 ArmorDefinitionSO**

Armor menjadi penyedia modifier, bukan tempat melakukan perhitungan statistik.

Field:

| Field            | Fungsi            |
| ---------------- | ----------------- |
| ArmorId          | Identitas         |
| ArmorMaterial    | Material visual   |
| ArmorVisualId    | Model atau visual |
| StatModifiers    | Daftar modifier   |
| ArmorDescription | Deskripsi         |

Modifier yang dapat disediakan:

- MaxHealth.
- DamageReduction.
- PoiseResistance.
- KnockbackResistance.
- Modifier lain yang memang sudah didukung PlayerStatController.

Poin penting: Poise Resistance dan Knockback Resistance jangan digabung. Keduanya mengatur karakteristik gameplay yang berbeda.&#x20;

Pasted text (2)(4).txt



Untuk MVP, cukup satu armor yang dapat digunakan sebagai pembuktian sistem.

### **5.4 ArtifactDefinitionSO**

Artefak menjadi perlengkapan pasif.

Field:

| Field              | Fungsi             |
| ------------------ | ------------------ |
| ArtifactId         | Identitas          |
| PassiveDescription | Deskripsi efek     |
| StatModifiers      | Modifier statistik |
| ArtifactVisual     | Icon atau visual   |

Rekomendasi: jangan langsung membuat sistem efek artefak yang kompleks.

Mulai dari modifier sederhana.

Contoh ilustratif:

| Artefak       | Efek                    |
| ------------- | ----------------------- |
| Batu Ambang   | Meningkatkan Max Health |
| Pecahan Arwah | Meningkatkan Damage     |

Keduanya harus menggunakan jalur modifier yang sama.

Bukan membuat masing-masing artefak memanggil `Health.SetMaxHealth()` atau mengubah `AttackController.Definition` secara langsung.

### **5.5 ItemCatalogSO**

Ini tambahan yang sangat aku rekomendasikan.

Buat satu database berisi seluruh definisi item:

```
ItemCatalogSO
    |
    ├── weapon.klewang
    ├── weapon.pedang_cepat
    ├── armor.besi_ambat
    ├── artifact.batu_ambang
    └── artifact.pecahan_arwah
```

Fungsinya:

1. Mencari item berdasarkan ID.
2. Memvalidasi ID ganda.
3. Membantu SaveSystem memulihkan item.
4. Menghindari pencarian asset secara acak ketika runtime.
5. Menjadi daftar referensi resmi seluruh item game.

Tambahkan validasi ketika asset dibuat atau diubah.

**Catatan implementasi:** hierarki inheritance di atas adalah target arsitektur. Sebelum mengubah base class ScriptableObject yang sudah digunakan, audit deklarasi class aktual agar asset Unity yang sudah ada tidak mengalami masalah migrasi.

# **6. Inventory Architecture**

Sekarang kita tentukan bentuk Inventory yang sebenarnya.

## **6.1 Konsep penyimpanan**

Aku merekomendasikan Inventory berbasis slot dengan kapasitas awal 20 slot, sesuai implementasi kamu sekarang.

**Inventory**

20 Slots

1

3

1

1

Ilustrasi grid Inventory, bukan tampilan UI final.

4 item entries

**4 / 20**

Satu slot berisi satu jenis item dan quantity.

Contohnya:

```
Slot 1:
Item = Pecahan Arwah
Quantity = 3

Slot 2:
Item = Klewang
Quantity = 1

Slot 3:
Item = Besi Ambat
Quantity = 1
```

Aturan:

- Material dapat ditumpuk sesuai MaxStackSize.
- Weapon tidak dapat ditumpuk.
- Armor tidak dapat ditumpuk.
- Artifact tidak dapat ditumpuk.
- Item yang sudah dipasang tetap tercatat dalam Inventory.
- Equipment hanya memberikan penanda `Equipped`, bukan mengambil item tersebut dari Inventory.

Untuk versi pertama, aku menyarankan satu kepemilikan untuk setiap definisi equipment. Sistem duplikasi equipment dengan instance ID dapat ditambahkan saat memang ada kebutuhan untuk senjata dengan upgrade atau atribut individual.

## **6.2 Struktur class**

Target:

```
Inventory
    |
    ├── InventoryModel
    │     └── InventorySlot[]
    |
    ├── InventoryController
    │     ├── TryAddItem()
    │     ├── TryRemoveItem()
    │     ├── CanAddItem()
    │     └── FindItem()
    |
    └── Inventory Events
          ├── OnItemAdded
          ├── OnItemRemoved
          └── OnInventoryChanged
```

Pembagian tanggung jawab:

| Class                 | Fungsi                              |
| --------------------- | ----------------------------------- |
| `InventoryModel`      | Menyimpan state kepemilikan         |
| `InventorySlot`       | Data satu slot                      |
| `InventoryController` | Operasi tambah, hapus, dan validasi |
| `InventoryUI`         | Menampilkan isi Inventory           |

Dengan demikian, `InventorySlot` bukan lagi class yang berdiri sendiri tanpa hubungan jelas dengan Inventory.

## **6.3 Aturan operasi Inventory**

Tentukan kontrak metode berikut.

| Method            | Fungsi                                 |
| ----------------- | -------------------------------------- |
| `CanAddItem()`    | Memeriksa ruang sebelum mengambil item |
| `TryAddItem()`    | Menambahkan item dan menangani stack   |
| `TryRemoveItem()` | Mengurangi jumlah item                 |
| `FindItem()`      | Mencari item berdasarkan ID            |
| `HasItem()`       | Memeriksa kepemilikan                  |
| `GetSlots()`      | Mengambil snapshot data untuk UI       |
| `GetItemCount()`  | Mendapatkan quantity item              |

`TryAddItem()` perlu mampu menangani tiga kondisi:

1. Item berhasil ditambahkan seluruhnya.
2. Item hanya dapat ditambahkan sebagian.
3. Inventory penuh dan tidak dapat menerima item.

Jangan membuat sistem drop mengambil item dari dunia sebelum kapasitas dan validitas transaksi diperiksa.

# **7. Equipment Architecture**

Ini adalah pusat dari keseluruhan sistem perlengkapan.

## **7.1 Susunan slot final yang direkomendasikan**

**Player Equipment Layout**

**Weapon Slot 1**

Primary weapon

**Weapon Slot 2**

Secondary weapon

**Armor Slot**

One armor slot initially

**Artifact Slots**

Up to two, as a proposed demo configuration

**Active Weapon**

Current selection

Slot 1

Weapon 1 / Weapon 2 tetap tersimpan. Pergantian hanya mengubah senjata aktif.

Dua weapon slot memang sudah sesuai implementasi dan roadmap. Armor satu slot mengikuti `EquippedArmor` yang ada. Dua artifact slot adalah rekomendasi konfigurasi awal, bukan ketentuan jumlah slot yang sudah dikonfirmasi GDD.

## **7.2 PlayerEquipmentState**

Jadikan class ini representasi state equipment yang sedang digunakan.

Struktur konseptual:

```
PlayerEquipmentState
    |
    ├── WeaponSlot1
    ├── WeaponSlot2
    ├── ActiveWeaponSlot
    └── EquippedArmor
```

`ArtifactController` mengelola slot artefak secara terpisah.

Dengan begitu, satu class tidak perlu mengetahui seluruh implementasi artefak sekaligus.

## **7.3 EquipmentController**

Ini menjadi pengelola utama weapon dan armor.

Tanggung jawab:

- Memeriksa kepemilikan item.
- Mengatur Weapon Slot 1 dan 2.
- Mengatur Active Weapon.
- Mengatur Armor.
- Memvalidasi pergantian.
- Memperbarui sistem combat.
- Meminta pembaruan statistik.
- Mengirim event perubahan equipment.

Kontrak fungsi yang disarankan:

```
TryEquipWeapon(slot, weapon)
TrySwitchWeaponSlot(slot)
TryEquipArmor(armor)
TryUnequipWeapon(slot)
TryUnequipArmor()

GetActiveWeapon()
GetEquipmentState()
```

### **Alur pergantian senjata**

```
PlayerInputReader
       |
       v
ItemSwitching
       |
       v
EquipmentController
       |
       ├── Validate Slot
       |
       ├── Check Ownership
       |
       ├── Check PlayerActionController
       |
       v
Update ActiveWeaponSlot
       |
       ├── WeaponCombatAdapter
       |       └── AttackController
       |
       ├── EquipmentVisualController
       |       └── Weapon Model
       |
       └── PlayerStatController
               └── RecalculateStats()
```

`ItemSwitching` tidak lagi memiliki array `Attacks` sebagai data utama. `Index` juga sebaiknya tidak disimpan secara independen; cukup membaca `ActiveWeaponSlot` dari EquipmentController.

`ItemSwitching` hanya menerjemahkan input 1/2 menjadi perintah pergantian.

## **7.4 Equipment Visual Controller**

Ini adalah sistem yang harus menggantikan Instantiate/Destroy weapon visual.

Susunan di Hierarchy:

```
Player
│
├── WeaponSocket
│   │
│   ├── WeaponSlot_01
│   │   └── KlewangVisual
│   │
│   └── WeaponSlot_02
│       └── WeaponVisual_02
│
└── ArmorSocket
    └── ArmorVisual
```

Semua object sudah dipasang di Editor.

Saat senjata berubah:

```
Active Slot = 1
    |
    ├── WeaponSlot_01.SetActive(true)
    └── WeaponSlot_02.SetActive(false)
```

Kemudian:

```
Active Slot = 2
    |
    ├── WeaponSlot_01.SetActive(false)
    └── WeaponSlot_02.SetActive(true)
```

Untuk versi demo, jangan membuat weapon model dari kode saat pergantian.

Pemisahan visual dan state juga sejalan dengan rekomendasi refactoring sebelumnya.&#x20;

Pasted text (2)(4).txt



# **8. ArtifactController & PlayerStatController**

Kedua class ini harus bekerja bersama, tetapi tidak saling mengambil alih.

## **8.1 ArtifactController**

Fungsi:

- Menyimpan artifact yang terpasang.
- Memeriksa slot artifact.
- Mengaktifkan modifier.
- Menonaktifkan modifier ketika dilepas.
- Mengirim perubahan kepada PlayerStatController.

Kontrak fungsi:

```
TryEquipArtifact(slot, artifact)
TryUnequipArtifact(slot)
GetActiveArtifacts()
GetArtifactModifiers()
```

Jangan membuat ArtifactController melakukan perhitungan Max HP atau damage sendiri.

## **8.2 PlayerStatController**

Tetapkan sebagai satu-satunya pusat perhitungan statistik akhir.

Arsitektur:

```
Base Player Stats
       |
       v
Permanent Upgrades
       |
       v
In-Run Progression
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
Temporary Effects
       |
       v
Final Player Stats
```

Ini mengembangkan rancangan PlayerStatController yang sudah ada dan menghindari setiap equipment menulis statistik secara langsung.&#x20;

Pasted text (2)(4).txt



### **Modifier System**

Gunakan `IStatModifier` yang sudah direncanakan.

Bentuk data:

```
StatModifier
    |
    ├── StatType
    ├── ModifierOperation
    ├── Value
    └── SourceId
```

Contoh operasi:

| Operation      | Fungsi                                             |
| -------------- | -------------------------------------------------- |
| Additive       | Menambah nilai                                     |
| Multiplicative | Mengalikan nilai                                   |
| Override       | Mengganti nilai, hanya bila benar-benar dibutuhkan |

Rumus konseptual:

\\[ FinalStat=(Base+\sum Additive)\times\prod Multipliers \\]

Gunakan urutan modifier yang konsisten dan tentukan batas nilai berdasarkan desain gameplay.

**Jangan mengubah ScriptableObject ketika modifier aktif.** Ambil nilai dasarnya, hitung hasil efektif, lalu simpan sebagai runtime data. Ini penting untuk menghindari perubahan asset permanen ketika artefak atau armor digunakan.&#x20;

Pasted text (2)(4).txt



# **BAGIAN IV — LOOT, REWARD, SAVE & PRESENTATION**

## **9. Loot Architecture**

Inventory tidak seharusnya mengetahui bagaimana item diperoleh.

Pemain bisa mendapatkan item melalui:

- Musuh yang dikalahkan.
- Peti atau objek loot.
- Hadiah encounter.
- Sistem reward lain yang mungkin ditambahkan.

Semua sumber tersebut harus menggunakan jalur penambahan item yang sama.

### **9.1 LootDefinitionSO**

Gunakan definisi loot yang berisi:

| Field           | Kegunaan            |
| --------------- | ------------------- |
| LootId          | Identitas           |
| RewardType      | Jenis reward        |
| ItemDefinition  | Referensi item      |
| Amount          | Jumlah              |
| PickupPrefab    | Visual pickup       |
| PickupCondition | Kondisi pengambilan |

Jenis reward:

```
LootRewardType
    |
    ├── Item
    ├── Currency
    └── Experience
```

Contoh:

```
Loot_Klewang
    RewardType = Item
    ItemDefinition = Weapon_Klewang
    Amount = 1
```

```
Loot_Souls
    RewardType = Currency
    Amount = 50
```

```
Loot_EXP
    RewardType = Experience
    Amount = 100
```

### **9.2 Pembagian sistem yang benar**

Kamu sudah mempunyai `RewardCollector`, `LootPickup`, `CurrencyWallet`, dan `ExperienceSystem`.

Jangan membuat sistem kedua yang memiliki fungsi sama.

Aku merekomendasikan:

| Sistem                | Tanggung jawab                  |
| --------------------- | ------------------------------- |
| `RewardCollector`     | Reward ketika musuh mati        |
| `LootDropController`  | Menentukan loot yang dijatuhkan |
| `LootDefinitionSO`    | Konfigurasi loot                |
| `LootPickup`          | Representasi pickup di dunia    |
| `LootCollector`       | Memproses pengambilan           |
| `InventoryController` | Menyimpan item                  |
| `CurrencyWallet`      | Menyimpan Souls                 |
| `ExperienceSystem`    | Mengelola EXP                   |

Alurnya:

```
Enemy Dies
    |
    v
RewardCollector
    |
    ├── EXP → ExperienceSystem
    |
    ├── Souls → CurrencyWallet
    |
    └── Optional Item Drop
              |
              v
       LootDropController
              |
              v
          LootPickup
              |
              v
         LootCollector
              |
              v
        Check RewardType
              |
       ┌──────┼────────┐
       v      v        v
   Inventory Wallet  Experience
```

Dengan arsitektur ini, pickup biasa dan reward musuh tidak harus mempunyai sistem pengumpulan berbeda.

### **9.3 Aturan penting**

1. Pickup tidak boleh memberikan reward dua kali.
2. Inventory penuh tidak boleh menyebabkan item hilang secara tidak sengaja.
3. Loot yang menggunakan pooling harus mengembalikan statusnya ketika diaktifkan ulang.
4. Pickup item harus melalui validasi Inventory.
5. Reward Souls dan EXP tidak dihitung sebagai slot Inventory.

Roadmap awal juga mengarahkan penggunaan satu pola pengumpulan umum dan memisahkan In-game EXP dari Souls Coin permanen. Beberapa aturan distribusi reward ketika pemain mati masih membutuhkan keputusan Project Lead.&#x20;

Pasted text(20261127-010202).txt



## **10. Save & Load Architecture**

Bagian ini sangat penting karena item dan equipment nantinya akan mempunyai hubungan langsung dengan progression permanen.

Kamu sudah mempunyai `SaveSystem` dengan JSON lokal `tls_save.json`.

Kita tidak perlu membuat save system baru. Kita perlu memperluas struktur datanya.

### **10.1 Jangan menyimpan ScriptableObject secara langsung**

Contoh yang tidak disarankan:

```
InventorySaveData
    Item = WeaponDefinitionSO object
```

Sebaliknya:

```
InventorySaveData
    ItemId = "weapon.klewang"
    Quantity = 1
```

Ketika load, `ItemCatalogSO` mencari item berdasarkan ID.

### **10.2 Struktur save yang direkomendasikan**

```
TLS_SaveData
│
├── SaveVersion
│
├── PlayerProgressionData
│   ├── PermanentLevel
│   └── PermanentUpgrades
│
├── CurrencyData
│   └── Souls
│
├── InventorySaveData
│   └── Items[]
│        ├── ItemId
│        └── Quantity
│
├── EquipmentSaveData
│   ├── WeaponSlot1
│   ├── WeaponSlot2
│   ├── ActiveWeaponSlot
│   ├── ArmorId
│   └── Artifacts[]
│
└── SettingsData
```

EXP yang berlaku selama satu run harus ditangani sesuai kebijakan run, bukan otomatis dianggap currency permanen.

### **10.3 Validasi ketika Load**

Urutan:

1. Baca JSON.
2. Periksa SaveVersion.
3. Validasi struktur data.
4. Cari setiap ItemId pada ItemCatalogSO.
5. Buang atau laporkan referensi item yang tidak ditemukan.
6. Pulihkan Inventory.
7. Pulihkan Equipment.
8. Validasi item yang terpasang benar-benar dimiliki.
9. Hitung ulang statistik.
10. Perbarui UI.

Jangan memulihkan `FinalStats` langsung dari save. FinalStats harus dihitung ulang dari konfigurasi dan modifier yang aktif.

### **10.4 Keputusan desain yang belum boleh diasumsikan**

Ada satu hal penting: apakah equipment dan item yang diperoleh selama run akan tetap dimiliki setelah run selesai?

Dokumen yang tersedia sudah membedakan EXP sementara dan Souls Coin permanen, tetapi belum menetapkan kebijakan persistensi seluruh jenis item.&#x20;

Pasted text(20261127-010202).txt



Karena itu, buat pembagian konsep:

| Data                          | Lingkup                                    |
| ----------------------------- | ------------------------------------------ |
| Souls Coin                    | Meta progression                           |
| In-Run EXP                    | Run                                        |
| Inventory Run                 | Kebijakan run yang perlu disahkan          |
| Equipment Run                 | Kebijakan run yang perlu disahkan          |
| Permanent Equipment Ownership | Meta progression bila memang menjadi fitur |

Jangan membuat sistem permanent inventory dan run inventory sekaligus sebelum desain game menetapkannya.

## **11. UI Architecture**

UI harus dibuat setelah kontrak data Inventory dan Equipment stabil.

Aku merekomendasikan tiga tampilan utama.

### **11.1 Inventory Screen**

[ArtStation - Dark Lands - RPG UI Kit | Game Assets | Game assets, Ui kit, Kit games](https://images.openai.com/static-rsc-4/chvaFAxRSyYgNi5r3Sz30SM2Qad8GOr6On9fVgpU23wPcTO6XxlkUdFGejddAFy3Jx0ZyrCgsMto-ZicHq2qTucMm_2tL7VeZKw7d7Sj5T9vlwmqfWqtzyj8fThH4XoDgSLGNbsCcChheApZgY_MCiHhjx-1ZAjl_-0iveVS_Zo?purpose=inline)

[pinterest.com](https://www.pinterest.com/pin/artstation-dark-lands-rpg-ui-kit--512425263864868670/?utm_source=chatgpt.com)

Isi:

- Grid 20 slot.
- Icon item.
- Quantity.
- Kategori.
- Penanda item yang sedang dipakai.
- Empty slot.
- Item information.

Filter:

```
All | Weapon | Armor | Artifact
```

Material bisa diaktifkan setelah kategori tersebut benar-benar digunakan.

### **11.2 Equipment Screen**

[ลองเล่น Blade of Gods II: Orisols บู๊เดือดโลกดาร์คแฟนตาซีปะทะบอสสุดมันส์ในสไตล์ดาร์คโซล : mustplay.in.th](https://images.openai.com/static-rsc-4/QtgxqHnPwMu0DSlOlY_Zts3JLh1rBdiMmNBPOga-KyNe7CPmqS2LvI2uxzhHnnn9FIfgm-q7oQI5gtYgL77oHXRV1MceetWNy2f21t7L7qC98IaTGJMz0Jm-glmIpQrg89zUrsY3CQhyCr_JX7sEckdLg0zhe2H4NzZf8SGmwbI?purpose=inline)

[mustplay.in.th](https://www.mustplay.in.th/content/page/63a6f1f5cbab9b9862a6c09c?utm_source=chatgpt.com)

Isi:

- Weapon Slot 1.
- Weapon Slot 2.
- Active Weapon.
- Armor Slot.
- Artifact Slot 1.
- Artifact Slot 2.

Ketika memilih item dari Inventory, UI menampilkan tindakan yang sesuai dengan kategori item.

Contoh:

```
Selected: Klewang

[Equip to Slot 1]
[Equip to Slot 2]
[Inspect]
```

Untuk armor:

```
Selected: Besi Ambat

[Equip]
[Inspect]
```

### **11.3 Item Detail & Stat Comparison**

[Tower of Babel: Survivors of Chaos game revenue and stats on Steam – Steam Marketing Tool](https://images.openai.com/static-rsc-4/NQmCjION3d-8_COnLGcMIu8yBE_dSj_HgaIyAUFSHoQGMDlCimXtWqV70PeZnx3lcQ5NdhZz-PMaXx3ItLKbK-lVeFwx1kMREoSWzhSbynltLJXxezqGcAQTs3K6wDSf6ycy4D8_oYPnZkA_VOo4vevBN9-yXV9E0xWZ7LM3NTE?purpose=inline)

[games-stats.com](https://games-stats.com/steam/game/tower-of-babel-survivors-of-chaos/?utm_source=chatgpt.com)

Ini bagian yang akan membuat sistem item terasa lebih profesional.

Ketika pemain memilih equipment, tampilkan:

| Informasi      | Contoh               |
| -------------- | -------------------- |
| Nama           | Klewang              |
| Kategori       | Weapon               |
| Description    | Deskripsi senjata    |
| Current Weapon | Senjata aktif        |
| New Weapon     | Senjata yang dipilih |
| Damage         | Nilai efektif        |
| Attack Speed   | Nilai efektif        |
| Modifier       | Perubahan atribut    |

Contoh preview:

```
CURRENT WEAPON       NEW WEAPON

Damage: 25           Damage: 32
Attack Speed: 1.2    Attack Speed: 0.9

Changes:
Damage        +7
Attack Speed  -0.3
```

Nilai harus berasal dari kalkulasi runtime stat, bukan angka yang ditulis ulang oleh UI.

### **11.4 UI tidak boleh menjadi tempat penyimpanan state**

Struktur:

```
InventoryUI
    |
    v
InventoryController
    |
    v
InventoryModel
```

Untuk Equipment:

```
EquipmentUI
    |
    v
EquipmentController
    |
    v
PlayerEquipmentState
```

UI hanya mengirim command dan mendengarkan perubahan.

Buat event untuk memperbarui tampilan setelah transaksi berhasil.

# **BAGIAN V — STRUKTUR FOLDER FINAL**

## **12. Target struktur Assets/\_Project**

Aku merekomendasikan struktur berikut. Ini pengembangan dari pemisahan folder Weapon, Armor, Artefak, dan ScriptableObjects di engineering roadmap sebelumnya.

```
Assets/
└── _Project/
    │
    ├── Scripts/
    │   │
    │   ├── Items/
    │   │   │
    │   │   ├── Definitions/
    │   │   │   ├── ItemDefinitionSO.cs
    │   │   │   ├── EquipmentDefinitionSO.cs
    │   │   │   ├── WeaponDefinitionSO.cs
    │   │   │   ├── ArmorDefinitionSO.cs
    │   │   │   ├── ArtifactDefinitionSO.cs
    │   │   │   └── ItemCategory.cs
    │   │   │
    │   │   ├── Catalog/
    │   │   │   └── ItemCatalogSO.cs
    │   │   │
    │   │   ├── Inventory/
    │   │   │   ├── Inventory.cs
    │   │   │   ├── InventoryModel.cs
    │   │   │   ├── InventorySlot.cs
    │   │   │   └── InventoryController.cs
    │   │   │
    │   │   ├── Equipment/
    │   │   │   ├── EquipmentController.cs
    │   │   │   ├── PlayerEquipmentState.cs
    │   │   │   ├── ItemSwitching.cs
    │   │   │   ├── EquipmentVisualController.cs
    │   │   │   └── WeaponCombatAdapter.cs
    │   │   │
    │   │   ├── Artifact/
    │   │   │   └── ArtifactController.cs
    │   │   │
    │   │   ├── Modifiers/
    │   │   │   ├── IStatModifier.cs
    │   │   │   ├── StatModifier.cs
    │   │   │   └── ModifierCalculator.cs
    │   │   │
    │   │   └── Loot/
    │   │       ├── LootDefinitionSO.cs
    │   │       ├── LootCollector.cs
    │   │       └── LootDropController.cs
    │   │
    │   ├── Player/
    │   │   └── Stats/
    │   │       └── PlayerStatController.cs
    │   │
    │   ├── Systems/
    │   │   ├── Progression/
    │   │   └── Save/
    │   │       ├── InventorySaveData.cs
    │   │       ├── EquipmentSaveData.cs
    │   │       └── SaveSystem.cs
    │   │
    │   └── UI/
    │       ├── Inventory/
    │       │   ├── InventoryUI.cs
    │       │   ├── InventorySlotUI.cs
    │       │   └── ItemDetailUI.cs
    │       │
    │       └── Equipment/
    │           ├── EquipmentUI.cs
    │           ├── EquipmentSlotUI.cs
    │           └── StatComparisonUI.cs
    │
    ├── ScriptableObjects/
    │   ├── Items/
    │   │   ├── Weapons/
    │   │   ├── Armors/
    │   │   ├── Artifacts/
    │   │   └── Materials/
    │   │
    │   ├── ItemCatalog/
    │   ├── Stats/
    │   └── Loot/
    │
    ├── Prefabs/
    │   ├── Items/
    │   │   └── Pickups/
    │   └── UI/
    │       ├── Inventory/
    │       └── Equipment/
    │
    └── Scenes/
        ├── CombatTest.unity
        ├── ForestRun.unity
        └── Hub.unity
```

**Catatan migrasi:** struktur tersebut adalah target organisasi. Jangan memindahkan seluruh script dan namespace sekaligus. Migrasikan berdasarkan fase supaya referensi Inspector dan prefab tidak rusak. `Inventory.cs` yang saat ini menjadi MonoBehaviour juga perlu ditetapkan apakah akan menjadi facade/controller atau diganti dengan komposisi `InventoryModel` secara bertahap.

# **BAGIAN VI — MASTER IMPLEMENTATION ROADMAP**

Ini bagian yang paling penting: urutan pengerjaan yang harus kamu ikuti.

Aku tidak menyarankan mengerjakan Inventory UI, puluhan item, atau efek visual lebih dahulu. Kita harus membangun data contract, kemudian transaction, baru presentation.

# **Development Sequence**

1. **Phase 0 — Item System Design Lock**

   Keputusan desain, kontrak data, dan audit sistem lama
2. **Phase 1 — Item Definition & Catalog**

   ScriptableObject dan database item
3. **Phase 2 — Equipment Core Refactoring**

   Single source of truth dan weapon switching
4. **Phase 3 — Inventory Core**

   Penyimpanan, slot, stack, dan transaksi
5. **Phase 4 — Equipment Integration**

   Equip, unequip, visual, dan combat adapter
6. **Phase 5 — Stat Modifier & Artifact**

   Armor, artifact, dan kalkulasi statistik
7. **Phase 6 — Loot & Reward Integration**

   Drop, pickup, dan distribusi reward
8. **Phase 7 — Persistence & Save/Load**

   Penyimpanan Inventory dan Equipment
9. **Phase 8 — Inventory & Equipment UI**

   Tampilan dan interaksi pemain
10. **Phase 9 — Validation, Polish & Domain Freeze**

    Testing, balancing, dan baseline release

## **PHASE 0 — Item System Design Lock**

P0 — Wajib

**Tujuan:** Menetapkan aturan item sebelum membuat kode dan asset baru.

### **Point 0.1 — Audit script yang sudah ada**

Periksa:

- `Inventory.cs`
- `InventoryItem.cs`
- `InventorySlot.cs`
- `EquipmentController.cs`
- `PlayerEquipmentState.cs`
- `ItemSwitching.cs`
- `WeaponDefinitionSO.cs`
- `ArmorDefinitionSO.cs`
- `ArtifactController.cs`
- `PlayerStatController.cs`
- `LootPickup.cs`
- `RewardCollector.cs`
- `SaveSystem.cs`

Tentukan script yang dipertahankan, diperbaiki, digabung, atau dihapus.

### **Point 0.2 — Tetapkan aturan item**

Keputusan awal yang aku rekomendasikan:

| Ketentuan           | Keputusan                             |
| ------------------- | ------------------------------------- |
| Inventory capacity  | 20 slot                               |
| Weapon slots        | 2                                     |
| Armor slots         | 1                                     |
| Artifact slots      | Maksimal 2, perlu persetujuan desain  |
| Weapon stack        | 1                                     |
| Armor stack         | 1                                     |
| Artifact stack      | 1                                     |
| Material stack      | Mengikuti MaxStackSize                |
| Equipment ownership | Tetap berada dalam Inventory          |
| Weapon switching    | Hanya ketika action state mengizinkan |
| Currency            | Terpisah dari Inventory               |
| EXP                 | Terpisah dari Inventory               |

### **Point 0.3 — Putuskan aturan persistence**

Buat keputusan Project Lead mengenai apakah equipment yang diperoleh ketika run berlangsung akan menjadi koleksi permanen atau hanya berlaku pada run tersebut.

Ini tidak boleh dibiarkan menjadi asumsi programmer.

**Output Phase 0:**

- `ItemSystemArchitecture.md`
- `ItemDataContract.md`
- `ItemRules.md`
- Daftar script yang dipertahankan dan dimigrasikan.

**Acceptance criteria:** Seluruh programmer mempunyai pemahaman yang sama tentang kepemilikan item, equipment, slot, dan alur reward.

## **PHASE 1 — Item Definition & Catalog**

P0 — Fondasi Data

**Tujuan:** Memastikan semua item mempunyai definisi yang konsisten.

### **Point 1.1 — Implementasi ItemDefinitionSO**

Buat identitas dasar seluruh item.

Validasi:

- ItemId tidak kosong.
- ItemId tidak duplikat.
- Nama tersedia.
- Icon tersedia untuk item yang ditampilkan UI.
- MaxStackSize tidak kurang dari satu.

### **Point 1.2 — Integrasikan WeaponDefinitionSO**

Audit field WeaponDefinitionSO yang sudah ada.

Pastikan:

- ComboChain terisi.
- Setiap attack valid.
- Modifier dapat dibaca.
- Weapon visual mempunyai identitas yang jelas.
- AnimatorOverride dapat kosong apabila memang tidak diperlukan.

### **Point 1.3 — Integrasikan ArmorDefinitionSO**

Buat satu konfigurasi armor pengujian.

Pastikan material visual dan modifier statistik dapat dibaca.

### **Point 1.4 — Implementasi ArtifactDefinitionSO**

Buat satu artefak terlebih dahulu.

Gunakan `IStatModifier`.

### **Point 1.5 — Implementasi ItemCatalogSO**

Masukkan seluruh item ke catalog.

Lakukan validasi seluruh ID.

### **Output asset Phase 1**

| Asset                | Jumlah                                       |
| -------------------- | -------------------------------------------- |
| WeaponDefinitionSO   | 2                                            |
| AttackDefinitionSO   | 6 jika menggunakan tiga serangan per senjata |
| ArmorDefinitionSO    | 1                                            |
| ArtifactDefinitionSO | 1                                            |
| ItemCatalogSO        | 1                                            |
| Player Base Stats    | 1                                            |

Komposisi tersebut merupakan baseline yang sejalan dengan rekomendasi konfigurasi Player Domain sebelumnya.&#x20;

Pasted text (2)(4).txt



**Acceptance criteria:** Seluruh asset bisa dibaca, tidak ada ID duplikat, dan setiap senjata memiliki konfigurasi serangan yang valid.

## **PHASE 2 — Equipment Core Refactoring**

P0 — Blocker

**Tujuan:** Menghilangkan dual-source state dan menjadikan EquipmentController sumber kebenaran.

### **Point 2.1 — Refactor PlayerEquipmentState**

Buat state yang jelas.

```
PlayerEquipmentState
    WeaponSlot1
    WeaponSlot2
    ActiveWeaponSlot
    EquippedArmor
```

Jangan mencampurkan struktur save di dalamnya.

### **Point 2.2 — Refactor EquipmentController**

Tanggung jawab utama:

- Mengatur slot.
- Memeriksa validasi.
- Menjalankan equip.
- Menjalankan unequip.
- Menjalankan weapon switching.
- Mengirim event.

Pastikan tidak ada perubahan statistik langsung di dalam logic serangan.

### **Point 2.3 — Refactor ItemSwitching**

Pertahankan nama `ItemSwitching` sesuai rancangan awal.

Namun, ubah menjadi input adapter.

Contoh alur:

```
PlayerInputReader
        |
        v
ItemSwitching
        |
        v
EquipmentController.TrySwitchWeaponSlot()
```

Yang harus dihilangkan secara bertahap:

- Kepemilikan `AttackDefinitionSO[] Attacks` sebagai loadout utama.
- `Index` sebagai state terpisah.
- Runtime fallback serangan yang menyembunyikan konfigurasi kosong.

### **Point 2.4 — Validasi Action State**

Periksa:

```
PlayerActionController.CanSwitchWeapon()
```

Pergantian weapon harus ditolak ketika action state tidak mengizinkan.

Untuk MVP, tidak perlu membuat sistem queued weapon switch.

### **Output Phase 2**

- Dua weapon slot dapat disimpan.
- ActiveWeapon dapat berubah.
- Tidak ada state ganda.
- Pergantian senjata mengikuti aturan PlayerActionController.

**Acceptance criteria:** Weapon Slot 1 dan 2 bekerja secara independen dan ActiveWeapon selalu sesuai dengan slot yang dipilih.

## **PHASE 3 — Inventory Core**

P0 — Core System

**Tujuan:** Membangun penyimpanan item yang benar-benar bisa digunakan.

### **Point 3.1 — Refactor InventoryItem**

Ganti struktur yang sekarang terlalu sederhana.

Jangan lagi mengandalkan:

```
Id
DisplayName
Souls
Exp
```

Gunakan referensi definisi item dan jumlah kepemilikan.

### **Point 3.2 — Implementasi InventorySlot**

Setiap slot menyimpan:

```
InventorySlot
    ItemDefinition
    Quantity
```

### **Point 3.3 — Implementasi InventoryModel**

Tanggung jawab:

- Menyimpan slot.
- Menghitung kapasitas.
- Menemukan item.
- Mengatur stack.
- Menghitung total item.

### **Point 3.4 — Implementasi InventoryController**

Implementasikan operasi:

1. `CanAddItem`
2. `TryAddItem`
3. `TryRemoveItem`
4. `FindItem`
5. `HasItem`
6. `GetItemCount`
7. `GetSlots`

Tambahkan event perubahan inventory.

### **Point 3.5 — Inventory Unit Testing**

Uji:

- Menambahkan item baru.
- Menambahkan item yang sudah ada.
- Stack mencapai MaxStackSize.
- Inventory hampir penuh.
- Inventory benar-benar penuh.
- Menghapus sebagian jumlah.
- Menghapus seluruh jumlah.
- Item tidak ditemukan.
- Item null.

### **Output Phase 3**

**Inventory Backend v1.0**

Sudah mampu menyimpan berbagai jenis item tanpa bergantung pada UI.

**Acceptance criteria:** Semua operasi Inventory konsisten, tidak ada item hilang atau terduplikasi akibat transaksi.

## **PHASE 4 — Equipment Integration & Visual Management**

P0 — Core Gameplay

**Tujuan:** Menghubungkan Inventory dengan Equipment dan Combat.

### **Point 4.1 — Equip Transaction**

Buat alur:

```
Inventory
    |
    v
Select Item
    |
    v
Check Ownership
    |
    v
Check Item Category
    |
    v
Validate Equipment Slot
    |
    v
Check Player Action State
    |
    v
Apply Equipment
    |
    v
Notify Systems
```

Jika salah satu validasi gagal, equipment tidak boleh berubah.

### **Point 4.2 — Weapon Combat Adapter**

Pastikan pergantian weapon memperbarui:

- AttackController.Definition atau loadout aktif.
- ComboChain.
- AnimatorOverride.
- Weapon-specific combat modifiers.

Jangan sampai visual dan combat mempunyai referensi senjata berbeda.

### **Point 4.3 — Equipment Visual Controller**

Pasang kedua weapon visual pada scene Player.

Jangan membuat GameObject cadangan melalui `Instantiate`.

### **Point 4.4 — Armor Visual**

Implementasikan pergantian armor menggunakan referensi Renderer dan material.

Jika model armor berbeda secara keseluruhan, gunakan child visual yang telah disiapkan untuk konfigurasi demo.

### **Point 4.5 — Equipment Event**

Rekomendasi event:

```
OnWeaponEquipped
OnWeaponSwitched
OnArmorEquipped
OnEquipmentChanged
```

Pastikan subscriber dilepas dengan benar ketika object dinonaktifkan atau dihancurkan.

### **Output Phase 4**

- Equip dan unequip weapon.
- Switching weapon.
- Equip dan unequip armor.
- Visual sesuai equipment.
- Attack combo sesuai weapon aktif.
- Semua perubahan mengikuti validasi action state.

## **PHASE 5 — Stat Modifier, Armor & Artifact**

P0 — Integrasi Statistik

**Tujuan:** Membuat semua equipment memberikan pengaruh statistik melalui satu jalur.

### **Point 5.1 — Audit PlayerStatController**

Pastikan ia menjadi pusat perhitungan FinalStats.

Jangan sampai `ArmorDefinitionSO`, `ArtifactController`, dan `EquipmentController` masing-masing mengubah Max HP.

### **Point 5.2 — Implementasi Modifier Calculator**

Pisahkan:

- Base value.
- Additive modifier.
- Multiplicative modifier.
- Final value.

Semua harus memiliki urutan perhitungan deterministik.

### **Point 5.3 — Armor Modifier**

Implementasikan satu armor dengan beberapa modifier yang benar-benar didukung sistem statistik.

Uji DamageReduction, MaxHealth, PoiseResistance, dan KnockbackResistance apabila atributnya sudah tersedia.

### **Point 5.4 — ArtifactController**

Tambahkan slot artifact.

Uji:

- Equip artifact.
- Unequip artifact.
- Modifier aktif.
- Modifier dilepas.
- Artifact tidak menggandakan efek.
- Dua artifact berbeda bisa berinteraksi dengan benar.

### **Point 5.5 — Health Recalculation Policy**

Tentukan kebijakan perubahan Max HP.

Misalnya:

Player HP = 50/100.

Armor meningkatkan Max HP menjadi 120.

Perubahan Max HP tidak boleh otomatis menjadi full heal kecuali desain upgrade memang menginginkannya.

### **Output Phase 5**

**Equipment & Modifier System v1.0**

Seluruh weapon, armor, dan artifact menggunakan satu jalur kalkulasi statistik.

## **PHASE 6 — Loot & Reward Integration**

P1 — World Integration

**Tujuan:** Menghubungkan item dengan dunia permainan.

### **Point 6.1 — Audit LootPickup**

Pertahankan pickup yang sudah dipasang di CombatTest.

Pastikan pickup dapat menerima `LootDefinitionSO`.

### **Point 6.2 — Implementasi LootDefinitionSO**

Buat beberapa konfigurasi:

- Item pickup.
- Currency pickup.
- EXP pickup.

### **Point 6.3 — LootCollector**

Hubungkan dengan:

- PlayerInteract.
- InventoryController.
- CurrencyWallet.
- ExperienceSystem.

### **Point 6.4 — RewardCollector Integration**

Pastikan reward saat musuh mati tidak masuk dua kali.

Pisahkan reward langsung dengan item drop.

### **Point 6.5 — Pooling**

Apabila pickup dijatuhkan secara dinamis, gunakan prefab yang memang sudah disiapkan untuk pooling.

Pastikan state pickup kembali bersih ketika digunakan ulang.

### **Output Phase 6**

Pemain bisa mengalahkan musuh, mendapatkan reward, mengambil item, dan menyimpannya dalam Inventory.

## **PHASE 7 — Save & Load**

P1 — Persistence

**Tujuan:** Menjaga kepemilikan item dan equipment setelah permainan dimuat ulang.

### **Point 7.1 — InventorySaveData**

Simpan:

```
ItemId
Quantity
```

### **Point 7.2 — EquipmentSaveData**

Simpan:

```
WeaponSlot1
WeaponSlot2
ActiveWeaponSlot
ArmorId
ArtifactIds
```

### **Point 7.3 — Integrasi ItemCatalogSO**

Semua ID harus bisa di-resolve kembali.

### **Point 7.4 — Save Versioning**

Tambahkan:

```
SaveVersion
```

Siapkan penanganan ketika struktur save berubah pada versi berikutnya.

### **Point 7.5 — Load Validation**

Jalankan validasi setelah semua data dipulihkan.

Jangan mengaktifkan equipment yang sudah tidak tersedia atau tidak dimiliki.

**Output Phase 7:** Inventory dan Equipment dapat dipulihkan dengan benar menggunakan sistem JSON yang sudah dimiliki.

## **PHASE 8 — Inventory & Equipment UI**

P1 — User Experience

**Tujuan:** Membuat sistem item nyaman digunakan dan memberikan informasi gameplay yang jelas.

Urutan pengerjaan:

| Urutan | Komponen            | Fungsi                       |
| ------ | ------------------- | ---------------------------- |
| 1      | InventoryUI         | Grid inventory               |
| 2      | InventorySlotUI     | Icon, quantity, selection    |
| 3      | ItemDetailUI        | Deskripsi item               |
| 4      | EquipmentUI         | Slot perlengkapan            |
| 5      | EquipmentSlotUI     | Tampilan item terpasang      |
| 6      | StatComparisonUI    | Perbandingan statistik       |
| 7      | ItemCategoryFilter  | Filter kategori              |
| 8      | InventoryFeedbackUI | Pesan penuh, berhasil, gagal |

Jangan langsung membuat animasi UI rumit.

Prioritasnya adalah fungsi, keterbacaan, dan konsistensi data.

Setelah itu tambahkan:

- Highlight item.
- Equipped indicator.
- Tooltip.
- Selection animation.
- Controller/gamepad navigation.
- Sound feedback.

**Output Phase 8:** Seluruh sistem item dapat digunakan pemain tanpa mengandalkan Inspector atau debug command.

## **PHASE 9 — Validation, Polish & Domain Freeze**

Final Milestone

**Tujuan:** Menyelesaikan integrasi dan memastikan tidak ada masalah yang tersembunyi.

Lakukan pengujian terpadu di CombatTest, kemudian ForestRun dan Hub.

# **BAGIAN VII — TEST PLAN**

## **13. Item & Inventory Testing**

| ID  | Pengujian           | Expected Result                |
| --- | ------------------- | ------------------------------ |
| I01 | Add Item            | Item berhasil masuk            |
| I02 | Stack Item          | Quantity bertambah             |
| I03 | Max Stack           | Slot baru digunakan            |
| I04 | Inventory Full      | Penambahan ditolak dengan aman |
| I05 | Remove Item         | Quantity berkurang benar       |
| I06 | Invalid Item        | Item null/invalid ditolak      |
| I07 | Duplicate ID        | Catalog mendeteksi duplikasi   |
| I08 | Currency Separation | Souls tidak masuk Inventory    |
| I09 | EXP Separation      | EXP tidak masuk Inventory      |

## **14. Equipment Testing**

| ID  | Pengujian              | Expected Result               |
| --- | ---------------------- | ----------------------------- |
| E01 | Equip Weapon 1         | Weapon 1 aktif                |
| E02 | Equip Weapon 2         | Weapon 2 aktif                |
| E03 | Switch Weapon          | Aktif berpindah               |
| E04 | Weapon Visual          | Model sesuai                  |
| E05 | Attack Definition      | ComboChain sesuai             |
| E06 | Animator               | Override sesuai               |
| E07 | Switch During Attack   | Mengikuti aturan action state |
| E08 | Equip Unowned Item     | Ditolak                       |
| E09 | Invalid Equipment Type | Ditolak                       |
| E10 | Armor Equip            | Material/visual sesuai        |

## **15. Stat & Artifact Testing**

| ID  | Pengujian          | Expected Result                |
| --- | ------------------ | ------------------------------ |
| S01 | Base Stats         | Nilai dasar benar              |
| S02 | Weapon Modifier    | Statistik berubah              |
| S03 | Armor Modifier     | Statistik berubah              |
| S04 | Artifact Modifier  | Efek pasif aktif               |
| S05 | Unequip Artifact   | Modifier dihapus               |
| S06 | Multiple Modifiers | Perhitungan deterministik      |
| S07 | Max HP Change      | HP mengikuti policy            |
| S08 | Repeated Equip     | Modifier tidak terduplikasi    |
| S09 | SO Integrity       | Asset tidak berubah ketika run |

## **16. Loot & Persistence Testing**

| ID  | Pengujian             | Expected Result                   |
| --- | --------------------- | --------------------------------- |
| L01 | Item Pickup           | Item masuk Inventory              |
| L02 | Full Inventory Pickup | Tidak terjadi kehilangan item     |
| L03 | Currency Pickup       | Souls bertambah                   |
| L04 | EXP Pickup            | EXP bertambah                     |
| L05 | Repeated Pickup       | Tidak ada double reward           |
| L06 | Pooled Pickup         | State reset dengan benar          |
| P01 | Save Inventory        | Semua slot tersimpan              |
| P02 | Load Inventory        | Semua item pulih                  |
| P03 | Save Equipment        | Semua slot tersimpan              |
| P04 | Load Equipment        | Equipment pulih                   |
| P05 | Invalid Item ID       | Ditangani aman                    |
| P06 | Version Migration     | Save lama ditangani sesuai aturan |

**Syarat domain freeze:** Tidak cukup hanya semua script berhasil compile. Semua test di atas harus selesai dan lulus, termasuk pengujian dari build Windows.

# **BAGIAN VIII — HASIL AKHIR YANG HARUS DICAPAI**

## **17. Definisi selesai Item Domain v1.0**

## **Item & Equipment Domain v1.0**

Target Final

**Content**

- Dua weapon dengan moveset berbeda.
- Satu armor untuk validasi.
- Satu sampai dua artifact.
- Item catalog dengan identitas unik.

**Inventory**

- Kapasitas 20 slot.
- Stacking.
- Add/Remove.
- Validasi kepemilikan.
- Inventory UI.

**Equipment**

- Weapon Slot 1/2.
- Active Weapon.
- Armor Slot.
- Artifact Slots.
- Visual dan combat tersinkronisasi.

**Progression**

- Modifier system.
- Armor stat.
- Artifact passive.
- PlayerStatController integration.

**World Integration**

- Loot pickup.
- Reward distribution.
- Inventory collection.
- Pooling lifecycle.

**Persistence**

- Inventory Save/Load.
- Equipment Save/Load.
- Item ID resolution.
- Save versioning.

**Quality**

- Automated tests.
- Play Mode validation.
- UI consistency.
- No duplicate state.
- No unintended asset mutation.

## **18. Implementasi setelah v1.0**

Ada beberapa fitur yang sebaiknya belum dikerjakan sekarang.

| Fitur                    | Prioritas                           |
| ------------------------ | ----------------------------------- |
| Weapon Upgrade           | Setelah equipment stabil            |
| Armor Upgrade            | Setelah stat modifier stabil        |
| Item Rarity              | Setelah loot table disepakati       |
| Randomized Item Stats    | Setelah instance system dibutuhkan  |
| Weapon Durability        | Jika ditetapkan GDD                 |
| Crafting                 | Belum perlu                         |
| Shop & Trading           | Integrasikan setelah Hub economy    |
| Consumable System        | Setelah kebutuhan gameplay disahkan |
| Item Set Bonus           | Pengembangan berikutnya             |
| Equipment Skill Modifier | Setelah core equipment stabil       |

Kita tidak perlu membangun semuanya sekaligus.

# **BAGIAN IX — URUTAN PEKERJAAN YANG HARUS KAMU MULAI SEKARANG**

Aku ingin memperjelas hal terpenting: **jangan mulai dari UI dan jangan langsung membuat banyak ScriptableObject.**

Mulai dari refactoring data dan kepemilikan state.

Berikut pekerjaan nyata yang harus dikerjakan secara berurutan.

**Implementation Checklist**

0/14 selesai

**1. Audit seluruh script Inventory dan Equipment**

Periksa field, references, dependencies, dan penggunaan setiap class.

**2. Tetapkan ItemSystemArchitecture.md**

Dokumentasikan kategori, slot, stacking, ownership, dan persistence policy.

**3. Desain ItemDefinitionSO dan ItemCatalogSO**

Pastikan kontrak data sudah disetujui sebelum membuat banyak asset.

**4. Refactor PlayerEquipmentState**

Jadikan state weapon dan armor sebagai satu sumber data.

**5. Refactor ItemSwitching**

Hapus kepemilikan loadout ganda dan arahkan semua switching ke EquipmentController.

**6. Rapikan EquipmentController**

Pisahkan pengelolaan state, visual, dan integrasi combat.

**7. Implementasikan InventoryModel dan InventorySlot**

Bangun penyimpanan slot dan stacking.

**8. Implementasikan InventoryController**

Tambahkan transaksi Add, Remove, dan validasi.

**9. Integrasikan Equipment dengan Inventory**

Validasi ownership sebelum equip dan unequip.

**10. Integrasikan PlayerStatController dan ArtifactController**

Pastikan semua modifier dihitung melalui jalur tunggal.

**11. Integrasikan LootPickup dan SaveSystem**

Hubungkan perolehan item, wallet, EXP, dan penyimpanan.

**12. Bangun Inventory UI dan Equipment UI**

Mulai dari fungsi utama, kemudian visual polish.

**13. Jalankan seluruh test plan**

Uji Inventory, Equipment, Stats, Loot, dan Persistence.

**14. Freeze Item & Equipment Domain v1.0**

Dokumentasikan API, konfigurasi, hasil pengujian, dan known issues.

Reset checklist

## **Rekomendasi final**

Ada tiga hal yang menurutku paling penting untuk kualitas engineering proyek ini.

**Pertama, jangan membiarkan ItemSwitching dan EquipmentController mempunyai dua sumber state.** Bereskan itu sebelum membuat arsenal senjata lebih banyak.

**Kedua, jangan mencampurkan Inventory dengan Resource System.** Souls, EXP, item, dan equipment memiliki siklus hidup yang berbeda. Pisahkan penyimpanan dan tanggung jawabnya.

**Ketiga, jadikan PlayerStatController sebagai pusat seluruh perubahan statistik.** Weapon, armor, artifact, progression, dan permanent upgrades harus berkontribusi melalui sistem modifier yang konsisten.

Dengan demikian, ketika nantinya Kasantara menambahkan weapon baru, armor baru, atau artifact baru, programmer tidak perlu membuat mekanisme gameplay baru untuk setiap item.

Cukup membuat definisi data, menentukan modifier, memasang visual, dan memastikan konfigurasi lolos validasi.

**Urutan milestone besar yang aku rekomendasikan:**

```
Player Domain Stabilization
          |
          v
Item & Equipment Domain
          |
          v
Loot & Reward Integration
          |
          v
Inventory & Equipment UI
          |
          v
Domain Validation & Freeze
          |
          v
Gameplay Vertical Slice
          |
          v
Further Item Content & Progression
```

Target kita bukan sekadar memiliki Inventory yang bisa menyimpan item. Targetnya adalah membangun **ekosistem item yang extensible, konsisten, data-driven, dan tidak mengganggu fondasi combat The Last Sasak**.