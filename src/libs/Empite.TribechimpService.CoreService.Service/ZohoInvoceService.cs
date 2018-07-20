using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Empite.Core.Extensions;
using Empite.TribechimpService.Core;
using Empite.TribechimpService.PaymentService.Data;
using Empite.TribechimpService.PaymentService.Domain.Dto;
using Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated;
using Empite.TribechimpService.PaymentService.Domain.Interface.Service;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Empite.TribechimpService.PaymentService.Service
{
    public class ZohoInvoceService: IZohoInvoceService
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IZohoInvoiceSingleton _zohoTokenService;
        private static bool _isJobsProcessing = false;
        private const int RowsPerPage = 100;
        private readonly Settings _settings;
        private const int ZohoSuccessResponseCode = 0;
        private const int ZohoPaymentTermDaysGap = 10;
        private readonly IHttpClientFactory _httpClientFactory;
        private IServiceProvider _services { get; }

        public ZohoInvoceService(IZohoInvoiceSingleton zohoTokenService, IOptions<Settings> options, IHttpClientFactory httpClientFactory, IServiceProvider services)
        {
            
            _zohoTokenService = zohoTokenService;
            _settings = options.Value;
            _httpClientFactory = httpClientFactory;
            _services = services;
        }

        public async Task AddJob(dynamic DataObject, ZohoInvoiceJobQueueType JobType)
        {
            using (ApplicationDbContext _dbContext = _services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                ZohoInvoiceJobQueue job = new ZohoInvoiceJobQueue();
                job.IsSuccess = false;
                job.JobType = JobType;
                if (JobType == ZohoInvoiceJobQueueType.CreateContact)
                {
                    if (DataObject?.GetType() != typeof(Domain.Dto.CreateContact))
                    {
                        throw new Exception("Invalid data type for the DataObject parameter, it should be created from CreateContact class");
                    }
                    job.JsonData = JsonConvert.SerializeObject(DataObject);
                }
                else if (JobType == ZohoInvoiceJobQueueType.EnableClientPortle)
                {
                    if (DataObject?.GetType() != typeof(string))
                    {
                        throw new Exception("Invalid data type for the DataObject parameter, it should be a string");
                    }
                    job.JsonData = DataObject;
                }
                else if (JobType == ZohoInvoiceJobQueueType.EnablePaymentReminders)
                {
                    if (DataObject?.GetType() != typeof(string))
                    {
                        throw new Exception("Invalid data type for the DataObject parameter, it should be a string");
                    }
                    job.JsonData = DataObject;
                }else if (JobType == ZohoInvoiceJobQueueType.CreateItem)
                {
                    if (DataObject?.GetType() != typeof(Domain.Dto.CreateZohoItemDto))
                    {
                        throw new Exception("Invalid data type for the DataObject parameter, it should be created from CreateZohoItemDto class");
                    }
                    job.JsonData = JsonConvert.SerializeObject(DataObject);
                }else if (JobType == ZohoInvoiceJobQueueType.CreateRecurringInvoice)
                {
                    if (DataObject?.GetType() != typeof(Domain.Dto.CreateRecurringInvoiceDto))
                    {
                        throw new Exception("Invalid data type for the DataObject parameter, it should be created from CreateZohoItemDto class");
                    }
                    job.JsonData = JsonConvert.SerializeObject(DataObject);
                }else if (JobType == ZohoInvoiceJobQueueType.CreateFirstInvoice)
                {
                    if (DataObject?.GetType() != typeof(string))
                    {
                        throw new Exception("Invalid data type for the DataObject parameter, it should be a string");
                    }
                    job.JsonData = DataObject;
                }
                else
                {
                    throw new Exception("Invalid Job type");
                }
                _dbContext.ZohoInvoiceJobQueues.Add(job);
                await _dbContext.SaveChangesAsync(); 
            }
        }
        /// <summary>
        /// Root function for the Running saved invoice related jobs in the database
        /// </summary>
        /// <returns></returns>
        public async Task RunJobs()
        {
            if (_isJobsProcessing)
            {
                return;

            }

            _isJobsProcessing = true;
            try
            {
                using (ApplicationDbContext _dbContext = _services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
                {
                    int count = _dbContext.ZohoInvoiceJobQueues.Count(x => x.IsSuccess == false);
                    int pages = Convert.ToInt32((count / 100));

                    for (int y = 0; y <= pages; y++)
                    {
                        List<ZohoInvoiceJobQueue> jobs = _dbContext.ZohoInvoiceJobQueues.Where(x => x.IsSuccess == false).OrderBy(x => x.CreatedAt)
                            .Skip(pages * RowsPerPage).Take(RowsPerPage).ToList();
                        foreach (ZohoInvoiceJobQueue job in jobs)
                        {
                            await Task.Delay(10);
                            try
                            {
                                await JobRunner(job, _dbContext);
                            }
                            catch (Exception ex)
                            {
                                //Todo logging
                                //Log With the Job ID
                                job.ReTryCount++;
                                job.LastErrorMessage =
                                    $"Exception message is => {ex.Message}, Stacktrace is => {ex.StackTrace}";
                                await _dbContext.SaveChangesAsync();
                            }

                        }
                    } 
                }
                
            }
            catch (Exception ex)
            {
                _isJobsProcessing = false;
                //Todo Logging
            }

            _isJobsProcessing = false;
            
        }
        private async Task<InvoiceContact> CreateContact(CreateContact model, ApplicationDbContext _dbContext)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("contacts");
            ContactPersonRoot contactPerson = new ContactPersonRoot
            {
                contact_name = model.FirstName + " " + model.LastName,
                payment_terms = _settings.ZohoAccount.PaymentTerm,
                contact_persons = new List<ContactPerson>
                {
                    new ContactPerson
                    {
                        email = model.Email,
                        first_name = model.FirstName,
                        is_primary_contact = true,
                        last_name = model.LastName,
                        mobile = model.Mobile
                    }
                }
            };
            string jsonString = JsonConvert.SerializeObject(contactPerson);
            MultipartFormDataContent form = new MultipartFormDataContent();
            StringContent content = new StringContent(jsonString);
            form.Add(content, "JSONString");
            HttpResponseMessage response = await httpClient.PostAsync(url, form);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                RootContactResponse contactResponse =
                    JsonConvert.DeserializeObject<RootContactResponse>(responseString);
                if (contactResponse.code == ZohoSuccessResponseCode)
                {
                    if (!string.IsNullOrWhiteSpace(contactResponse.contact.contact_id) &&
                        !string.IsNullOrWhiteSpace(contactResponse.contact.primary_contact_id))
                    {
                        InvoiceContact dbInvoiceContact = new InvoiceContact
                        {
                            Email = model.Email,
                            UserId = model.UserId,
                            ZohoContactUserId = contactResponse.contact.contact_id,
                            ZohoPrimaryContactId = contactResponse.contact.primary_contact_id
                        };
                        _dbContext.InvoiceContacts.Add(dbInvoiceContact);
                        await _dbContext.SaveChangesAsync();
                        return dbInvoiceContact;
                    }
                    else
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.Append((string.IsNullOrWhiteSpace(contactResponse.contact.contact_id) ? "Contact id is empty. " : ""));
                        builder.Append((string.IsNullOrWhiteSpace(contactResponse.contact.primary_contact_id) ? "Primary Contact id is empty. " : ""));
                        throw new Exception($"Response Filed came empty. Fields => {builder.ToString()}");
                    }

                }
                else
                {
                    throw new Exception($"Zoho returns a erro code. Erro code is => {contactResponse.code}. Message is => {contactResponse.message}.");

                }
            }
            else
            {
                throw new Exception($"Zoho Api call failed Erro code is => {response.StatusCode}. Reason sent by server is => {response.ReasonPhrase}.");
            }
        }

        private async Task<bool> EnableClientProtle(InvoiceContact contactDetails)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(_zohoTokenService.GetOAuthToken().Result);
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("contacts", contactDetails.ZohoContactUserId, "portal", "enable");

            ContactPersonPortalRoot contactPersonPortalRoot = new ContactPersonPortalRoot
            {
                contact_persons = new List<ContactPersonPortal>
                    {
                        new ContactPersonPortal
                        {
                            contact_person_id = contactDetails.ZohoPrimaryContactId
                        }
                    }
            };
            string jsonRequestBody = JsonConvert.SerializeObject(contactPersonPortalRoot);
            MultipartFormDataContent form = new MultipartFormDataContent();
            StringContent content = new StringContent(jsonRequestBody);
            form.Add(content, "JSONString");
            HttpResponseMessage response = await httpClient.PostAsync(url, form);
            if (response.IsSuccessStatusCode)
            {
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                RootZohoBasicResponse enablePortalResponse =
                    JsonConvert.DeserializeObject<RootZohoBasicResponse>(responseString);
                if (enablePortalResponse.code == ZohoSuccessResponseCode)
                {
                    return true;
                }
                else
                {

                    throw new Exception($"Zoho Portle Enable failed. Respond with code {enablePortalResponse.code}, Message is => {enablePortalResponse.message}");
                }
            }
            else
            {
                throw new Exception($"Zoho Api call failed Erro code is => {response.StatusCode}. Reason sent by server is => {response.ReasonPhrase}.");
            }

        }

        private async Task<bool> EnablePaymentReminder(InvoiceContact contactDetails)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("contacts", contactDetails.ZohoContactUserId, "paymentreminder", "enable");
            HttpResponseMessage response = await httpClient.PostAsync(url, null);
            if (response.IsSuccessStatusCode)
            {
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                RootZohoBasicResponse enablePortalResponse =
                    JsonConvert.DeserializeObject<RootZohoBasicResponse>(responseString);
                if (enablePortalResponse.code == ZohoSuccessResponseCode)
                {
                    return true;
                }
                else
                {

                    throw new Exception($"Zoho Portle Enable failed. Respond with code {enablePortalResponse.code}, Message is => {enablePortalResponse.message}");
                }
            }
            else
            {
                throw new Exception($"Zoho Api call failed Erro code is => {response.StatusCode}. Reason sent by server is => {response.ReasonPhrase}.");
            }
        }

        private async Task<ZohoItem> CreateItem(CreateZohoItemDto model, ApplicationDbContext _dbContext)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("items");
            ZohoCreateItemReqeust zohoItem = new ZohoCreateItemReqeust();
            zohoItem.description = model.Description;
            zohoItem.name = model.Name;
            zohoItem.rate = model.Rate;
            zohoItem.product_type = model.ProdcuType.ToString();
            
            string jsonString = JsonConvert.SerializeObject(zohoItem);
            MultipartFormDataContent form = new MultipartFormDataContent();
            StringContent content = new StringContent(jsonString);
            form.Add(content, "JSONString");
            HttpResponseMessage response = await httpClient.PostAsync(url, form);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                RootItemCreateResponse itemCreateResponse =
                    JsonConvert.DeserializeObject<RootItemCreateResponse>(responseString);
                if (itemCreateResponse.code == ZohoSuccessResponseCode)
                {
                    if (!string.IsNullOrWhiteSpace(itemCreateResponse.item.item_id))
                    {
                        ZohoItem dbZohoItem = new ZohoItem
                        {
                            ZohoItemId = itemCreateResponse.item.item_id,
                            Description = model.Description,
                            Name = model.Name,
                            Rate = model.Rate
                        };

                        _dbContext.ZohoItems.Add(dbZohoItem);
                        await _dbContext.SaveChangesAsync();
                        return dbZohoItem;
                    }
                    else
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.Append((string.IsNullOrWhiteSpace(itemCreateResponse.item.item_id) ? "Item id is empty. " : ""));
                        
                        throw new Exception($"Response Filed came empty. Fields => {builder.ToString()}");
                    }

                }
                else
                {
                    throw new Exception($"Zoho returns a erro code. Erro code is => {itemCreateResponse.code}. Message is => {itemCreateResponse.message}.");

                }
            }
            else
            {
                throw new Exception($"Zoho Api call failed Erro code is => {response.StatusCode}. Reason sent by server is => {response.ReasonPhrase}.");
            }
        }

        private async Task<RecurringInvoice> CreateRecurringInvoice(CreateRecurringInvoiceDto recurringInfo, ApplicationDbContext dbContext)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("recurringinvoices");
            
            InvoiceContact contact = dbContext.InvoiceContacts.Include(x=> x.RecurringInvoices).FirstOrDefault(x => x.UserId == recurringInfo.UserId);
            List<ZohoItemRecurringInvoice> items = new List<ZohoItemRecurringInvoice>();
            //double totalPrice = 0; 
            foreach (var itemId in recurringInfo.Items)
            {
                ZohoItem zohoItem = await dbContext.ZohoItems.FirstOrDefaultAsync(y => y.Id == itemId.ItemId);
                if(zohoItem == null)
                      throw new Exception($"Item id is not found in the database. Item id is => {itemId.ItemId}");
                //totalPrice = totalPrice + zohoItem.Rate * itemId.Qty;
                items.Add(new ZohoItemRecurringInvoice{Qty = itemId.Qty,ZohoItem = zohoItem});
            }
            
            
                
            if(contact == null)
                  throw new Exception($"User is not found in the database UserId is => {recurringInfo.UserId}");
            string recurringInvoiceName = contact.ZohoContactUserId + Guid.NewGuid().ToString();
            RootRecurringInvoiceCreateRequest recurringInvoiceCreateRequest = new RootRecurringInvoiceCreateRequest
            {
                customer_id = contact.ZohoContactUserId,
                line_items = items.Select(x=> new LineItemRecurringInvoiceCreateRequest{item_id = x.ZohoItem.ZohoItemId,quantity = x.Qty}).ToList(),
                payment_options = new PaymentOptionsRecurringInvoiceCreateRequest { payment_gateways = dbContext.ConfiguredPaymentGateways.Select(x => new PaymentGatewayRecurringInvoiceCreateRequest{configured = x.IsEnabled,gateway_name = x.GatewayName} ).ToList() },
                payment_terms = ZohoPaymentTermDaysGap,
                recurrence_frequency = "months",
                recurrence_name = recurringInvoiceName,
                repeat_every = 1,
                start_date = DateTime.UtcNow.AddDays(-1*ZohoPaymentTermDaysGap).ToString("yyyy-MM-dd")
            };


            string jsonString = JsonConvert.SerializeObject(recurringInvoiceCreateRequest);
            MultipartFormDataContent form = new MultipartFormDataContent();
            StringContent content = new StringContent(jsonString);
            form.Add(content, "JSONString");
            HttpResponseMessage response = await httpClient.PostAsync(url, form);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                RootRecurringInvoiceResponse itemCreateResponse =
                    JsonConvert.DeserializeObject<RootRecurringInvoiceResponse>(responseString);
                if (itemCreateResponse.code == ZohoSuccessResponseCode)
                {
                    if (!string.IsNullOrWhiteSpace(itemCreateResponse.recurring_invoice.recurring_invoice_id))
                    {
                        try
                        {
                            RecurringInvoice dbRecurringInvoice = new RecurringInvoice();
                            dbRecurringInvoice.InvoiceContact = contact;
                            dbRecurringInvoice.ZohoItems = items;
                            dbRecurringInvoice.IsDue = true;
                            dbRecurringInvoice.AllTaskCompleted = false;
                            dbRecurringInvoice.RecurringInvoiceId =
                                itemCreateResponse.recurring_invoice.recurring_invoice_id;
                            dbRecurringInvoice.RecurringInvoiceName = recurringInvoiceName;
                            dbRecurringInvoice.UpdatedAt = DateTime.UtcNow;
                            dbRecurringInvoice.ReferenceGuid = recurringInfo.ReferenceGuid;
                            dbContext.RecurringInvoices.Add(dbRecurringInvoice);
                            await dbContext.SaveChangesAsync();
                            return dbRecurringInvoice;
                        }
                        catch (Exception ex)
                        {
                            //Todo logging to serilog
                            await DeleteRecurringInvoice(itemCreateResponse.recurring_invoice.recurring_invoice_id);
                            throw ex;
                        }
                        
                       
                    }
                    else
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.Append((string.IsNullOrWhiteSpace(itemCreateResponse.recurring_invoice.recurring_invoice_id) ? "Recurring Invoice id is empty. " : ""));

                        throw new Exception($"Response Filed came empty. Fields => {builder.ToString()}");
                    }

                }
                else
                {
                    throw new Exception($"Zoho returns a erro code. Erro code is => {itemCreateResponse.code}. Message is => {itemCreateResponse.message}.");

                }
            }
            else
            {
                throw new Exception($"Zoho Api call failed Erro code is => {response.StatusCode}. Reason sent by server is => {response.ReasonPhrase}.");
            }
        }

        private async Task DeleteRecurringInvoice(string recurringInvoiceId)
        {
            try
            {
                HttpClient httpClient = _httpClientFactory.CreateClient();
                httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
                Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("recurringinvoices", recurringInvoiceId);
                HttpResponseMessage response = await httpClient.DeleteAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Deleteing Recurring Invoice Failed. Recurring Invoice Id is => {recurringInvoiceId}");
                }
                else
                {
                    //Todo Logging success delete of the Invoice deletion
                    var byteArray = await response.Content.ReadAsByteArrayAsync();
                    var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                    RootZohoBasicResponse sendEmailZohoResponse =
                        JsonConvert.DeserializeObject<RootZohoBasicResponse>(responseString);
                    if (sendEmailZohoResponse.code != ZohoSuccessResponseCode)
                    {
                        throw new Exception(
                            $"Deleteing Recurring Invoice Failed. Zoho Error code is => {sendEmailZohoResponse.code}, message is => {sendEmailZohoResponse.message}");
                    }
                }
                
            }
            catch (Exception ex)
            {
                //Todo Logging
                throw new Exception($"Deleteing Recurring Invoice Failed. Recurring Invoice Id is => {recurringInvoiceId}");
            }
            

        }
        private async Task DeleteInvoice(string invoiceId)
        {
            try
            {
                HttpClient httpClient = _httpClientFactory.CreateClient();
                httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
                Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("invoices", invoiceId);
                HttpResponseMessage response = await httpClient.DeleteAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Deleteing Invoice Failed. Recurring Invoice Id is => {invoiceId}");
                }
                else
                {
                    //Todo Logging success delete of the Invoice deletion
                    var byteArray = await response.Content.ReadAsByteArrayAsync();
                    var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                    RootZohoBasicResponse sendEmailZohoResponse =
                        JsonConvert.DeserializeObject<RootZohoBasicResponse>(responseString);
                    if (sendEmailZohoResponse.code != ZohoSuccessResponseCode)
                    {
                        throw new Exception(
                            $"Deleteing Invoice Failed. Zoho Error code is => {sendEmailZohoResponse.code}, message is => {sendEmailZohoResponse.message}");
                    }
                    
                }

            }
            catch (Exception ex)
            {
                //Todo Logging
                throw new Exception($"Deleteing Recurring Invoice Failed. Recurring Invoice Id is => {invoiceId}");
            }


        }
        private async Task<string> CreateInvoice(string recurringInvoiceId, ApplicationDbContext dbContext)
        {
            RecurringInvoice recurringInvoice = dbContext.RecurringInvoices.Include(x=> x.ZohoItems).First(x => x.Id == recurringInvoiceId);
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("invoices");

            InvoiceContact contact = dbContext.InvoiceContacts.Include(x => x.RecurringInvoices).FirstOrDefault(x => x.RecurringInvoices.Contains(recurringInvoice));
            
            if (contact == null)
                throw new Exception($"User is not found in the database UserId is => {contact.UserId}");
            RootInvoiceCreateRequest invoiceCreateRequest = new RootInvoiceCreateRequest
            {
                customer_id = contact.ZohoContactUserId,
                line_items = recurringInvoice.ZohoItems.Select(x => new LineItemRecurringInvoiceCreateRequest { item_id = x.ZohoItem.ZohoItemId, quantity = x.Qty }).ToList(),
                payment_options = new PaymentOptionsRecurringInvoiceCreateRequest { payment_gateways = dbContext.ConfiguredPaymentGateways.Select(x => new PaymentGatewayRecurringInvoiceCreateRequest { configured = x.IsEnabled, gateway_name = x.GatewayName }).ToList() },
                payment_terms = 0,
                date = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
                recurring_invoice_id = recurringInvoice.RecurringInvoiceId
            };


            string jsonString = JsonConvert.SerializeObject(invoiceCreateRequest);
            MultipartFormDataContent form = new MultipartFormDataContent();
            StringContent content = new StringContent(jsonString);
            form.Add(content, "JSONString");
            HttpResponseMessage response = await httpClient.PostAsync(url, form);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                RootInvoiceResponse itemCreateResponse =
                    JsonConvert.DeserializeObject<RootInvoiceResponse>(responseString);
                if (itemCreateResponse.code == ZohoSuccessResponseCode)
                {
                    if (!string.IsNullOrWhiteSpace(itemCreateResponse.invoice.invoice_id))
                    {
                        
                        //Add send mail as a task 
                        ZohoInvoiceJobQueue firstInvoiceMail = new ZohoInvoiceJobQueue();
                        try
                        {
                            
                            firstInvoiceMail.IsSuccess = false;
                            firstInvoiceMail.JobType = ZohoInvoiceJobQueueType.SendFirstInvoiceMail;
                            firstInvoiceMail.JsonData = JsonConvert.SerializeObject(new SendFirstMailJob{invoiceId = itemCreateResponse.invoice.invoice_id, userEmail = contact.Email,recurringInvoiceId = recurringInvoiceId });
                            dbContext.ZohoInvoiceJobQueues.Add(firstInvoiceMail);
                            await dbContext.SaveChangesAsync();

                        }
                        catch (Exception ex)
                        {
                            //Todo logging
                            await DeleteInvoice(itemCreateResponse.invoice.invoice_id);
                            throw ex;
                        }

                        try
                        {
                            bool result = await SendFristInvoiceMail(itemCreateResponse.invoice.invoice_id, contact.Email);
                            if (result)
                            {
                                firstInvoiceMail.IsSuccess = true;
                                firstInvoiceMail.UpdatedAt = DateTime.UtcNow;
                                recurringInvoice.AllTaskCompleted = true;
                                await dbContext.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            //Todo Logging dont throw the exception just log it
                        }

                        return itemCreateResponse.invoice.invoice_id;

                    }
                    else
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.Append((string.IsNullOrWhiteSpace(itemCreateResponse.invoice.invoice_id) ? "Recurring Invoice id is empty. " : ""));

                        throw new Exception($"Response Filed came empty. Fields => {builder.ToString()}");
                    }

                }
                else
                {
                    throw new Exception($"Zoho returns a erro code. Erro code is => {itemCreateResponse.code}. Message is => {itemCreateResponse.message}.");

                }
            }
            else
            {
                throw new Exception($"Zoho Api call failed Erro code is => {response.StatusCode}. Reason sent by server is => {response.ReasonPhrase}.");
            }
        }

        private async Task<bool> SendFristInvoiceMail(string invoiceId,string email)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("invoices").Append(invoiceId).Append("email");
            SendFristInvoiceEMailRequest sendFristInvoiceEMailRequest =
                new SendFristInvoiceEMailRequest {to_mail_ids = new List<string> { email }};
            string jsonString = JsonConvert.SerializeObject(sendFristInvoiceEMailRequest);
            MultipartFormDataContent form = new MultipartFormDataContent();
            StringContent content = new StringContent(jsonString);
            form.Add(content, "JSONString");
            HttpResponseMessage response = await httpClient.PostAsync(url, form);
            if (response.IsSuccessStatusCode)
            {
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                RootZohoBasicResponse sendEmailZohoResponse =
                    JsonConvert.DeserializeObject<RootZohoBasicResponse>(responseString);
                if (sendEmailZohoResponse.code == ZohoSuccessResponseCode)
                {
                    
                    return true;
                }
                else
                {
                    throw new Exception(
                        $"Zoho Error code is => {sendEmailZohoResponse.code}, message is => {sendEmailZohoResponse.message}");
                }
            }
            else
            {
                throw new Exception($"Zoho Api call failed Erro code is => {response.StatusCode}. Reason sent by server is => {response.ReasonPhrase}.");
            }
        }
        private async Task<bool> JobRunner(ZohoInvoiceJobQueue job, ApplicationDbContext _dbContext)
        {
            if (job.JobType == ZohoInvoiceJobQueueType.CreateContact)
            {
                CreateContact model = JsonConvert.DeserializeObject<CreateContact>(job.JsonData);
                InvoiceContact contact =await CreateContact(model, _dbContext);
                job.UpdatedAt = DateTime.UtcNow;
                if (contact == null)
                {
                    throw new Exception($"Invoice contact creating failed for job ID {job.Id}");
                }
                else
                {
                    
                    job.IsSuccess = true;
                    await _dbContext.SaveChangesAsync();
                }
            }
            else if (job.JobType == ZohoInvoiceJobQueueType.EnableClientPortle)
            {
                var UserId = GetInvoceContactByUserid(job, out var contact, _dbContext);

                bool result =await EnableClientProtle(contact);
                job.UpdatedAt = DateTime.UtcNow;
                if (result)
                {
                    job.IsSuccess = true;
                }
                else
                {
                    throw new Exception($"Enable Portle Access is faild for the user {UserId} => Job Id {job.Id}");
                }

                await _dbContext.SaveChangesAsync();
            }
            else if (job.JobType == ZohoInvoiceJobQueueType.EnablePaymentReminders)
            {
                var UserId = GetInvoceContactByUserid(job, out var contact, _dbContext);

                bool result = await EnablePaymentReminder(contact);
                job.UpdatedAt = DateTime.UtcNow;
                if (result)
                {
                    job.IsSuccess = true;
                }
                else
                {
                    throw new Exception($"Enable Payment Reminder is faild for the user {UserId} => Job Id {job.Id}");
                }

                await _dbContext.SaveChangesAsync();
            }else if (job.JobType == ZohoInvoiceJobQueueType.CreateItem)
            {
                CreateZohoItemDto model = JsonConvert.DeserializeObject<CreateZohoItemDto>(job.JsonData);
                ZohoItem zohoItem = await CreateItem(model, _dbContext);
                job.UpdatedAt = DateTime.UtcNow;
                if (zohoItem == null)
                {
                    throw new Exception($"Item creating failed for job ID {job.Id}");
                }
                else
                {

                    job.IsSuccess = true;
                    await _dbContext.SaveChangesAsync();
                }
            }
            else if (job.JobType == ZohoInvoiceJobQueueType.CreateRecurringInvoice)
            {
                CreateRecurringInvoiceDto model = JsonConvert.DeserializeObject<CreateRecurringInvoiceDto>(job.JsonData);
                RecurringInvoice recurringInvoice = await CreateRecurringInvoice(model, _dbContext);
                job.UpdatedAt = DateTime.UtcNow;
                if (recurringInvoice == null)
                {
                    throw new Exception($"Item creating failed for job ID {job.Id}");
                }
                else
                {
                    ZohoInvoiceJobQueue firstInvoiceJob = new ZohoInvoiceJobQueue();
                    try
                    {
                       
                        firstInvoiceJob.IsSuccess = false;
                        firstInvoiceJob.JobType = ZohoInvoiceJobQueueType.CreateFirstInvoice;
                        firstInvoiceJob.JsonData = recurringInvoice.Id;
                        _dbContext.ZohoInvoiceJobQueues.Add(firstInvoiceJob);
                        await _dbContext.SaveChangesAsync();
                        job.IsSuccess = true;
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Guid guid = Guid.NewGuid();
                        await DeleteRecurringInvoice(recurringInvoice.RecurringInvoiceId);
                        //Todo Logging

                        //todo remove recurring invoice from the db but use a another try catch
                        throw new Exception($"Adding First invoice is failed deleteing recurring invoice. invoice primary key is => {recurringInvoice.Id}, zoho invoice id is {recurringInvoice.RecurringInvoiceId}, View Log for more details. Guid is => {guid.ToString()}");
                    }

                    string result = await CreateInvoice(recurringInvoice.Id, _dbContext);
                    try
                    {
                        firstInvoiceJob.IsSuccess = true;
                        firstInvoiceJob.UpdatedAt = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        //Todo log ex, but dont throw ex
                        await DeleteInvoice(result);
                        
                    }

                }
            }else if (job.JobType == ZohoInvoiceJobQueueType.CreateFirstInvoice)
            {
                string recurringInvoiceId = job.JsonData;
                job.UpdatedAt = DateTime.UtcNow;
                string result = await CreateInvoice(recurringInvoiceId, _dbContext);
                try
                {
                    job.IsSuccess = true;
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    //Todo logging
                    await DeleteInvoice(result);
                    throw ex;
                }

            }else if (job.JobType == ZohoInvoiceJobQueueType.SendFirstInvoiceMail)
            {
                SendFirstMailJob jobData = JsonConvert.DeserializeObject<SendFirstMailJob>(job.JsonData);
                job.UpdatedAt = DateTime.UtcNow;
                try
                {
                    bool result = await SendFristInvoiceMail(jobData.invoiceId, jobData.userEmail);
                    if (result)
                    {
                        RecurringInvoice recurringInvoice =
                            dbContext.RecurringInvoices.First(x => x.Id == jobData.recurringInvoiceId);
                        recurringInvoice.AllTaskCompleted = true;
                        job.IsSuccess = true;
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    //Todo Logging
                    throw ex;
                }
            }
            else
            {
                throw new Exception($"Job Queue type is not found for job ID {job.Id}");
            }

            return true;
        }

        //private async Task CreateInvoiceWrapper(string recurringInvoiceId, ApplicationDbContext dbContext)
        //{
        //    bool IsSuccess = await CreateInvoice(recurringInvoiceId, dbContext);
        //}
        private string GetInvoceContactByUserid(ZohoInvoiceJobQueue job, out InvoiceContact contact, ApplicationDbContext _dbContext)
        {
            string UserId = job.JsonData;
            contact = _dbContext.InvoiceContacts.FirstOrDefault(x => x.UserId == UserId);
            if (contact == null)
            {
                throw new Exception($"A contact for the user {UserId} is not found in the database");
            }

            return UserId;
        }


        #region response classes
        internal class RootZohoBasicResponse
        {
            public int code { get; set; } = -1;
            public string message { get; set; }
        }
        #region ContactResponse

        internal class RootContactResponse: RootZohoBasicResponse
        {
            public Contact contact { get; set; }
        }
        internal class Contact
        {
            public string contact_id { get; set; }
            public string primary_contact_id { get; set; }
        }

        #endregion

        #region ItemCreateResponse
        internal class ItemResponse
        {
            public string item_id { get; set; }
        }

        internal class RootItemCreateResponse: RootZohoBasicResponse
        {
            
            public ItemResponse item { get; set; }
        }


        #endregion

        #region RecurringInvoiceResponse

        internal class RootRecurringInvoiceResponse:RootZohoBasicResponse
        {
            public RecurringInvoiceData recurring_invoice { get; set; }
        }
        internal class RecurringInvoiceData
        {
            public string recurring_invoice_id { get; set; }
        }


        #endregion

        #region InvoiceCreateResponse

        internal class RootInvoiceResponse : RootZohoBasicResponse
        {
            public SubInvoiceResponse invoice { get; set; }
        }

        internal class SubInvoiceResponse
        {
            public string invoice_id { get; set; }
        }

        #endregion
        #region RecurringInvoicePaymentCheckResponse

        internal class RootRecurringPaymentCheckInvoiceClass : RootZohoBasicResponse
        {
            public RecurringPaymentCheckInvoiceClass recurring_invoice { get; set; }
        }
        internal class RecurringPaymentCheckInvoiceClass
        {
            public double unpaid_invoices_balance { get; set; }
            public int unpaid_child_invoices_count { get; set; }
        }
        #endregion
        #endregion

        #region InternalJobSturctures

        internal class SendFirstMailJob
        {
            public string invoiceId { get; set; }
            public string userEmail { get; set; }
            public string recurringInvoiceId { get; set; }
        }

       
        #endregion

        /// <summary>
        /// Region for Request classes
        /// </summary>
        #region Request Classes
        #region Create Contact

        internal class ContactPerson
        {
            //public string salutation { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string email { get; set; }
            //public string phone { get; set; }
            public string mobile { get; set; }
            public bool is_primary_contact { get; set; }
        }

        internal class ContactPersonRoot
        {
            public string contact_name { get; set; }
            public List<ContactPerson> contact_persons { get; set; }
            public int payment_terms { get; set; }
        }

        #endregion
        #region Enable Portal

        internal class ContactPersonPortal
        {
            
            public string contact_person_id { get; set; }
            
        }

        internal class ContactPersonPortalRoot
        {
            
            public List<ContactPersonPortal> contact_persons { get; set; }
            
        }

        #endregion

        #region Create Item
        internal class ZohoCreateItemReqeust
        {
            public string name { get; set; }
            public double rate { get; set; }
            public string description { get; set; }
            public string product_type { get; set; }
        }


        #endregion

        #region SendFristInvoiceEMailRequest

        internal class SendFristInvoiceEMailRequest
        {
            public List<string> to_mail_ids { get; set; }
        }

        #endregion
        #region Create Recurring Invoice

        internal class LineItemRecurringInvoiceCreateRequest
        {
        
            public string item_id { get; set; }
            public int quantity { get; set; }
        }

        internal class PaymentGatewayRecurringInvoiceCreateRequest
        {
        
            public bool configured { get; set; }
            public string gateway_name { get; set; }
        }

        internal class PaymentOptionsRecurringInvoiceCreateRequest
        {
            public List<PaymentGatewayRecurringInvoiceCreateRequest> payment_gateways { get; set; }
        }

        internal class RootRecurringInvoiceCreateRequest: CommonRootRecurringInvoiceCreateRequest
        {
            public string start_date { get; set; }
            public string recurrence_name { get; set; }
            public string recurrence_frequency { get; set; }
            public int repeat_every { get; set; }
        }

        internal class CommonRootRecurringInvoiceCreateRequest
        {
           
            public string customer_id { get; set; }
            public List<string> contact_persons { get; set; }
            
            public List<LineItemRecurringInvoiceCreateRequest> line_items { get; set; }
            public PaymentOptionsRecurringInvoiceCreateRequest payment_options { get; set; }
            public int payment_terms { get; set; }
            
        }

        internal class RootInvoiceCreateRequest : CommonRootRecurringInvoiceCreateRequest
        {
            public string recurring_invoice_id { get; set; }
            public string date { get; set; }
        }
        #endregion
        #endregion
    }

    #region ZohoAuthorization Extension

    public static class AddZohoAuthorization
    {
        public static void AddZohoAuthorizationHeader(this HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", token);
        }
    }

    #endregion

    #region Uri Extension
    public static class UriExtensions
    {
        public static Uri Append(this Uri uri, params string[] paths)
        {
            return new Uri(paths.Aggregate(uri.AbsoluteUri, (current, path) => string.Format("{0}/{1}", current.TrimEnd('/'), path.TrimStart('/'))));
        }
    }


    #endregion

    

}
