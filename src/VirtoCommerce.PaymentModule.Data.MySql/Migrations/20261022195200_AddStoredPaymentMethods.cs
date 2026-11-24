using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.PaymentModule.Data.MySql.Migrations
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
                    Id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StoreId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StoredPaymentMethodId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Purpose = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TermsVersion = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TermsText = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TermsHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Channel = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AcceptedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevokedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ObjectType = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ObjectId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifiedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentConsent", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StoredPaymentMethod",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StoreId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrganizationId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentMethodCode = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderCustomerRef = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Token = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Brand = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Last4 = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpirationMonth = table.Column<int>(type: "int", nullable: true),
                    ExpirationYear = table.Column<int>(type: "int", nullable: true),
                    CardholderName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fingerprint = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NetworkTransactionId = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InitialTransactionRef = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InitialOrderId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InitialPaymentId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Usage = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAuthenticated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastUsedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedByProviderDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConsentId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifiedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredPaymentMethod", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
