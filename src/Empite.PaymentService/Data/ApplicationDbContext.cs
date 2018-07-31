using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Microsoft.EntityFrameworkCore;

namespace Empite.PaymentService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
            builder.Entity<InvoiceContact>().HasIndex(x => x.ExternalContactUserId);
            builder.Entity<Item>().HasIndex(x => x.ItemId);
            builder.Entity<Purchese>().HasIndex(x => x.ReferenceGuid);
            builder.Entity<Purchese>().HasIndex(x => x.InvoiceGatewayType);
            builder.Entity<InvoiceHistory>().HasIndex(x => x.InvoiceId);

            builder.Entity<InvoiceJobQueue>().HasIndex(x => x.InvoiceGatewayType);
            builder.Entity<InvoiceJobQueue>().Property(x=> x.LastErrorMessage).HasColumnType("longtext");
            builder.Entity<InvoiceJobQueue>().Property(x => x.JsonData).HasColumnType("longtext");

            builder.Entity<Item_Purchese>().HasKey(x => new {x.RecurringInvoiceId, x.ItemId});
            builder.Entity<Item_Purchese>().HasOne(p => p.Item).WithMany(p => p.RecurringInvoices)
                .HasForeignKey(p => p.ItemId);
            builder.Entity<Item_Purchese>().HasOne(p => p.Purchese).WithMany(p => p.Items)
                .HasForeignKey(p => p.RecurringInvoiceId);

        }
        public DbSet<ConfiguredPaymentGateway> ConfiguredPaymentGateways { get; set; }
        public DbSet<InvoiceContact> InvoiceContacts { get; set; }
        public DbSet<Purchese> Purcheses { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<InvoiceJobQueue> InvoiceJobQueues { get; set; }
        public DbSet<InvoiceHistory> InvoiceHistories { get; set; }
    }
}
