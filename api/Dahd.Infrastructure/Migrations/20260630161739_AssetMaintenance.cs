using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dahd.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssetMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetTag = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationNote = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PurchaseCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WarrantyUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Assets_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AmcContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AnnualCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Coverage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmcContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmcContracts_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FrequencyDays = table.Column<int>(type: "int", nullable: false),
                    LastServiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportedBy = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AssignedTo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceJobs_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceJobs_MaintenanceSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "MaintenanceSchedules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmcContracts_AssetId",
                table: "AmcContracts",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AmcContracts_ContractNumber",
                table: "AmcContracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetTag",
                table: "Assets",
                column: "AssetTag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_FacilityId",
                table: "Assets",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Status",
                table: "Assets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_WarehouseId",
                table: "Assets",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceJobs_AssetId_Status",
                table: "MaintenanceJobs",
                columns: new[] { "AssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceJobs_JobNumber",
                table: "MaintenanceJobs",
                column: "JobNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceJobs_ScheduleId",
                table: "MaintenanceJobs",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_AssetId",
                table: "MaintenanceSchedules",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_NextDueDate",
                table: "MaintenanceSchedules",
                column: "NextDueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmcContracts");

            migrationBuilder.DropTable(
                name: "MaintenanceJobs");

            migrationBuilder.DropTable(
                name: "MaintenanceSchedules");

            migrationBuilder.DropTable(
                name: "Assets");
        }
    }
}
