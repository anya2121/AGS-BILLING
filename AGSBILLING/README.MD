# 🎮 AGS-Billing

Aplikasi desktop billing untuk usaha rental PlayStation, dibuat untuk mata kuliah Algoritma dan Pemrograman. Aplikasi ini menghitung biaya sewa per unit, mencatat pesanan makanan dan minuman, menyimpan riwayat transaksi, serta dapat menyalakan dan mematikan unit secara otomatis melalui perangkat keras yang terhubung ke port serial.

<!-- Tambahkan screenshot di sini, misalnya:
![Login](docs/login.png)
![Billing](docs/billing.png)
-->

## Fitur

**Login dengan dua peran:** Admin dan Staff.

**Staff**
- Memantau status semua unit (aktif atau idle) secara real-time
- Memulai dan menghentikan sesi bermain dengan tiga jenis tarif:
  - **Prabayar:** dihitung per jam
  - **Paket:** harga tetap untuk durasi tertentu, misalnya paket 3 jam
  - **Pascabayar:** dihitung per jam berdasarkan lama bermain
- Menambahkan pesanan makanan dan minuman ke sesi, dengan pengecekan stok
- Memindahkan sesi dari satu unit ke unit lain
- Mengirim perintah `ON:<unit>` dan `OFF:<unit>` ke perangkat keras (misalnya mikrokontroler dengan relay) melalui port serial

**Admin**
- Mengelola akun pengguna
- Mengelola stok dan harga makanan dan minuman
- Mengelola paket dan tarif sewa

Setiap sesi yang selesai dicatat ke riwayat transaksi, berisi waktu, unit, staff, durasi, biaya sewa, dan total makanan dan minuman.

## Teknologi

| Bagian | Teknologi |
|---|---|
| Bahasa | C# |
| Framework | .NET 8, Windows Forms |
| Komunikasi perangkat keras | `System.IO.Ports` (serial port) |
| Penyimpanan data | File CSV |

## Struktur Folder

```
AGSBILLING/
├── AGSBILLING.sln
└── AGSBILLING/
    ├── AGSBilling.cs        # seluruh kode aplikasi: model data, penyimpanan, serial port, dan tampilan
    └── data/
        ├── Users.csv        # akun pengguna
        ├── FnbStock.csv     # stok makanan dan minuman
        ├── Prices.csv       # paket dan tarif sewa
        └── Transactions.csv # riwayat transaksi
```

## Menjalankan

1. Buka `AGSBILLING/AGSBILLING.sln` di Visual Studio 2022 dengan workload **.NET desktop development**.
2. Klik **Start** atau tekan F5.
3. Login menggunakan akun yang ada di `data/Users.csv`.

Fitur serial port bersifat opsional. Aplikasi tetap bisa dipakai untuk menghitung billing tanpa perangkat keras yang terhubung.

## Pembuat

**Benedikta Anyarita Pratiwi**
Mahasiswi Teknologi Informasi, Universitas Negeri Yogyakarta
[LinkedIn](https://www.linkedin.com/in/benedikta-anyarita-pratiwi/) | [Portofolio](https://portofolio-anya-pi.vercel.app/)
