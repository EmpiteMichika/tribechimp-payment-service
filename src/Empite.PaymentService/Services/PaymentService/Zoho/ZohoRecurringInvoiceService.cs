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
using Microsoft.Extensions.Logging;
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
        private readonly Guid _sessionGuid;
        private ILogger<ZohoRecurringInvoiceService> _logger;

        private const int ZohoSuccessResponseCode = 0;
        public ZohoRecurringInvoiceService(IServiceProvider services, ILogger<ZohoRecurringInvoiceService> logger, IZohoInvoiceSingleton zohoTokenService, IOptions<Settings> options, IHttpClientFactory httpClientFactory)
        {
            _zohoTokenService = zohoTokenService;
            _settings = options.Value;
            _httpClientFactory = httpClientFactory;
            _services = services;
            _sessionGuid = Guid.NewGuid();
            _logger = logger;
        }
        /// <summary>
        /// Log Errors
        /// </summary>
        /// <param name="message"></param>
        private void LogError(string message)
        {
            string finalMessage = ($"Guid:{_sessionGuid.ToString()} || ");
            finalMessage += message;
            _logger.LogError(finalMessage);
        }
        /// <summary>
        /// Log Informations
        /// </summary>
        /// <param name="message"></param>
        private void LogInformation(string message)
        {
            string finalMessage = ($"Guid:{_sessionGuid.ToString()} || ");
            finalMessage += message;
            _logger.LogInformation(finalMessage);
        }

        [AutomaticRetry(Attempts = 0, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task CreateRecurringInvoice()
        {
            if (ZohoRecurringInvoiceServiceStatics.isRunningRecurringInvoiceCreate)
            {
                return;
            }
            LogInformation("+++++++++++++++++++++ Starting Creating Recurring invoice Service +++++++++++++++++++++++++++");
            ZohoRecurringInvoiceServiceStatics.isRunningRecurringInvoiceCreate = true;
            try
            {

                using (ApplicationDbContext dbContext = _services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
                {
                    int currentPage = 0;
                    
                    while (true)
                    {
                        await Task.Delay(5000);
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
                                
                                LogError($"Error in Purcess loop.Purches id is {purchese.Id} Exception is => {ex.Message}, Stacktrace is => {ex.StackTrace}");
                                throw ex;
                            }


                        }

                        currentPage++;
                    }


                }
            }
            catch (Exception ex)
            {
                ZohoRecurringInvoiceServiceStatics.isRunningRecurringInvoiceCreate = false;
                
                LogError($"Error in creating recurring invoices service. Exception is => {ex.Message}, Stacktrace is => {ex.StackTrace}");
                LogInformation("+++++++++++++++++++++ Stop Creating Recurring invoice Service +++++++++++++++++++++++++++");
                throw ex;
            }
            ZohoRecurringInvoiceServiceStatics.isRunningRecurringInvoiceCreate = false;
            LogInformation("+++++++++++++++++++++ Stop Creating Recurring invoice Service +++++++++++++++++++++++++++");
        }
    }

    public static class ZohoRecurringInvoiceServiceStatics
    {
        public static bool isRunningRecurringInvoiceCreate { get; set; } = false;
    }

}
