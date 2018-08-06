using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Interface.Service;
using Empite.PaymentService.Interface.Service.Zoho;
using Empite.PaymentService.Models.Configs;
using Empite.PaymentService.Models.Dto;
using Empite.PaymentService.Models.Dto.Zoho;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Empite.PaymentService.Services.PaymentService.Zoho
{
    public class ZohoInvoiceWorkerService : IInvoiceWorkerService<ZohoInvoiceWorkerService>
    {
        private readonly IZohoInvoiceSingleton _zohoTokenService;
        private readonly IHttpClientFactory _httpClientFactory;
        private IServiceProvider _services { get; }
        private const int ZohoSuccessResponseCode = 0;
        private readonly Settings _settings;
        public ZohoInvoiceWorkerService(IZohoInvoiceSingleton zohoTokenService, IHttpClientFactory httpClientFactory, IServiceProvider services, IOptions<Settings> options)
        {
            _zohoTokenService = zohoTokenService;
            _httpClientFactory = httpClientFactory;
            _services = services;
            _settings = options.Value;
        }
        public async Task<InvoiceContact> CreateContact(ZohoCreateContact model, ApplicationDbContext _dbContext)
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
                            ExternalContactUserId = contactResponse.contact.contact_id,
                            ExternalPrimaryContactId = contactResponse.contact.primary_contact_id
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
        public async Task<bool> EnablePaymentReminder(InvoiceContact contactDetails)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("contacts", contactDetails.ExternalContactUserId, "paymentreminder", "enable");
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

        public async Task<Item> CreateItem(ZohoCreateItemDto model, ApplicationDbContext _dbContext)
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
                        Item dbItem = new Item
                        {
                            ItemId = itemCreateResponse.item.item_id,
                            Description = model.Description,
                            Name = model.Name,
                            Rate = model.Rate
                        };

                        _dbContext.Items.Add(dbItem);
                        await _dbContext.SaveChangesAsync();
                        return dbItem;
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

        public async Task DeleteInvoice(string invoiceId)
        {
            try
            {
                HttpClient httpClient = _httpClientFactory.CreateClient();
                httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
                Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("invoices", invoiceId);
                HttpResponseMessage response = await httpClient.DeleteAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Deleteing Purchese Failed. Recurring Purchese Id is => {invoiceId}");
                }
                else
                {
                    //Todo Logging success delete of the Purchese deletion
                    var byteArray = await response.Content.ReadAsByteArrayAsync();
                    var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                    RootZohoBasicResponse sendEmailZohoResponse =
                        JsonConvert.DeserializeObject<RootZohoBasicResponse>(responseString);
                    if (sendEmailZohoResponse.code != ZohoSuccessResponseCode)
                    {
                        throw new Exception(
                            $"Deleteing Purchese Failed. Zoho Error code is => {sendEmailZohoResponse.code}, message is => {sendEmailZohoResponse.message}, invoice id is => {invoiceId}");
                    }

                }

            }
            catch (Exception ex)
            {
                //Todo Logging
                throw new Exception($"Deleteing Recurring Purchese Failed. Recurring Purchese Id is => {invoiceId}");
            }


        }

        public async Task<bool> CreateSubInvoice(InvoiceJobQueue job, string purchaseId, ApplicationDbContext dbContext)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("invoices?send=true");

            Purchese dbPurchese = dbContext.Purcheses.Include(x => x.Items).ThenInclude(x => x.Item).Include(x => x.InvoiceContact).First(x => x.Id == purchaseId);

            //InvoiceContact contact = dbContext.InvoiceContacts.First(x => x.UserId == model.UserId);
            if (dbPurchese == null)
                throw new Exception($"Purchase not found in the database");
            if (dbPurchese.InvoiceContact == null)
            {
                throw new Exception($"Contact not found in the database");
            }


            RootInvoiceCreateRequest invoiceCreateRequest = new RootInvoiceCreateRequest
            {
                customer_id = dbPurchese.InvoiceContact.ExternalContactUserId,
                line_items = dbPurchese.Items.Select(x => new LineItemRecurringInvoiceCreateRequest
                {
                    item_id = x.Item.ItemId,
                    quantity = x.Qty
                }).ToList(),
                payment_options = new PaymentOptionsRecurringInvoiceCreateRequest { payment_gateways = dbContext.ConfiguredPaymentGateways.Select(x => new PaymentGatewayRecurringInvoiceCreateRequest { configured = x.IsEnabled, gateway_name = x.GatewayName }).ToList() },
                payment_terms = _settings.ZohoAccount.PaymentTerm,
                date = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
                contact_persons = new List<string> { dbPurchese.InvoiceContact.ExternalPrimaryContactId }
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

                        try
                        {



                            InvoiceHistory dbHistory = new InvoiceHistory();
                            dbHistory.InvoiceStatus = InvoiceStatus.Unpaid;
                            dbHistory.InvoiceId = itemCreateResponse.invoice.invoice_id;
                            dbHistory.InvoiceNumber = itemCreateResponse.invoice.invoice_number;
                            
                            dbPurchese.LastSuccessInvoiceIssue = dbPurchese.LastSuccessInvoiceIssue.AddMonths(1);
                            dbHistory.DueDate = dbPurchese.LastSuccessInvoiceIssue.AddDays(_settings.ZohoAccount.PaymentTerm);
                            dbHistory.Purchese = dbPurchese;
                            dbContext.InvoiceHistories.Add(dbHistory);


                            await dbContext.SaveChangesAsync();

                            job.UpdatedAt = DateTime.UtcNow;
                            job.IsSuccess = true;
                            await dbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            //Fall back method
                            //todo logging
                            await DeleteInvoice(itemCreateResponse.invoice.invoice_id);
                            throw ex;
                        }



                    }
                    else
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.Append((string.IsNullOrWhiteSpace(itemCreateResponse.invoice.invoice_id) ? "Recurring Purchese id is empty. " : ""));

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

            return true;
        }
        public async Task<bool> CreateInvoice(InvoiceJobQueue job, ZohoCreatePurchesDto model, ApplicationDbContext dbContext)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("invoices?send=true");

            InvoiceContact contact = dbContext.InvoiceContacts.First(x => x.UserId == model.UserId);
            if (contact == null)
                throw new Exception($"User is not found in the database UserId is => {model.UserId}");
            List<Item> zohoItems = new List<Item>();
            zohoItems = model.Items.Select(x =>
            {
                var dbZohoItems = dbContext.Items.First(y => y.Id == x.ItemId);
                return dbZohoItems;
            }).ToList();

            RootInvoiceCreateRequest invoiceCreateRequest = new RootInvoiceCreateRequest
            {
                customer_id = contact.ExternalContactUserId,
                line_items = model.Items.Select(x =>
                {
                    var dbZohoItems = zohoItems.First(y => y.Id == x.ItemId);
                    return new LineItemRecurringInvoiceCreateRequest
                    {
                        item_id = dbZohoItems.ItemId,
                        quantity = x.Qty
                    };
                }).ToList(),
                payment_options = new PaymentOptionsRecurringInvoiceCreateRequest { payment_gateways = dbContext.ConfiguredPaymentGateways.Select(x => new PaymentGatewayRecurringInvoiceCreateRequest { configured = x.IsEnabled, gateway_name = x.GatewayName }).ToList() },
                payment_terms = 0,
                date = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
                contact_persons = new List<string> { contact.ExternalPrimaryContactId }
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

                        try
                        {
                            Purchese dbPurchese = new Purchese();
                            dbPurchese.InvoiceContact = contact;
                            dbPurchese.InvoiceHistories = new List<InvoiceHistory>{new InvoiceHistory
                                {
                                    InvoiceStatus = InvoiceStatus.Unpaid,
                                    InvoiceId = itemCreateResponse.invoice.invoice_id,
                                    InvoiceNumber = itemCreateResponse.invoice.invoice_number,
                                    DueDate = DateTime.UtcNow
                                }};
                            dbPurchese.InvoiceName = "";
                            dbPurchese.Items = zohoItems.Select(x => new Item_Purchese
                            {
                                Qty = model.Items.First(y => y.ItemId == x.Id).Qty,
                                Item = x
                            }).ToList();
                            dbPurchese.InvoiceStatus = InvoicingStatus.Active;
                            dbPurchese.InvoiceType = InvoicingType.Recurring;
                            
                            dbPurchese.ReferenceGuid = model.ReferenceGuid;
                            dbPurchese.InvoiceGatewayType = ExternalInvoiceGatewayType.Zoho;
                            dbPurchese.LastSuccessInvoiceIssue = DateTime.UtcNow.AddDays(_settings.ZohoAccount.PaymentTerm*-1);
                            
                            dbContext.Purcheses.Add(dbPurchese);
                            await dbContext.SaveChangesAsync();

                            job.UpdatedAt = DateTime.UtcNow;
                            job.IsSuccess = true;
                            await dbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            //Fall back method
                            await DeleteInvoice(itemCreateResponse.invoice.invoice_id);
                        }



                    }
                    else
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.Append((string.IsNullOrWhiteSpace(itemCreateResponse.invoice.invoice_id) ? "Recurring Purchese id is empty. " : ""));

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

            return true;
        }

        public async Task<bool> IsPaidForCurrentDate(string purchaseId, ApplicationDbContext dbContext)
        {
            try
            {
                List<InvoiceHistory> invoiceHistory = await dbContext.InvoiceHistories.Include(x => x.Purchese)
                    .Where(x => x.Purchese.Id == purchaseId && x.DueDate.Date <= DateTime.UtcNow.Date)
                    .OrderByDescending(x => x.DueDate).Take(2).ToListAsync();
                if (invoiceHistory.Any())
                {
                    
                    if (invoiceHistory.Count == 1)
                    {
                        //Check for initial invoice
                        InvoiceHistory history = invoiceHistory[0];
                        return CheckIspaid(history);
                    }
                    else
                    {
                        InvoiceHistory newHistory = invoiceHistory[0];
                        InvoiceHistory oldHistory = invoiceHistory[1];
                        if (newHistory.DueDate.Date < DateTime.UtcNow.Date)
                        {
                            return CheckIspaid(newHistory);
                        }

                        return CheckIspaid(oldHistory);
                    }
                }
                else
                {
                    throw new Exception($"No history records found for purchaseId: {purchaseId}");
                }
            }
            catch (Exception ex)
            {
                //Todo Logging
                throw ex;
            }
        }

        private bool CheckIspaid(InvoiceHistory history)
        {
            
            if (history.DueDate.AddMonths(1).Date >= DateTime.UtcNow.Date)
            {
                //Check for 1 month range
                if (history.PaymentRecordedDate == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }
    }
}
