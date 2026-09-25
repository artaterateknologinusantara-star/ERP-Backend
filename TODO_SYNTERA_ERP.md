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
- ~~Bug risk: RecalcTotals tidak jalan otomatis kalau WorkItem/WorkDetail diubah lewat endpoint standalone (bukan UpdateAsync penuh)~~ — **DIPERBAIKI 24 Sep 2026** (ditemukan lagi & di-fix saat investigasi Portal Vendor RAB, lihat bagian baru di bawah), helper `RecalcAndSaveQuotationTotalsAsync` dipanggil di 6 endpoint standalone WorkItem/WorkDetail, dibungkus transaction per rule #5. Branch `scratch/vendor-portal-blockers` (commit `0c7265d`), sudah di-merge lokal ke `development`, belum di-push.
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
- Bug: `SequentialCodeHelper.NextCodeAsync`/`NextYearCodeAsync` pakai `COUNT()+1`, bukan `MAX()+1` (dan `NextYearCodeAsync` tidak di-scope per tahun secara query, cuma dipakai buat string) - kalau ada gap di sequence (soft-delete, retry gagal, dll), generator akan permanen menghasilkan kode yang sudah dipakai; `RunWithRetryAsync` tidak menolong untuk kasus gap permanen (COUNT tidak berubah antar percobaan, jadi retry mengulang collision yang sama). Scope-check (23 Sep 2026): dipakai HANYA untuk kode non-fiskal — Branch (BR), Customer (CUST), ItemMaster (ITM), Supplier (SUPP) lewat `NextCodeAsync` (ada retry wrapper, tapi rentan sama untuk gap permanen), dan Project (PRJ) lewat `NextYearCodeAsync` (TIDAK ada retry wrapper sama sekali — ditemukan lewat insiden nyata, `PRJ-2026-029` collision permanen karena `PRJ-2026-028` hilang). Dokumen fiskal/pajak (Invoice, Quotation, SalesOrder, PurchaseOrder, PurchaseRequest, SupplierInvoice, Expense, JournalEntry) semua pakai `NumberingConfig.GenerateNext()` (increment kolom `LastNumber` tersendiri, bukan COUNT tabel) — pola berbeda, tidak kena bug ini. Prioritas rendah (non-fiskal), belum di-fix.

### Keputusan desain yang sudah disepakati (untuk konteks, jangan ditanya ulang)
- RAB Civil ME: harga per baris BOQ selalu 1 kolom blended (bukan split Jasa/Material), diisi manual (transkrip dari PDF subcon/supplier)
- Kategori "material dari maincon" pakai tabel QuotationItem yang sama, digabung ke BOQ tanpa label sumber - client tidak boleh tahu ada subcon di baliknya
- GroupSubconPanel (Harga Beli/Harga Jual) tetap lump-sum manual terpisah, tidak diubah

### Portal Vendor RAB Self-Input (Civil ME) — 24 Sep 2026, backend Fase 1 (belum commit)
Dokumen investigasi+rencana lengkap: `Portal Vendor RAB Self-Input — Investigasi & Rencana.md` (root repo + `ERP-Frontend/docs/`). 6 keputusan produk yang tadinya terbuka sudah dijawab (24 Sep 2026):
1. Merge blocker fix (`FallbackPolicy` + `RecalcTotals`) ke `development` — **ya**.
2. Reject → versioning (submission baru per attempt), bukan edit-in-place — histori tetap tersimpan untuk audit trail.
3. Baris RAB: maincon draft dulu nama/volume/spesifikasi (`VendorRabRequestLine`), vendor cuma isi `UnitPrice`.
4. `SupplierPortalUser` (login vendor) dibatasi ke `Supplier.SupplierType` Subcontractor/Both saja (dicek di service layer, bukan skema).
5. Excel: **ClosedXML** (MIT, gratis komersial) — dependency baru ditambahkan.
6. 1 `QuotationGroup` boleh menerima request dari banyak vendor sekaligus (`VendorRabRequest` unique per Group+Supplier, bukan per Group saja).

