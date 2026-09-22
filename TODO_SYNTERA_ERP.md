# TODO List — Syntera ERP (per 8 Agustus 2026, updated)

## Status & Backlog — update 22 Sep 2026

### Selesai & sudah di-mainline (development, sebagian sudah sync ke production-demo)
- Invoice Termin (QuotationTermin/SalesOrderTermin, gating invoice per termin, DP sebagai termin pertama)
- Fix RecordDownPaymentAsync — cap validation sekarang ikut hitung Invoice existing
- Rename label "Supplier Invoice" -> "Bill" (tampilan)
- Rename tombol "Submit Penawaran" -> "Simpan Penawaran" + redirect ke Riwayat Penawaran setelah simpan
- Hide tombol Template (belum fungsional, di-flag TEMPLATE_FEATURE_ENABLED)
- Fitur Civil ME: field header baru (Facility ID, Renov PIC, Facility Name, Scope of Work, Location, Contractor, Validity Period, Area Block Tender), hapus field "Cabang" mati
- Fitur Civil ME: Total kategori sekarang menghitung FinalSellingPrice + QuotationItem + WorkDetail (sebelumnya cuma FinalSellingPrice) - fix bug nyata + fix bug lama duplikat Items.Add di UpsertGroups
- Fitur Civil ME: BOQ digabung (WorkDetail + QuotationItem tampil 1 tabel, tanpa label sumber - untuk kasus material dari maincon)
- Fitur Civil ME: PDF direstrukturisasi dari 3 halaman jadi 2 (Summary+tanda tangan digabung, BOQ+subtotal kategori)
- Fitur Civil ME: modal "Detail RAB/BQ" full-window, tombol dipindah ke sebelah dropdown Subkontraktor, header tabel & styling tombol diperbaiki
- Copy WorkItems/WorkDetails saat Duplicate/CreateRevision quotation (sebelumnya hilang total)

### Sedang dikerjakan
- Task #1: hilangkan gating "simpan dulu" untuk isi Detail RAB/BQ (WorkItem/WorkDetail local-state-first, upsert-by-Id 2 level) - backend+frontend sudah commit. Catatan: upload gambar tetap butuh quotation tersimpan (batasan permanen, bukan bug).

### Backlog terbuka (belum dikerjakan)
- Bug risk: RecalcTotals tidak jalan otomatis kalau WorkItem/WorkDetail diubah lewat endpoint standalone (bukan UpdateAsync penuh)
- Bug: Role/User HasData() pakai DateTimeOffset.UtcNow literal - timestamp ke-reset tiap migration baru (kelas bug sama seperti insiden NumberingConfig lama)
- Tech debt: test gagal nondeterministic karena SynteraERP_Scratch dipakai bareng banyak test class tanpa isolasi migration (race condition)
- Tidak ada validasi cegah double-invoice untuk SO yang sama
- InvoiceController.RecordPayment tidak ada [RequirePermission]
- Template CSV mutasi bank (download) - format: Tanggal,Keterangan,Debit,Kredit, yyyy-MM-dd
- Syarat dokumen sebelum buat Bill (PO/DO/Faktur Pajak/Invoice)
- Dashboard rekapitulasi AR/AP bulanan
- Rekapitulasi PPN Masukan/Keluaran + Kurang Bayar
- Dokumentasi stale soal bank/page.tsx
- AI Q&A data (Gemini function-calling) - sengaja ditunda
- TOCTOU race condition di semua cap-check pembayaran (tanpa isolation level/lock)
- Rekonsiliasi Bank versi "match ke Invoice, auto-lunas" - metode belum diputuskan

### Keputusan desain yang sudah disepakati (untuk konteks, jangan ditanya ulang)
- RAB Civil ME: harga per baris BOQ selalu 1 kolom blended (bukan split Jasa/Material), diisi manual (transkrip dari PDF subcon/supplier)
- Kategori "material dari maincon" pakai tabel QuotationItem yang sama, digabung ke BOQ tanpa label sumber - client tidak boleh tahu ada subcon di baliknya
- GroupSubconPanel (Harga Beli/Harga Jual) tetap lump-sum manual terpisah, tidak diubah

## ✅ Selesai Sejak Update Terakhir

1. **CompanySettings wiring lengkap** — form Company Profile sekarang wired penuh ke backend (GET/PUT), termasuk field NPWP, Logo (upload/download/delete), BankName/BankAccountNumber/BankAccountHolderName, dan validasi (Required/EmailAddress/StringLength di backend + guard eksplisit di service + validasi real-time di frontend). Teruji end-to-end via WebApplicationFactory+SQLite (21 pengecekan lolos). Ter-commit (backend `11b3072`, `1ab475c`; frontend `d7f5388`), ter-apply ke database development.

