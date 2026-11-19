using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.PaymentModule.Data.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredPaymentMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentConsent",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StoreId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CustomerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StoredPaymentMethodId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TermsVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TermsText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermsHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Channel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    AcceptedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ObjectId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentConsent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredPaymentMethod",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StoreId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CustomerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PaymentMethodCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderCustomerRef = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    ExpirationMonth = table.Column<int>(type: "int", nullable: true),
                    ExpirationYear = table.Column<int>(type: "int", nullable: true),
                    CardholderName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Fingerprint = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NetworkTransactionId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    InitialTransactionRef = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    InitialOrderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InitialPaymentId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Usage = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsAuthenticated = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    LastUsedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByProviderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsentId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredPaymentMethod", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConsent_CustomerId_StoreId",
                table: "PaymentConsent",
                columns: new[] { "CustomerId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConsent_ObjectType_ObjectId",
                table: "PaymentConsent",
                columns: new[] { "ObjectType", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredPaymentMethod_CustomerId_StoreId",
                table: "StoredPaymentMethod",
                columns: new[] { "CustomerId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredPaymentMethod_InitialOrderId",
                table: "StoredPaymentMethod",
                column: "InitialOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentConsent");

            migrationBuilder.DropTable(
                name: "StoredPaymentMethod");
        }
    }
}