**Backend Fase 1 sudah diimplementasikan** di branch `scratch/vendor-portal-phase1` (ERP-Backend, dari `development` yang sudah termasuk fix blocker `0c7265d`):
- Entity baru: `SupplierPortalUser`, `VendorRabRequest`, `VendorRabRequestLine`, `VendorRabSubmission` (`AttemptNumber` untuk versioning), `VendorRabSubmissionLine` (`UnitPrice` + `MarkupAmount` diisi maincon saat review). Migration `AddVendorRabPortal` (`20260924062257_AddVendorRabPortal` — di-regenerate 24 Sep 2026 setelah insiden working-tree, lihat catatan di bawah) — NumberingConfig snapshot before/after identik, dites di `SynteraERP_Scratch` (bukan `SynteraERP` development).
- Auth: scheme JWT kedua "Vendor" (audience terpisah `syntera-erp-vendor-portal`, key sama) + policy `VendorOnly` (`RequireClaim("principalType","vendor")`), terpisah total dari `AuthService`/`JwtHelper`/`User`/`Role` internal (lihat `VendorJwtHelper`/`SupplierPortalAuthService`).
- Endpoint vendor-facing (`api/vendor/auth/login`, `api/vendor/rab-requests` list/detail/submit form/submit Excel/download template) dan internal-facing (`api/quotations/groups/{id}/rab-requests` create+send, `api/vendor-submissions/{id}` review/markup/approve/reject). Approve menulis `QuotationWorkItem`/`WorkDetail` resmi lewat `QuotationService.ApplyApprovedVendorRabSubmissionAsync` (method baru) + `RecalcAndSaveQuotationTotalsAsync` dalam **1 transaction** bersama update status Submission/Request (rule #5) — data vendor tidak pernah menyentuh tabel Quotation manapun sebelum approve eksplisit.
- Excel: template per-request dengan kolom ID (GUID baris) tersembunyi+terkunci sebagai join key (bukan posisi baris, supaya vendor boleh insert/hapus/urutkan ulang baris) — parse balik reject-all kalau ada baris rusak/ID tidak dikenal/ID hilang, divalidasi dengan aturan **sama persis** seperti submit lewat form web (satu jalur validasi, `IVendorRabSubmissionService.CreateAsync`, dipakai kedua metode input).

**Bug ditemukan+diperbaiki selama kerja ini**: fix `FallbackPolicy` (blocker #1, commit `0c7265d`) ternyata mematikan `POST /api/auth/login` dan `POST /api/auth/reset-password` internal — keduanya dulu publik cuma karena TIDAK ADA `[Authorize]` sama sekali (bukan `[AllowAnonymous]` eksplisit), jadi begitu `FallbackPolicy` aktif keduanya otomatis butuh login untuk endpoint yang justru dipakai SEBELUM login. Ketahuan lewat smoke test end-to-end (bukan lewat code review), sudah ditambah `[AllowAnonymous]` eksplisit di `AuthController.Login`/`ResetPassword`. **Fix ini diisolasi ke branch terpisah `hotfix/auth-allowanonymous`** (dari `development` @ `0c7265d`, +9 baris di 1 file, build-verified) — menunggu "oke commit" TERPISAH dari phase1, supaya bisa masuk `development` cepat tanpa nunggu review phase1 yang jauh lebih besar selesai.

**Catatan operasional 24 Sep 2026 — insiden working-tree + recovery**: sempat salah jalankan `git checkout development -- .` sambil masih ada uncommitted changes di branch `scratch/vendor-portal-phase1`, yang menimpa working-tree copy dari 7 file tracked (`AuthController.cs`, `AppDbContext.cs`, `Program.cs`, `IQuotationService.cs`, `QuotationService.cs`, `.csproj`, `AppDbContextModelSnapshot.cs`) balik ke isi `development`. **Tidak ada commit yang hilang/rusak** (`development` HEAD tetap `0c7265d`) dan semua file BARU (10 entity/controller/service/migration) aman karena berstatus untracked, tidak tersentuh oleh command itu — tapi ke-7 edit di atas harus ditulis ulang manual dari diff yang sudah ada di riwayat sesi, termasuk regenerasi migration+snapshot dari nol (makanya timestamp migration berubah dari `20260923174432` ke `20260924062257`, tabel lama di `SynteraERP_Scratch` di-drop+reapply bersih). Pelajaran: jangan pernah pakai `git checkout <branch> -- .` (path-checkout) buat "bersih-bersih" working tree — pakai `git stash`/branch baru dari awal kalau perlu state bersih.

**Diverifikasi end-to-end** lewat smoke test terisolasi (rsync copy ke scratchpad, port 5299, DB `SynteraERP_Scratch`, tidak menyentuh dev server aktif di 5261/`SynteraERP`): isolasi auth 2 arah (token vendor → 401 di endpoint internal, token internal → 401 di endpoint vendor, tanpa token → 401 via FallbackPolicy), scoping vendor per-Supplier (request vendor lain → 404, bukan data bocor), submit via form web DAN via upload Excel (round-trip generate→isi (openpyxl)→upload, ID match benar), reject-all (ID tidak dikenal + ID hilang, masing-masing ditolak dengan pesan jelas), versioning (reject → resubmit jadi AttemptNumber 2, histori attempt 1 tetap ada berstatus Rejected), dan approve benar-benar mengubah `GrandTotal` Quotation lewat `RecalcAndSaveQuotationTotalsAsync` (dikonfirmasi angka: 100 × (Rp50.000 + markup Rp10.000) = Rp6.000.000 masuk `totalService` setelah `IsCivilMeMode=true`; sebelum toggle Civil ME, `RecalcTotals` tetap jalan tapi WorkDetail memang tidak disertakan di formula non-Civil-ME — sesuai desain lama, bukan bug). **Re-verifikasi pasca-recovery (24 Sep 2026, instance kedua, port 5300)**: setelah insiden working-tree + regenerasi migration di atas, dijalankan ulang subset inti (login internal+vendor, isolasi auth 2 arah, create request → submit → approve → cek total) untuk memastikan hasil ketik-ulang manual tidak salah — dikonfirmasi angka cocok persis: `totalService` naik dari Rp6.000.000 ke Rp8.000.000 (+20 × Rp100.000), `grandTotal` jadi Rp25.530.000 ((15.000.000+8.000.000) × 1,11). Round-trip Excel dan validasi reject-all tidak diulang di instance kedua (file `VendorRabExcelService.cs` untracked, tidak pernah tersentuh insiden, logikanya identik dengan yang sudah diverifikasi di instance pertama).

**Belum dikerjakan** (di luar scope task backend ini — hanya bagian 1/7/8 dokumen rencana): seluruh frontend (halaman login vendor, portal vendor untuk vendor, UI review+markup+approve untuk maincon), white-label vendor login (bagian 6), CRUD UI untuk `SupplierPortalUser` di halaman Vendor/Supplier existing (endpoint backend sudah ada: `api/suppliers/{id}/portal-users`).

**Belum di-commit** — 3 branch terpisah menunggu review + instruksi eksplisit "oke commit": `scratch/vendor-portal-blockers` (sudah di-merge lokal ke `development`, TAPI belum di-push — ada 3 commit lama lain di `development` yang juga belum push, perlu dikonfirmasi terpisah sebelum push); `hotfix/auth-allowanonymous` (fix `[AllowAnonymous]` 2 baris, prioritas darurat, terpisah dari phase1); `scratch/vendor-portal-phase1` (base dari `development` + seluruh kode fitur baru di atas).

**Code review 24 Sep 2026 (6 temuan, backend Fase 1 belum commit)**: 2 dari 6 langsung menyentuh integritas angka di `QuotationWorkDetail` resmi — **sudah diperbaiki** (build sukses, belum di-commit, sama seperti kode di atas): (1) `VendorRabSubmissionService.CreateAsync` (jalur submit form web) tidak pernah menolak `UnitPrice` negatif, padahal jalur Excel (`VendorRabExcelService`) sudah — sekarang divalidasi reject-all dengan pesan sama persis. (2) `SetLineMarkupAsync` menerima `MarkupAmount` berapa pun tanpa guard — sekarang ditolak kalau `UnitPrice + MarkupAmount < 0`. 4 temuan sisanya (minor, belum diperbaiki, kandidat follow-up): Excel `TryGetValue<decimal>()` bisa salah parse teks berformat Indonesia ("50.000") jadi 50 bukan 50000 tanpa error (kelas bug sama seperti rule #7, di permukaan baru); `SupplierPortalUserController` Activate/Deactivate tidak cek `id` benar-benar milik `supplierId` di route; `VendorRabRequestService.CreateAndSendAsync` bikin dead-end — begitu 1 request Group+Supplier terkirim, tidak ada endpoint untuk kirim batch RAB lanjutan ke vendor+group yang sama; `ListByGroupAsync`/`ListForVendorAsync` return list kosong (bukan 404) untuk `GroupId`/`SupplierId` yang tidak ada.

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
- Residual ±Rp1 di PDF Quotation Civil ME (25 Sep 2026): subtotal BOQ/rekap kategori (`QuotationPdfService.cs`, dibulatkan per-baris lalu dijumlah — fix P0 rounding lanjutan) bisa beda ±Rp1 dari subtotal blok pajak (`Quotation.TotalBeforeTax`, hasil `RecalcTotals` yang sum-then-round) kalau ada `WorkDetail.Volume` pecahan. Sengaja tidak disamakan — `RecalcTotals` adalah jalur tunggal untuk SalesOrder/Invoice/pajak, mengubah strategi roundingnya demi kosmetik PDF membuka blast radius jauh lebih besar daripada manfaatnya. Keputusan sadar, bukan terlewat.

## ⏸️ Ditunda (keputusan bisnis, dipikirkan lagi nanti)

- Integrasi AI Agent — beberapa opsi dibahas (asisten query/laporan, OCR dokumen, deteksi anomali, chat in-app, kategorisasi otomatis expense) — belum diputuskan use case mana yang diprioritaskan, dan model biaya API (siapa yang bayar).

## 📌 Keputusan Arsitektur Penting yang Sudah Ditetapkan

- Model deployment: **1 deployment = 1 customer** (white-label per-instalasi, BUKAN multi-tenant SaaS)
- Prefix dokumen ("SYN") akan dibuat **configurable manual** oleh customer via field `DocumentPrefix` di Company Settings — TIDAK ada penggantian otomatis massal untuk hardcode "Syntera" di seluruh sistem
- Perubahan DocumentPrefix TIDAK BOLEH mengubah NumberingConfig yang sudah ada (prinsip yang sama dengan insiden NumberingConfig yang sudah diperbaiki permanen) — hanya berlaku untuk DocType baru yang belum pernah dibuat
