using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Empite.PaymentService.Models.Dto.ApiController
{
    public class PaymentStatus
    {
        public string InvoiceId { get; set; }
        public double Amount { get; set; }
    }
}
