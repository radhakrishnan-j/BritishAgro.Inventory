using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BritishAgro.Inventory.Data;

public class StoreProductLot
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProductLotId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = default!;

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal QuantityAvailable { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal QuantityReceived { get; set; }

    [Required]
    public long ArrivalDate { get; set; }

    [Required]
    [StringLength(50)]
    public string AdditionType { get; set; } = "New";

    public int? UsageId { get; set; }

    [ForeignKey(nameof(UsageId))]
    public ProductUsage? ProductUsage { get; set; }
}
