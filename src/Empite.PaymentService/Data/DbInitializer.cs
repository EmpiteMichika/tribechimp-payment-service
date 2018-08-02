using System.Linq;
using System.Threading.Tasks;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Interface;
using Microsoft.EntityFrameworkCore;

namespace Empite.PaymentService.Data
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _appDbContext;

        public DbInitializer(ApplicationDbContext context)
        {
            _appDbContext = context;
        }

        public async Task Initialize()
        {
            _appDbContext.Database.Migrate();
            if (!_appDbContext.ConfiguredPaymentGateways.Any())
            {
                _appDbContext.ConfiguredPaymentGateways.AddRange(new ConfiguredPaymentGateway
                {
                    GatewayName = "stripe",
                    IsEnabled = true
                });
                _appDbContext.SaveChanges();
            }
            
        }
    }
}
