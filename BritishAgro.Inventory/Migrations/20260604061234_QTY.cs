using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BritishAgro.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class QTY : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "QuantityReceived",
                table: "StoreProductLots",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityReceived",
                table: "StoreProductLots");
        }
    }
}
