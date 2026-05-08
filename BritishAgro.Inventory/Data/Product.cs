using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BritishAgro.Inventory.Data;

public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ItemId { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? UnitOfMeasurement { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? ReorderLevel { get; set; }

    public ICollection<StoreProductLot> StoreStocks { get; set; } = new List<StoreProductLot>();
    public ICollection<ProductUsage> ProductUsages { get; set; } = new List<ProductUsage>();
    public ICollection<ProductReturn> ProductReturns { get; set; } = new List<ProductReturn>();
}
