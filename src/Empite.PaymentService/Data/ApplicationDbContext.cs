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
            builder.Entity<InvoiceContact>().HasIndex(x => x.ZohoContactUserId);
            builder.Entity<ZohoItem>().HasIndex(x => x.ZohoItemId);
            builder.Entity<Purchese>().HasIndex(x => x.ReferenceGuid);
            builder.Entity<InvoiceHistory>().HasIndex(x => x.ZohoInvoiceId);

            builder.Entity<ZohoInvoiceJobQueue>().Property(x=> x.LastErrorMessage).HasColumnType("longtext");
            builder.Entity<ZohoInvoiceJobQueue>().Property(x => x.JsonData).HasColumnType("longtext");

            builder.Entity<ZohoItem_Purchese>().HasKey(x => new {x.RecurringInvoiceId, x.ZohoItemId});
            builder.Entity<ZohoItem_Purchese>().HasOne(p => p.ZohoItem).WithMany(p => p.RecurringInvoices)
                .HasForeignKey(p => p.ZohoItemId);
            builder.Entity<ZohoItem_Purchese>().HasOne(p => p.Purchese).WithMany(p => p.ZohoItems)
                .HasForeignKey(p => p.RecurringInvoiceId);

        }
        public DbSet<ConfiguredPaymentGateway> ConfiguredPaymentGateways { get; set; }
        public DbSet<InvoiceContact> InvoiceContacts { get; set; }
        public DbSet<Purchese> Purcheses { get; set; }
        public DbSet<ZohoItem> ZohoItems { get; set; }
        public DbSet<ZohoInvoiceJobQueue> ZohoInvoiceJobQueues { get; set; }
        public DbSet<InvoiceHistory> InvoiceHistories { get; set; }
    }
}
