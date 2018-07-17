using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Empite.TribechimpService.Core;
using Empite.TribechimpService.PaymentService.Data;
using Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated;
using Empite.TribechimpService.PaymentService.Domain.Interface.Service;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Empite.TribechimpService.PaymentService.Service
{
    public class ZohoInvoiceDueCheckerSingleton: IZohoInvoiceDueCheckerSingleton
    {
        
        private readonly Settings _settings;
        private readonly IZohoInvoiceSingleton _zohoTokenService;
        private readonly IHttpClientFactory _httpClientFactory;
        private const int ResultPerPage = 10;
        private IServiceProvider _services { get; }
        private static bool isRunningCheckInvoicesDue = false;
        public ZohoInvoiceDueCheckerSingleton(IServiceProvider services)
        {
            _services = services;
        }
        [AutomaticRetry(Attempts = 0, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task CheckInvoicesDueAsync()
        {
            if (isRunningCheckInvoicesDue)
                return;
            isRunningCheckInvoicesDue = true;
            try
            {
                
                using (ApplicationDbContext dbContext = _services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
                {
                    int currentPage = 0;
                    while (true)
                    {
                        await Task.Delay(10);
                        List<RecurringInvoice> recurringInvoices = dbContext.RecurringInvoices
                            .Where(x => x.IsDue || x.UpdatedAt < DateTime.UtcNow.AddMonths(-1))
                            .Skip(currentPage * ResultPerPage).Take(ResultPerPage).ToList();
                        if(!recurringInvoices.Any())
                            break;
                        foreach (RecurringInvoice recurringInvoice in recurringInvoices)
                        {
                            
                        }
                    }
                    

                }
            }
            catch (Exception ex)
            {
                isRunningCheckInvoicesDue = false;
                //Todo Logger
            }
            isRunningCheckInvoicesDue = false;
        }
    }
}
