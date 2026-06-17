using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dahd.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StockFlowAndBreachAck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "ColdChainLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcknowledgedBy",
                table: "ColdChainLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AffectedBatchIdsJson",
                table: "ColdChainLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectiveAction",
                table: "ColdChainLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "ColdChainLogs");

            migrationBuilder.DropColumn(
                name: "AcknowledgedBy",
                table: "ColdChainLogs");

            migrationBuilder.DropColumn(
                name: "AffectedBatchIdsJson",
                table: "ColdChainLogs");

            migrationBuilder.DropColumn(
                name: "CorrectiveAction",
                table: "ColdChainLogs");
        }
    }
}
