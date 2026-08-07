using Microsoft.EntityFrameworkCore;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ─── Auth ────────────────────────────────────────────────────────────────
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();

    // ─── Master Data ──────────────────────────────────────────────────────────
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<NumberingConfig> NumberingConfigs => Set<NumberingConfig>();
    public DbSet<ItemMaster> ItemMasters => Set<ItemMaster>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();

    // ─── Accounting / GL ──────────────────────────────────────────────────────
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

    // ─── Quotation ────────────────────────────────────────────────────────────
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationTab> QuotationTabs => Set<QuotationTab>();
    public DbSet<QuotationGroup> QuotationGroups => Set<QuotationGroup>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<CustomerPO> CustomerPOs => Set<CustomerPO>();

    // ─── Sales ────────────────────────────────────────────────────────────────
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems { get; set; }

    // ─── Finance ─────────────────────────────────────────────────────────────
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<Payment> Payments => Set<Payment>();

    // ─── Purchasing ───────────────────────────────────────────────────────────
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseRequestItem> PurchaseRequestItems => Set<PurchaseRequestItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<POPayment> POPayments { get; set; }
    public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();
    public DbSet<SupplierInvoiceItem> SupplierInvoiceItems => Set<SupplierInvoiceItem>();
    public DbSet<SupplierInvoicePayment> SupplierInvoicePayments => Set<SupplierInvoicePayment>();

    // ─── Expense Management (Fase 5) ─────────────────────────────────────────
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();

    // ─── Inventory ────────────────────────────────────────────────────────────
    public DbSet<StockTransaction> StockTransactions { get; set; }
    public DbSet<DeliveryOrder> DeliveryOrders { get; set; }
    public DbSet<DeliveryOrderItem> DeliveryOrderItems { get; set; }

    // ─── Project ──────────────────────────────────────────────────────────────
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    // ─── Audit ────────────────────────────────────────────────────────────────
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ─── Global soft-delete filter ────────────────────────────────────────
        b.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Supplier>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<ItemMaster>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Quotation>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<CustomerPO>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<SalesOrder>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Invoice>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<PurchaseRequest>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<PurchaseOrder>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<StockTransaction>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<DeliveryOrder>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<TaxRate>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Account>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<JournalEntry>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<SupplierInvoice>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<ExpenseCategory>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Expense>().HasQueryFilter(e => !e.IsDeleted);

        // ─── Role ─────────────────────────────────────────────────────────────
        b.Entity<Role>(e =>
        {
            e.HasIndex(r => r.Name).IsUnique();
            e.Property(r => r.Name).HasMaxLength(50).IsRequired();
        });

        // ─── User ─────────────────────────────────────────────────────────────
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.Property(u => u.Name).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            e.HasOne(u => u.Role)
             .WithMany(r => r.Users)
             .HasForeignKey(u => u.RoleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── Customer ─────────────────────────────────────────────────────────
        b.Entity<Customer>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Code).HasMaxLength(20).IsRequired();
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
        });

        // ─── Supplier ─────────────────────────────────────────────────────────
        b.Entity<Supplier>(e =>
        {
            e.HasIndex(s => s.Code).IsUnique();
            e.Property(s => s.Code).HasMaxLength(20).IsRequired();
            e.Property(s => s.Name).HasMaxLength(200).IsRequired();
        });

        // ─── NumberingConfig ──────────────────────────────────────────────────
        b.Entity<NumberingConfig>(e =>
        {
            e.HasIndex(n => n.DocType).IsUnique();
            e.Property(n => n.DocType).HasMaxLength(30).IsRequired();
            e.Property(n => n.Prefix).HasMaxLength(20).IsRequired();
        });

        // ─── ItemMaster ───────────────────────────────────────────────────────
        b.Entity<ItemMaster>(e =>
        {
            e.HasIndex(i => i.Code).IsUnique();
            e.Property(i => i.Code).HasMaxLength(30).IsRequired();
            e.Property(i => i.Name).HasMaxLength(300).IsRequired();
            e.Property(i => i.Category).HasMaxLength(100);
            e.Property(i => i.Brand).HasMaxLength(100);
            e.Property(i => i.Uom).HasMaxLength(50).IsRequired();
            e.Property(i => i.Warehouse).HasMaxLength(100);
            e.Property(i => i.Stock).HasPrecision(18, 4);
            e.Property(i => i.MinStock).HasPrecision(18, 4);
            e.Property(i => i.SellingPrice).HasPrecision(18, 2);
            e.Property(i => i.PurchasePrice).HasPrecision(18, 2);
            e.Property(i => i.LastPurchasePrice).HasPrecision(18, 2);
            e.Property(i => i.CurrentAverageCost).HasPrecision(18, 2);
            e.Property(i => i.ReorderPoint).HasPrecision(18, 4);
            e.HasOne(i => i.PreferredVendor)
             .WithMany()
             .HasForeignKey(i => i.PreferredVendorId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // ─── Quotation ────────────────────────────────────────────────────────
        b.Entity<Quotation>(e =>
        {
            e.HasIndex(q => new { q.No, q.Revision }).IsUnique();
            e.Property(q => q.No).HasMaxLength(30).IsRequired();
            e.Property(q => q.ProjectName).HasMaxLength(300).IsRequired();
            e.Property(q => q.GrandTotal).HasPrecision(18, 2);
            e.Property(q => q.TotalMaterial).HasPrecision(18, 2);
            e.Property(q => q.TotalService).HasPrecision(18, 2);
            e.Property(q => q.TotalBeforeTax).HasPrecision(18, 2);
            e.Property(q => q.TaxAmount).HasPrecision(18, 2);
            e.Property(q => q.Discount).HasPrecision(5, 2);
            e.Property(q => q.TaxRate).HasPrecision(5, 2);
            e.Property(q => q.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(q => q.Customer)
             .WithMany(c => c.Quotations)
             .HasForeignKey(q => q.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(q => q.Sales)
             .WithMany()
             .HasForeignKey(q => q.SalesId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(q => q.Parent)
             .WithMany()
             .HasForeignKey(q => q.ParentId)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(q => q.SupersededBy)
             .WithMany()
             .HasForeignKey(q => q.SupersededByQuotationId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<QuotationTab>(e =>
        {
            e.Property(t => t.Label).HasMaxLength(100).IsRequired();
            e.HasOne(t => t.Quotation)
             .WithMany(q => q.Tabs)
             .HasForeignKey(t => t.QuotationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<QuotationGroup>(e =>
        {
            e.Property(g => g.Name).HasMaxLength(200).IsRequired();
            e.HasOne(g => g.Tab)
             .WithMany(t => t.Groups)
             .HasForeignKey(g => g.TabId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<QuotationItem>(e =>
        {
            e.Property(i => i.Equipment).HasMaxLength(200).IsRequired();
            e.Property(i => i.Unit).HasMaxLength(20).IsRequired();
            e.Property(i => i.ServicePrice).HasPrecision(18, 2);
            e.Property(i => i.MaterialPrice).HasPrecision(18, 2);
            e.Property(i => i.Qty).HasPrecision(12, 4);
            e.Ignore(i => i.TotalService);
            e.Ignore(i => i.TotalMaterial);
            e.Ignore(i => i.GrandLine);
            e.HasOne(i => i.Group)
             .WithMany(g => g.Items)
             .HasForeignKey(i => i.GroupId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── CustomerPO ──────────────────────────────────────────────────────────
        b.Entity<CustomerPO>(e =>
        {
            e.HasIndex(c => c.QuotationId).IsUnique();
            e.Property(c => c.PoNo).HasMaxLength(50).IsRequired();
            e.Property(c => c.Amount).HasPrecision(18, 2);
            e.Property(c => c.AttachmentPath).HasMaxLength(500);
            e.Property(c => c.AttachmentName).HasMaxLength(255);
            e.HasOne(c => c.Quotation)
             .WithMany()
             .HasForeignKey(c => c.QuotationId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── SalesOrder ───────────────────────────────────────────────────────
        b.Entity<SalesOrder>(e =>
        {
            e.HasIndex(s => s.No).IsUnique();
            e.Property(s => s.No).HasMaxLength(30).IsRequired();
            e.Property(s => s.ProjectName).HasMaxLength(300).IsRequired();
            e.Property(s => s.Total).HasPrecision(18, 2);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(s => s.Customer)
             .WithMany(c => c.SalesOrders)
             .HasForeignKey(s => s.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Sales)
             .WithMany()
             .HasForeignKey(s => s.SalesId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Quotation)
             .WithMany()
             .HasForeignKey(s => s.QuotationId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ─── SalesOrderItem ───────────────────────────────────────────────────
        b.Entity<SalesOrderItem>(e =>
        {
            e.Property(i => i.Amount).HasPrecision(18, 2);
            e.Property(i => i.UnitPrice).HasPrecision(18, 2);
            e.Property(i => i.Qty).HasPrecision(18, 4);
            e.Property(i => i.QtyShipped).HasPrecision(18, 4);
            e.Property(i => i.Discount).HasPrecision(5, 2);
            e.Ignore(i => i.QtyNotShipped);

            e.HasOne(i => i.SalesOrder)
             .WithMany(s => s.Items)
             .HasForeignKey(i => i.SalesOrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.ItemMaster)
             .WithMany()
             .HasForeignKey(i => i.ItemMasterId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // ─── Invoice ─────────────────────────────────────────────────────────
        b.Entity<Invoice>(e =>
        {
            e.HasIndex(i => i.No).IsUnique();
            e.Property(i => i.No).HasMaxLength(30).IsRequired();
            e.Property(i => i.Amount).HasPrecision(18, 2);
            e.Property(i => i.Paid).HasPrecision(18, 2);
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            e.Ignore(i => i.Balance);

            e.HasOne(i => i.Customer)
             .WithMany(c => c.Invoices)
             .HasForeignKey(i => i.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.SalesOrder)
             .WithMany(s => s.Invoices)
             .HasForeignKey(i => i.SalesOrderId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<InvoiceItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Qty).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne(x => x.Invoice).WithMany(x => x.Items)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
            e.HasOne(p => p.Invoice)
             .WithMany(i => i.Payments)
             .HasForeignKey(p => p.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── PurchaseRequest ──────────────────────────────────────────────────
        b.Entity<PurchaseRequest>(e =>
        {
            e.HasIndex(r => r.No).IsUnique();
            e.Property(r => r.No).HasMaxLength(30).IsRequired();
            e.Property(r => r.Total).HasPrecision(18, 2);
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(r => r.SalesOrder)
             .WithMany(s => s.PurchaseRequests)
             .HasForeignKey(r => r.SalesOrderId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(r => r.RequestedByUser)
             .WithMany()
             .HasForeignKey(r => r.RequestedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PurchaseRequestItem>(e =>
        {
            e.Property(i => i.EstPrice).HasPrecision(18, 2);
            e.Property(i => i.Qty).HasPrecision(12, 4);
            e.Property(i => i.OrderedQty).HasPrecision(12, 4);
            e.HasOne(i => i.PurchaseRequest)
             .WithMany(r => r.Items)
             .HasForeignKey(i => i.PRId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.ItemMaster)
             .WithMany()
             .HasForeignKey(i => i.ItemMasterId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // ─── PurchaseOrder ────────────────────────────────────────────────────
        b.Entity<PurchaseOrder>(e =>
        {
            e.HasIndex(o => o.No).IsUnique();
            e.Property(o => o.No).HasMaxLength(30).IsRequired();
            e.Property(o => o.Total).HasPrecision(18, 2);
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(o => o.Supplier)
             .WithMany(s => s.PurchaseOrders)
             .HasForeignKey(o => o.SupplierId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(o => o.PurchaseRequest)
             .WithMany(r => r.PurchaseOrders)
             .HasForeignKey(o => o.PurchaseRequestId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<PurchaseOrderItem>(e =>
        {
            e.Property(i => i.Price).HasPrecision(18, 2);
            e.Property(i => i.Qty).HasPrecision(12, 4);
            e.Property(i => i.ReceivedQty).HasPrecision(12, 4);
            e.Property(i => i.InvoicedQty).HasPrecision(12, 4);
            e.Ignore(i => i.Total);
            e.HasOne(i => i.PurchaseOrder)
             .WithMany(o => o.Items)
             .HasForeignKey(i => i.POId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.ItemMaster)
             .WithMany()
             .HasForeignKey(i => i.ItemMasterId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        b.Entity<POPayment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Method).HasMaxLength(50);
            e.HasOne(x => x.PurchaseOrder)
             .WithMany(x => x.Payments)
             .HasForeignKey(x => x.PurchaseOrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── SupplierInvoice (Fase 4) ───────────────────────────────────────────
        b.Entity<SupplierInvoice>(e =>
        {
            e.HasIndex(x => x.No).IsUnique();
            e.HasIndex(x => new { x.SupplierId, x.InvoiceNumber }).IsUnique();
            e.Property(x => x.No).HasMaxLength(30).IsRequired();
            e.Property(x => x.InvoiceNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.PPNMasukan).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(x => x.PurchaseOrder)
             .WithMany()
             .HasForeignKey(x => x.PurchaseOrderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Supplier)
             .WithMany()
             .HasForeignKey(x => x.SupplierId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<SupplierInvoiceItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Qty).HasPrecision(12, 4);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Ignore(x => x.Amount);

            e.HasOne(x => x.SupplierInvoice)
             .WithMany(x => x.Items)
             .HasForeignKey(x => x.SupplierInvoiceId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.PurchaseOrderItem)
             .WithMany()
             .HasForeignKey(x => x.PurchaseOrderItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<SupplierInvoicePayment>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasOne(x => x.SupplierInvoice)
             .WithMany(x => x.Payments)
             .HasForeignKey(x => x.SupplierInvoiceId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.POPayment)
             .WithMany()
             .HasForeignKey(x => x.POPaymentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── Expense Management (Fase 5) ────────────────────────────────────────
        b.Entity<ExpenseCategory>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(30).IsRequired();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();

            e.HasOne(x => x.Account)
             .WithMany()
             .HasForeignKey(x => x.AccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Expense>(e =>
        {
            e.HasIndex(x => x.ExpenseNo).IsUnique();
            e.Property(x => x.ExpenseNo).HasMaxLength(30).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Method).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(x => x.ExpenseCategory)
             .WithMany()
             .HasForeignKey(x => x.ExpenseCategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Vendor)
             .WithMany()
             .HasForeignKey(x => x.VendorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CashBankAccount)
             .WithMany()
             .HasForeignKey(x => x.CashBankAccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── StockTransaction ─────────────────────────────────────────────────
        b.Entity<StockTransaction>(e =>
        {
            e.Property(x => x.Qty).HasPrecision(18, 4);
            e.Property(x => x.StockBefore).HasPrecision(18, 4);
            e.Property(x => x.StockAfter).HasPrecision(18, 4);
            e.Property(x => x.Type).HasConversion<string>();
            e.Property(x => x.Source).HasConversion<string>();
            e.HasOne(x => x.ItemMaster).WithMany()
                .HasForeignKey(x => x.ItemMasterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByUser).WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── DeliveryOrder ────────────────────────────────────────────────────
        b.Entity<DeliveryOrder>(e =>
        {
            e.HasIndex(x => x.No).IsUnique();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.SalesOrder).WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            e.HasOne(x => x.Customer).WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            e.HasOne(x => x.CreatedByUser).WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── DeliveryOrderItem ────────────────────────────────────────────────
        b.Entity<DeliveryOrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Qty).HasPrecision(18, 4);
            e.Property(x => x.QtyOrdered).HasPrecision(18, 4);
            e.Property(x => x.QtyPreviouslyOut).HasPrecision(18, 4);
            e.HasOne(x => x.DeliveryOrder).WithMany(x => x.Items)
                .HasForeignKey(x => x.DeliveryOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ItemMaster).WithMany()
                .HasForeignKey(x => x.ItemMasterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── Project ──────────────────────────────────────────────────────────────
        b.Entity<Project>(e =>
        {
            e.HasIndex(p => p.Code).IsUnique();
            e.Property(p => p.Code).HasMaxLength(30).IsRequired();
            e.Property(p => p.Name).HasMaxLength(300).IsRequired();
            e.Property(p => p.Budget).HasPrecision(18, 2);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.HasQueryFilter(p => !p.IsDeleted);

            e.HasOne(p => p.Customer).WithMany()
             .HasForeignKey(p => p.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.SalesOrder).WithMany()
             .HasForeignKey(p => p.SalesOrderId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.ProjectManager).WithMany()
             .HasForeignKey(p => p.ProjectManagerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ProjectTask>(e =>
        {
            e.Property(t => t.Title).HasMaxLength(300).IsRequired();
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Priority).HasConversion<string>().HasMaxLength(10);
            e.HasQueryFilter(t => !t.IsDeleted);

            e.HasOne(t => t.Project).WithMany(p => p.Tasks)
             .HasForeignKey(t => t.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.AssignedTo).WithMany()
             .HasForeignKey(t => t.AssignedToId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ─── CompanySettings ─────────────────────────────────────────────────────
        b.Entity<CompanySettings>(e =>
        {
            e.Property(c => c.CompanyName).HasMaxLength(200).IsRequired();
            e.Property(c => c.LogoPath).HasMaxLength(500);
            e.Property(c => c.Address).HasMaxLength(500);
            e.Property(c => c.Phone).HasMaxLength(50);
            e.Property(c => c.Email).HasMaxLength(150);
            e.Property(c => c.Website).HasMaxLength(200);
            e.Property(c => c.FooterText).HasMaxLength(1000);
            e.Property(c => c.SignatureName).HasMaxLength(100);
            e.Property(c => c.SignatureTitle).HasMaxLength(100);
            e.Property(c => c.DocumentPrefix).HasMaxLength(20);
        });

        // ─── TaxRate ──────────────────────────────────────────────────────────
        b.Entity<TaxRate>(e =>
        {
            e.HasIndex(t => t.Code).IsUnique();
            e.Property(t => t.Code).HasMaxLength(20).IsRequired();
            e.Property(t => t.Name).HasMaxLength(100).IsRequired();
            e.Property(t => t.Rate).HasPrecision(6, 4);
        });

        // ─── Account ──────────────────────────────────────────────────────────
        b.Entity<Account>(e =>
        {
            e.HasIndex(a => a.Code).IsUnique();
            e.Property(a => a.Code).HasMaxLength(20).IsRequired();
            e.Property(a => a.Name).HasMaxLength(150).IsRequired();
            e.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.NormalBalance).HasConversion<string>().HasMaxLength(10);

            e.HasOne(a => a.ParentAccount)
             .WithMany(a => a.ChildAccounts)
             .HasForeignKey(a => a.ParentAccountId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // ─── JournalEntry ─────────────────────────────────────────────────────
        b.Entity<JournalEntry>(e =>
        {
            e.HasIndex(j => j.EntryNumber).IsUnique();
            e.Property(j => j.EntryNumber).HasMaxLength(30).IsRequired();
            e.Property(j => j.SourceType).HasConversion<string>().HasMaxLength(30);
            e.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(j => j.ReversedByEntry)
             .WithMany()
             .HasForeignKey(j => j.ReversedByEntryId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // ─── JournalEntryLine ─────────────────────────────────────────────────
        b.Entity<JournalEntryLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Debit).HasPrecision(18, 2);
            e.Property(l => l.Credit).HasPrecision(18, 2);

            e.HasOne(l => l.JournalEntry)
             .WithMany(j => j.Lines)
             .HasForeignKey(l => l.JournalEntryId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.Account)
             .WithMany()
             .HasForeignKey(l => l.AccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── AuditLog ─────────────────────────────────────────────────────────
        b.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).HasMaxLength(50).IsRequired();
            e.Property(a => a.Scope).HasMaxLength(200).IsRequired();
            e.Property(a => a.Details).HasColumnType("nvarchar(max)");
            e.Property(a => a.PerformedByName).HasMaxLength(100);
            e.Property(a => a.IpAddress).HasMaxLength(45);
        });

        // ─── Seed data ────────────────────────────────────────────────────────
        var adminRoleId = new Guid("10000000-0000-0000-0000-000000000001");
        var salesRoleId = new Guid("10000000-0000-0000-0000-000000000002");
        var financeRoleId = new Guid("10000000-0000-0000-0000-000000000003");

        b.Entity<Role>().HasData(
            new Role { Id = adminRoleId, Name = "Administrator", Description = "Full system access", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Role { Id = salesRoleId, Name = "Sales", Description = "Quotation and sales module access", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Role { Id = financeRoleId, Name = "Finance", Description = "Invoice and payment access", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );

        var adminId = new Guid("20000000-0000-0000-0000-000000000001");
        b.Entity<User>().HasData(
            new User
            {
                Id = adminId,
                RoleId = adminRoleId,
                Name = "Administrator",
                Email = "admin@syntera.id",
                // password: Admin@123
                PasswordHash = "$2a$11$K8VJO5Yq8pZ2kQ7M1mHsqOzGn5X9/K2Rj7sL3nH6P4dQ0wE1vTx9m",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        );

        // NumberingConfig TIDAK LAGI di-seed lewat HasData() di sini — lihat NumberingConfigSeeder.cs.
        // (Technical debt yang ditutup: HasData dengan Id=Guid.NewGuid() menyebabkan setiap migration
        // baru meregenerasi ulang seed ini dan berisiko mereset LastNumber ke nilai hardcode lama,
        // menimpa nilai aktual yang sudah bertumbuh dari transaksi nyata. Lihat 03_DEVELOPMENT_ROADMAP.md.)

        b.Entity<CompanySettings>().HasData(new CompanySettings
        {
            Id = new Guid("30000000-0000-0000-0000-000000000001"),
            CompanyName = "PT Syntera Teknologi Nusantara",
            Address = "Jl. Raya Teknologi No. 1, Jakarta Selatan 12190",
            Phone = "+62 21 5555-0100",
            Email = "info@syntera.id",
            Website = "www.syntera.id",
            FooterText = "Penawaran ini berlaku selama 14 hari. Harga belum termasuk biaya pengiriman dan instalasi kecuali disebutkan. Pembayaran 50% di muka, sisa 50% setelah pekerjaan selesai.",
            SignatureName = "Budi Santoso",
            SignatureTitle = "Sales Manager",
            DocumentPrefix = "SYN",
            UpdatedAt = new DateTimeOffset(new DateTime(2026, 1, 1), TimeSpan.Zero)
        });

        b.Entity<TaxRate>().HasData(new TaxRate
        {
            Id = new Guid("40000000-0000-0000-0000-000000000001"),
            Code = "PPN11",
            Name = "PPN 11%",
            Rate = 0.11m,
            IsDefault = true,
            IsActive = true,
            CreatedAt = new DateTimeOffset(new DateTime(2026, 1, 1), TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(new DateTime(2026, 1, 1), TimeSpan.Zero)
        });

        // ─── Chart of Accounts (Fase 1 Accounting) ─────────────────────────────
        var acctSeedDate = new DateTimeOffset(new DateTime(2026, 1, 1), TimeSpan.Zero);
        var acctKasBankId = new Guid("50000000-0000-0000-0000-000000000001");
        var acctBebanOpId = new Guid("50000000-0000-0000-0000-00000000000f");

        Account NewAccount(string idHex, string code, string name, AccountType type, NormalBalanceType normal, Guid? parentId = null, bool isControl = false) => new()
        {
            Id = new Guid($"50000000-0000-0000-0000-{idHex.PadLeft(12, '0')}"),
            Code = code,
            Name = name,
            Type = type,
            ParentAccountId = parentId,
            NormalBalance = normal,
            IsControlAccount = isControl,
            CreatedAt = acctSeedDate,
            UpdatedAt = acctSeedDate,
        };

        b.Entity<Account>().HasData(
            // ASET
            NewAccount("1", "1-1000", "Kas & Bank", AccountType.Asset, NormalBalanceType.Debit, isControl: true),
            NewAccount("2", "1-1001", "Kas", AccountType.Asset, NormalBalanceType.Debit, acctKasBankId),
            NewAccount("3", "1-1002", "Bank BCA", AccountType.Asset, NormalBalanceType.Debit, acctKasBankId),
            NewAccount("4", "1-1003", "Bank Mandiri", AccountType.Asset, NormalBalanceType.Debit, acctKasBankId),
            NewAccount("5", "1-1004", "Bank BNI", AccountType.Asset, NormalBalanceType.Debit, acctKasBankId),
            NewAccount("6", "1-2000", "Piutang Usaha", AccountType.Asset, NormalBalanceType.Debit),
            NewAccount("7", "1-3000", "Persediaan", AccountType.Asset, NormalBalanceType.Debit),
            NewAccount("21", "1-3500", "Barang Diterima Belum Ditagih (GRNI)", AccountType.Asset, NormalBalanceType.Debit),
            NewAccount("8", "1-4000", "Aset Tetap", AccountType.Asset, NormalBalanceType.Debit),
            // LIABILITAS
            NewAccount("9", "2-1000", "Utang Usaha", AccountType.Liability, NormalBalanceType.Credit),
            NewAccount("a", "2-2000", "Utang Pajak Keluaran (PPN Keluaran)", AccountType.Liability, NormalBalanceType.Credit),
            NewAccount("b", "2-3000", "Utang Pajak Masukan (PPN Masukan)", AccountType.Liability, NormalBalanceType.Credit),
            // EKUITAS
            NewAccount("c", "3-1000", "Modal", AccountType.Equity, NormalBalanceType.Credit),
            // PENDAPATAN
            NewAccount("d", "4-1000", "Pendapatan Penjualan", AccountType.Revenue, NormalBalanceType.Credit),
            // BEBAN
            NewAccount("e", "5-1000", "Harga Pokok Penjualan (HPP)", AccountType.Expense, NormalBalanceType.Debit),
            NewAccount("f", "5-2000", "Beban Operasional", AccountType.Expense, NormalBalanceType.Debit, isControl: true),
            NewAccount("10", "5-2001", "Beban Sewa Kantor", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("11", "5-2002", "Beban ATK", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("12", "5-2003", "Beban Listrik", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("13", "5-2004", "Beban Air", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("14", "5-2005", "Beban Internet", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("15", "5-2006", "Beban Telepon", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("16", "5-2007", "Beban BBM", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("17", "5-2008", "Beban Parkir", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("18", "5-2009", "Beban Perjalanan Bisnis", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("19", "5-2010", "Beban Entertainment", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("1a", "5-2011", "Beban Kurir", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("1b", "5-2012", "Beban Maintenance Kantor", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("1c", "5-2013", "Beban Bank Charges", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("1d", "5-2014", "Beban Asuransi", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("1e", "5-2015", "Beban Training", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("1f", "5-2016", "Beban Pajak", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId),
            NewAccount("20", "5-2017", "Beban Lain-lain", AccountType.Expense, NormalBalanceType.Debit, acctBebanOpId)
        );

        // ─── ExpenseCategory seed (Fase 5) — mapping 1:1 ke 17 akun beban 5-2001..5-2017 di atas ────
        ExpenseCategory NewExpenseCategory(string idHex, string code, string name, string accountIdHex) => new()
        {
            Id = new Guid($"70000000-0000-0000-0000-{idHex.PadLeft(12, '0')}"),
            Code = code,
            Name = name,
            AccountId = new Guid($"50000000-0000-0000-0000-{accountIdHex.PadLeft(12, '0')}"),
            IsActive = true,
            CreatedAt = acctSeedDate,
            UpdatedAt = acctSeedDate,
        };

        b.Entity<ExpenseCategory>().HasData(
            NewExpenseCategory("1", "RENT", "Office Rent", "10"),
            NewExpenseCategory("2", "ATK", "Office Supplies (ATK)", "11"),
            NewExpenseCategory("3", "ELEC", "Office Electricity", "12"),
            NewExpenseCategory("4", "WATER", "Water", "13"),
            NewExpenseCategory("5", "INTERNET", "Internet", "14"),
            NewExpenseCategory("6", "PHONE", "Telephone", "15"),
            NewExpenseCategory("7", "FUEL", "Fuel (BBM)", "16"),
            NewExpenseCategory("8", "PARKING", "Parking", "17"),
            NewExpenseCategory("9", "TRAVEL", "Business Travel", "18"),
            NewExpenseCategory("a", "ENTERTAINMENT", "Entertainment", "19"),
            NewExpenseCategory("b", "COURIER", "Courier", "1a"),
            NewExpenseCategory("c", "MAINTENANCE", "Office Maintenance", "1b"),
            NewExpenseCategory("d", "BANKCHARGE", "Bank Charges", "1c"),
            NewExpenseCategory("e", "INSURANCE", "Insurance", "1d"),
            NewExpenseCategory("f", "TRAINING", "Training", "1e"),
            NewExpenseCategory("10", "TAX", "Tax", "1f"),
            NewExpenseCategory("11", "OTHER", "Other/Miscellaneous", "20")
        );

        // JOURNAL_ENTRY/SUPPLIER_INVOICE/EXPENSE NumberingConfig juga dipindah ke NumberingConfigSeeder.cs
        // (konsolidasi penuh - lihat catatan di seed NumberingConfig di atas).
    }
}
