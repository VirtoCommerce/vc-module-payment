using System.Reflection;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.PaymentModule.Data.Repositories
{
    public class PaymentDbContext : DbContextBase
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        protected PaymentDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StorePaymentMethodEntity>().ToTable("StorePaymentMethod").HasKey(x => x.Id);
            modelBuilder.Entity<StorePaymentMethodEntity>().Property(x => x.Id).HasMaxLength(128).ValueGeneratedOnAdd();
            modelBuilder.Entity<StorePaymentMethodEntity>().Property(x => x.StoreId).HasMaxLength(128);
            modelBuilder.Entity<StorePaymentMethodEntity>().Property(x => x.TypeName).HasMaxLength(128);

            modelBuilder.Entity<StorePaymentMethodEntity>().HasIndex(x => new { x.TypeName, x.StoreId })
                .HasDatabaseName("IX_StorePaymentMethodEntity_TypeName_StoreId")
                .IsUnique();

            modelBuilder.Entity<PaymentMethodLocalizedNameEntity>(builder =>
            {
                builder.ToTable("PaymentMethodLocalizedName").HasKey(x => x.Id);
                builder.Property(x => x.Id).HasMaxLength(IdLength).ValueGeneratedOnAdd();

                builder.HasOne(x => x.ParentEntity)
                    .WithMany(x => x.LocalizedNames)
                    .HasForeignKey(x => x.ParentEntityId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasIndex(x => new { x.LanguageCode, x.ParentEntityId }).IsUnique()
                    .HasDatabaseName("IX_PaymentMethodLocalizedName_LanguageCode_ParentEntityId");
            });

            base.OnModelCreating(modelBuilder);


            // Allows configuration for an entity type for different database types.
            // Applies configuration from all <see cref="IEntityTypeConfiguration{TEntity}" in VirtoCommerce.PaymentModule.Data.XXX project. /> 
            switch (this.Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.PaymentModule.Data.MySql"));
                    break;
                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.PaymentModule.Data.PostgreSql"));
                    break;
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.PaymentModule.Data.SqlServer"));
                    break;
            }

        }
    }
}
