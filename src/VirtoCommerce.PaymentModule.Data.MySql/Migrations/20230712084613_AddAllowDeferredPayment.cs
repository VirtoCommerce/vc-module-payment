using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.PaymentModule.Data.MySql.Migrations
{
    public partial class AddAllowDeferredPayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowDeferredPayment",
                table: "StorePaymentMethod",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowDeferredPayment",
                table: "StorePaymentMethod");
        }
    }
}
