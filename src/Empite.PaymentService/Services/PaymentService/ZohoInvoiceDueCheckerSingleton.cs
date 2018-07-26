using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.TribechimpService.Core;
using Empite.TribechimpService.PaymentService.Data;
using Empite.TribechimpService.PaymentService.Domain.Interface.Service;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Empite.TribechimpService.PaymentService.Service
{
    public class ZohoInvoiceDueCheckerSingleton: IZohoInvoiceDueCheckerSingleton
    {
        
        private readonly Settings _settings;
        private readonly IZohoInvoiceSingleton _zohoTokenService;
        private readonly IHttpClientFactory _httpClientFactory;
        private const int ResultPerPage = 100;
        private IServiceProvider _services { get; }
        private static bool isRunningCheckInvoicesDue = false;
        private const int ZohoSuccessResponseCode = 0;
        public ZohoInvoiceDueCheckerSingleton(IServiceProvider services, IZohoInvoiceSingleton zohoTokenService, IOptions<Settings> options, IHttpClientFactory httpClientFactory)
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
            //if (isRunningCheckInvoicesDue)
            //    return;
            //isRunningCheckInvoicesDue = true;
            //try
            //{
                
            //    using (ApplicationDbContext dbContext = _services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
            //    {
            //        int currentPage = 0;
            //        int successCount = 0;
            //        while (true)
            //        {
            //            await Task.Delay(10);
            //            List<purchese> recurringInvoices = dbContext.RecurringInvoices
            //                .Where(x =>( x.IsDue || x.UpdatedAt < DateTime.UtcNow.AddMonths(-1) ) && x.DeletedAt == null )
            //                .Skip((currentPage * ResultPerPage )- successCount).Take(ResultPerPage).ToList();
            //            if(!recurringInvoices.Any())
            //                break;
            //            foreach (purchese purchese in recurringInvoices)
            //            {
            //                //usin try catch to contine the flow
            //                try
            //                {
            //                    bool res = await ProcessRecurringInvoice(purchese, dbContext);
            //                    if (res)
            //                        successCount++;
            //                }
            //                catch (Exception ex)
            //                {
            //                    //Todo Logging

            //                }
                           

            //            }

            //            currentPage++;
            //        }
                    

            //    }
            //}
            //catch (Exception ex)
            //{
            //    isRunningCheckInvoicesDue = false;
            //    //Todo Logger
            //}
            //isRunningCheckInvoicesDue = false;
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
    }
}
