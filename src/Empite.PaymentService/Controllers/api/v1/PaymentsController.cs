using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Models.Dto.ApiController;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Empite.PaymentService.Controllers.api.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private ILogger<PaymentsController> _logger;
        public PaymentsController(ApplicationDbContext dbContext, ILogger<PaymentsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> RecordPayment([FromBody] PaymentStatus status)
        {
            try
            {
                InvoiceHistory dbInvoiceHistory = await _dbContext.InvoiceHistories.Include(x => x.Purchese).ThenInclude(x => x.Items).ThenInclude(x => x.Item)
                    .Where(x => x.InvoiceId == status.InvoiceId).FirstOrDefaultAsync();
                if (dbInvoiceHistory == null)
                {
                    //Todo log
                    return StatusCode(StatusCodes.Status400BadRequest, "Invoice not found");
                }

                
                double total = dbInvoiceHistory.Purchese.Items.Sum(x => x.Qty * x.Item.Rate) + dbInvoiceHistory.PaidAmount;
                if (total <= status.Amount)
                {
                    dbInvoiceHistory.InvoiceStatus = InvoiceStatus.Paid;
                    dbInvoiceHistory.PaidAmount += status.Amount;
                    
                }
                else
                {
                    dbInvoiceHistory.InvoiceStatus = InvoiceStatus.PartialPayment;
                    dbInvoiceHistory.PaidAmount += status.Amount;
                }
                dbInvoiceHistory.PaymentRecordedDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Payment Controller RecordPayment method. Exception message is => {ex.Message}, Stacktace is => {ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}