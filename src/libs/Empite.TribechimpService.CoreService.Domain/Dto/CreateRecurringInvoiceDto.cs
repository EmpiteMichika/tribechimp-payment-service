using System;
using System.Collections.Generic;
using System.Text;

namespace Empite.TribechimpService.PaymentService.Domain.Dto
{
    public class CreateRecurringInvoiceDto
    {
        public string UserId { get; set; }
        public List<string> ItemIds { get; set; }
    }
}
