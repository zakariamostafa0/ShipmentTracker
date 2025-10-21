using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBatchWarehouseStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DestinationWarehouseId",
                table: "Batches",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SourceWarehouseId",
                table: "Batches",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Batches_DestinationWarehouseId",
                table: "Batches",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_SourceWarehouseId",
                table: "Batches",
                column: "SourceWarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Warehouses_DestinationWarehouseId",
                table: "Batches",
                column: "DestinationWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Warehouses_SourceWarehouseId",
                table: "Batches",
                column: "SourceWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Warehouses_DestinationWarehouseId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Warehouses_SourceWarehouseId",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Batches_DestinationWarehouseId",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Batches_SourceWarehouseId",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "DestinationWarehouseId",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "SourceWarehouseId",
                table: "Batches");
        }
    }
}
