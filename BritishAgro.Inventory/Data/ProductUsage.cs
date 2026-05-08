using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BritishAgro.Inventory.Data;

public class ProductUsage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UsageId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = default!;

    [Required]
    public long Date { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Issued { get; set; }

    [Required]
    [StringLength(255)]
    public string ReceivedBy { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Remarks { get; set; } = string.Empty;

    public ICollection<ProductReturn> Returns { get; set; } = new List<ProductReturn>();
    public ICollection<StoreProductLot> StoreProductLots { get; set; } = new List<StoreProductLot>();
}
