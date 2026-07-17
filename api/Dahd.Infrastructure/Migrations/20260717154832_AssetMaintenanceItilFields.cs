using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dahd.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssetMaintenanceItilFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                table: "MaintenanceJobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Impact",
                table: "MaintenanceJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "MaintenanceJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProblemType",
                table: "MaintenanceJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Urgency",
                table: "MaintenanceJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CalibrationDate",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CalibrationDueDate",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Criticality",
                table: "Assets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "InstallationDate",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "InvoiceDate",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PoDate",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoNumber",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractType",
                table: "AmcContracts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deadline",
                table: "MaintenanceJobs");

            migrationBuilder.DropColumn(
                name: "Impact",
                table: "MaintenanceJobs");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "MaintenanceJobs");

            migrationBuilder.DropColumn(
                name: "ProblemType",
                table: "MaintenanceJobs");

            migrationBuilder.DropColumn(
                name: "Urgency",
                table: "MaintenanceJobs");

            migrationBuilder.DropColumn(
                name: "CalibrationDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CalibrationDueDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Criticality",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "InstallationDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "InvoiceDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PoDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PoNumber",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ContractType",
                table: "AmcContracts");
        }
    }
}
