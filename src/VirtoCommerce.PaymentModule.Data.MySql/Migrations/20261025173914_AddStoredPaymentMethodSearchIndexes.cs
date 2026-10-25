using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.PaymentModule.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredPaymentMethodSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StoredPaymentMethod_InitialPaymentId",
                table: "StoredPaymentMethod",
                column: "InitialPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredPaymentMethod_OrganizationId",
                table: "StoredPaymentMethod",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredPaymentMethod_PaymentMethodCode",
                table: "StoredPaymentMethod",
                column: "PaymentMethodCode");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConsent_StoredPaymentMethodId",
                table: "PaymentConsent",
                column: "StoredPaymentMethodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredPaymentMethod_InitialPaymentId",
                table: "StoredPaymentMethod");

            migrationBuilder.DropIndex(
                name: "IX_StoredPaymentMethod_OrganizationId",
                table: "StoredPaymentMethod");

            migrationBuilder.DropIndex(
                name: "IX_StoredPaymentMethod_PaymentMethodCode",
                table: "StoredPaymentMethod");

            migrationBuilder.DropIndex(
                name: "IX_PaymentConsent_StoredPaymentMethodId",
                table: "PaymentConsent");
        }
    }
}