2. **DocumentPrefix** — field manual di Company Settings untuk white-label document numbering, TIDAK mengubah NumberingConfig existing, hanya mempengaruhi DocType baru. Ter-commit, ter-apply.

3. **Regenerate Prefix untuk Dokumen Baru** — endpoint + tombol UI terpisah (dengan dialog konfirmasi eksplisit) untuk menerapkan DocumentPrefix baru ke SEMUA NumberingConfig existing, TANPA mengubah LastNumber (histori nomor dokumen lama tetap utuh). Format: `{KodeJenisDokumen}.{DocumentPrefix}-{tahun}.{nomor}` (contoh: `Q.SYN-26.0151` → `Q.Aruna-26.0152` untuk dokumen baru, dokumen lama tidak berubah). Diverifikasi user langsung di browser — histori lama utuh, dokumen baru pakai prefix baru, nomor lanjut benar. Ter-commit (backend `7b6c9ca`).

4. **Branding dinamis (White-Label)** — hardcode "SynteraERP" diganti jadi dinamis dari CompanySettings.CompanyName + Logo di: Sidebar, breadcrumb (dipusatkan di AppLayout.tsx, 56 file lama dibersihkan), halaman Login (heading+footer), tab browser title. Endpoint baru `/company-settings/public` (AllowAnonymous, cuma expose companyName+hasLogo) untuk kebutuhan halaman sebelum login. Bonus: ditemukan & diperbaiki bug lama `AppImage.tsx` (tidak sync src saat prop berubah setelah mount). Diverifikasi user langsung di browser (Sidebar, breadcrumb, Login, PDF Quotation semua tampil "PT. Aruna" dengan benar). Ter-commit (frontend `5243737`).

5. **Branch/Cabang dibangun dari nol** — sebelumnya 100% mock (hardcode array, tombol tanpa handler, backend nol total). Sekarang: Model+Migration+DTO+Service+Controller CRUD lengkap dengan validasi Required/StringLength sejak awal, soft-delete via IsDeleted, Code auto-generate (BR0001 dst). Frontend wired penuh (modal Create/Edit via ERPModal, konfirmasi hapus via ConfirmModal). Ter-commit (backend `6a2ed4c`, frontend `7ff2f7a`), ter-apply ke database development. Catatan gap terpisah: field "Cabang" di form Quotation (BRANCH_OPTIONS hardcode) BELUM terhubung ke entity Branch ini — dicatat di Known Gaps, belum dikerjakan.

6. **Seed data dummy Supplier** — 6 supplier dummy ditambahkan (Network/CCTV/Fiber/Server/UPS/ATK) untuk mengisi dropdown modal "Buat Purchase Order" yang sebelumnya kosong. Idempoten per-item (aman berdampingan dengan data manual user). Ter-commit (`ce28444`).

7. **Sweep branding "Syntera" menyeluruh** — ditemukan tersebar di PDF (8 titik fallback+footer di 4 PdfService), login page (default value+placeholder), bank/page.tsx (3 akun mock), dead code SettingsModule.tsx (dihapus), dan 4 fallback text runtime+statis ("ERP System"). Diuji dengan network throttling untuk membuktikan fallback benar-benar netral. Swagger title juga diganti. Grep akhir 0 hit di kedua repo (exclude skip list: namespace/JWT config/seed default row/localStorage keys). Ter-commit (backend `6c94462`, frontend `63969a9`).

8. **UI kecil: reposisi "+ Tambah Baris"** di CostingTable — dipindah dari header kategori ke atas baris Subtotal (lebih mudah dipakai), sekaligus fix numbering yang sekarang dihitung live (bukan field statis) sehingga otomatis benar setelah delete baris. Diuji dengan 2 kategori, targeting benar, renumbering benar. Ter-commit (`663edad`).

9. **UI kecil: hapus badge kosong + footer Sidebar dinamis** — badge angka "3" di menu Penawaran dihapus (field tidak pernah di-set, selalu falsy). Footer Sidebar (nama+role) sekarang dinamis dari user yang login, bukan hardcode "Budi Santoso". Sudah masuk commit `63969a9`.

10. **Pembersihan data test CurrencyInput bug** — TIDAK RELEVAN LAGI. Ternyata database development yang dipakai sekarang tetap sama (via SSH tunnel ke server remote, bukan lokal), tapi setelah investigasi ulang tidak ditemukan kerusakan data yang perlu dibersihkan pada pengecekan terakhir — dianggap tuntas.

