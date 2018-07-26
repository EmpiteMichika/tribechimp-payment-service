using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.TribechimpService.PaymentService.Data;
using Empite.TribechimpService.PaymentService.Domain.Dto;
using Empite.TribechimpService.PaymentService.Domain.Interface.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empite.TribechimpService.PaymentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IZohoInvoceService _zohoInvoceService;
        private readonly ApplicationDbContext _dbContext;
        public InvoiceController(IZohoInvoceService zohoService,ApplicationDbContext dbContext)
        {
            _zohoInvoceService = zohoService;
            _dbContext = dbContext;
        }
        
        [HttpPost("createContact")]
        public async Task<IActionResult> CreateContact([FromBody]CreateContact createContact)
        {
            try
            {
                await _zohoInvoceService.AddJob(createContact, ZohoInvoiceJobQueueType.CreateContact);
                await _zohoInvoceService.AddJob(createContact.UserId, ZohoInvoiceJobQueueType.EnablePaymentReminders);
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
            _zohoInvoceService.RunJobs();
            return Ok();
        }

        [HttpPost("createItem")]
        public async Task<IActionResult> CreateItem([FromBody] CreateZohoItemDto itemDto)
        {
            try
            {
                await _zohoInvoceService.AddJob(itemDto, ZohoInvoiceJobQueueType.CreateItem);
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
        public async Task<IActionResult> CreateRecurringInvoice([FromBody]CreatePurchesDto model)
        {
            try
            {
                await _zohoInvoceService.AddJob(model, ZohoInvoiceJobQueueType.CreateFirstInvoice);
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