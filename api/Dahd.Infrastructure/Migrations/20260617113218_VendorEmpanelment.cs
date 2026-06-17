using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dahd.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VendorEmpanelment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    State = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Pincode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Gstin = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Pan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UdyamRegNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsManufacturer = table.Column<bool>(type: "bit", nullable: false),
                    IsMsme = table.Column<bool>(type: "bit", nullable: false),
                    Categories = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnderReviewAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlacklistedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ReviewRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ScheduledInspectionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InspectionRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BlacklistReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EmpanelmentValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    StorageRef = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IssuingAuthority = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CertificateNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IssuedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorDocuments_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorDocuments_VendorId_DocumentType",
                table: "VendorDocuments",
                columns: new[] { "VendorId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Status",
                table: "Vendors",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_UserId",
                table: "Vendors",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorDocuments");

            migrationBuilder.DropTable(
                name: "Vendors");
        }
    }
}
