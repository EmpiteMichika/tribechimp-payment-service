using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Interface.Service.Zoho;
using Empite.PaymentService.Models.Configs;
using Empite.PaymentService.Models.Dto;
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
        public async Task<InvoiceContact> CreateContact(CreateContact model, ApplicationDbContext _dbContext)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("contacts");
            InvoceService.ContactPersonRoot contactPerson = new InvoceService.ContactPersonRoot
            {
                contact_name = model.FirstName + " " + model.LastName,
                payment_terms = _settings.ZohoAccount.PaymentTerm,
                contact_persons = new List<InvoceService.ContactPerson>
                {
                    new InvoceService.ContactPerson
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
                InvoceService.RootContactResponse contactResponse =
                    JsonConvert.DeserializeObject<InvoceService.RootContactResponse>(responseString);
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
        public async Task<bool> EnablePaymentReminder(InvoiceContact contactDetails)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("contacts", contactDetails.ZohoContactUserId, "paymentreminder", "enable");
            HttpResponseMessage response = await httpClient.PostAsync(url, null);
            if (response.IsSuccessStatusCode)
            {
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                InvoceService.RootZohoBasicResponse enablePortalResponse =
                    JsonConvert.DeserializeObject<InvoceService.RootZohoBasicResponse>(responseString);
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

        public async Task<ZohoItem> CreateItem(CreateZohoItemDto model, ApplicationDbContext _dbContext)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("items");
            InvoceService.ZohoCreateItemReqeust zohoItem = new InvoceService.ZohoCreateItemReqeust();
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
                InvoceService.RootItemCreateResponse itemCreateResponse =
                    JsonConvert.DeserializeObject<InvoceService.RootItemCreateResponse>(responseString);
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
                    InvoceService.RootZohoBasicResponse sendEmailZohoResponse =
                        JsonConvert.DeserializeObject<InvoceService.RootZohoBasicResponse>(responseString);
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

        public async Task<bool> CreateInvoice(ZohoInvoiceJobQueue job,CreatePurchesDto model,ApplicationDbContext dbContext,bool isFirst = false)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.AddZohoAuthorizationHeader(await _zohoTokenService.GetOAuthToken());
            Uri url = new Uri(_settings.ZohoAccount.ApiBasePath).Append("invoices?send=true");

            InvoiceContact contact = dbContext.InvoiceContacts.First(x => x.UserId == model.UserId);
            if (contact == null)
                    throw new Exception($"User is not found in the database UserId is => {contact.UserId}");
            List<ZohoItem> zohoItems = new List<ZohoItem>();
            zohoItems = model.Items.Select(x =>
            {
                var dbZohoItems = dbContext.ZohoItems.First(y => y.Id == x.ItemId);
                return dbZohoItems;
            }).ToList();

            InvoceService.RootInvoiceCreateRequest invoiceCreateRequest = new InvoceService.RootInvoiceCreateRequest
            {
                customer_id = contact.ZohoContactUserId,
                line_items = model.Items.Select(x =>
                {
                    var dbZohoItems = zohoItems.First(y => y.Id == x.ItemId);
                    return new InvoceService.LineItemRecurringInvoiceCreateRequest
                    {
                        item_id = dbZohoItems.ZohoItemId,
                        quantity = x.Qty
                    };
                }).ToList(),
                payment_options = new InvoceService.PaymentOptionsRecurringInvoiceCreateRequest { payment_gateways = dbContext.ConfiguredPaymentGateways.Select(x => new InvoceService.PaymentGatewayRecurringInvoiceCreateRequest { configured = x.IsEnabled, gateway_name = x.GatewayName }).ToList() },
                payment_terms = (isFirst)? 0:_settings.ZohoAccount.PaymentTerm,
                date = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
                contact_persons = new List<string> { contact.ZohoPrimaryContactId}
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
                InvoceService.RootInvoiceResponse itemCreateResponse =
                    JsonConvert.DeserializeObject<InvoceService.RootInvoiceResponse>(responseString);
                if (itemCreateResponse.code == ZohoSuccessResponseCode)
                {
                    if (!string.IsNullOrWhiteSpace(itemCreateResponse.invoice.invoice_id))
                    {

                        try
                        {
                            if (isFirst)
                            {
                                Purchese dbPurchese = new Purchese();
                                dbPurchese.InvoiceContact = contact;
                                dbPurchese.InvoiceHistories = new List<InvoiceHistory>{new InvoiceHistory
                                {
                                    InvoiceStatus = InvoiceStatus.Unpaid,
                                    ZohoInvoiceId = itemCreateResponse.invoice.invoice_id
                                }};
                                dbPurchese.InvoiceName = "";
                                dbPurchese.ZohoItems = zohoItems.Select(x => new ZohoItem_Purchese
                                {
                                    Qty = model.Items.First(y => y.ItemId == x.Id).Qty,
                                    ZohoItem = x
                                }).ToList();
                                dbPurchese.InvoiceStatus = InvoicingStatus.Active;
                                dbPurchese.InvoiceType = InvoicingType.Recurring;
                                dbPurchese.IsPaidForThisMonth = false;
                                dbPurchese.ReferenceGuid = model.ReferenceGuid;
                            }
                            else
                            {
                                Purchese dbPurchese =
                                    dbContext.Purcheses.First(x => x.ReferenceGuid == model.ReferenceGuid);
                                InvoiceHistory dbHistory = new InvoiceHistory();
                                dbHistory.InvoiceStatus = InvoiceStatus.Unpaid;
                                dbHistory.ZohoInvoiceId = itemCreateResponse.invoice.invoice_id;
                                dbHistory.Purchese = dbPurchese;
                            }

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

    }
}
