using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Interface.Service.Zoho;
using Empite.PaymentService.Models.Configs;
using Empite.PaymentService.Models.Dto.Zoho;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Empite.PaymentService.Services.PaymentService.Zoho
{
    public class ZohoRecurringInvoiceService: IRecurringInvoiceService<ZohoRecurringInvoiceService>
    {
        
        private readonly Settings _settings;
        private readonly IZohoInvoiceSingleton _zohoTokenService;
        private readonly IHttpClientFactory _httpClientFactory;
        private const int ResultPerPage = 300;
        private IServiceProvider _services { get; }
        
        private static bool isRunningRecurringInvoiceCreate = false;

        private const int ZohoSuccessResponseCode = 0;
        public ZohoRecurringInvoiceService(IServiceProvider services, IZohoInvoiceSingleton zohoTokenService, IOptions<Settings> options, IHttpClientFactory httpClientFactory)
        {
            _zohoTokenService = zohoTokenService;
            _settings = options.Value;
            _httpClientFactory = httpClientFactory;
            _services = services;
        }
        
        [AutomaticRetry(Attempts = 0, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task CreateRecurringInvoice()
        {
            //throw new NotImplementedException();
            if (isRunningRecurringInvoiceCreate)
            {
                return;
            }

            isRunningRecurringInvoiceCreate = true;
            try
            {

                using (ApplicationDbContext dbContext = _services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
                {
                    int currentPage = 0;
                    
                    while (true)
                    {
                        await Task.Delay(10);
                        List<Purchese> recurringInvoices = dbContext.Purcheses
                            .Where(x => x.InvoiceType ==InvoicingType.Recurring && x.InvoiceStatus == InvoicingStatus.Active 
                                                                                && x.InvoiceGatewayType == ExternalInvoiceGatewayType.Zoho
                                                                                && x.LastSuccessInvoiceIssue.Date <= DateTime.UtcNow.AddMonths(-1).Date)
                            .Skip((currentPage * ResultPerPage)).OrderBy(x=> x.CreatedAt).Take(ResultPerPage).ToList();
                        if (!recurringInvoices.Any())
                        {
                            var temp = dbContext.Purcheses
                                .Any(x => x.InvoiceType == InvoicingType.Recurring && x.InvoiceStatus ==
                                                                             InvoicingStatus.Active
                                                                             && x.InvoiceGatewayType ==
                                                                             ExternalInvoiceGatewayType.Zoho
                                                                             && x.LastSuccessInvoiceIssue.Date <=
                                                                             DateTime.UtcNow.AddMonths(-1).Date);
                            if (temp)
                            {
                                currentPage--;
                                continue;
                            }
                            else
                            {
                                break;
                            }
                            
                        }
                            
                        foreach (Purchese purchese in recurringInvoices)
                        {
                            //using try catch to contine the flow
                            try
                            {
                                await Task.Delay(50);
                                
                                bool isDbJobQueExists = dbContext.InvoiceJobQueues.Any(x =>
                                    x.CreatedAt.Date > DateTime.UtcNow.AddMonths(-1).Date && x.JsonData == purchese.Id && x.JobType == InvoiceJobQueueType.CreateSubInvoice);
                                if (isDbJobQueExists)
                                {
                                    continue;
                                }
                                InvoiceJobQueue dbJobQueue = new InvoiceJobQueue
                                {
                                    InvoiceGatewayType = ExternalInvoiceGatewayType.Zoho,
                                    JobType = InvoiceJobQueueType.CreateSubInvoice,
                                    JsonData = purchese.Id
                                };
                                dbContext.InvoiceJobQueues.Add(dbJobQueue);
                                await dbContext.SaveChangesAsync();
                                
                            }
                            catch (Exception ex)
                            {
                                //Todo Logging
                                throw ex;
                            }


                        }

                        currentPage++;
                    }


                }
            }
            catch (Exception ex)
            {
                isRunningRecurringInvoiceCreate = false;
                //Todo Logger
                throw ex;
            }
            isRunningRecurringInvoiceCreate = false;
        }
    }
}