11. **Fix AR filter** (Amount > Paid di FinanceController) — SUDAH di-commit sejak awal (`5794938`), diverifikasi ulang masih utuh di source code setelah pindah laptop. Tidak ada tindakan lanjutan.

**Catatan operasional:** koneksi database development bergantung pada SSH tunnel aktif ke `localhost:1433`. Kalau tunnel putus/laptop restart, `dotnet ef`/`dotnet run` akan gagal connect sampai tunnel dibuka ulang — bukan bug, cuma dependency operasional yang perlu diingat sebelum mulai kerja di sesi berikutnya.

**⚠️ Catatan keamanan — RESET_ALL (System Administration):** fitur ini sudah tercatat sebagai celah keamanan sejak awal proyek (hard delete sungguhan, hanya butuh login tanpa pembatasan role). User sempat memakainya sendiri untuk keperluan testing (development, aman). JANGAN PERNAH pakai fitur ini di environment yang sudah berisi data produksi nyata setelah aplikasi ini dijual ke customer — siapapun yang login bisa memicu penghapusan total tanpa batasan role. Perlu ditambah role-check sebelum go-live (belum ada di antrian resmi, tambahkan kalau belum tercatat).

## 🟡 Antrian Jangka Panjang (Accounting/GL) — urutan prioritas

1. **Down Payment dari Customer** — 🔄 SEDANG BERJALAN. Investigasi Langkah 0 selesai (ground truth AR flow, Chart of Accounts, Journal Posting, analog POPayment). Opsi C dikonfirmasi (SalesOrderPayment + DownPaymentApplication bridge table, reuse mesin Invoice.Paid/Balance, akun baru 2-4000 Uang Muka Pelanggan). Menunggu laporan Langkah 0 tambahan (SupplierInvoicePayment detail, PaymentMethod reuse, validasi SalesOrderService) sebelum lanjut implementasi detail.
2. Retention / Termin Pembayaran Proyek (didesain bersamaan dengan Revenue Recognition)
3. Revenue Recognition — Percentage of Completion
4. Segregation of Duties (Journal Entry Manual)
5. Rekonsiliasi Bank
6. Down Payment ke Supplier
7. Credit Note / Debit Note
8. Cash Flow Statement & Cash/Bank Enhancement
9. Fixed Asset Register
10. Period Closing / Lock Tanggal Buku (penutup, wajib disambungkan ke baris "Laba Rugi Berjalan" di Neraca)

## 🟢 Gap Kecil yang Sudah Dicatat (tidak mendesak, tercatat di Known Gaps)

- Frontend Form Create SupplierInvoice belum ada (backend siap sejak Fase 4)
- Gating tidak konsisten antar 2 modal Record Payment Invoice (list mengizinkan bayar Draft, detail tidak)
- Duplikasi 2 implementasi modal Record Payment Invoice (harusnya 1 komponen shared, bukan ditulis 2x)
- Risiko HasData literal untuk CompanySettings/TaxRate (kalau developer edit literal seed di masa depan)
- Tidak ada pencegahan sistemik (lint rule) untuk mencegah bug CurrencyInput berulang — sudah 3x insiden serupa (RecordPoModal, CostingTable, Record Payment Invoice x2 + POPayment + 4 lokasi lain)
- Tidak ada transisi status Invoice Draft→Sent (endpoint tidak ada, tombol UI tidak ada)
- Item PO tanpa ItemMasterId tidak masuk perhitungan GRNI/Persediaan
- Validasi status PATCH manual (PurchaseRequestService.UpdateStatusAsync) tidak ketat

## ⏸️ Ditunda (keputusan bisnis, dipikirkan lagi nanti)

- Integrasi AI Agent — beberapa opsi dibahas (asisten query/laporan, OCR dokumen, deteksi anomali, chat in-app, kategorisasi otomatis expense) — belum diputuskan use case mana yang diprioritaskan, dan model biaya API (siapa yang bayar).

## 📌 Keputusan Arsitektur Penting yang Sudah Ditetapkan

- Model deployment: **1 deployment = 1 customer** (white-label per-instalasi, BUKAN multi-tenant SaaS)
- Prefix dokumen ("SYN") akan dibuat **configurable manual** oleh customer via field `DocumentPrefix` di Company Settings — TIDAK ada penggantian otomatis massal untuk hardcode "Syntera" di seluruh sistem
- Perubahan DocumentPrefix TIDAK BOLEH mengubah NumberingConfig yang sudah ada (prinsip yang sama dengan insiden NumberingConfig yang sudah diperbaiki permanen) — hanya berlaku untuk DocType baru yang belum pernah dibuat
