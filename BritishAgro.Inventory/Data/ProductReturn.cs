using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BritishAgro.Inventory.Data;

public class ProductReturn
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReturnId { get; set; }

    public int? UsageId { get; set; }

    [ForeignKey(nameof(UsageId))]
    public ProductUsage? ProductUsage { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = default!;

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal QuantityReturned { get; set; }

    [Required]
    public long Date { get; set; }

    [Required]
    [StringLength(255)]
    public string ReturnedBy { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Remarks { get; set; } = string.Empty;
}
