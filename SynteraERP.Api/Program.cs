using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SynteraERP.Api.Data;
using SynteraERP.Api.Helpers;
using SynteraERP.Api.Middleware;
using SynteraERP.Api.Services;
using SynteraERP.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Authentication / JWT ──────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing from configuration.");

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

builder.Services.AddAuthorization();

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
builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IItemMasterService, ItemMasterService>();
builder.Services.AddScoped<ICustomerPoService, CustomerPoService>();
builder.Services.AddScoped<QuotationPdfService>();

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Syntera ERP API",
        Version = "v1",
        Description = "Backend API for Syntera ERP — IT Infrastructure & Project Services"
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
        opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Syntera ERP API v1");
        opt.RoutePrefix = "swagger";
    });
}

app.UseCors("FrontEnd");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed reference data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await CustomerSeeder.SeedAsync(db);
    await ItemMasterSeeder.SeedAsync(db);
}

app.Run();
