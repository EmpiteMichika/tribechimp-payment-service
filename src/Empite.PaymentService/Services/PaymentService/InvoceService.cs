﻿using System;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Empite.PaymentService.Services.PaymentService
{
    public class InvoceService: IInvoceService
    {
        
       
        private const int RowsPerPage = 100;
        private readonly Settings _settings;
        private const int ZohoSuccessResponseCode = 0;
        private const int ZohoPaymentTermDaysGap = 10;
        private IServiceProvider _services { get; }
        private IInvoiceWorkerService<ZohoInvoiceWorkerService> _workerService;
        private ILogger<InvoceService> _logger;
        private readonly Guid _sessionGuid;
        public InvoceService(IOptions<Settings> options, IServiceProvider services,IInvoiceWorkerService<ZohoInvoiceWorkerService> workerService, ILogger<InvoceService> logger)
        {

            _services = services;
            _workerService = workerService;
            _logger = logger;
            _sessionGuid = Guid.NewGuid();
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
        public async Task AddJob(dynamic DataObject, InvoiceJobQueueType JobType, ExternalInvoiceGatewayType externalInvoiceGatewayType)
        {
            LogInformation($"++++++++++++++++++++++++++++++ Start AddJob. Jobtype is => {JobType.ToString()}, ExternalGateway type is => {externalInvoiceGatewayType.ToString()} ++++++++++++++++++++++++++++++");
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
            LogInformation($"++++++++++++++++++++++++++++++ Stop AddJob. Jobtype is => {JobType.ToString()}, ExternalGateway type is => {externalInvoiceGatewayType.ToString()} ++++++++++++++++++++++++++++++");
        }
        /// <summary>
        /// Root function for the Running saved purchese related jobs in the database
        /// </summary>
        /// <returns></returns>
        ///
        [AutomaticRetry(Attempts = 0, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task RunJobs()
        {
            
            if (InvoiceServiceStatics._isJobsProcessing)
            {
                return;

            }
            LogInformation($"++++++++++++++++++++++++++++++ Start RunJob ++++++++++++++++++++++++++++++");
            InvoiceServiceStatics._isJobsProcessing = true;
            try
            {
                using (ApplicationDbContext _dbContext = _services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>())
                {
                    int count = _dbContext.InvoiceJobQueues.Count(x => x.IsSuccess == false);
                    int pages = Convert.ToInt32((count / 100));

                    for (int y = 0; y <= pages; y++)
                    {
                        List<InvoiceJobQueue> jobs = _dbContext.InvoiceJobQueues.Where(x => x.IsSuccess == false).OrderBy(x => x.CreatedAt)
                            .Take(RowsPerPage).ToList();
                        foreach (InvoiceJobQueue job in jobs)
                        {
                            LogInformation($"-------------- Start processing Job id {job.Id} ---------------------------");
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
                                
                                //Log With the Job ID
                                LogError($"Error in Invoice service job loog. Job id is => {job.Id}, exception message is => {ex.Message}, Stacktrace is => {ex.StackTrace}");
                                job.ReTryCount++;
                                job.LastErrorMessage =
                                    $"Exception message is => {ex.Message}, Stacktrace is => {ex.StackTrace}";
                                await _dbContext.SaveChangesAsync();
                                
                            }
                            LogInformation($"-------------- Stop processing Job id {job.Id} ---------------------------");
                        }
                    } 
                }
                
            }
            catch (Exception ex)
            {
                InvoiceServiceStatics._isJobsProcessing = false;
               
                LogError($"Error in RunJob. Exception is => {ex.Message}, StackTrace is => {ex.StackTrace}");
                throw ex;
            }

            InvoiceServiceStatics._isJobsProcessing = false;
            LogInformation($"++++++++++++++++++++++++++++++ Stop RunJob ++++++++++++++++++++++++++++++");
        }
        private async Task<bool> ZohoJobRunner(InvoiceJobQueue job, ApplicationDbContext _dbContext)
        {
            LogInformation($"+++++++++++++++++++++++++++++++ Start Zoho Job runner. JobDetails job id => {job.Id} +++++++++++++++++++++++++++++++++++++++++");
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
            LogInformation($"+++++++++++++++++++++++++++++++ Stop Zoho Job runner. JobDetails job id => {job.Id} +++++++++++++++++++++++++++++++++++++++++");
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


    public static class InvoiceServiceStatics
    {
        public static bool _isJobsProcessing { get; set; } = false;
    }
}
