using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKTX.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudentRoomBuilding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "ResidentProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassName",
                table: "ResidentProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ResidentProfiles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ResidentProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Faculty",
                table: "ResidentProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "ResidentProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "ResidentProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentCode",
                table: "ResidentProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Buildings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Buildings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Buildings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalFloors",
                table: "Buildings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentOccupancy",
                table: "Apartments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Floor",
                table: "Apartments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxOccupancy",
                table: "Apartments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Apartments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RoomPrice",
                table: "Apartments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Apartments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_BuildingId",
                table: "Apartments",
                column: "BuildingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Apartments_Buildings_BuildingId",
                table: "Apartments",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Apartments_Buildings_BuildingId",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_BuildingId",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "ResidentProfiles");

            migrationBuilder.DropColumn(
                name: "ClassName",
                table: "ResidentProfiles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ResidentProfiles");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "ResidentProfiles");

            migrationBuilder.DropColumn(
                name: "Faculty",
                table: "ResidentProfiles");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "ResidentProfiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "ResidentProfiles");

            migrationBuilder.DropColumn(
                name: "StudentCode",
                table: "ResidentProfiles");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "TotalFloors",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "CurrentOccupancy",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "Floor",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "MaxOccupancy",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "RoomPrice",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Apartments");
        }
    }
}
