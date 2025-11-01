using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGenderToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column as nullable first to allow setting default for existing records
            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Users",
                type: "int",
                nullable: true);

            // Set default value for existing records (PreferNotToSay = 3)
            migrationBuilder.Sql("UPDATE Users SET Gender = 3 WHERE Gender IS NULL");

            // Make column non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");
        }
    }
}
