using Empite.TribechimpService.PaymentService.Domain.Entity;
using Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated;
using Microsoft.EntityFrameworkCore;

namespace Empite.TribechimpService.PaymentService.Data
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
            builder.Entity<ZohoInvoiceJobQueue>().Property(x=> x.LastErrorMessage).HasColumnType("longtext");
            builder.Entity<ZohoInvoiceJobQueue>().Property(x => x.JsonData).HasColumnType("longtext");

            builder.Entity<ZohoItemRecurringInvoice>().HasKey(x => new {x.RecurringInvoiceId, x.ZohoItemId});
            builder.Entity<ZohoItemRecurringInvoice>().HasOne(p => p.ZohoItem).WithMany(p => p.RecurringInvoices)
                .HasForeignKey(p => p.ZohoItemId);
            builder.Entity<ZohoItemRecurringInvoice>().HasOne(p => p.RecurringInvoice).WithMany(p => p.ZohoItems)
                .HasForeignKey(p => p.RecurringInvoiceId);

        }
        public DbSet<ConfiguredPaymentGateway> ConfiguredPaymentGateways { get; set; }
        public DbSet<InvoiceContact> InvoiceContacts { get; set; }
        public DbSet<RecurringInvoice> RecurringInvoices { get; set; }
        public DbSet<ZohoItem> ZohoItems { get; set; }
        public DbSet<ZohoInvoiceJobQueue> ZohoInvoiceJobQueues { get; set; }
    }
}
