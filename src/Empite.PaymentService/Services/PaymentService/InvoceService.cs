using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Interface.Service;
using Empite.PaymentService.Interface.Service.Zoho;
using Empite.PaymentService.Models.Configs;
using Empite.PaymentService.Models.Dto;
using Empite.PaymentService.Models.Dto.Zoho;
using Empite.PaymentService.Services.PaymentService.Zoho;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Empite.PaymentService.Services.PaymentService
{
    public class InvoceService: IInvoceService
    {
        
        private static bool _isJobsProcessing = false;
        private const int RowsPerPage = 100;
        private readonly Settings _settings;
        private const int ZohoSuccessResponseCode = 0;
        private const int ZohoPaymentTermDaysGap = 10;
        private IServiceProvider _services { get; }
        private IInvoiceWorkerService<ZohoInvoiceWorkerService> _workerService;

        public InvoceService(IOptions<Settings> options, IServiceProvider services,IInvoiceWorkerService<ZohoInvoiceWorkerService> workerService)
        {

            _services = services;
            _workerService = workerService;


        }

        public async Task AddJob(dynamic DataObject, InvoiceJobQueueType JobType, ExternalInvoiceGatewayType externalInvoiceGatewayType)
        {
            using (ApplicationDbContext _dbContext = _services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                InvoiceJobQueue job = new InvoiceJobQueue();
                job.IsSuccess = false;
                job.JobType = JobType;
                job.InvoiceGatewayType = externalInvoiceGatewayType;
                if (JobType == InvoiceJobQueueType.CreateContact)
                {
                    if (DataObject?.GetType() != typeof(ZohoCreateContact))
                    {
                        throw new Exception("Invalid data type for the DataObject parameter, it should be created from CreateContact class");
                    }
                    job.JsonData = JsonConvert.SerializeObject(DataObject);
                }
                else if (JobType == InvoiceJobQueueType.EnablePaymentReminders)
                {
                    if (DataObject?.GetType() != typeof(string))
                    {
                        throw new Exception("Invalid data type for the DataObject parameter, it should be a string");
                    }
                    job.JsonData = DataObject;
                }else if (JobType == InvoiceJobQueueType.CreateItem)
                {
                    if (DataObject?.GetType() != typeof(ZohoCreateItemDto))
                    {
                        throw new Exception("Invalid data type for the DataObject parameter, it should be created from CreateZohoItemDto class");
                    }
                    job.JsonData = JsonConvert.SerializeObject(DataObject);
                }else if (JobType == InvoiceJobQueueType.CreateFirstInvoice)
                {
                    if (DataObject?.GetType() != typeof(ZohoCreatePurchesDto))
                    {
                        throw new Exception("Invalid data type for the DataObject parameter, it should be created from CreatePurchesDto class");
                    }
                    job.JsonData = JsonConvert.SerializeObject(DataObject);
                }
                else
                {
                    throw new Exception("Invalid Job type");
                }
                _dbContext.InvoiceJobQueues.Add(job);
                await _dbContext.SaveChangesAsync(); 
            }
        }
        /// <summary>
        /// Root function for the Running saved purchese related jobs in the database
        /// </summary>
        /// <returns></returns>
        ///
        [AutomaticRetry(Attempts = 0, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
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
                    int count = _dbContext.InvoiceJobQueues.Count(x => x.IsSuccess == false);
                    int pages = Convert.ToInt32((count / 100));

                    for (int y = 0; y <= pages; y++)
                    {
                        List<InvoiceJobQueue> jobs = _dbContext.InvoiceJobQueues.Where(x => x.IsSuccess == false).OrderBy(x => x.CreatedAt)
                            .Skip(pages * RowsPerPage).Take(RowsPerPage).ToList();
                        foreach (InvoiceJobQueue job in jobs)
                        {
                            await Task.Delay(10);
                            try
                            {
                                if (job.InvoiceGatewayType == ExternalInvoiceGatewayType.Zoho)
                                {
                                    await ZohoJobRunner(job, _dbContext);
                                }
                                
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
        private async Task<bool> ZohoJobRunner(InvoiceJobQueue job, ApplicationDbContext _dbContext)
        {
            if (job.JobType == InvoiceJobQueueType.CreateContact)
            {
                ZohoCreateContact model = JsonConvert.DeserializeObject<ZohoCreateContact>(job.JsonData);
                InvoiceContact contact =await _workerService.CreateContact(model, _dbContext);
                job.UpdatedAt = DateTime.UtcNow;
                if (contact == null)
                {
                    throw new Exception($"Purchese contact creating failed for job ID {job.Id}");
                }
                else
                {
                    
                    job.IsSuccess = true;
                    await _dbContext.SaveChangesAsync();
                }
            }
            else if (job.JobType == InvoiceJobQueueType.EnablePaymentReminders)
            {
                var UserId = GetInvoceContactByUserid(job, out var contact, _dbContext);

                bool result = await _workerService.EnablePaymentReminder(contact);
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
            }else if (job.JobType == InvoiceJobQueueType.CreateItem)
            {
                ZohoCreateItemDto model = JsonConvert.DeserializeObject<ZohoCreateItemDto>(job.JsonData);
                Item item = await _workerService.CreateItem(model, _dbContext);
                job.UpdatedAt = DateTime.UtcNow;
                if (item == null)
                {
                    throw new Exception($"Item creating failed for job ID {job.Id}");
                }
                else
                {

                    job.IsSuccess = true;
                    await _dbContext.SaveChangesAsync();
                }
            }else if (job.JobType == InvoiceJobQueueType.CreateFirstInvoice)
            {
                ZohoCreatePurchesDto model = JsonConvert.DeserializeObject<ZohoCreatePurchesDto>(job.JsonData);
                bool resut = await _workerService.CreateInvoice(job,model, _dbContext);
                if (!resut)
                {
                    throw new Exception($"Frist invoice creating failed for job ID {job.Id}");
                }
            }
            else if (job.JobType == InvoiceJobQueueType.CreateSubInvoice)
            {
                
                bool resut = await _workerService.CreateSubInvoice(job, job.JsonData, _dbContext);
                if (!resut)
                {
                    throw new Exception($"Frist invoice creating failed for job ID {job.Id}");
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
        //    bool IsSuccess = await CreateFirstInvoice(recurringInvoiceId, dbContext);
        //}
        private string GetInvoceContactByUserid(InvoiceJobQueue job, out InvoiceContact contact, ApplicationDbContext _dbContext)
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
            public string invoice_number { get; set; }
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
        #region Create Recurring Purchese

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
