using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BritishAgro.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreProductLotAdditionTypeAndUsageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionType",
                table: "StoreProductLots",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "New");

            migrationBuilder.AddColumn<int>(
                name: "UsageId",
                table: "StoreProductLots",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductLots_UsageId",
                table: "StoreProductLots",
                column: "UsageId");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreProductLots_ProductUsages_UsageId",
                table: "StoreProductLots",
                column: "UsageId",
                principalTable: "ProductUsages",
                principalColumn: "UsageId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreProductLots_ProductUsages_UsageId",
                table: "StoreProductLots");

            migrationBuilder.DropIndex(
                name: "IX_StoreProductLots_UsageId",
                table: "StoreProductLots");

            migrationBuilder.DropColumn(
                name: "AdditionType",
                table: "StoreProductLots");

            migrationBuilder.DropColumn(
                name: "UsageId",
                table: "StoreProductLots");
        }
    }
}
