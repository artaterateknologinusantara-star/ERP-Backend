using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SynteraERP.Api.Models;

namespace SynteraERP.Api.Helpers;

// Sengaja terpisah total dari JwtHelper (internal User/Role) — signing key sama (tidak ada
// secret kedua yang perlu dikelola), tapi Audience beda ("Vendor" scheme di Program.cs
// memvalidasi ValidAudience = Jwt:VendorAudience, bukan Jwt:Audience). Ini membuat token
// internal dan token vendor TIDAK saling tertukar walau key sama: token internal punya
// aud=syntera-erp-client, akan ditolak scheme "Vendor" (yang mengharapkan aud=vendor-portal),
// dan sebaliknya. Claim "principalType"="vendor" adalah lapis kedua di policy "VendorOnly".
public class VendorJwtHelper
{
    private readonly IConfiguration _config;

    public VendorJwtHelper(IConfiguration config)
    {
        _config = config;
    }

    public (string token, DateTimeOffset expiresAt) Generate(SupplierPortalUser portalUser)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(
            double.Parse(_config["Jwt:ExpiryHours"] ?? "24"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, portalUser.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, portalUser.Email),
            new Claim(ClaimTypes.Name, portalUser.Name),
            new Claim("supplierId", portalUser.SupplierId.ToString()),
            new Claim("principalType", "vendor"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:VendorAudience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
