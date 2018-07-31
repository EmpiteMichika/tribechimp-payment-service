using System;
using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Interface.Service;

using Empite.PaymentService.Models.Dto;
using Empite.PaymentService.Models.Dto.Zoho;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empite.PaymentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoceService _invoceService;
        private readonly ApplicationDbContext _dbContext;
        public InvoiceController(IInvoceService service,ApplicationDbContext dbContext)
        {
            _invoceService = service;
            _dbContext = dbContext;
        }
        
        [HttpPost("createContact")]
        public async Task<IActionResult> CreateContact([FromBody]ZohoCreateContact createContact)
        {
            try
            {
                await _invoceService.AddJob(createContact, InvoiceJobQueueType.CreateContact,ExternalInvoiceGatewayType.Zoho);
                await _invoceService.AddJob(createContact.UserId, InvoiceJobQueueType.EnablePaymentReminders, ExternalInvoiceGatewayType.Zoho);
                return Ok();
            }
            catch (Exception e)
            {
                //Todo Logging
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Exception is => {e.Message}, stacktrace => {e.StackTrace}");
            }
        }

        

        [HttpGet("runjobs")]
        public async Task<IActionResult> RunJobs()
        {
            _invoceService.RunJobs();
            return Ok();
        }

        [HttpPost("createItem")]
        public async Task<IActionResult> CreateItem([FromBody] ZohoCreateItemDto itemDto)
        {
            try
            {
                await _invoceService.AddJob(itemDto, InvoiceJobQueueType.CreateItem, ExternalInvoiceGatewayType.Zoho);
                return Ok();
            }
            catch (Exception e)
            {
                //Todo Logging
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Exception is => {e.Message}, stacktrace => {e.StackTrace}");
            }
        }

        [HttpPost("createRecurringInvoice")]
        public async Task<IActionResult> CreateRecurringInvoice([FromBody]ZohoCreatePurchesDto model)
        {
            try
            {
                await _invoceService.AddJob(model, InvoiceJobQueueType.CreateFirstInvoice, ExternalInvoiceGatewayType.Zoho);
                return Ok();
            }
            catch (Exception e)
            {
                //Todo Logging
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Exception is => {e.Message}, stacktrace => {e.StackTrace}");
            }
        }

        [HttpGet("getRecInvoiceStatus/{guid}")]
        public async Task<IActionResult> GetRecInvoiceStatus(string guid)
        {
            try
            {
                Purchese purchese = await _dbContext.Purcheses.FirstAsync(x => x.ReferenceGuid.ToString() == guid);
                return Ok();
                //status is 0 if its paid, 1 if its unpaid       
                //return Ok((purchese.IsDue)?(new {status = 1}): new { status = 0 });
            }
            catch (Exception e)
            {
                //Todo Logging
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Exception is => {e.Message}, stacktrace => {e.StackTrace}");
            }
        }
    }

    
    
}