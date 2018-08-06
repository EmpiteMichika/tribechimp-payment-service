using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Interface.Service;
using Empite.PaymentService.Models.Dto.Zoho;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Empite.PaymentService.Controllers.api.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly IInvoceService _invoceService;
        private readonly ApplicationDbContext _dbContext;
        public ItemController(IInvoceService service, ApplicationDbContext dbContext)
        {
            _invoceService = service;
            _dbContext = dbContext;
        }
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] ZohoCreateItemDto itemDto)
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
    }
}