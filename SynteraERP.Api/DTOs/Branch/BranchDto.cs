using System.ComponentModel.DataAnnotations;

namespace SynteraERP.Api.DTOs.Branch;

public class BranchDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Manager { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateBranchRequest
{
    [Required(ErrorMessage = "Nama Cabang wajib diisi.")]
    [StringLength(200, ErrorMessage = "Nama Cabang maksimal 200 karakter.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Alamat maksimal 500 karakter.")]
    public string? Address { get; set; }

    [StringLength(50, ErrorMessage = "Telepon maksimal 50 karakter.")]
    public string? Phone { get; set; }

    [StringLength(100, ErrorMessage = "Manager maksimal 100 karakter.")]
    public string? Manager { get; set; }
}

public class UpdateBranchRequest : CreateBranchRequest
{
    public bool IsActive { get; set; } = true;
}
