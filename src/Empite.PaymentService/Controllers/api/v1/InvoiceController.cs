using System;
using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Interface.Service;

using Empite.PaymentService.Models.Dto;
using Empite.PaymentService.Models.Dto.Zoho;
using Empite.PaymentService.Services.PaymentService.Zoho;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empite.PaymentService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoceService _invoceService;
        private readonly ApplicationDbContext _dbContext;
        private IInvoiceWorkerService<ZohoInvoiceWorkerService> _workerService;
        public InvoiceController(IInvoceService service,ApplicationDbContext dbContext, IInvoiceWorkerService<ZohoInvoiceWorkerService> workerService)
        {
            _invoceService = service;
            _dbContext = dbContext;
            _workerService = workerService;
        }
        [HttpPut]
        public async Task<IActionResult> Put([FromBody]ZohoCreatePurchesDto model)
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

        [HttpGet("{guid}")]
        public async Task<IActionResult> GetRecInvoiceStatus(Guid guid)
        {
            try
            {
                var res = await _workerService.IsPaidForCurrentDate(guid, _dbContext);
                //status is 1 if its paid, 0 if its unpaid       
                return Ok((res) ?(new {paid = 1}): new { paid = 0 });
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