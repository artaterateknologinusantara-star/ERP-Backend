using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Data;

public static class ItemMasterSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.ItemMasters.AnyAsync()) return;

        var now = DateTimeOffset.UtcNow;
        var items = new List<ItemMaster>
        {
            new() { Code = "ITM-001", Name = "Managed Switch 24 Port", Description = "Switch jaringan terkelola 24 port Gigabit dengan VLAN, QoS, dan manajemen berbasis web", Category = "Network", Brand = "Ruijie", Uom = "Unit", Stock = 15, MinStock = 5, SellingPrice = 8500000, PurchasePrice = 7225000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-002", Name = "Access Point Indoor", Description = "Access point WiFi 6 dual-band untuk pemakaian dalam ruangan, mendukung hingga 256 klien", Category = "Network", Brand = "Ruijie", Uom = "Unit", Stock = 32, MinStock = 10, SellingPrice = 1200000, PurchasePrice = 960000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-003", Name = "Fiber Optic Cable SM 9/125", Description = "Kabel serat optik single-mode diameter 9/125 µm untuk transmisi jarak jauh dengan redaman rendah", Category = "Cabling", Brand = "Draka", Uom = "Meter", Stock = 2500, MinStock = 500, SellingPrice = 8500, PurchasePrice = 6800, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-004", Name = "Patch Panel 24 Port Cat6", Description = "Patch panel Cat6 24 port untuk manajemen kabel terstruktur di rak server", Category = "Cabling", Brand = "Netviel", Uom = "Unit", Stock = 8, MinStock = 3, SellingPrice = 450000, PurchasePrice = 360000, Warehouse = "Gudang Surabaya", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-005", Name = "Server Rack 42U", Description = "Rak server besi 42U dengan pintu kaca depan, ventilasi samping, dan rel adjustable", Category = "Rack", Brand = "Indorack", Uom = "Unit", Stock = 3, MinStock = 2, SellingPrice = 12000000, PurchasePrice = 9600000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-006", Name = "IP Camera 4MP Outdoor", Description = "Kamera IP 4 megapiksel tahan cuaca IP67 dengan IR night vision hingga 30 meter", Category = "CCTV", Brand = "Hikvision", Uom = "Unit", Stock = 45, MinStock = 10, SellingPrice = 1800000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-007", Name = "UTP Cable Cat6 305m", Description = "Kabel UTP Category 6 panjang 305 meter per roll, mendukung kecepatan hingga 10 Gbps", Category = "Cabling", Brand = "Netviel", Uom = "Roll", Stock = 2, MinStock = 5, SellingPrice = 850000, Warehouse = "Gudang Surabaya", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-008", Name = "Core Switch 48 Port", Description = "Core switch Layer 3 48 port SFP+ berkapasitas tinggi untuk jaringan backbone enterprise", Category = "Network", Brand = "Cisco", Uom = "Unit", Stock = 4, MinStock = 2, SellingPrice = 45000000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-009", Name = "ODF 12 Core", Description = "Optical Distribution Frame 12 core untuk terminasi dan manajemen kabel fiber optik", Category = "Fiber Optic", Brand = "Netviel", Uom = "Unit", Stock = 12, MinStock = 4, SellingPrice = 750000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-010", Name = "NVR 16 Channel", Description = "Network Video Recorder 16 channel H.265+ mendukung resolusi 4K dan penyimpanan hingga 4 HDD", Category = "CCTV", Brand = "Hikvision", Uom = "Unit", Stock = 1, MinStock = 3, SellingPrice = 5500000, Warehouse = "Gudang Surabaya", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-011", Name = "UPS 1kVA Rackmount", Description = "UPS rackmount 1 kVA / 800 W dengan AVR, LCD display, dan port USB untuk monitoring", Category = "Power", Brand = "APC", Uom = "Unit", Stock = 7, MinStock = 3, SellingPrice = 4200000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-012", Name = "Server Blade 2U", Description = "Server blade 2U dengan prosesor Xeon, RAM ECC DDR4, dan dua slot drive NVMe hot-swap", Category = "Server", Brand = "Dell", Uom = "Unit", Stock = 5, MinStock = 2, SellingPrice = 18000000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-013", Name = "Power Cable C13", Description = "Kabel daya IEC C13 ke socket standar Indonesia, panjang 1,8 meter, kapasitas 10A/250V", Category = "Cabling", Brand = "Belden", Uom = "Meter", Stock = 320, MinStock = 50, SellingPrice = 9500, Warehouse = "Gudang Surabaya", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-014", Name = "Kabel HDMI 3m", Description = "Kabel HDMI 2.0 panjang 3 meter mendukung resolusi 4K 60Hz dan HDR", Category = "AV", Brand = "Prolink", Uom = "Unit", Stock = 19, MinStock = 5, SellingPrice = 75000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-015", Name = "UPS 3kVA Tower", Description = "UPS tower 3 kVA / 2700 W online double-conversion dengan bypass otomatis dan baterai internal", Category = "Power", Brand = "Eaton", Uom = "Unit", Stock = 6, MinStock = 2, SellingPrice = 15800000, Warehouse = "Gudang Surabaya", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-016", Name = "Wireless Keyboard", Description = "Keyboard nirkabel ergonomis dengan koneksi USB dongle, jangkauan 10 meter dan baterai tahan lama", Category = "Accessory", Brand = "Logitech", Uom = "Unit", Stock = 26, MinStock = 10, SellingPrice = 450000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-017", Name = "USB-C Hub 7 Port", Description = "Hub USB-C 7 in 1 dengan port HDMI 4K, USB 3.0, SD card reader, dan PD 100W", Category = "Accessory", Brand = "Anker", Uom = "Unit", Stock = 14, MinStock = 5, SellingPrice = 850000, Warehouse = "Gudang Surabaya", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-018", Name = "Desktop Monitor 24\"", Description = "Monitor LED 24 inci IPS Full HD 1080p dengan refresh rate 75Hz dan eye-care mode", Category = "IT Equipment", Brand = "LG", Uom = "Unit", Stock = 11, MinStock = 4, SellingPrice = 2200000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-019", Name = "Notebook Lenovo ThinkPad", Description = "Laptop bisnis ThinkPad Intel Core i5 generasi terbaru, RAM 16GB, SSD 512GB, layar 14 inci FHD", Category = "IT Equipment", Brand = "Lenovo", Uom = "Unit", Stock = 3, MinStock = 2, SellingPrice = 17000000, Warehouse = "Gudang Jakarta", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Code = "ITM-020", Name = "Scanner A4 Duplex", Description = "Scanner dokumen A4 dupleks berkecepatan 40 ppm dengan ADF 50 lembar dan koneksi USB/WiFi", Category = "Office", Brand = "Fujitsu", Uom = "Unit", Stock = 9, MinStock = 3, SellingPrice = 8600000, Warehouse = "Gudang Surabaya", IsActive = true, CreatedAt = now, UpdatedAt = now },
        };

        db.ItemMasters.AddRange(items);
        await db.SaveChangesAsync();
    }
}
