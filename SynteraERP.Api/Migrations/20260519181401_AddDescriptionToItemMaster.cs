using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToItemMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("1a434a5c-15d9-4984-a74f-a8dfb94b1970"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("2ca38d06-794d-44f6-9717-10173a7429fb"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("8d465c86-515c-42ac-86d2-0de7b2af3fd9"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("a048ece7-c35d-41d0-9b25-8f8377b18271"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("ceb7f556-d9e6-4a43-a276-ee46c4cf465b"));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ItemMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ItemMasters SET Description = 'Switch jaringan terkelola 24 port Gigabit dengan VLAN, QoS, dan manajemen berbasis web' WHERE Code = 'ITM-001';
                UPDATE ItemMasters SET Description = 'Access point WiFi 6 dual-band untuk pemakaian dalam ruangan, mendukung hingga 256 klien' WHERE Code = 'ITM-002';
                UPDATE ItemMasters SET Description = 'Kabel serat optik single-mode diameter 9/125 µm untuk transmisi jarak jauh dengan redaman rendah' WHERE Code = 'ITM-003';
                UPDATE ItemMasters SET Description = 'Patch panel Cat6 24 port untuk manajemen kabel terstruktur di rak server' WHERE Code = 'ITM-004';
                UPDATE ItemMasters SET Description = 'Rak server besi 42U dengan pintu kaca depan, ventilasi samping, dan rel adjustable' WHERE Code = 'ITM-005';
                UPDATE ItemMasters SET Description = 'Kamera IP 4 megapiksel tahan cuaca IP67 dengan IR night vision hingga 30 meter' WHERE Code = 'ITM-006';
                UPDATE ItemMasters SET Description = 'Kabel UTP Category 6 panjang 305 meter per roll, mendukung kecepatan hingga 10 Gbps' WHERE Code = 'ITM-007';
                UPDATE ItemMasters SET Description = 'Core switch Layer 3 48 port SFP+ berkapasitas tinggi untuk jaringan backbone enterprise' WHERE Code = 'ITM-008';
                UPDATE ItemMasters SET Description = 'Optical Distribution Frame 12 core untuk terminasi dan manajemen kabel fiber optik' WHERE Code = 'ITM-009';
                UPDATE ItemMasters SET Description = 'Network Video Recorder 16 channel H.265+ mendukung resolusi 4K dan penyimpanan hingga 4 HDD' WHERE Code = 'ITM-010';
                UPDATE ItemMasters SET Description = 'UPS rackmount 1 kVA / 800 W dengan AVR, LCD display, dan port USB untuk monitoring' WHERE Code = 'ITM-011';
                UPDATE ItemMasters SET Description = 'Server blade 2U dengan prosesor Xeon, RAM ECC DDR4, dan dua slot drive NVMe hot-swap' WHERE Code = 'ITM-012';
                UPDATE ItemMasters SET Description = 'Kabel daya IEC C13 ke socket standar Indonesia, panjang 1,8 meter, kapasitas 10A/250V' WHERE Code = 'ITM-013';
                UPDATE ItemMasters SET Description = 'Kabel HDMI 2.0 panjang 3 meter mendukung resolusi 4K 60Hz dan HDR' WHERE Code = 'ITM-014';
                UPDATE ItemMasters SET Description = 'UPS tower 3 kVA / 2700 W online double-conversion dengan bypass otomatis dan baterai internal' WHERE Code = 'ITM-015';
                UPDATE ItemMasters SET Description = 'Keyboard nirkabel ergonomis dengan koneksi USB dongle, jangkauan 10 meter dan baterai tahan lama' WHERE Code = 'ITM-016';
                UPDATE ItemMasters SET Description = 'Hub USB-C 7 in 1 dengan port HDMI 4K, USB 3.0, SD card reader, dan PD 100W' WHERE Code = 'ITM-017';
                UPDATE ItemMasters SET Description = 'Monitor LED 24 inci IPS Full HD 1080p dengan refresh rate 75Hz dan eye-care mode' WHERE Code = 'ITM-018';
                UPDATE ItemMasters SET Description = 'Laptop bisnis ThinkPad Intel Core i5 generasi terbaru, RAM 16GB, SSD 512GB, layar 14 inci FHD' WHERE Code = 'ITM-019';
                UPDATE ItemMasters SET Description = 'Scanner dokumen A4 dupleks berkecepatan 40 ppm dengan ADF 50 lembar dan koneksi USB/WiFi' WHERE Code = 'ITM-020';
            ");

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0b664bd4-839d-4ee6-b3b5-221ef7396425"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(230), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("98d93e34-2eda-469c-9578-39fa9cd11164"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(211), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c6796f69-70f3-4bed-a132-9605aac10362"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(232), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("eabe1004-4edb-4a8a-b052-72c3260c0c8e"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(227), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f584f8a4-2e5c-4c5d-89c3-5e8a9bcf234c"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(234), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9956), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9957), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9960), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9961), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9964), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 627, DateTimeKind.Unspecified).AddTicks(9965), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(186), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 18, 14, 0, 628, DateTimeKind.Unspecified).AddTicks(186), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("0b664bd4-839d-4ee6-b3b5-221ef7396425"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("98d93e34-2eda-469c-9578-39fa9cd11164"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("c6796f69-70f3-4bed-a132-9605aac10362"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("eabe1004-4edb-4a8a-b052-72c3260c0c8e"));

            migrationBuilder.DeleteData(
                table: "NumberingConfigs",
                keyColumn: "Id",
                keyValue: new Guid("f584f8a4-2e5c-4c5d-89c3-5e8a9bcf234c"));

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ItemMasters");

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("1a434a5c-15d9-4984-a74f-a8dfb94b1970"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1823), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("2ca38d06-794d-44f6-9717-10173a7429fb"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1839), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("8d465c86-515c-42ac-86d2-0de7b2af3fd9"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1831), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("a048ece7-c35d-41d0-9b25-8f8377b18271"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1834), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ceb7f556-d9e6-4a43-a276-ee46c4cf465b"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1828), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1543), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1544), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1548), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1548), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1551), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1552), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1786), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 5, 19, 15, 26, 7, 252, DateTimeKind.Unspecified).AddTicks(1787), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
