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
            RootRecurringInvoiceCreateRequest recurringInvoiceCreateRequest = new RootRecurringInvoiceCreateRequest
            {
                customer_id = contact.ZohoContactUserId,
                line_items = items.Select(x=> new LineItemRecurringInvoiceCreateRequest{item_id = x.ZohoItem.ZohoItemId,quantity = x.Qty}).ToList(),
                PaymentOptionsRecurringInvoiceCreateRequest = new PaymentOptionsRecurringInvoiceCreateRequest { payment_gateways = dbContext.ConfiguredPaymentGateways.Select(x => new PaymentGatewayRecurringInvoiceCreateRequest{configured = x.IsEnabled,gateway_name = x.GatewayName} ).ToList() },
                payment_terms = ZohoPaymentTermDaysGap,
                recurrence_frequency = "months",
                recurrence_name = Guid.NewGuid().ToString(),
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
                RootRecurringInvoice itemCreateResponse =
                    JsonConvert.DeserializeObject<RootRecurringInvoice>(responseString);
                if (itemCreateResponse.code == ZohoSuccessResponseCode)
                {
                    if (!string.IsNullOrWhiteSpace(itemCreateResponse.recurring_invoice.recurring_invoice_id))
                    {
                        RecurringInvoice dbRecurringInvoice = new RecurringInvoice();
                        dbRecurringInvoice.InvoiceContact = contact;
                        dbRecurringInvoice.ZohoItems = items;

                        dbContext.RecurringInvoices.Add(dbRecurringInvoice);
                        await dbContext.SaveChangesAsync();
                        return dbRecurringInvoice;
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

                    job.IsSuccess = true;
                    await _dbContext.SaveChangesAsync();
                }
            }
            else
            {
                throw new Exception($"Job Queue type is not found for job ID {job.Id}");
            }

            return true;
        }

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
            public int code { get; set; }
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

        internal class RootRecurringInvoice:RootZohoBasicResponse
        {
            public RecurringInvoiceData recurring_invoice { get; set; }
        }
        internal class RecurringInvoiceData
        {
            public string recurring_invoice_id { get; set; }
        }


        #endregion
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

        internal class RootRecurringInvoiceCreateRequest
        {
            public string recurrence_name { get; set; }
            public string customer_id { get; set; }
            public string recurrence_frequency { get; set; }
            public int repeat_every { get; set; }
            public List<LineItemRecurringInvoiceCreateRequest> line_items { get; set; }
            public PaymentOptionsRecurringInvoiceCreateRequest PaymentOptionsRecurringInvoiceCreateRequest { get; set; }
            public int payment_terms { get; set; }
            public string start_date { get; set; }
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
