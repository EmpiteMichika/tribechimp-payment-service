using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Interface.Service;
using Empite.PaymentService.Models.Dto.Zoho;
using Empite.PaymentService.Services.PaymentService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Data;

namespace Empite.PaymentService.Controllers.api.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly IInvoceService _invoceService;
        private readonly ApplicationDbContext _dbContext;
        private ILogger<ContactController> _logger;
        public ContactController(IInvoceService service, ApplicationDbContext dbContext, ILogger<ContactController> logger)
        {
            _invoceService = service;
            _dbContext = dbContext;
            _logger = logger;
        }
        [HttpPut]
        public async Task<IActionResult> Put([FromBody]ZohoCreateContact createContact)
        {
            try
            {
                await _invoceService.AddJob(createContact, InvoiceJobQueueType.CreateContact, ExternalInvoiceGatewayType.Zoho);
                await _invoceService.AddJob(createContact.UserId, InvoiceJobQueueType.EnablePaymentReminders, ExternalInvoiceGatewayType.Zoho);
                return Ok();
            }
            catch (Exception e)
            {
                
                _logger.LogError($"Exception in Contact controller put method. Message is => {e.Message}, Stacktrace is => {e.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Exception is => {e.Message}, stacktrace => {e.StackTrace}");
            }
        }
    }
}