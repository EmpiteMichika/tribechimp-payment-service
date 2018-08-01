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
        private const int ResultPerPage = 100;
        private IServiceProvider _services { get; }
        private static bool isRunningCheckInvoicesDue = false;
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
        public async Task CheckInvoicesDueAsync()
        {
            throw new NotImplementedException();

            if (isRunningCheckInvoicesDue)
                return;
            isRunningCheckInvoicesDue = true;
            try
            {

                using (ApplicationDbContext dbContext = _services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
                {
                    int currentPage = 0;
                    int successCount = 0;
                    while (true)
                    {
                        await Task.Delay(10);
                        List<Purchese> recurringInvoices = dbContext.Purcheses
                            .Where(x => (x.IsPaidForThisMonth || x.UpdatedAt < DateTime.UtcNow.AddMonths(-1)) && x.DeletedAt == null)
                            .Skip((currentPage * ResultPerPage) - successCount).Take(ResultPerPage).ToList();
                        if (!recurringInvoices.Any())
                            break;
                        foreach (Purchese purchese in recurringInvoices)
                        {
                            //usin try catch to contine the flow
                            try
                            {
                                //bool res = await ProcessRecurringInvoice(purchese, dbContext);
                                //if (res)
                                //    successCount++;
                            }
                            catch (Exception ex)
                            {
                                //Todo Logging

                            }


                        }

                        currentPage++;
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
        //Retrn True If the recurring purchese is changes to paid, so we can get the skkippin elements
        private async Task<bool> CheckInvoice(Purchese purchese, ApplicationDbContext dbContext)
        {
            throw new NotImplementedException();
            //try
            //{
            //    HttpClient httpClient = _httpClientFactory.CreateClient();
            //    httpClient.AddZohoAuthorizationHeader(_zohoTokenService.GetOAuthToken().Result);
            //    Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("recurringinvoices", );
            //    HttpResponseMessage response = await httpClient.GetAsync(url);
            //    if (response.IsSuccessStatusCode)
            //    {
            //        var byteArray = await response.Content.ReadAsByteArrayAsync();
            //        var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
            //        ZohoInvoceService.RootRecurringPaymentCheckInvoiceClass enablePortalResponse =
            //            JsonConvert.DeserializeObject<ZohoInvoceService.RootRecurringPaymentCheckInvoiceClass>(responseString);
            //        if (enablePortalResponse.code == ZohoSuccessResponseCode)
            //        {
            //            if (enablePortalResponse.recurring_invoice.unpaid_child_invoices_count == 0)
            //            {
            //                purchese.IsDue = false;
            //                purchese.UpdatedAt = DateTime.UtcNow;
                           
            //                await dbContext.SaveChangesAsync();
            //                return true;
            //            }
            //            else
            //            {
            //                if (!purchese.IsDue)
            //                {
            //                    purchese.IsDue = true;
            //                    purchese.UpdatedAt = DateTime.UtcNow;
            //                    await dbContext.SaveChangesAsync();
            //                    return false;
            //                }
            //            }
            //        }
            //        else
            //        {

            //            throw new Exception($"Zoho Portle Enable failed. Respond with code {enablePortalResponse.code}, Message is => {enablePortalResponse.message}");
            //        }
            //    }
            //    else
            //    {
            //        throw new Exception($"Zoho Api call failed Erro code is => {response.StatusCode}. Reason sent by server is => {response.ReasonPhrase}.");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    //Todo Logging
            //    return false;
            //}

            //return false;
            return false;
        }

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
                            .Where(x => x.InvoiceType ==InvoicingType.Recurring && x.InvoiceStatus == InvoicingStatus.Active && x.InvoiceGatewayType == ExternalInvoiceGatewayType.Zoho)
                            .Skip((currentPage * ResultPerPage)).Take(ResultPerPage).ToList();
                        if (!recurringInvoices.Any())
                            break;
                        foreach (Purchese purchese in recurringInvoices)
                        {
                            //usin try catch to contine the flow
                            try
                            {
                                await Task.Delay(50);
                                // keep this logic to check the fresh created invoices. since it doesnt contain any Sub invoices in the job queue, and it doesn't do any performance hit.
                                bool isHistoryRecordExixsits = dbContext.InvoiceHistories.Include(x => x.Purchese)
                                    .Any(x => x.Purchese.Id == purchese.Id && x.CreatedAt.Date > DateTime.UtcNow.AddMonths(-1).Date);
                                if (isHistoryRecordExixsits)
                                {
                                    continue;
                                }

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
            }
            isRunningRecurringInvoiceCreate = false;
        }
    }
}
