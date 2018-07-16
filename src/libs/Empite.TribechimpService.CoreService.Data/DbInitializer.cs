using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Empite.TribechimpService.PaymentService.Domain.Dto;
using Empite.TribechimpService.PaymentService.Domain.Entity;
using Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated;
using Empite.TribechimpService.PaymentService.Domain.Interface.Service;
using Microsoft.EntityFrameworkCore;

namespace Empite.TribechimpService.PaymentService.Data
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
