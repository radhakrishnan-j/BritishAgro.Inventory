namespace BritishAgro.Inventory.Configuration;

public sealed class InventorySettings
{
    public string ActiveEnvironment { get; set; } = "Development";
    public bool RequireConfirmedAccount { get; set; }
}
