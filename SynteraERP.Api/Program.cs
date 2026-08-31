using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
using SynteraERP.Api.Authorization;
using SynteraERP.Api.Data;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Middleware;
using SynteraERP.Api.Models;
using SynteraERP.Api.Services;
using SynteraERP.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Authentication / JWT ──────────────────────────────────────────────────────
// Not stored in appsettings.json. Supply it via `dotnet user-secrets set "Jwt:Key" "..."`
// in Development, or a Jwt__Key environment variable in any other environment.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "Jwt:Key is missing. Set it via 'dotnet user-secrets set \"Jwt:Key\" \"...\"' " +
        "(Development) or the Jwt__Key environment variable (other environments).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddScoped<IAuthorizationHandler, ModulePermissionHandler>();
builder.Services.AddAuthorization(opt =>
{
    foreach (var module in Modules.All)
        foreach (var action in PermissionActions.All)
        {
            var requirement = new ModulePermissionRequirement(module, action);
            opt.AddPolicy(requirement.PolicyName, policy => policy.Requirements.Add(requirement));
        }
});
builder.Services.AddHttpContextAccessor();

// ── CORS ───────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddPolicy("FrontEnd", policy =>
        policy.WithOrigins(
            builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? ["http://localhost:3000"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<ISalesOrderService, SalesOrderService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ISalesOrderPaymentService, SalesOrderPaymentService>();
builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IItemMasterService, ItemMasterService>();
builder.Services.AddScoped<ICustomerPoService, CustomerPoService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ITaxRateService, TaxRateService>();
builder.Services.AddScoped<ICompanySettingsService, CompanySettingsService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IJournalPostingService, JournalPostingService>();
builder.Services.AddScoped<ISupplierInvoiceService, SupplierInvoiceService>();
builder.Services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<QuotationPdfService>();
builder.Services.AddScoped<SalesOrderPdfService>();
builder.Services.AddScoped<InvoicePdfService>();
builder.Services.AddScoped<ReportsPdfService>();
builder.Services.AddScoped<ISystemResetService, SystemResetService>();
builder.Services.AddHostedService<InvoiceOverdueStatusService>();
builder.Services.AddHostedService<DatabaseBackupService>();

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Sengaja di-disable supaya CompanySettingsController bisa return format ApiResponse konsisten
// alih-alih ProblemDetails default. WARNING: controller LAIN yang menambah data annotation
// ([Required] dkk) di masa depan TIDAK akan otomatis dapat validasi 400 dari [ApiController] lagi —
// WAJIB tambah ModelState.IsValid check manual sendiri seperti pola di CompanySettingsController,
// atau auto-400 tidak akan terjadi.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(opt =>
{
    opt.SuppressModelStateInvalidFilter = true;
});

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERP API",
        Version = "v1",
        Description = "Backend API for ERP — IT Infrastructure & Project Services"
    });

    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without 'Bearer ' prefix).",
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP API v1");
        opt.RoutePrefix = "swagger";
    });
}

app.UseCors("FrontEnd");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed reference data on startup (skip when running integration tests)
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await CustomerSeeder.SeedAsync(db);
        await ItemMasterSeeder.SeedAsync(db);
        await SupplierSeeder.SeedAsync(db);
        await NumberingConfigSeeder.SeedAsync(db);
    }
}

app.Run();

// Exposes the top-level Program for WebApplicationFactory<Program>-based integration tests
// (standard .NET idiom for minimal-hosting apps; no effect on runtime behavior).
public partial class Program { }
