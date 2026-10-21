using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKTX.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentRegistrationApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Faculty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NationalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentRegistrations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentRegistrationDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentRegistrationId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRegistrationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentRegistrationDocuments_StudentRegistrations_StudentRegistrationId",
                        column: x => x.StudentRegistrationId,
                        principalTable: "StudentRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentRegistrationDocuments_StudentRegistrationId",
                table: "StudentRegistrationDocuments",
                column: "StudentRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRegistrations_StudentCode",
                table: "StudentRegistrations",
                column: "StudentCode");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRegistrations_UserId",
                table: "StudentRegistrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRegistrations_Username",
                table: "StudentRegistrations",
                column: "Username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentRegistrationDocuments");

            migrationBuilder.DropTable(
                name: "StudentRegistrations");
        }
    }
}
