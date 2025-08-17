using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoesEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class RefactorStockService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocks_ProductVariantId",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "ProductVariants");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "StockTransactions",
                newName: "ReservedQuantityBefore");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "StockTransactions",
                newName: "ReferenceType");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Stocks",
                newName: "ReservedQuantity");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "StockEntries",
                newName: "QuantityReceived");

            migrationBuilder.AddColumn<int>(
                name: "AvailableQuantityAfter",
                table: "StockTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AvailableQuantityBefore",
                table: "StockTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StockTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "StockTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "QuantityChange",
                table: "StockTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "StockTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReferenceId",
                table: "StockTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReservedQuantityAfter",
                table: "StockTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AvailableQuantity",
                table: "Stocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Stocks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Stocks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "StockEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsProcessed",
                table: "StockEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "StockEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceivedBy",
                table: "StockEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "StockEntries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductVariantId",
                table: "Stocks",
                column: "ProductVariantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocks_ProductVariantId",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "AvailableQuantityAfter",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "AvailableQuantityBefore",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "QuantityChange",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "ReservedQuantityAfter",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "AvailableQuantity",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "StockEntries");

            migrationBuilder.DropColumn(
                name: "IsProcessed",
                table: "StockEntries");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "StockEntries");

            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                table: "StockEntries");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "StockEntries");

            migrationBuilder.RenameColumn(
                name: "ReservedQuantityBefore",
                table: "StockTransactions",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "ReferenceType",
                table: "StockTransactions",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "ReservedQuantity",
                table: "Stocks",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "QuantityReceived",
                table: "StockEntries",
                newName: "Quantity");

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductVariantId",
                table: "Stocks",
                column: "ProductVariantId");
        }
    }
}
