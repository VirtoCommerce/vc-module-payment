using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.PaymentModule.Data.SqlServer.Migrations
{
    public partial class AddAllowDeferredPayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StorePaymentMethodEntity_TypeName_StoreId",
                table: "StorePaymentMethod");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "StorePaymentMethod");

            migrationBuilder.AddColumn<bool>(
                name: "AllowDeferredPayment",
                table: "StorePaymentMethod",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_StorePaymentMethodEntity_TypeName_StoreId",
                table: "StorePaymentMethod",
                columns: new[] { "TypeName", "StoreId" },
                unique: true,
                filter: "[TypeName] IS NOT NULL AND [StoreId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StorePaymentMethodEntity_TypeName_StoreId",
                table: "StorePaymentMethod");

            migrationBuilder.DropColumn(
                name: "AllowDeferredPayment",
                table: "StorePaymentMethod");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "StorePaymentMethod",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorePaymentMethodEntity_TypeName_StoreId",
                table: "StorePaymentMethod",
                columns: new[] { "TypeName", "StoreId" },
                unique: true,
                filter: "[StoreId] IS NOT NULL");
        }
    }
}
