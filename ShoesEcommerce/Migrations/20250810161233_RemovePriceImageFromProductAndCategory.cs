using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoesEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class RemovePriceImageFromProductAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRole_Customers_CustomerId",
                table: "UserRole");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRole_Customers_CustomerId1",
                table: "UserRole");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRole_Roles_RoleId",
                table: "UserRole");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRole_Staffs_StaffId",
                table: "UserRole");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRole_Staffs_StaffId1",
                table: "UserRole");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRole",
                table: "UserRole");

            migrationBuilder.DropIndex(
                name: "IX_UserRole_StaffId1",
                table: "UserRole");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "StaffId1",
                table: "UserRole");

            migrationBuilder.RenameTable(
                name: "UserRole",
                newName: "UserRoles");

            migrationBuilder.RenameColumn(
                name: "PhnoneNumber",
                table: "Staffs",
                newName: "PhoneNumber");

            migrationBuilder.RenameIndex(
                name: "IX_UserRole_StaffId",
                table: "UserRoles",
                newName: "IX_UserRoles_StaffId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRole_RoleId",
                table: "UserRoles",
                newName: "IX_UserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRole_CustomerId1",
                table: "UserRoles",
                newName: "IX_UserRoles_CustomerId1");

            migrationBuilder.RenameIndex(
                name: "IX_UserRole_CustomerId",
                table: "UserRoles",
                newName: "IX_UserRoles_CustomerId");

            migrationBuilder.AddColumn<string>(
                name: "ContactInfo",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Customers_CustomerId",
                table: "UserRoles",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Customers_CustomerId1",
                table: "UserRoles",
                column: "CustomerId1",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Staffs_StaffId",
                table: "UserRoles",
                column: "StaffId",
                principalTable: "Staffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Customers_CustomerId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Customers_CustomerId1",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Staffs_StaffId",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "ContactInfo",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "ProductVariants");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                newName: "UserRole");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Staffs",
                newName: "PhnoneNumber");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_StaffId",
                table: "UserRole",
                newName: "IX_UserRole_StaffId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRole",
                newName: "IX_UserRole_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_CustomerId1",
                table: "UserRole",
                newName: "IX_UserRole_CustomerId1");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_CustomerId",
                table: "UserRole",
                newName: "IX_UserRole_CustomerId");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StaffId1",
                table: "UserRole",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRole",
                table: "UserRole",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_StaffId1",
                table: "UserRole",
                column: "StaffId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRole_Customers_CustomerId",
                table: "UserRole",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRole_Customers_CustomerId1",
                table: "UserRole",
                column: "CustomerId1",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRole_Roles_RoleId",
                table: "UserRole",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRole_Staffs_StaffId",
                table: "UserRole",
                column: "StaffId",
                principalTable: "Staffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRole_Staffs_StaffId1",
                table: "UserRole",
                column: "StaffId1",
                principalTable: "Staffs",
                principalColumn: "Id");
        }
    }
}
