using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKTX.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleLicensePlateImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LicensePlateImagePath",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicensePlateImagePath",
                table: "Vehicles");
        }
    }
}
