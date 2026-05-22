using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SynteraERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Industry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Npwp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NumberingConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberingConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Npwp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    No = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    SalesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TotalMaterial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalService = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalBeforeTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentTerms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quotations_Quotations_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Quotations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Quotations_Users_SalesId",
                        column: x => x.SalesId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotationTabs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationTabs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationTabs_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    No = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    QuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SalesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Users_SalesId",
                        column: x => x.SalesId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotationGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TabId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationGroups_QuotationTabs_TabId",
                        column: x => x.TabId,
                        principalTable: "QuotationTabs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    No = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Paid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    No = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRequests_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseRequests_Users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Equipment = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Qty = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServicePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaterialPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationItems_QuotationGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "QuotationGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    No = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PurchaseRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_PurchaseRequests_PurchaseRequestId",
                        column: x => x.PurchaseRequestId,
                        principalTable: "PurchaseRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequestItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PRId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestItems_PurchaseRequests_PRId",
                        column: x => x.PRId,
                        principalTable: "PurchaseRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    POId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceivedQty = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_POId",
                        column: x => x.POId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "NumberingConfigs",
                columns: new[] { "Id", "DocType", "LastNumber", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0e7571ea-e82c-4d9f-9b33-a4c7859ecab4"), "INVOICE", 64, "INV.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1175), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("19ad6189-7c9e-4f08-8cb1-f00b67b11f33"), "SALES_ORDER", 48, "SO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1173), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7e518652-ee87-427f-96f4-a93e07054e35"), "PURCHASE_ORDER", 19, "PO.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1184), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("81ff545b-2a1a-4036-bf7f-bb1a2b67d4ff"), "PURCHASE_REQUEST", 34, "PR.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1182), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d1d9f212-154c-4b94-a9ff-a07d7d387269"), "QUOTATION", 148, "Q.SYN", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1169), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "IsActive", "IsDeleted", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(853), new TimeSpan(0, 0, 0, 0, 0)), null, "Full system access", true, false, "Administrator", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(854), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(858), new TimeSpan(0, 0, 0, 0, 0)), null, "Quotation and sales module access", true, false, "Sales", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(858), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(871), new TimeSpan(0, 0, 0, 0, 0)), null, "Invoice and payment access", true, false, "Finance", new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(872), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Email", "IsActive", "IsDeleted", "LastLoginAt", "Name", "PasswordHash", "RoleId", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1140), new TimeSpan(0, 0, 0, 0, 0)), null, "admin@syntera.id", true, false, null, "Administrator", "$2a$11$K8VJO5Yq8pZ2kQ7M1mHsqOzGn5X9/K2Rj7sL3nH6P4dQ0wE1vTx9m", new Guid("10000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 5, 19, 11, 28, 54, 150, DateTimeKind.Unspecified).AddTicks(1141), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Code",
                table: "Customers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_No",
                table: "Invoices",
                column: "No",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SalesOrderId",
                table: "Invoices",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_NumberingConfigs_DocType",
                table: "NumberingConfigs",
                column: "DocType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceId",
                table: "Payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_POId",
                table: "PurchaseOrderItems",
                column: "POId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_No",
                table: "PurchaseOrders",
                column: "No",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PurchaseRequestId",
                table: "PurchaseOrders",
                column: "PurchaseRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestItems_PRId",
                table: "PurchaseRequestItems",
                column: "PRId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_No",
                table: "PurchaseRequests",
                column: "No",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_RequestedBy",
                table: "PurchaseRequests",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_SalesOrderId",
                table: "PurchaseRequests",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationGroups_TabId",
                table: "QuotationGroups",
                column: "TabId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItems_GroupId",
                table: "QuotationItems",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_CustomerId",
                table: "Quotations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_No",
                table: "Quotations",
                column: "No",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_ParentId",
                table: "Quotations",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_SalesId",
                table: "Quotations",
                column: "SalesId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationTabs_QuotationId",
                table: "QuotationTabs",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId",
                table: "SalesOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_No",
                table: "SalesOrders",
                column: "No",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_QuotationId",
                table: "SalesOrders",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalesId",
                table: "SalesOrders",
                column: "SalesId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                table: "Suppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NumberingConfigs");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "PurchaseRequestItems");

            migrationBuilder.DropTable(
                name: "QuotationItems");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "QuotationGroups");

            migrationBuilder.DropTable(
                name: "PurchaseRequests");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "QuotationTabs");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "Quotations");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
